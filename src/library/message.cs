namespace CCXT.Collector.Library
{
    public class QMessage
    {
        /// <summary>
        ///
        /// </summary>
        public string command
        {
            get;
            set;
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
        ///
        /// </summary>
        public string symbol
        {
            get;
            set;
        }

        /// <summary>
        /// sequential id
        /// </summary>
        public long sequentialId
        {
            get;
            set;
        }

        /// <summary>
        ///
        /// </summary>
        public int type
        {
            get;
            set;
        }

        /// <summary>
        ///
        /// </summary>
        public string stream
        {
            get;
            set;
        }

        /// <summary>
        ///
        /// </summary>
        public string id
        {
            get;
            set;
        }

        /// <summary>
        ///
        /// </summary>
        public string topic
        {
            get;
            set;
        }

        /// <summary>
        ///
        /// </summary>
        public string payload
        {
            get;
            set;
        }

        /// <summary>
        ///
        /// </summary>
        public string table
        {
            get;
            set;
        }
    }
}