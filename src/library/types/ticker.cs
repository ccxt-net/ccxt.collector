using OdinSdk.BaseLib.Coin.Public;
using System.Collections.Generic;

namespace CCXT.Collector.Library.Types
{
    /// <summary>
    ///
    /// </summary>
    public class STickerItem : OdinSdk.BaseLib.Coin.Public.TickerItem, ITickerItem
    {
    }

    /// <summary>
    ///
    /// </summary>
    public class STickers
    {
        /// <summary>
        /// exchange
        /// </summary>
        public virtual string exchange
        {
            get;
            set;
        }

        /// <summary>
        /// stream
        /// </summary>
        public virtual string stream
        {
            get;
            set;
        }
        
        /// <summary>
        /// symbol
        /// </summary>
        public virtual string symbol
        {
            get;
            set;
        }

        /// <summary>
        /// sequential id
        /// </summary>
        public virtual long sequential_id
        {
            get;
            set;
        }

        /// <summary>
        /// data
        /// </summary>
        public virtual List<STickerItem> data
        {
            get;
            set;
        }
    }
}