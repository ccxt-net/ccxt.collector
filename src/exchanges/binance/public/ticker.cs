using Newtonsoft.Json;

namespace CCXT.Collector.Binance.Public
{
    /// <summary>
    ///
    /// </summary>
    public class BTickerItem : CCXT.Collector.Library.Public.STickerItem
    {
        /// <summary>
        /// 
        /// </summary>
        public string symbol
        {
            get;
            set;
        }

        /// <summary>
        /// ask quantity
        /// </summary>
        [JsonProperty(PropertyName = "askQty")]
        public override decimal askSize
        {
            get;
            set;
        }

        /// <summary>
        /// bid quantity
        /// </summary>
        [JsonProperty(PropertyName = "bidQty")]
        public override decimal bidSize
        {
            get;
            set;
        }
    }
}