using CCXT.Collector.Service;
using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CCXT.Collector.Core.Abstractions
{
    /// <summary>
    /// Base WebSocket client implementation
    /// </summary>
    public abstract class WebSocketClientBase : IWebSocketClient
    {
        protected ClientWebSocket _webSocket;
        protected ClientWebSocket _privateWebSocket; // For authenticated channels
        protected CancellationTokenSource _cancellationTokenSource;
        protected readonly SemaphoreSlim _sendSemaphore;
        protected Timer _pingTimer;
        protected int _reconnectAttempts = 0;
        protected readonly int _maxReconnectAttempts = 10;  // Increased from 5
        protected readonly int _reconnectDelayMs = 5000;
        
        // Exchange rate support for multi-currency 
        protected decimal _exchangeRate = 1.0m;
        
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
        public event Action<STrade> OnTradeReceived;
        public event Action<SOrderBook> OnOrderbookReceived;
        public event Action<SCandle> OnCandleReceived;

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
                    RaiseConnected();
                    
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
                RaiseError($"Connection failed: {ex.Message}");
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
                if (!String.IsNullOrEmpty(authMessage))
                {
                    var socket = _privateWebSocket ?? _webSocket;
                    await SendMessageAsync(authMessage, socket);
                    
                    // Wait for auth confirmation (simplified - real implementation would wait for response)
                    await Task.Delay(1000);
                    
                    _isAuthenticated = true;
                    RaiseAuthenticated();
                    return true;
                }
                
                return false;
            }
            catch (Exception ex)
            {
                RaiseError($"Authentication failed: {ex.Message}");
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
                
                RaiseDisconnected();
            }
            catch (Exception ex)
            {
                RaiseError($"Disconnect error: {ex.Message}");
            }
        }

        protected virtual void ConfigureWebSocket(ClientWebSocket webSocket)
        {
            // Override in derived classes for exchange-specific configuration
        }

        protected virtual async Task ReceiveLoop(ClientWebSocket socket, bool isPrivate)
        {
            // Dynamic buffer sizing for large messages 
            var bufferSize = 1024 * 16;  // 16KB initial buffer
            var buffer = new ArraySegment<byte>(new byte[bufferSize]);
            var messageBuilder = new StringBuilder();
            var binaryData = new System.Collections.Generic.List<byte>();

            try
            {
                while (socket?.State == WebSocketState.Open && !_cancellationTokenSource.Token.IsCancellationRequested)
                {
                    WebSocketReceiveResult result;
                    messageBuilder.Clear();
                    binaryData.Clear();

                    do
                    {
                        result = await socket.ReceiveAsync(buffer, _cancellationTokenSource.Token);
                        
                        if (result.MessageType == System.Net.WebSockets.WebSocketMessageType.Text)
                        {
                            messageBuilder.Append(Encoding.UTF8.GetString(buffer.Array, 0, result.Count));
                            
                            // Resize buffer if nearly full 
                            if (result.Count == buffer.Count && !result.EndOfMessage)
                            {
                                bufferSize *= 2;
                                buffer = new ArraySegment<byte>(new byte[bufferSize]);
                            }
                        }
                        else if (result.MessageType == System.Net.WebSockets.WebSocketMessageType.Binary)
                        {
                            // Handle binary messages (some exchanges like Upbit send binary data)
                            binaryData.AddRange(buffer.Array.Take(result.Count));
                            
                            // Resize buffer if nearly full 
                            if (result.Count == buffer.Count && !result.EndOfMessage)
                            {
                                bufferSize *= 2;
                                buffer = new ArraySegment<byte>(new byte[bufferSize]);
                            }
                        }
                        else if (result.MessageType == System.Net.WebSockets.WebSocketMessageType.Close)
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
                    else if (binaryData.Count > 0)
                    {
                        // Convert binary data to string (assuming UTF-8 encoding)
                        var message = Encoding.UTF8.GetString(binaryData.ToArray());
                        await ProcessMessageAsync(message, isPrivate);
                    }
                }
            }
            catch (Exception ex)
            {
                RaiseError($"Receive error: {ex.Message}");
                await HandleReconnectAsync();
            }
        }

        protected abstract Task ProcessMessageAsync(string message, bool isPrivate = false);

        protected async Task SendMessageAsync(string message, ClientWebSocket socket = null)
        {
            socket ??= _webSocket;
            
            if (socket?.State != WebSocketState.Open)
            {
                RaiseError("Cannot send message: Not connected");
                return;
            }

            await _sendSemaphore.WaitAsync();
            try
            {
                var bytes = Encoding.UTF8.GetBytes(message);
                await socket.SendAsync(new ArraySegment<byte>(bytes), 
                    System.Net.WebSockets.WebSocketMessageType.Text, true, _cancellationTokenSource.Token);
            }
            catch (Exception ex)
            {
                RaiseError($"Send error: {ex.Message}");
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
                    if (!String.IsNullOrEmpty(pingMessage))
                    {
                        await SendMessageAsync(pingMessage);
                    }
                }
            }
            catch (Exception ex)
            {
                RaiseError($"Ping error: {ex.Message}");
            }
        }

        protected virtual string CreatePingMessage()
        {
            // Override in derived classes
            return null;
        }

        protected async Task HandleDisconnectAsync()
        {
            RaiseDisconnected();
            await HandleReconnectAsync();
        }

        protected async Task HandleReconnectAsync()
        {
            if (_reconnectAttempts >= _maxReconnectAttempts)
            {
                RaiseError($"Max reconnection attempts ({_maxReconnectAttempts}) reached");
                return;
            }

            _reconnectAttempts++;
            
            // Exponential backoff with cap 
            var delay = Math.Min(_reconnectDelayMs * _reconnectAttempts, 60000);  // Cap at 60 seconds
            RaiseError($"Reconnecting in {delay}ms (attempt {_reconnectAttempts}/{_maxReconnectAttempts})...");
            
            await Task.Delay(delay);
            
            await ConnectAsync();
        }

        #region Abstract Methods for Public Subscriptions

        public abstract Task<bool> SubscribeTickerAsync(string symbol);
        public abstract Task<bool> SubscribeOrderbookAsync(string symbol);
        public abstract Task<bool> SubscribeTradesAsync(string symbol);
        public abstract Task<bool> SubscribeCandlesAsync(string symbol, string interval);
        public abstract Task<bool> UnsubscribeAsync(string channel, string symbol);
        
        // Market-based overloads - virtual with default implementation for backward compatibility
        public virtual Task<bool> SubscribeOrderbookAsync(Market market)
        {
            return SubscribeOrderbookAsync(market.ToString());
        }
        
        public virtual Task<bool> SubscribeTradesAsync(Market market)
        {
            return SubscribeTradesAsync(market.ToString());
        }
        
        public virtual Task<bool> SubscribeTickerAsync(Market market)
        {
            return SubscribeTickerAsync(market.ToString());
        }
        
        public virtual Task<bool> SubscribeCandlesAsync(Market market, string interval)
        {
            return SubscribeCandlesAsync(market.ToString(), interval);
        }
        
        public virtual Task<bool> UnsubscribeAsync(string channel, Market market)
        {
            return UnsubscribeAsync(channel, market.ToString());
        }
        
        // Virtual method for exchange-specific symbol formatting - default implementation uses ToString()
        protected virtual string FormatSymbol(Market market)
        {
            return market.ToString();
        }
        
        // Set exchange rate for currency conversion (useful for KRW/USD conversions)
        public virtual void SetExchangeRate(decimal rate)
        {
            _exchangeRate = rate;
        }

        #endregion

        #region Virtual Methods for Private Subscriptions

        public virtual async Task<bool> SubscribeBalanceAsync()
        {
            if (!IsAuthenticated)
            {
                RaiseError("Not authenticated. Please connect with API credentials.");
                return false;
            }
            
            // Override in derived classes
            return await Task.FromResult(false);
        }

        public virtual async Task<bool> SubscribeOrdersAsync()
        {
            if (!IsAuthenticated)
            {
                RaiseError("Not authenticated. Please connect with API credentials.");
                return false;
            }
            
            // Override in derived classes
            return await Task.FromResult(false);
        }

        public virtual async Task<bool> SubscribePositionsAsync()
        {
            if (!IsAuthenticated)
            {
                RaiseError("Not authenticated. Please connect with API credentials.");
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

        protected void InvokeTradeCallback(STrade trade)
        {
            OnTradeReceived?.Invoke(trade);
        }

        protected void InvokeOrderbookCallback(SOrderBook orderbook)
        {
            OnOrderbookReceived?.Invoke(orderbook);
        }

        // Event raising helper methods for derived classes
        protected virtual void RaiseError(string message)
        {
            OnError?.Invoke(message);
        }

        protected virtual void RaiseConnected()
        {
            OnConnected?.Invoke();
        }

        protected virtual void RaiseAuthenticated()
        {
            OnAuthenticated?.Invoke();
        }

        protected virtual void RaiseDisconnected()
        {
            OnDisconnected?.Invoke();
        }

        protected void InvokeCandleCallback(SCandle candle)
        {
            OnCandleReceived?.Invoke(candle);
        }

        // Private data callbacks
        protected void InvokeBalanceCallback(SBalance balance)
        {
            OnBalanceUpdate?.Invoke(balance);
        }

        protected void InvokeOrderCallback(SOrder orders)
        {
            OnOrderUpdate?.Invoke(orders);
        }

        protected void InvokePositionCallback(SPosition positions)
        {
            OnPositionUpdate?.Invoke(positions);
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