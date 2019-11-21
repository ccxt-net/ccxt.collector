using Newtonsoft.Json;
using System.Collections.Generic;

namespace CCXT.Collector.Library.Public
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
    }
}