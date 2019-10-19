using OdinSdk.BaseLib.Coin.Public;
using System.Collections.Generic;

namespace CCXT.Collector.Library.Types
{
    /// <summary>
    /// item of orderbook
    /// </summary>
    public class SOrderBook : OdinSdk.BaseLib.Coin.Public.OrderBookItem, IOrderBookItem
    {
        /// <summary>
        /// I,U,D
        /// </summary>
        public string action
        {
            get;
            set;
        }

        /// <summary>
        ///
        /// </summary>
        public string side
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
        public string exchange
        {
            get;
            set;
        }

        /// <summary>
        /// S, R
        /// </summary>
        public string stream
        {
            get;
            set;
        }

        /// <summary>
        /// string symbol of the market ('BTCUSD', 'ETHBTC', ...)
        /// </summary>
        public string symbol
        {
            get;
            set;
        }

        /// <summary>
        ///
        /// </summary>
        public long sequential_id
        {
            get;
            set;
        }

        /// <summary>
        ///
        /// </summary>
        public List<SOrderBook> data
        {
            get;
            set;
        }
    }
}