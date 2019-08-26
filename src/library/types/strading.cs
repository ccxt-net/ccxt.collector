using CCXT.Collector.Upbit.Types;
using System.Collections.Generic;

namespace CCXT.Collector.Library
{
    /// <summary>
    /// 
    /// </summary>
    public class STradeItem : UTradeItem
    {
    }

    /// <summary>
    ///
    /// </summary>
    public class STrading
    {
        /// <summary>
        ///
        /// </summary>
        public STrading(string exchange, string stream, string symbol)
        {
            this.exchange = exchange;
            this.stream = stream;
            this.symbol = symbol;

            this.data = new List<STradeItem>();
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
        public List<STradeItem> data
        {
            get;
            set;
        }
    }
}