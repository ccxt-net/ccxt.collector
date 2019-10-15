using System.Collections.Generic;

namespace CCXT.Collector.Library.Types
{
    /// <summary>
    /// item of orderbook
    /// </summary>
    public class SOrderBook
    {
        /// <summary>
        /// I,U,D
        /// </summary>
        public virtual string action
        {
            get;
            set;
        }

        /// <summary>
        ///
        /// </summary>
        public virtual string side
        {
            get;
            set;
        }

        /// <summary>
        /// quantity
        /// </summary>
        public virtual decimal quantity
        {
            get;
            set;
        }

        /// <summary>
        /// price
        /// </summary>
        public virtual decimal price
        {
            get;
            set;
        }
    }

    /// <summary>
    ///
    /// </summary>
    public class SOrderBooks
    {
        /// <summary>
        ///
        /// </summary>
        public SOrderBooks(string exchange, string stream, string symbol)
        {
            this.exchange = exchange;
            this.stream = stream;
            this.symbol = symbol;

            this.data = new List<SOrderBook>();
        }

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
        /// string symbol of the market ('BTCUSD', 'ETHBTC', ...)
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

        /// <summary>
        ///
        /// </summary>
        public virtual List<SOrderBook> data
        {
            get;
            set;
        }
    }
}