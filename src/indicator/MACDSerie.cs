using System.Collections.Generic;

namespace CCXT.Collector.Indicator
{
    public class MACDSerie : IIndicatorSerie
    {
        public List<decimal?> MACDLine
        {
            get; set;
        }

        public List<decimal?> MACDHistogram
        {
            get; set;
        }

        public List<decimal?> Signal
        {
            get; set;
        }

        public MACDSerie()
        {
            this.MACDLine = new List<decimal?>();
            this.MACDHistogram = new List<decimal?>();
            this.Signal = new List<decimal?>();
        }
    }
}