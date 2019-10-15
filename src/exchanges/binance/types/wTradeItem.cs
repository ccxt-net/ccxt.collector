using Newtonsoft.Json;

namespace CCXT.Collector.Binance.Types
{
    //{
    //    "e": "aggTrade",  // Event type
    //    "E": 123456789,   // Event time
    //    "s": "BNBBTC",    // Symbol
    //    "a": 12345,       // Aggregate trade ID
    //    "p": "0.001",     // Price
    //    "q": "100",       // Quantity
    //    "f": 100,         // First trade ID
    //    "l": 105,         // Last trade ID
    //    "T": 123456785,   // Trade time
    //    "m": true,        // Is the buyer the market maker?
    //    "M": true         // Ignore
    //}

    public class BWTrade
    {
        /// <summary>
        ///
        /// </summary>
        public string stream
        {
            get;
            set;
        }

        /// <summary>
        ///
        /// </summary>
        public BWTradeItem data
        {
            get;
            set;
        }
    }

    /// <summary>
    ///
    /// </summary>
    public class BWTradeItem : BTradeItem
    {
        /// <summary>
        /// Event type
        /// </summary>
        [JsonProperty(PropertyName = "e")]
        public string eventType
        {
            get;
            set;
        }

        /// <summary>
        /// Event time
        /// </summary>
        [JsonProperty(PropertyName = "E")]
        public long eventTime
        {
            get;
            set;
        }

        /// <summary>
        /// Symbol
        /// </summary>
        [JsonProperty(PropertyName = "s")]
        public override string symbol
        {
            get;
            set;
        }

        /// <summary>
        /// Aggregate tradeId
        /// </summary>
        [JsonProperty(PropertyName = "a")]
        public override long tradeId
        {
            get;
            set;
        }

        /// <summary>
        /// Price
        /// </summary>
        [JsonProperty(PropertyName = "p")]
        public override decimal price
        {
            get;
            set;
        }

        /// <summary>
        /// Quantity
        /// </summary>
        [JsonProperty(PropertyName = "q")]
        public override decimal quantity
        {
            get;
            set;
        }

        /// <summary>
        /// First tradeId
        /// </summary>
        [JsonProperty(PropertyName = "f")]
        public override long firstTradeId
        {
            get;
            set;
        }

        /// <summary>
        /// Last tradeId
        /// </summary>
        [JsonProperty(PropertyName = "l")]
        public override long lastTradeId
        {
            get;
            set;
        }

        /// <summary>
        /// Trade time
        /// </summary>
        [JsonProperty(PropertyName = "T")]
        public override long timestamp
        {
            get;
            set;
        }

        /// <summary>
        /// Is the buyer the market maker?
        /// </summary>
        [JsonProperty(PropertyName = "m")]
        public override bool isBuyerMaker
        {
            get;
            set;
        }

        /// <summary>
        /// Ignore
        /// </summary>
        [JsonProperty(PropertyName = "M")]
        public override bool isBestMatch
        {
            get;
            set;
        }
    }
}