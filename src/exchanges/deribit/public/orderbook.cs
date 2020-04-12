using Newtonsoft.Json;
using OdinSdk.BaseLib.Coin.Public;
using OdinSdk.BaseLib.Coin.Types;

namespace CCXT.Collector.Deribit.Public
{
    /*
    {
        "timestamp":1586715917140
        "stats":
        {
            "volume_usd":84270120
            "volume":12112.91838284
            "price_change":4.6947
            "low":6768.5
            "high":7199
        }
        "state":"open"
        "settlement_price":6835.65
        "open_interest":57362380
        "min_price":7017.27
        "max_price":7230.99
        "mark_price":7124.29
        "last_price":7125
        "instrument_name":"BTC-PERPETUAL"
        "index_price":7125.68
        "funding_8h":-0.00022331
        "estimated_delivery_price":7125.68
        "current_funding":0
        "change_id":18497226614
        "best_bid_price":7124.5
        "best_bid_amount":2180
        "best_ask_price":7125
        "best_ask_amount":56330
        "bids":[ price, qty ]
        "asks":[ price, qty ]
    */

    /// <summary>
    ///
    /// </summary>
    public class DOrderBookItem : OdinSdk.BaseLib.Coin.Public.OrderBookItem, IOrderBookItem
    {
        /// <summary>
        ///
        /// </summary>
        [JsonProperty(PropertyName = "symbol")]
        public string symbol
        {
            get;
            set;
        }

        /// <summary>
        ///
        /// </summary>
        [JsonProperty(PropertyName = "id")]
        public long id
        {
            get;
            set;
        }

        /// <summary>
        ///
        /// </summary>
        [JsonProperty(PropertyName = "originSide")]
        public SideType sideType
        {
            get;
            set;
        }

        /// <summary>
        /// 주문 종류1 (partial, update)
        /// </summary>
        [JsonProperty(PropertyName = "side")]
        private string sideValue
        {
            set
            {
                sideType = SideTypeConverter.FromString(value);
            }
        }

        /// <summary>
        /// 주문 종류2 (insert, delete)
        /// </summary>
        [JsonProperty(PropertyName = "sideType")]
        private string sideValue2
        {
            set
            {
                sideType = SideTypeConverter.FromString(value);
            }
        }

        /// <summary>
        ///
        /// </summary>
        [JsonProperty(PropertyName = "size")]
        public override decimal quantity
        {
            get;
            set;
        }

        /// <summary>
        ///
        /// </summary>
        [JsonProperty(PropertyName = "price")]
        public override decimal price
        {
            get;
            set;
        }
    }
}