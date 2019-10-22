using OdinSdk.BaseLib.Coin.Public;
using System.Collections.Generic;

namespace CCXT.Collector.Library.Types
{
    /// <summary>
    /// 
    /// </summary>
    public class SCompleteOrder : OdinSdk.BaseLib.Coin.Public.CompleteOrderItem, ICompleteOrderItem
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
        public virtual long sequential_id
        {
            get;
            set;
        }
    }

    /// <summary>
    ///
    /// </summary>
    public class SCompleteOrders
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

        /// <summary>
        ///
        /// </summary>
        public virtual List<SCompleteOrder> data
        {
            get;
            set;
        }
    }
}