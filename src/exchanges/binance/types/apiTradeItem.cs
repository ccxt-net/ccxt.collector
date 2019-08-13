using Newtonsoft.Json;
using System.Collections.Generic;

namespace CCXT.Collector.Binance.Types
{
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
}