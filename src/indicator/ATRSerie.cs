using System.Collections.Generic;

namespace CCXT.Collector.Indicator
{
    /// <summary>
    ///
    /// </summary>
    public class ATRSerie : IIndicatorSerie
    {
        /// <summary>
        ///
        /// </summary>
        public List<decimal?> TrueHigh
        {
            get; private set;
        }

        /// <summary>
        ///
        /// </summary>
        public List<decimal?> TrueLow
        {
            get; private set;
        }

        /// <summary>
        ///
        /// </summary>
        public List<decimal?> TrueRange
        {
            get; private set;
        }

        /// <summary>
        ///
        /// </summary>
        public List<decimal?> ATR
        {
            get; private set;
        }

        /// <summary>
        ///
        /// </summary>
        public ATRSerie()
        {
            TrueHigh = new List<decimal?>();
            TrueLow = new List<decimal?>();
            TrueRange = new List<decimal?>();
            ATR = new List<decimal?>();
        }
    }
}