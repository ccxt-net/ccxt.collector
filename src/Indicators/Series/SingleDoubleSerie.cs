using System.Collections.Generic;

namespace CCXT.Collector.Indicator
{
    public class SingleDoubleSerie : IIndicatorSerie
    {
        public List<decimal?> Values
        {
            get; set;
        }

        public SingleDoubleSerie()
        {
            Values = new List<decimal?>();
        }
    }
}