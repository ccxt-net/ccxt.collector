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
        public string type
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
        public string topic
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

        /// <summary>
        ///
        /// </summary>
        public object tag
        {
            get;
            set;
        }

        /// <summary>
        ///
        /// </summary>
        public string json
        {
            get;
            set;
        }

        /// <summary>
        ///
        /// </summary>
        public object value
        {
            get;
            set;
        }
    }
}