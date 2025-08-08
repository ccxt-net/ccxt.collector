using CCXT.Collector.Library;
using System.Collections.Generic;

namespace CCXT.Collector.Service
{
    /// <summary>
    ///
    /// </summary>
    public class STickerItem
    {
        /// <summary>
        /// 
        /// </summary>
        public virtual decimal askPrice
        {
            get;
            set;
        }

        /// <summary>
        /// 
        /// </summary>
        public virtual decimal bidPrice
        {
            get;
            set;
        }

        /// <summary>
        /// 
        /// </summary>
        public virtual decimal askSize
        {
            get;
            set;
        }

        /// <summary>
        /// 
        /// </summary>
        public virtual decimal bidSize
        {
            get;
            set;
        }
    }

    /// <summary>
    ///
    /// </summary>
    public class STickers : SApiResult<List<STickerItem>>
    {
        /// <summary>
        /// 64-bit Unix Timestamp in milliseconds since Epoch 1 Jan 1970
        /// </summary>
        public long timestamp
        {
            get;
            set;
        }

        /// <summary>
        /// 
        /// </summary>
        public decimal totalAskSize
        {
            get;
            set;
        }

        /// <summary>
        /// 
        /// </summary>
        public decimal totalBidSize
        {
            get;
            set;
        }
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
    public delegate void TickerEventHandler(object sender, CCEventArgs e);


    /// <summary>
    /// 
    /// </summary>
    public class CCTicker
    {
        public static event TickerEventHandler TickerEvent;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="exchange"></param>
        /// <param name="jsonMessage"></param>
        public void Write(object sender, string exchange, string jsonMessage)
        {
            if (TickerEvent != null)
            {
                TickerEvent(sender, new CCEventArgs
                {
                    exchange = exchange,
                    message = jsonMessage
                });
            }
        }
    }
}