using CCXT.Collector.Upbit.Public;
using Newtonsoft.Json;

namespace CCXT.Collector.Upbit.Types
{
    //{
    //    "type": "orderbook",
    //    "code": "KRW-BTC",
    //    "timestamp": 1553853571184,
    //    "total_ask_size": 36.90715747,
    //    "total_bid_size": 29.12064978,
    //    "orderbook_units": [{
    //        "ask_price": 4627000.0,
    //        "bid_price": 4626000.0,
    //        "ask_size": 1.40706623,
    //        "bid_size": 3.57203617
    //            }],
    //    "stream_type": "SNAPSHOT"
    //}

    /// <summary>
    ///
    /// </summary>
    public class UWOrderBook : UOrderBook
    {
        /// <summary>
        ///
        /// </summary>
        [JsonProperty(PropertyName = "code")]
        public override string symbol
        {
            get;
            set;
        }
    }
}