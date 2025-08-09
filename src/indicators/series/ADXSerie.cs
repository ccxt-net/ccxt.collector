using System.Collections.Generic;

namespace CCXT.Collector.Indicator
{
    public class ADXSerie : IIndicatorSerie
    {
        public List<decimal?> TrueRange
        {
            get; set;
        }

        public List<decimal?> DINegative
        {
            get; set;
        }

        public List<decimal?> DIPositive
        {
            get; set;
        }

        public List<decimal?> DX
        {
            get; set;
        }

        public List<decimal?> ADX
        {
            get; set;
        }

        public ADXSerie()
        {
            TrueRange = new List<decimal?>();
            DINegative = new List<decimal?>();
            DIPositive = new List<decimal?>();
            DX = new List<decimal?>();
            ADX = new List<decimal?>();
        }
    }
}