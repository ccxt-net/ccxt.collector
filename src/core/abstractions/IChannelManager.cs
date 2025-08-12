using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CCXT.Collector.Core.Abstractions
{
    /// <summary>
    /// Interface for managing WebSocket channel subscriptions
    /// </summary>
    public interface IChannelManager
    {
        /// <summary>
        /// Register a new channel subscription
        /// </summary>
        /// <param name="exchange">Exchange name</param>
        /// <param name="symbol">Trading pair symbol (e.g., BTC/USDT)</param>
        /// <param name="dataType">Data type (orderbook, trades, ticker, candles)</param>
        /// <param name="interval">Interval for candle data (optional)</param>
        /// <returns>Unique channel ID if successful, null if failed</returns>
        Task<string> RegisterChannelAsync(string exchange, string symbol, ChannelDataType dataType, string interval = null);

        /// <summary>
        /// Remove a channel subscription
        /// </summary>
        /// <param name="channelId">Unique channel ID</param>
        /// <returns>True if successful</returns>
        Task<bool> RemoveChannelAsync(string channelId);

        /// <summary>
        /// Remove all channels for a specific exchange
        /// </summary>
        /// <param name="exchange">Exchange name</param>
        /// <returns>Number of channels removed</returns>
        Task<int> RemoveExchangeChannelsAsync(string exchange);

        /// <summary>
        /// Get all active channel subscriptions
        /// </summary>
        /// <returns>List of active channels</returns>
        IEnumerable<ChannelInfo> GetActiveChannels();

        /// <summary>
        /// Get active channels for a specific exchange
        /// </summary>
        /// <param name="exchange">Exchange name</param>
        /// <returns>List of active channels for the exchange</returns>
        IEnumerable<ChannelInfo> GetExchangeChannels(string exchange);

        /// <summary>
        /// Get channel info by ID
        /// </summary>
        /// <param name="channelId">Unique channel ID</param>
        /// <returns>Channel info if found, null otherwise</returns>
        ChannelInfo GetChannel(string channelId);

        /// <summary>
        /// Check if a channel is active
        /// </summary>
        /// <param name="channelId">Unique channel ID</param>
        /// <returns>True if channel is active</returns>
        bool IsChannelActive(string channelId);

        /// <summary>
        /// Get statistics for all channels
        /// </summary>
        /// <returns>Channel statistics</returns>
        ChannelStatistics GetStatistics();

        /// <summary>
        /// Apply pending subscriptions for an exchange
        /// </summary>
        /// <param name="exchange">Exchange name</param>
        /// <returns>True if successful</returns>
        Task<bool> ApplyBatchSubscriptionsAsync(string exchange);

        /// <summary>
        /// Get pending subscriptions for an exchange
        /// </summary>
        /// <param name="exchange">Exchange name</param>
        /// <returns>List of pending subscription requests</returns>
        IEnumerable<ChannelSubscriptionRequest> GetPendingSubscriptions(string exchange);

        /// <summary>
        /// Clear pending subscriptions for an exchange
        /// </summary>
        /// <param name="exchange">Exchange name</param>
        void ClearPendingSubscriptions(string exchange);
    }

    /// <summary>
    /// Channel data types
    /// </summary>
    public enum ChannelDataType
    {
        Orderbook,
        Trades,
        Ticker,
        Candles,
        Balance,
        Orders,
        Positions
    }

    /// <summary>
    /// Channel subscription information
    /// </summary>
    public class ChannelInfo
    {
        /// <summary>
        /// Unique channel identifier
        /// </summary>
        public string ChannelId { get; set; } = string.Empty;

        /// <summary>
        /// Exchange name
        /// </summary>
        public string Exchange { get; set; } = string.Empty;

        /// <summary>
        /// Trading pair symbol
        /// </summary>
        public string Symbol { get; set; } = string.Empty;

        /// <summary>
        /// Data type
        /// </summary>
        public ChannelDataType DataType { get; set; }

        /// <summary>
        /// Interval for candle data
        /// </summary>
        public string Interval { get; set; } = string.Empty;

        /// <summary>
        /// Subscription timestamp
        /// </summary>
        public DateTime SubscribedAt { get; set; }

        /// <summary>
        /// Last data received timestamp
        /// </summary>
        public DateTime? LastDataAt { get; set; }

        /// <summary>
        /// Is channel currently active
        /// </summary>
        public bool IsActive { get; set; }

        /// <summary>
        /// Number of messages received
        /// </summary>
        public long MessageCount { get; set; }

        /// <summary>
        /// Last error message if any
        /// </summary>
        public string LastError { get; set; } = string.Empty;

        /// <summary>
        /// Error count
        /// </summary>
        public int ErrorCount { get; set; }

        /// <summary>
        /// Generate a unique channel ID
        /// </summary>
        public static string GenerateChannelId(string exchange, string symbol, ChannelDataType dataType, string interval = null)
        {
            var baseId = $"{exchange}:{symbol}:{dataType}";
            return string.IsNullOrEmpty(interval) ? baseId : $"{baseId}:{interval}";
        }

        /// <summary>
        /// Create a display-friendly description
        /// </summary>
        public string GetDescription()
        {
            var desc = $"{Exchange} - {Symbol} - {DataType}";
            return string.IsNullOrEmpty(Interval) ? desc : $"{desc} ({Interval})";
        }
    }

    /// <summary>
    /// Channel subscription request
    /// </summary>
    public class ChannelSubscriptionRequest
    {
        /// <summary>
        /// Trading pair symbol
        /// </summary>
        public string Symbol { get; set; } = string.Empty;

        /// <summary>
        /// Data type to subscribe
        /// </summary>
        public ChannelDataType DataType { get; set; }

        /// <summary>
        /// Interval for candle data (optional)
        /// </summary>
        public string Interval { get; set; } = string.Empty;
    }

    /// <summary>
    /// Channel statistics
    /// </summary>
    public class ChannelStatistics
    {
        /// <summary>
        /// Total number of channels
        /// </summary>
        public int TotalChannels { get; set; }

        /// <summary>
        /// Number of active channels
        /// </summary>
        public int ActiveChannels { get; set; }

        /// <summary>
        /// Number of inactive channels
        /// </summary>
        public int InactiveChannels => TotalChannels - ActiveChannels;

        /// <summary>
        /// Channels per exchange
        /// </summary>
        public Dictionary<string, int> ChannelsByExchange { get; set; } = new Dictionary<string, int>();

        /// <summary>
        /// Channels per data type
        /// </summary>
        public Dictionary<ChannelDataType, int> ChannelsByType { get; set; } = new Dictionary<ChannelDataType, int>();

        /// <summary>
        /// Total messages received
        /// </summary>
        public long TotalMessages { get; set; }

        /// <summary>
        /// Total errors
        /// </summary>
        public long TotalErrors { get; set; }

        /// <summary>
        /// Oldest channel subscription time
        /// </summary>
        public DateTime? OldestSubscription { get; set; }

        /// <summary>
        /// Newest channel subscription time
        /// </summary>
        public DateTime? NewestSubscription { get; set; }
    }
}