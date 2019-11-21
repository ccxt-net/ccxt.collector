using System.Collections.Generic;

namespace CCXT.Collector.Indicator
{
    public class AroonSerie : IIndicatorSerie
    {
        public List<decimal?> Up
        {
            get; private set;
        }

        public List<decimal?> Down
        {
            get; private set;
        }

        public AroonSerie()
        {
            Up = new List<decimal?>();
            Down = new List<decimal?>();
        }
    }
}