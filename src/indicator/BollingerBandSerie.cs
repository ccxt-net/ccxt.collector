using System.Collections.Generic;

namespace CCXT.Collector.Indicator
{
    /// <summary>
    ///
    /// </summary>
    public class BollingerBandSerie : IIndicatorSerie
    {
        /// <summary>
        ///
        /// </summary>
        public List<decimal?> LowerBand
        {
            get; set;
        }

        /// <summary>
        ///
        /// </summary>
        public List<decimal?> MidBand
        {
            get; set;
        }

        /// <summary>
        ///
        /// </summary>
        public List<decimal?> UpperBand
        {
            get; set;
        }

        /// <summary>
        ///
        /// </summary>
        public List<decimal?> BandWidth
        {
            get; set;
        }

        /// <summary>
        ///
        /// </summary>
        public List<decimal?> BPercent
        {
            get; set;
        }

        /// <summary>
        ///
        /// </summary>
        public BollingerBandSerie()
        {
            LowerBand = new List<decimal?>();
            MidBand = new List<decimal?>();
            UpperBand = new List<decimal?>();
            BandWidth = new List<decimal?>();
            BPercent = new List<decimal?>();
        }
    }
}