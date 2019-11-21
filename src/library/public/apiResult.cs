namespace CCXT.Collector.Library.Public
{
    public class SApiResult<T>
    {
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
        public string action
        {
            get;
            set;
        }

        /// <summary>
        ///
        /// </summary>
        public long sequentialId
        {
            get;
            set;
        }

        /// <summary>
        ///
        /// </summary>
        public virtual T result
        {
            get;
            set;
        }
    }
}