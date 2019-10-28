using OdinSdk.BaseLib.Coin;
using OdinSdk.BaseLib.Coin.Types;
using OdinSdk.BaseLib.Configuration;
using System.Collections.Generic;

namespace CCXT.Collector.Library.Types
{
    /// <summary>
    /// 
    /// </summary>
    public class SCompleteOrderItem //: OdinSdk.BaseLib.Coin.Public.CompleteOrderItem, ICompleteOrderItem
    {
        /// <summary>
        /// 
        /// </summary>
        public virtual string symbol
        {
            get;
            set;
        }

        /// <summary>
        ///
        /// </summary>
        public virtual long timestamp
        {
            get;
            set;
        }

        /// <summary>
        /// ISO 8601 datetime string with milliseconds
        /// </summary>
        public virtual string datetime
        {
            get
            {
                return CUnixTime.ConvertToUtcTimeMilli(timestamp).ToString("o");
            }
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
        public virtual decimal amount
        {
            get
            {
                return price * quantity;
            }
        }

        /// <summary>
        ///
        /// </summary>
        public virtual long sequential_id
        {
            get;
            set;
        }

        /// <summary>
        /// sell or buy
        /// </summary>
        public virtual SideType sideType
        {
            get;
            set;
        }
    }

    /// <summary>
    ///
    /// </summary>
    public class SCompleteOrders : ApiResult<List<SCompleteOrderItem>>
    {
        /// <summary>
        ///
        /// </summary>
        public virtual string exchange
        {
            get;
            set;
        }

        /// <summary>
        /// S, R
        /// </summary>
        public virtual string stream
        {
            get;
            set;
        }

        /// <summary>
        ///
        /// </summary>
        public virtual string symbol
        {
            get;
            set;
        }

        ///// <summary>
        /////
        ///// </summary>
        //public virtual List<SCompleteOrderItem> data
        //{
        //    get;
        //    set;
        //}

#if DEBUG
        /// <summary>
        ///
        /// </summary>
        public virtual string rawJson
        {
            get;
            set;
        }
#endif
    }
}