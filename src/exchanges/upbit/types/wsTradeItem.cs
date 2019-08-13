using Newtonsoft.Json;

namespace CCXT.Collector.Upbit.Types
{
    //{
    //    "type": "trade",
    //    "code": "KRW-BTC",
    //    "timestamp": 1553853571255,
    //    "trade_date": "2019-03-29",
    //    "trade_time": "09:59:31",
    //    "trade_price": 4627000.0,
    //    "trade_volume": 0.75,
    //    "prev_closing_price": 4550000.00000000,
    //    "change_price": 77000.00000000,
    //    "ask_bid": "ASK",
    //    "sequential_id": 1553853571000002,
    //    "trade_timestamp": 1553853571000,
    //    "change": "RISE",
    //    "stream_type": "SNAPSHOT"
    //}

    /// <summary>
    ///
    /// </summary>
    public class UWTradeItem : UTradeItem
    {
        /// <summary>
        ///
        /// </summary>
        public string type
        {
            get;
            set;
        }

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