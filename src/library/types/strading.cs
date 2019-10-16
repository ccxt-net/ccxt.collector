using OdinSdk.BaseLib.Coin.Types;
using System.Collections.Generic;

namespace CCXT.Collector.Library.Types
{
    /// <summary>
    /// 
    /// </summary>
    public class STrade 
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
        ///
        /// </summary>
        public virtual long sequential_id
        {
            get;
            set;
        }

        /// <summary>
        ///
        /// </summary>
        public virtual decimal quantity
        {
            get;
            set;
        }

        /// <summary>
        ///
        /// </summary>
        public virtual decimal price
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
    public class STrades
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
        public virtual List<STrade> data
        {
            get;
            set;
        }
    }
}