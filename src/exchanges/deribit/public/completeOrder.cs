using Newtonsoft.Json;
using OdinSdk.BaseLib.Coin.Public;
using OdinSdk.BaseLib.Coin.Types;
using System.Collections.Generic;

namespace CCXT.Collector.Deribit.Public
{
    /*
     {
        "trade_seq":44753343
        "trade_id":"72055078"
        "timestamp":1586627123487
        "tick_direction":1
        "price":6820.5
        "instrument_name":"BTC-PERPETUAL"
        "index_price":6825.29
        "direction":"buy"
        "amount":50
    }
    */

    /// <summary>
    /// recent trade data
    /// </summary>
    public class DCompleteOrderItem : OdinSdk.BaseLib.Coin.Public.CompleteOrderItem, ICompleteOrderItem
    {
        /// <summary>
        /// The sequence number of the trade within instrument
        /// </summary>
        /// <value>The sequence number of the trade within instrument</value>
        [JsonProperty(PropertyName = "trade_seq")]
        public long tradeSeq
        {
            get; set;
        }

        /// <summary>
        ///
        /// </summary>
        [JsonProperty(PropertyName = "trade_id")]
        public override string transactionId
        {
            get;
            set;
        }

        /// <summary>
        ///
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
        [JsonProperty(PropertyName = "amount")]
        public override decimal quantity
        {
            get;
            set;
        }

        /// <summary>
        /// The price of the trade
        /// </summary>
        /// <value>The price of the trade</value>
        [JsonProperty(PropertyName = "price")]
        public override decimal price
        {
            get; set;
        }

        /// <summary>
        /// Index Price at the moment of trade
        /// </summary>
        /// <value>Index Price at the moment of trade</value>
        [JsonProperty(PropertyName = "index_price")]
        public decimal indexPrice
        {
            get; set;
        }

        /// <summary>
        ///
        /// </summary>
        [JsonProperty(PropertyName = "instrument_name")]
        public string symbol
        {
            get;
            set;
        }

        /// <summary>
        /// Direction of the tick (0: Plus Tick, 1: Zero-Plus Tick, 2: Minus Tick, 3: Zero-Minus Tick)
        /// </summary>
        [JsonProperty(PropertyName = "tick_direction")]
        public int tickDirection
        {
            get;
            set;
        }

        /// <summary>
        /// Trade direction of the taker
        /// </summary>
        [JsonProperty(PropertyName = "direction")]
        private string sideValue
        {
            set
            {
                sideType = SideTypeConverter.FromString(value);
            }
        }
    }

    public class DCompleteOrders
    {
        /// <summary>
        /// 
        /// </summary>
        public List<DCompleteOrderItem> trades
        {
            get; set;
        }

        /// <summary>
        /// 
        /// </summary>
        public bool has_more
        {
            get; set;
        }
    }
}