using System.Collections.Generic;

namespace CCXT.Collector.Upbit.Public
{
    /// <summary>
    /// item of orderbook
    /// </summary>
    public class UOrderBookItem
    {
        /// <summary>
        ///
        /// </summary>
        public virtual decimal ask_price
        {
            get;
            set;
        }

        /// <summary>
        ///
        /// </summary>
        public virtual decimal ask_size
        {
            get;
            set;
        }

        /// <summary>
        ///
        /// </summary>
        public virtual decimal bid_price
        {
            get;
            set;
        }

        /// <summary>
        ///
        /// </summary>
        public virtual decimal bid_size
        {
            get;
            set;
        }
    }

    /// <summary>
    ///
    /// </summary>
    public class UOrderBook
    {
        /// <summary>
        ///
        /// </summary>
        public virtual string type
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
        public virtual long timestamp
        {
            get;
            set;
        }

        /// <summary>
        ///
        /// </summary>
        public virtual decimal total_ask_size
        {
            get;
            set;
        }

        /// <summary>
        ///
        /// </summary>
        public virtual decimal total_bid_size
        {
            get;
            set;
        }

        /// <summary>
        ///
        /// </summary>
        public virtual List<UOrderBookItem> orderbook_units
        {
            get;
            set;
        }

        /// <summary>
        ///
        /// </summary>
        public virtual string stream_type
        {
            get;
            set;
        }
    }
}