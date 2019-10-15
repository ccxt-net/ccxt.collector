using System.Collections.Generic;

namespace CCXT.Collector.Library.Types
{
    /// <summary>
    ///
    /// </summary>
    public class SBookTicker
    {
        /// <summary>
        /// symbol
        /// </summary>
        public virtual string symbol
        {
            get;
            set;
        }

        /// <summary>
        /// ask price
        /// </summary>
        public virtual decimal askPrice
        {
            get;
            set;
        }

        /// <summary>
        /// ask quantity
        /// </summary>
        public virtual decimal askQty
        {
            get;
            set;
        }

        /// <summary>
        /// bid price
        /// </summary>
        public virtual decimal bidPrice
        {
            get;
            set;
        }

        /// <summary>
        /// bid quantity
        /// </summary>
        public virtual decimal bidQty
        {
            get;
            set;
        }
    }

    /// <summary>
    ///
    /// </summary>
    public class SBookTickers
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
        public virtual List<SBookTicker> data
        {
            get;
            set;
        }
    }
}