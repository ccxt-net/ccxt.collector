using System;

namespace CCXT.Collector.Models.WebSocket
{
    /// <summary>
    /// Represents subscription information for WebSocket channels
    /// </summary>
    public class SubscriptionInfo
    {
        /// <summary>
        /// Gets or sets the channel name
        /// </summary>
        public string Channel { get; set; }

        /// <summary>
        /// Gets or sets the symbol being subscribed to
        /// </summary>
        public string Symbol { get; set; }

        /// <summary>
        /// Gets or sets the subscription ID (if applicable)
        /// </summary>
        public string SubscriptionId { get; set; }

        /// <summary>
        /// Gets or sets whether the subscription is active
        /// </summary>
        public bool IsActive { get; set; }

        /// <summary>
        /// Gets or sets the timestamp when the subscription was created
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Gets or sets the timestamp when the subscription was activated
        /// </summary>
        public DateTime SubscribedAt { get; set; }

        /// <summary>
        /// Gets or sets the last update timestamp
        /// </summary>
        public DateTime? LastUpdateAt { get; set; }

        /// <summary>
        /// Gets or sets extra data (e.g., interval for candle subscriptions)
        /// </summary>
        public string Extra { get; set; }

        /// <summary>
        /// Creates a new instance of SubscriptionInfo
        /// </summary>
        public SubscriptionInfo()
        {
            CreatedAt = DateTime.UtcNow;
            IsActive = true;
        }

        /// <summary>
        /// Creates a new instance of SubscriptionInfo with channel and symbol
        /// </summary>
        public SubscriptionInfo(string channel, string symbol) : this()
        {
            Channel = channel;
            Symbol = symbol;
        }
    }
}