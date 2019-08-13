using Newtonsoft.Json;
using System.Collections.Generic;

namespace CCXT.Collector.Upbit.Types
{
    //{
    //    "market": "KRW-BTC",
    //    "timestamp": 1554047009000,
    //    "trade_date_utc": "2019-03-31",
    //    "trade_time_utc": "15:43:29",
    //    "trade_price": 4656000.00000000,
    //    "trade_volume": 0.31198825,
    //    "prev_closing_price": 4636000.00000000,
    //    "change_price": 20000.00000000,
    //    "ask_bid": "BID",
    //    "sequential_id": 1554047009000005
    //}

    /// <summary>
    ///
    /// </summary>
    public class UATradeItem : UTradeItem
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

        /// <summary>
        ///
        /// </summary>
        [JsonProperty(PropertyName = "trade_date_utc")]
        public override string trade_date
        {
            get;
            set;
        }

        /// <summary>
        ///
        /// </summary>
        [JsonProperty(PropertyName = "trade_time_utc")]
        public override string trade_time
        {
            get;
            set;
        }
    }

    /// <summary>
    ///
    /// </summary>
    public class UATrade
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
        public string symbol
        {
            get;
            set;
        }

        /// <summary>
        ///
        /// </summary>
        public List<UATradeItem> data
        {
            get;
            set;
        }
    }
}