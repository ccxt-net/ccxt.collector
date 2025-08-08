using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CCXT.Collector.Service;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace CCXT.Collector.Library
{
    /// <summary>
    /// Base WebSocket client implementation
    /// </summary>
    public abstract class WebSocketClientBase : IWebSocketClient
    {
        protected ClientWebSocket _webSocket;
        protected ClientWebSocket _privateWebSocket; // For authenticated channels
        protected CancellationTokenSource _cancellationTokenSource;
        protected readonly ConcurrentDictionary<string, SubscriptionInfo> _subscriptions;
        protected readonly SemaphoreSlim _sendSemaphore;
        protected Timer _pingTimer;
        protected int _reconnectAttempts = 0;
        protected readonly int _maxReconnectAttempts = 5;
        protected readonly int _reconnectDelayMs = 5000;
        
        // Authentication
        protected string _apiKey;
        protected string _secretKey;
        protected bool _isAuthenticated = false;

        public abstract string ExchangeName { get; }
        protected abstract string WebSocketUrl { get; }
        protected virtual string PrivateWebSocketUrl => WebSocketUrl; // Some exchanges use different URLs
        protected abstract int PingIntervalMs { get; }

        public bool IsConnected => _webSocket?.State == WebSocketState.Open;
        public bool IsAuthenticated => _isAuthenticated && (_privateWebSocket?.State == WebSocketState.Open || IsConnected);

        #region Events - Public Data

        public event Action<STicker> OnTickerReceived;
        public event Action<SCompleteOrders> OnTradeReceived;
        public event Action<SOrderBooks> OnOrderbookReceived;
        public event Action<SCandlestick> OnCandleReceived;

        #endregion

        #region Events - Private Data

        public event Action<SBalance> OnBalanceUpdate;
        public event Action<SOrder> OnOrderUpdate;
        public event Action<SPosition> OnPositionUpdate;

        #endregion

        #region Events - Connection

        public event Action OnConnected;
        public event Action OnAuthenticated;
        public event Action OnDisconnected;
        public event Action<string> OnError;

        #endregion

        protected WebSocketClientBase()
        {
            _subscriptions = new ConcurrentDictionary<string, SubscriptionInfo>();
            _sendSemaphore = new SemaphoreSlim(1, 1);
        }

        public virtual async Task<bool> ConnectAsync()
        {
            try
            {
                if (IsConnected)
                    return true;

                _cancellationTokenSource = new CancellationTokenSource();
                _webSocket = new ClientWebSocket();
                
                // Configure WebSocket options
                _webSocket.Options.KeepAliveInterval = TimeSpan.FromSeconds(30);
                ConfigureWebSocket(_webSocket);

                await _webSocket.ConnectAsync(new Uri(WebSocketUrl), _cancellationTokenSource.Token);

                if (IsConnected)
                {
                    _reconnectAttempts = 0;
                    OnConnected?.Invoke();
                    
                    // Start receive loop
                    _ = Task.Run(() => ReceiveLoop(_webSocket, false));
                    
                    // Start ping timer if needed
                    if (PingIntervalMs > 0)
                    {
                        _pingTimer = new Timer(async _ => await SendPingAsync(), null, 
                            PingIntervalMs, PingIntervalMs);
                    }

                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                OnError?.Invoke($"Connection failed: {ex.Message}");
                await HandleReconnectAsync();
                return false;
            }
        }

        public virtual async Task<bool> ConnectAsync(string apiKey, string secretKey)
        {
            _apiKey = apiKey;
            _secretKey = secretKey;
            
            // First connect to public channels
            var publicConnected = await ConnectAsync();
            if (!publicConnected)
                return false;
            
            // Then authenticate for private channels
            return await AuthenticateAsync();
        }

        protected virtual async Task<bool> AuthenticateAsync()
        {
            try
            {
                // Some exchanges use same connection for auth, others use separate
                if (RequiresSeparatePrivateConnection())
                {
                    _privateWebSocket = new ClientWebSocket();
                    ConfigureWebSocket(_privateWebSocket);
                    await _privateWebSocket.ConnectAsync(new Uri(PrivateWebSocketUrl), _cancellationTokenSource.Token);
                    
                    // Start receive loop for private socket
                    _ = Task.Run(() => ReceiveLoop(_privateWebSocket, true));
                }
                
                // Send authentication message
                var authMessage = CreateAuthenticationMessage(_apiKey, _secretKey);
                if (!string.IsNullOrEmpty(authMessage))
                {
                    var socket = _privateWebSocket ?? _webSocket;
                    await SendMessageAsync(authMessage, socket);
                    
                    // Wait for auth confirmation (simplified - real implementation would wait for response)
                    await Task.Delay(1000);
                    
                    _isAuthenticated = true;
                    OnAuthenticated?.Invoke();
                    return true;
                }
                
                return false;
            }
            catch (Exception ex)
            {
                OnError?.Invoke($"Authentication failed: {ex.Message}");
                return false;
            }
        }

        protected virtual bool RequiresSeparatePrivateConnection() => false;
        protected virtual string CreateAuthenticationMessage(string apiKey, string secretKey) => null;

        public virtual async Task DisconnectAsync()
        {
            try
            {
                _pingTimer?.Dispose();
                _cancellationTokenSource?.Cancel();

                if (_webSocket?.State == WebSocketState.Open)
                {
                    await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, 
                        "Client disconnecting", CancellationToken.None);
                }

                _webSocket?.Dispose();
                _webSocket = null;
                
                OnDisconnected?.Invoke();
            }
            catch (Exception ex)
            {
                OnError?.Invoke($"Disconnect error: {ex.Message}");
            }
        }

        protected virtual void ConfigureWebSocket(ClientWebSocket webSocket)
        {
            // Override in derived classes for exchange-specific configuration
        }

        protected async Task ReceiveLoop(ClientWebSocket socket, bool isPrivate)
        {
            var buffer = new ArraySegment<byte>(new byte[4096]);
            var messageBuilder = new StringBuilder();

            try
            {
                while (socket?.State == WebSocketState.Open && !_cancellationTokenSource.Token.IsCancellationRequested)
                {
                    WebSocketReceiveResult result;
                    messageBuilder.Clear();

                    do
                    {
                        result = await socket.ReceiveAsync(buffer, _cancellationTokenSource.Token);
                        
                        if (result.MessageType == WebSocketMessageType.Text)
                        {
                            messageBuilder.Append(Encoding.UTF8.GetString(buffer.Array, 0, result.Count));
                        }
                        else if (result.MessageType == WebSocketMessageType.Close)
                        {
                            await HandleDisconnectAsync();
                            return;
                        }
                    }
                    while (!result.EndOfMessage);

                    if (messageBuilder.Length > 0)
                    {
                        var message = messageBuilder.ToString();
                        await ProcessMessageAsync(message, isPrivate);
                    }
                }
            }
            catch (Exception ex)
            {
                OnError?.Invoke($"Receive error: {ex.Message}");
                await HandleReconnectAsync();
            }
        }

        protected abstract Task ProcessMessageAsync(string message, bool isPrivate = false);

        protected async Task SendMessageAsync(string message, ClientWebSocket socket = null)
        {
            socket ??= _webSocket;
            
            if (socket?.State != WebSocketState.Open)
            {
                OnError?.Invoke("Cannot send message: Not connected");
                return;
            }

            await _sendSemaphore.WaitAsync();
            try
            {
                var bytes = Encoding.UTF8.GetBytes(message);
                await socket.SendAsync(new ArraySegment<byte>(bytes), 
                    WebSocketMessageType.Text, true, _cancellationTokenSource.Token);
            }
            catch (Exception ex)
            {
                OnError?.Invoke($"Send error: {ex.Message}");
                await HandleReconnectAsync();
            }
            finally
            {
                _sendSemaphore.Release();
            }
        }

        protected virtual async Task SendPingAsync()
        {
            try
            {
                if (IsConnected)
                {
                    var pingMessage = CreatePingMessage();
                    if (!string.IsNullOrEmpty(pingMessage))
                    {
                        await SendMessageAsync(pingMessage);
                    }
                }
            }
            catch (Exception ex)
            {
                OnError?.Invoke($"Ping error: {ex.Message}");
            }
        }

        protected virtual string CreatePingMessage()
        {
            // Override in derived classes
            return null;
        }

        protected async Task HandleDisconnectAsync()
        {
            OnDisconnected?.Invoke();
            await HandleReconnectAsync();
        }

        protected async Task HandleReconnectAsync()
        {
            if (_reconnectAttempts >= _maxReconnectAttempts)
            {
                OnError?.Invoke($"Max reconnection attempts ({_maxReconnectAttempts}) reached");
                return;
            }

            _reconnectAttempts++;
            OnError?.Invoke($"Reconnecting... Attempt {_reconnectAttempts}/{_maxReconnectAttempts}");
            
            await Task.Delay(_reconnectDelayMs * _reconnectAttempts);
            
            if (await ConnectAsync())
            {
                // Resubscribe to all active channels
                foreach (var sub in _subscriptions.Values.Where(s => s.IsActive))
                {
                    await ResubscribeAsync(sub);
                }
            }
        }

        protected virtual async Task ResubscribeAsync(SubscriptionInfo subscription)
        {
            // Override in derived classes
            await Task.CompletedTask;
        }

        #region Abstract Methods for Public Subscriptions

        public abstract Task<bool> SubscribeTickerAsync(string symbol);
        public abstract Task<bool> SubscribeOrderbookAsync(string symbol);
        public abstract Task<bool> SubscribeTradesAsync(string symbol);
        public abstract Task<bool> SubscribeCandlesAsync(string symbol, string interval);
        public abstract Task<bool> UnsubscribeAsync(string channel, string symbol);

        #endregion

        #region Virtual Methods for Private Subscriptions

        public virtual async Task<bool> SubscribeBalanceAsync()
        {
            if (!IsAuthenticated)
            {
                OnError?.Invoke("Not authenticated. Please connect with API credentials.");
                return false;
            }
            
            // Override in derived classes
            return await Task.FromResult(false);
        }

        public virtual async Task<bool> SubscribeOrdersAsync()
        {
            if (!IsAuthenticated)
            {
                OnError?.Invoke("Not authenticated. Please connect with API credentials.");
                return false;
            }
            
            // Override in derived classes
            return await Task.FromResult(false);
        }

        public virtual async Task<bool> SubscribePositionsAsync()
        {
            if (!IsAuthenticated)
            {
                OnError?.Invoke("Not authenticated. Please connect with API credentials.");
                return false;
            }
            
            // Override in derived classes for futures exchanges
            return await Task.FromResult(false);
        }

        #endregion

        #region Protected Helper Methods

        // Public data callbacks
        protected void InvokeTickerCallback(STicker ticker)
        {
            OnTickerReceived?.Invoke(ticker);
        }

        protected void InvokeTradeCallback(SCompleteOrders trade)
        {
            OnTradeReceived?.Invoke(trade);
        }

        protected void InvokeOrderbookCallback(SOrderBooks orderbook)
        {
            OnOrderbookReceived?.Invoke(orderbook);
        }

        protected void InvokeCandleCallback(SCandlestick candle)
        {
            OnCandleReceived?.Invoke(candle);
        }

        // Private data callbacks
        protected void InvokeBalanceCallback(SBalance balance)
        {
            OnBalanceUpdate?.Invoke(balance);
        }

        protected void InvokeOrderCallback(SOrder order)
        {
            OnOrderUpdate?.Invoke(order);
        }

        protected void InvokePositionCallback(SPosition position)
        {
            OnPositionUpdate?.Invoke(position);
        }

        protected string CreateSubscriptionKey(string channel, string symbol)
        {
            return $"{channel}:{symbol}";
        }

        #endregion

        public virtual void Dispose()
        {
            _ = DisconnectAsync().ConfigureAwait(false);
            _sendSemaphore?.Dispose();
            _cancellationTokenSource?.Dispose();
            _pingTimer?.Dispose();
            _privateWebSocket?.Dispose();
        }
    }
}