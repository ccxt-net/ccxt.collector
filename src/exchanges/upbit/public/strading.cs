using System.Collections.Generic;

namespace CCXT.Collector.Upbit.Public
{
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

            this.data = new List<UTradeItem>();
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
        public List<UTradeItem> data
        {
            get;
            set;
        }
    }
}