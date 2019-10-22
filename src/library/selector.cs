namespace CCXT.Collector.Library
{
    public class QSelector
    {
        /// <summary>
        /// for Upbit
        /// </summary>
        public virtual string type
        {
            get;
            set;
        }

        /// <summary>
        /// for Binance
        /// </summary>
        public virtual string stream
        {
            get;
            set;
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
        ///
        /// </summary>
        public virtual string symbol
        {
            get;
            set;
        }
    }
}