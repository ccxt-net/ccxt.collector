using Newtonsoft.Json;
using CCXT.NET.Shared.Coin.Types;

namespace CCXT.Collector.Upbit.Public
{
    /// <summary>
    ///
    /// </summary>
    public class UCompleteOrderItem : CCXT.Collector.Service.SCompleteOrderItem
    {
        /// <summary>
        ///
        /// </summary>
        [JsonProperty(PropertyName = "trade_volume")]
        public override decimal quantity
        {
            get;
            set;
        }

        /// <summary>
        ///
        /// </summary>
        [JsonProperty(PropertyName = "trade_price")]
        public override decimal price
        {
            get;
            set;
        }

        /// <summary>
        ///
        /// </summary>
        [JsonProperty(PropertyName = "prev_closing_price")]
        public decimal prevPrice
        {
            get;
            set;
        }

        /// <summary>
        ///
        /// </summary>
        [JsonProperty(PropertyName = "change_price")]
        public decimal changePrice
        {
            get;
            set;
        }

        /// <summary>
        ///
        /// </summary>
        [JsonProperty(PropertyName = "ask_bid")]
        private string sideValue
        {
            set
            {
                sideType = SideTypeConverter.FromString(value ?? "");
            }
        }
    }

    //[{
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
    //}]

    /// <summary>
    ///
    /// </summary>
    public class UACompleteOrderItem : UCompleteOrderItem
    {
        /// <summary>
        /// 마켓 구분 코드
        /// </summary>
        [JsonProperty(PropertyName = "market")]
        public string symbol
        {
            get;
            set;
        }

        /// <summary>
        /// 
        /// </summary>
        public long sequential_id
        {
            get;
            set;
        }
    }

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
    public class UWCompleteOrderItem : UCompleteOrderItem
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
        public string symbol
        {
            get;
            set;
        }

        /// <summary>
        /// 체결 타임스탬프
        /// </summary>
        [JsonProperty(PropertyName = "trade_timestamp")]
        public override long timestamp
        {
            get;
            set;
        }

        /// <summary>
        /// 
        /// </summary>
        public long sequential_id
        {
            get;
            set;
        }

        /// <summary>
        ///
        /// </summary>
        public string change
        {
            get;
            set;
        }

        /// <summary>
        ///
        /// </summary>
        public string stream_type
        {
            get;
            set;
        }
    }
}