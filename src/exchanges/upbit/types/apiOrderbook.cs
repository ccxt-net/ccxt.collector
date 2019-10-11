using CCXT.Collector.Upbit.Public;
using Newtonsoft.Json;

namespace CCXT.Collector.Upbit.Types
{
    //{
    //  "market": "KRW-BTC",
    //  "timestamp": 1529910247984,
    //  "total_ask_size": 8.83621228,
    //  "total_bid_size": 2.43976741,
    //  "orderbook_units": [{
    //      "ask_price": 6956000,
    //      "bid_price": 6954000,
    //      "ask_size": 0.24078656,
    //      "bid_size": 0.00718341
    //  }]
    //}

    /// <summary>
    ///
    /// </summary>
    public class UAOrderBook : UOrderBook
    {
        /// <summary>
        ///
        /// </summary>
        [JsonProperty(PropertyName = "market")]
        public override string symbol
        {
            get;
            set;
        }
    }
}