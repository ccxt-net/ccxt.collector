using System.Collections.Generic;

namespace CCXT.Collector.Deribit
{
    /// <summary>
    ///
    /// </summary>
    public class DRResult<T>
    {
        public string jsonrpc
        {
            get; set;
        }

        public long usIn
        {
            get; set;
        }

        public long usOut
        {
            get; set;
        }

        public int usDiff
        {
            get; set;
        }

        public bool testnet
        {
            get; set;
        }

        /// <summary>
        ///
        /// </summary>
        public List<T> result
        {
            get;
            set;
        }
    }
}