using CCXT.Collector.Service;

namespace CCXT.Collector.Indicator
{
    public class Ohlcv : SOhlcvItem
    {
        /// <summary>
        /// Volume in base currency
        /// </summary>
        public decimal volume { get; set; }

        /// <summary>
        ///
        /// </summary>
        public double adjClose
        {
            get; set;
        }
    }
}