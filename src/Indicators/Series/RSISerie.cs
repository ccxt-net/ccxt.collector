using System.Collections.Generic;

namespace CCXT.Collector.Indicator
{
    /// <summary>
    ///
    /// </summary>
    public class RSISerie : IIndicatorSerie
    {
        /// <summary>
        ///
        /// </summary>
        public List<decimal?> RSI
        {
            get; set;
        }

        /// <summary>
        ///
        /// </summary>
        public List<decimal?> RS
        {
            get; set;
        }

        /// <summary>
        ///
        /// </summary>
        public RSISerie()
        {
            RSI = new List<decimal?>();
            RS = new List<decimal?>();
        }
    }
}