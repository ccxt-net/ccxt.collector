using CCXT.Collector.Library;
using Newtonsoft.Json;
using CCXT.NET.Shared.Coin.Types;
using System.Collections.Generic;

namespace CCXT.Collector.Service
{
    /// <summary>
    /// 
    /// </summary>
    public class SCompleteOrderItem
    {
        /// <summary>
        ///
        /// </summary>
        public virtual long timestamp
        {
            get;
            set;
        }

        public virtual decimal quantity
        {
            get;
            set;
        }

        public virtual decimal price
        {
            get;
            set;
        }

        /// <summary>
        /// 
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
    ///
    /// </summary>
    public class SCompleteOrders : SApiResult<List<SCompleteOrderItem>>
    {
#if RAWJSON
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