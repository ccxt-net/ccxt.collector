using Newtonsoft.Json;
using System.Collections.Generic;

namespace CCXT.Collector.BitMEX.Public
{
    /// <summary>
    ///
    /// </summary>
    public class BTradeItem
    {
        /// <summary>
        /// Symbol
        /// </summary>
        public virtual string symbol
        {
            get;
            set;
        }

        /// <summary>
        /// Aggregate tradeId
        /// </summary>
        public virtual long tradeId
        {
            get;
            set;
        }

        /// <summary>
        /// Price
        /// </summary>
        public virtual decimal price
        {
            get;
            set;
        }

        /// <summary>
        /// Quantity
        /// </summary>
        public virtual decimal quantity
        {
            get;
            set;
        }

        /// <summary>
        /// First tradeId
        /// </summary>
        public virtual long firstTradeId
        {
            get;
            set;
        }

        /// <summary>
        /// Last tradeId
        /// </summary>
        public virtual long lastTradeId
        {
            get;
            set;
        }

        /// <summary>
        /// Timestamp
        /// </summary>
        public virtual long timestamp
        {
            get;
            set;
        }

        /// <summary>
        /// Was the buyer the maker?
        /// </summary>
        public virtual bool isBuyerMaker
        {
            get;
            set;
        }

        /// <summary>
        /// Was the trade the best price match?
        /// </summary>
        public virtual bool isBestMatch
        {
            get;
            set;
        }
    }

    //[
    //  {
    //      "a": 26129,         // Aggregate tradeId
    //      "p": "0.01633102",  // Price
    //      "q": "4.70443515",  // Quantity
    //      "f": 27781,         // First tradeId
    //      "l": 27781,         // Last tradeId
    //      "T": 1498793709153, // Timestamp
    //      "m": true,          // Was the buyer the maker?
    //      "M": true           // Was the trade the best price match?
    //  }
    //]

    /// <summary>
    ///
    /// </summary>
    public class BATradeItem : BTradeItem
    {
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
        /// Timestamp
        /// </summary>
        [JsonProperty(PropertyName = "T")]
        public override long timestamp
        {
            get;
            set;
        }

        /// <summary>
        /// Was the buyer the maker?
        /// </summary>
        [JsonProperty(PropertyName = "m")]
        public override bool isBuyerMaker
        {
            get;
            set;
        }

        /// <summary>
        /// Was the trade the best price match?
        /// </summary>
        [JsonProperty(PropertyName = "M")]
        public override bool isBestMatch
        {
            get;
            set;
        }
    }

    /// <summary>
    ///
    /// </summary>
    public class BATrade
    {
        /// <summary>
        ///
        /// </summary>
        public string exchange
        {
            get;
            set;
        }

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
        public string symbol
        {
            get;
            set;
        }

        /// <summary>
        ///
        /// </summary>
        public List<BATradeItem> data
        {
            get;
            set;
        }
    }

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
        public BWCompleteOrder data
        {
            get;
            set;
        }
    }

    /// <summary>
    ///
    /// </summary>
    public class BWCompleteOrder : BTradeItem
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