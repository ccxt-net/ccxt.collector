using System.Collections.Generic;

namespace CCXT.Collector.Binance.Types
{
    /// <summary>
    ///
    /// </summary>
    public class BOrderBookItem
    {
        /// <summary>
        /// Symbol
        /// </summary>
        public virtual string symbol
        {
            get;
            set;
        }

        /// <summary>
        /// First update ID in event
        /// </summary>
        public virtual long firstId
        {
            get;
            set;
        }

        /// <summary>
        ///
        /// </summary>
        public virtual long lastId
        {
            get;
            set;
        }

        /// <summary>
        ///
        /// </summary>
        public virtual List<decimal[]> asks
        {
            get;
            set;
        }

        /// <summary>
        ///
        /// </summary>
        public virtual List<decimal[]> bids
        {
            get;
            set;
        }
    }
}