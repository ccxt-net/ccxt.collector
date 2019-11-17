using Newtonsoft.Json;

namespace CCXT.Collector.Binance.Public
{
    /// <summary>
    ///
    /// </summary>
    public class BTickerItem : CCXT.Collector.Library.Public.STickerItem
    {
        /// <summary>
        /// ask price
        /// </summary>
        public override decimal askPrice
        {
            get;
            set;
        }

        /// <summary>
        /// ask quantity
        /// </summary>
        [JsonProperty(PropertyName = "askQty")]
        public override decimal askQuantity
        {
            get;
            set;
        }

        /// <summary>
        /// bid price
        /// </summary>
        public override decimal bidPrice
        {
            get;
            set;
        }

        /// <summary>
        /// bid quantity
        /// </summary>
        [JsonProperty(PropertyName = "bidQty")]
        public override decimal bidQuantity
        {
            get;
            set;
        }
    }
}