using CCXT.Collector.Library.Types;
using Newtonsoft.Json;
using OdinSdk.BaseLib.Coin.Types;

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
    public class UWTrade : STrade
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
                sideType = SideTypeConverter.FromString(value);
            }
        }
    }
}