namespace CCXT.Collector.Deribit.Private
{
    /// <summary>
    ///
    /// </summary>
    public class DAddress
    {
        /// <summary>
        ///
        /// </summary>
        public string type
        {
            get;
            set;
        }

        /// <summary>
        ///
        /// </summary>
        public string status
        {
            get;
            set;
        }

        /// <summary>
        ///
        /// </summary>
        public bool requires_confirmation
        {
            get;
            set;
        }

        /// <summary>
        ///
        /// </summary>
        public string currency
        {
            get;
            set;
        }

        /// <summary>
        ///
        /// </summary>
        public long creation_timestamp
        {
            get;
            set;
        }

        /// <summary>
        ///
        /// </summary>
        public string address
        {
            get;
            set;
        }
    }
}