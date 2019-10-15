using CCXT.Collector.Library.Types;
using Newtonsoft.Json;
using OdinSdk.BaseLib.Coin.Types;

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
    public class UATrade : STrade
    {
        /// <summary>
        /// 마켓 구분 코드
        /// </summary>
        [JsonProperty(PropertyName = "market")]
        public override string symbol
        {
            get;
            set;
        }

        /// <summary>
        /// 체결 타임스탬프
        /// </summary>
        [JsonProperty(PropertyName = "timestamp")]
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