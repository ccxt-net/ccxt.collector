namespace CCXT.Collector.Library.Types
{
    public class Settings
    {
        /// <summary>
        ///
        /// </summary>
        public long last_trade_id
        {
            get;
            set;
        }

        /// <summary>
        ///
        /// </summary>
        public long last_trade_time
        {
            get;
            set;
        }

        /// <summary>
        ///
        /// </summary>
        public long last_orderbook_id
        {
            get;
            set;
        }

        /// <summary>
        ///
        /// </summary>
        public long last_orderbook_time
        {
            get;
            set;
        }

        /// <summary>
        ///
        /// </summary>
        public long orderbook_count
        {
            get;
            set;
        }

        /// <summary>
        ///
        /// </summary>
        public decimal before_trade_ask_size
        {
            get;
            set;
        }

        /// <summary>
        ///
        /// </summary>
        public decimal before_trade_bid_size
        {
            get;
            set;
        }

        /// <summary>
        ///
        /// </summary>
        public decimal last_order_ask_size
        {
            get;
            set;
        }

        /// <summary>
        ///
        /// </summary>
        public decimal last_order_bid_size
        {
            get;
            set;
        }

        /// <summary>
        ///
        /// </summary>
        public bool trades_flag
        {
            get;
            set;
        }

        /// <summary>
        ///
        /// </summary>
        public decimal first_ask_price
        {
            get;
            set;
        }

        /// <summary>
        ///
        /// </summary>
        public decimal first_bid_price
        {
            get;
            set;
        }
    }
}