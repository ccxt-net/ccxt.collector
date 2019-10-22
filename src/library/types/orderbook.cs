using OdinSdk.BaseLib.Coin.Public;
using System.Collections.Generic;

namespace CCXT.Collector.Library.Types
{
    /// <summary>
    /// item of orderbook
    /// </summary>
    public class SOrderBookItem : OdinSdk.BaseLib.Coin.Public.OrderBookItem, IOrderBookItem
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
    public class SOrderBook : OdinSdk.BaseLib.Coin.Public.OrderBook, IOrderBook
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
        /// 호가 매도 총 잔량
        /// </summary>
        public virtual decimal totalAskQuantity
        {
            get;
            set;
        }

        /// <summary>
        /// 호가 매수 총 잔량
        /// </summary>
        public virtual decimal totalBidQuantity
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

            this.data = new List<SOrderBookItem>();
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
        public virtual List<SOrderBookItem> data
        {
            get;
            set;
        }
    }
}