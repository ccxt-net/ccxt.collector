using System.Collections.Generic;

namespace CCXT.Collector.Indicator
{
    public class EnvelopeSerie : IIndicatorSerie
    {
        public List<decimal?> Upper
        {
            get; set;
        }

        public List<decimal?> Lower
        {
            get; set;
        }

        public EnvelopeSerie()
        {
            Upper = new List<decimal?>();
            Lower = new List<decimal?>();
        }
    }
}