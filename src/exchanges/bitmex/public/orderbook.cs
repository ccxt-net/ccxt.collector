using Newtonsoft.Json;
using CCXT.NET.Shared.Coin.Public;
using CCXT.NET.Shared.Coin.Types;

namespace CCXT.Collector.BitMEX.Public
{
    /// <summary>
    ///
    /// </summary>
    public class BOrderBookItem : CCXT.NET.Shared.Coin.Public.OrderBookItem, IOrderBookItem
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