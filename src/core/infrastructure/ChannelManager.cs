using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CCXT.Collector.Core.Abstractions;
using CCXT.Collector.Service;

namespace CCXT.Collector.Core.Infrastructure
{
    /// <summary>
    /// Implementation of channel manager for WebSocket subscriptions
    /// </summary>
    public class ChannelManager : IChannelManager
    {
        private readonly ConcurrentDictionary<string, ChannelInfo> _channels;
        private readonly ConcurrentDictionary<string, IWebSocketClient> _exchangeClients;
        private readonly ConcurrentDictionary<string, List<ChannelSubscriptionRequest>> _pendingSubscriptions;
        private readonly object _lockObject = new object();

        public ChannelManager()
        {
            _channels = new ConcurrentDictionary<string, ChannelInfo>();
            _exchangeClients = new ConcurrentDictionary<string, IWebSocketClient>();
            _pendingSubscriptions = new ConcurrentDictionary<string, List<ChannelSubscriptionRequest>>();
        }

        /// <summary>
        /// Register WebSocket client for an exchange
        /// </summary>
        public void RegisterExchangeClient(string exchange, IWebSocketClient client)
        {
            var exchangeLower = exchange.ToLower();
            _exchangeClients[exchangeLower] = client;
            
            // Initialize pending subscriptions list for this exchange
            _pendingSubscriptions[exchangeLower] = new List<ChannelSubscriptionRequest>();
        }

        /// <summary>
        /// Unregister WebSocket client for an exchange
        /// </summary>
        public void UnregisterExchangeClient(string exchange)
        {
            _exchangeClients.TryRemove(exchange.ToLower(), out _);
        }

        public async Task<string> RegisterChannelAsync(string exchange, string symbol, ChannelDataType dataType, string interval = null)
        {
            try
            {
                var exchangeLower = exchange.ToLower();
                
                // Check if exchange client is registered
                if (!_exchangeClients.TryGetValue(exchangeLower, out var client))
                {
                    throw new InvalidOperationException($"Exchange client for '{exchange}' is not registered");
                }

                // Generate channel ID
                var channelId = ChannelInfo.GenerateChannelId(exchange, symbol, dataType, interval);

                // Check if channel already exists
                if (_channels.ContainsKey(channelId))
                {
                    var existing = _channels[channelId];
                    if (existing.IsActive)
                    {
                        Console.WriteLine($"Channel already registered: {channelId}");
                        return channelId; // Already registered
                    }
                }

                // Initialize pending list if not exists
                if (!_pendingSubscriptions.TryGetValue(exchangeLower, out var pendingList))
                {
                    pendingList = new List<ChannelSubscriptionRequest>();
                    _pendingSubscriptions[exchangeLower] = pendingList;
                }

                // Create subscription request
                var subscriptionRequest = new ChannelSubscriptionRequest
                {
                    Symbol = symbol,
                    DataType = dataType,
                    Interval = interval
                };

                lock (_lockObject)
                {
                    // Check if already in pending list
                    var exists = pendingList.Any(r => 
                        r.Symbol == symbol && 
                        r.DataType == dataType && 
                        r.Interval == interval);
                    
                    if (!exists)
                    {
                        pendingList.Add(subscriptionRequest);
                    }
                }

                // Create channel info but mark as pending (not active yet)
                var channelInfo = new ChannelInfo
                {
                    ChannelId = channelId,
                    Exchange = exchange,
                    Symbol = symbol,
                    DataType = dataType,
                    Interval = interval,
                    SubscribedAt = DateTime.UtcNow,
                    IsActive = false,  // Not active until ApplySubscriptions is called
                    MessageCount = 0,
                    ErrorCount = 0
                };

                _channels[channelId] = channelInfo;
                
                Console.WriteLine($"Channel registered (pending): {channelId}");
                Console.WriteLine($"Total pending channels for {exchange}: {pendingList.Count}");

                return channelId;
            }
            catch (Exception ex)
            {
                // Log error
                Console.WriteLine($"Failed to register channel: {ex.Message}");
                return null;
            }
        }

        public async Task<bool> RemoveChannelAsync(string channelId)
        {
            try
            {
                if (!_channels.TryGetValue(channelId, out var channelInfo))
                {
                    return false; // Channel not found
                }

                var exchangeLower = channelInfo.Exchange.ToLower();
                if (!_exchangeClients.TryGetValue(exchangeLower, out var client))
                {
                    // Client not found, just remove from dictionary
                    _channels.TryRemove(channelId, out _);
                    return true;
                }

                // Unsubscribe from the channel
                var channelName = channelInfo.DataType.ToString().ToLower();
                var unsubscribed = await client.UnsubscribeAsync(channelName, channelInfo.Symbol);

                if (unsubscribed)
                {
                    // Mark as inactive and remove
                    channelInfo.IsActive = false;
                    _channels.TryRemove(channelId, out _);
                    
                    // Check if this was the last channel for this exchange
                    var remainingChannels = GetExchangeChannels(channelInfo.Exchange).Count(c => c.IsActive);
                    if (remainingChannels == 0 && client.IsConnected)
                    {
                        // Disconnect WebSocket if no more channels
                        Console.WriteLine($"[{channelInfo.Exchange}] No active channels remaining, disconnecting WebSocket");
                        await client.DisconnectAsync();
                    }
                    
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to remove channel: {ex.Message}");
                return false;
            }
        }

        public async Task<int> RemoveExchangeChannelsAsync(string exchange)
        {
            var exchangeChannels = GetExchangeChannels(exchange).ToList();
            var removedCount = 0;
            
            // Get the client first to disconnect after removing all channels
            var exchangeLower = exchange.ToLower();
            IWebSocketClient client = null;
            _exchangeClients.TryGetValue(exchangeLower, out client);

            foreach (var channel in exchangeChannels)
            {
                // Remove channel from tracking
                if (_channels.TryRemove(channel.ChannelId, out var removed))
                {
                    removed.IsActive = false;
                    removedCount++;
                    
                    // Try to unsubscribe from server if client is available
                    if (client != null && client.IsConnected)
                    {
                        try
                        {
                            var channelName = removed.DataType.ToString().ToLower();
                            await client.UnsubscribeAsync(channelName, removed.Symbol);
                        }
                        catch
                        {
                            // Continue even if unsubscribe fails
                        }
                    }
                }
            }

            // Disconnect the WebSocket if client exists and is connected
            if (client != null && client.IsConnected && removedCount > 0)
            {
                Console.WriteLine($"[{exchange}] All {removedCount} channels removed, disconnecting WebSocket");
                await client.DisconnectAsync();
            }

            return removedCount;
        }

        public IEnumerable<ChannelInfo> GetActiveChannels()
        {
            return _channels.Values.Where(c => c.IsActive);
        }

        public IEnumerable<ChannelInfo> GetExchangeChannels(string exchange)
        {
            return _channels.Values.Where(c => 
                string.Equals(c.Exchange, exchange, StringComparison.OrdinalIgnoreCase));
        }

        public ChannelInfo GetChannel(string channelId)
        {
            _channels.TryGetValue(channelId, out var channel);
            return channel;
        }

        public bool IsChannelActive(string channelId)
        {
            if (_channels.TryGetValue(channelId, out var channel))
            {
                return channel.IsActive;
            }
            return false;
        }

        public ChannelStatistics GetStatistics()
        {
            var stats = new ChannelStatistics
            {
                TotalChannels = _channels.Count,
                ActiveChannels = _channels.Values.Count(c => c.IsActive),
                TotalMessages = _channels.Values.Sum(c => c.MessageCount),
                TotalErrors = _channels.Values.Sum(c => (long)c.ErrorCount)
            };

            // Group by exchange
            stats.ChannelsByExchange = _channels.Values
                .GroupBy(c => c.Exchange)
                .ToDictionary(g => g.Key, g => g.Count());

            // Group by data type
            stats.ChannelsByType = _channels.Values
                .GroupBy(c => c.DataType)
                .ToDictionary(g => g.Key, g => g.Count());

            // Get oldest and newest subscriptions
            if (_channels.Any())
            {
                stats.OldestSubscription = _channels.Values.Min(c => c.SubscribedAt);
                stats.NewestSubscription = _channels.Values.Max(c => c.SubscribedAt);
            }

            return stats;
        }

        /// <summary>
        /// Update channel statistics when data is received
        /// </summary>
        public void UpdateChannelData(string channelId)
        {
            if (_channels.TryGetValue(channelId, out var channel))
            {
                channel.LastDataAt = DateTime.UtcNow;
                channel.MessageCount++;
            }
        }

        /// <summary>
        /// Update channel error information
        /// </summary>
        public void UpdateChannelError(string channelId, string error)
        {
            if (_channels.TryGetValue(channelId, out var channel))
            {
                channel.LastError = error;
                channel.ErrorCount++;
            }
        }

        /// <summary>
        /// Get all registered exchange clients
        /// </summary>
        public IEnumerable<string> GetRegisteredExchanges()
        {
            return _exchangeClients.Keys;
        }

        /// <summary>
        /// Check if an exchange client is registered
        /// </summary>
        public bool IsExchangeRegistered(string exchange)
        {
            return _exchangeClients.ContainsKey(exchange.ToLower());
        }

        /// <summary>
        /// Check if an exchange client is connected
        /// </summary>
        public bool IsExchangeConnected(string exchange)
        {
            var exchangeLower = exchange.ToLower();
            if (_exchangeClients.TryGetValue(exchangeLower, out var client))
            {
                return client.IsConnected;
            }
            return false;
        }

        /// <summary>
        /// Disconnect an exchange if it has no active channels
        /// </summary>
        public async Task<bool> DisconnectIdleExchangeAsync(string exchange)
        {
            var exchangeLower = exchange.ToLower();
            if (!_exchangeClients.TryGetValue(exchangeLower, out var client))
            {
                return false;
            }

            var activeChannels = GetExchangeChannels(exchange).Count(c => c.IsActive);
            if (activeChannels == 0 && client.IsConnected)
            {
                Console.WriteLine($"[{exchange}] Disconnecting idle WebSocket (no active channels)");
                await client.DisconnectAsync();
                return true;
            }

            return false;
        }

        /// <summary>
        /// Disconnect all idle exchanges (those with no active channels)
        /// </summary>
        public async Task<int> DisconnectAllIdleExchangesAsync()
        {
            var disconnectedCount = 0;
            foreach (var exchange in _exchangeClients.Keys)
            {
                if (await DisconnectIdleExchangeAsync(exchange))
                {
                    disconnectedCount++;
                }
            }
            return disconnectedCount;
        }

        /// <summary>
        /// Apply pending batch subscriptions for an exchange
        /// For exchanges that require batch subscription mode
        /// </summary>
        public async Task<bool> ApplyBatchSubscriptionsAsync(string exchange)
        {
            var exchangeLower = exchange.ToLower();
            
            // Get client
            if (!_exchangeClients.TryGetValue(exchangeLower, out var client))
            {
                Console.WriteLine($"Client for {exchange} not found");
                return false;
            }

            // Get pending subscriptions
            if (!_pendingSubscriptions.TryGetValue(exchangeLower, out var pendingList) || !pendingList.Any())
            {
                Console.WriteLine($"No pending subscriptions for {exchange}");
                return true;
            }

            // Connect if not connected
            if (!client.IsConnected)
            {
                var connected = await client.ConnectAsync();
                if (!connected)
                {
                    Console.WriteLine($"Failed to connect to {exchange}");
                    return false;
                }
            }

            // Apply all pending subscriptions
            Console.WriteLine($"Applying {pendingList.Count} subscriptions for {exchange}");
            
            bool success = true;
            
            // Apply subscriptions one by one for all exchanges (unified batch mode)
            foreach (var request in pendingList)
            {
                bool subscriptionSuccess = false;
                
                switch (request.DataType)
                {
                    case ChannelDataType.Orderbook:
                        subscriptionSuccess = await client.SubscribeOrderbookAsync(request.Symbol);
                        break;
                    case ChannelDataType.Trades:
                        subscriptionSuccess = await client.SubscribeTradesAsync(request.Symbol);
                        break;
                    case ChannelDataType.Ticker:
                        subscriptionSuccess = await client.SubscribeTickerAsync(request.Symbol);
                        break;
                    case ChannelDataType.Candles:
                        if (!string.IsNullOrEmpty(request.Interval))
                        {
                            subscriptionSuccess = await client.SubscribeCandlesAsync(request.Symbol, request.Interval);
                        }
                        break;
                }
                
                if (subscriptionSuccess)
                {
                    // Mark channel as active
                    var channelId = ChannelInfo.GenerateChannelId(exchange, request.Symbol, request.DataType, request.Interval);
                    if (_channels.TryGetValue(channelId, out var channel))
                    {
                        channel.IsActive = true;
                    }
                }
                else
                {
                    Console.WriteLine($"Failed to subscribe to {request.DataType} for {request.Symbol}");
                    success = false;
                }
            }
            
            if (success)
            {
                // Clear pending list after successful application
                pendingList.Clear();
                Console.WriteLine($"Successfully applied subscriptions for {exchange}");
            }
            else
            {
                Console.WriteLine($"Some subscriptions failed for {exchange}");
            }

            return success;
        }

        /// <summary>
        /// Get pending subscriptions for an exchange
        /// </summary>
        public IEnumerable<ChannelSubscriptionRequest> GetPendingSubscriptions(string exchange)
        {
            var exchangeLower = exchange.ToLower();
            if (_pendingSubscriptions.TryGetValue(exchangeLower, out var pendingList))
            {
                lock (_lockObject)
                {
                    return pendingList.ToList();
                }
            }
            return Enumerable.Empty<ChannelSubscriptionRequest>();
        }

        /// <summary>
        /// Clear pending subscriptions for an exchange
        /// </summary>
        public void ClearPendingSubscriptions(string exchange)
        {
            var exchangeLower = exchange.ToLower();
            if (_pendingSubscriptions.TryGetValue(exchangeLower, out var pendingList))
            {
                lock (_lockObject)
                {
                    pendingList.Clear();
                }
            }
        }

        private void SetupEventHandlers(IWebSocketClient client, ChannelInfo channelInfo)
        {
            // Note: In a real implementation, we would need to be careful about 
            // multiple subscriptions and properly manage event handler lifecycle
            
            switch (channelInfo.DataType)
            {
                case ChannelDataType.Orderbook:
                    // Track orderbook updates for this channel
                    break;
                case ChannelDataType.Trades:
                    // Track trade updates for this channel
                    break;
                case ChannelDataType.Ticker:
                    // Track ticker updates for this channel
                    break;
                case ChannelDataType.Candles:
                    // Track candle updates for this channel
                    break;
                // Add other data types as needed
            }
        }

        /// <summary>
        /// Dispose of all resources
        /// </summary>
        public void Dispose()
        {
            // Disconnect all clients
            foreach (var client in _exchangeClients.Values)
            {
                try
                {
                    client.DisconnectAsync().Wait(TimeSpan.FromSeconds(5));
                    client.Dispose();
                }
                catch
                {
                    // Ignore errors during disposal
                }
            }

            _exchangeClients.Clear();
            _channels.Clear();
        }
    }
}