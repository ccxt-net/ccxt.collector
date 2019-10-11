namespace CCXT.Collector.Upbit.Public
{
    /// <summary>
    ///
    /// </summary>
    public class UTradeItem
    {
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
        public virtual string trade_date
        {
            get;
            set;
        }

        /// <summary>
        ///
        /// </summary>
        public virtual string trade_time
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
        public virtual long trade_timestamp
        {
            get;
            set;
        }

        /// <summary>
        ///
        /// </summary>
        public virtual decimal trade_price
        {
            get;
            set;
        }

        /// <summary>
        ///
        /// </summary>
        public virtual decimal trade_volume
        {
            get;
            set;
        }

        /// <summary>
        ///
        /// </summary>
        public virtual decimal prev_closing_price
        {
            get;
            set;
        }

        /// <summary>
        ///
        /// </summary>
        public virtual string change
        {
            get;
            set;
        }

        /// <summary>
        ///
        /// </summary>
        public virtual decimal change_price
        {
            get;
            set;
        }

        /// <summary>
        ///
        /// </summary>
        public virtual string ask_bid
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
        public virtual string stream_type
        {
            get;
            set;
        }
    }
}