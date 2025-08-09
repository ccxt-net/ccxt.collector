using CCXT.Collector.Core.Abstractions;
using CCXT.Collector.Library;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace CCXT.Collector.Service
{
    /// <summary>
    /// Order side type (Buy/Sell)
    /// </summary>
    public enum SideType
    {
        /// <summary>
        /// Buy order (bid)
        /// </summary>
        Bid,
        
        /// <summary>
        /// Sell order (ask)
        /// </summary>
        Ask,
        
        /// <summary>
        /// Unknown side
        /// </summary>
        Unknown
    }

    /// <summary>
    /// Trade/Complete order item
    /// </summary>
    public class SCompleteOrderItem
    {
        /// <summary>
        /// Order/Trade ID
        /// </summary>
        public string orderId
        {
            get;
            set;
        }

        /// <summary>
        /// Unix timestamp in milliseconds
        /// </summary>
        public virtual long timestamp
        {
            get;
            set;
        }

        /// <summary>
        /// Trade quantity
        /// </summary>
        public virtual decimal quantity
        {
            get;
            set;
        }

        /// <summary>
        /// Trade price
        /// </summary>
        public virtual decimal price
        {
            get;
            set;
        }

        /// <summary>
        /// Trade amount (price * quantity)
        /// </summary>
        public virtual decimal amount
        {
            get;
            set;
        }

        /// <summary>
        /// Order type (Market, Limit, etc.)
        /// </summary>
        [JsonIgnore]
        public OrderType orderType
        {
            get;
            set;
        }

        /// <summary>
        /// String representation of side
        /// </summary>
        public string side
        {
            get
            {
                return sideType.ToString();
            }
        }

        /// <summary>
        /// sell or buy
        /// </summary>
        [JsonIgnore]
        public SideType sideType
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Complete orders/trades data structure
    /// </summary>
    public class SCompleteOrders : SApiResult<List<SCompleteOrderItem>>
    {
        /// <summary>
        /// Exchange name
        /// </summary>
        public new string exchange
        {
            get;
            set;
        }

        /// <summary>
        /// Trading symbol/pair
        /// </summary>
        public new string symbol
        {
            get;
            set;
        }

        /// <summary>
        /// Unix timestamp in milliseconds
        /// </summary>
        public new long timestamp
        {
            get;
            set;
        }

#if DEBUG
        /// <summary>
        ///
        /// </summary>
        [JsonIgnore]
        public string rawJson
        {
            get;
            set;
        }
#endif
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    public delegate void TradingEventHandler(object sender, CCEventArgs e);


    /// <summary>
    /// 
    /// </summary>
    public class CCTrading
    {
        public static event TradingEventHandler TradingEvent;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="exchange"></param>
        /// <param name="jsonMessage"></param>
        public void Write(object sender, string exchange, string jsonMessage)
        {
            if (TradingEvent != null)
            {
                TradingEvent(sender, new CCEventArgs
                {
                    exchange = exchange,
                    message = jsonMessage
                });
            }
        }
    }
}