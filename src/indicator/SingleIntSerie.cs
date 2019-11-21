using System.Collections.Generic;

namespace CCXT.Collector.Indicator
{
    public class SingleIntSerie : IIndicatorSerie
    {
        public List<int?> Values
        {
            get; set;
        }

        public SingleIntSerie()
        {
            Values = new List<int?>();
        }
    }
}