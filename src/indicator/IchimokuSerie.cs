using System.Collections.Generic;

namespace CCXT.Collector.Indicator
{
    public class IchimokuSerie : IIndicatorSerie
    {
        public List<decimal?> ConversionLine
        {
            get; set;
        }

        public List<decimal?> BaseLine
        {
            get; set;
        }

        public List<decimal?> LeadingSpanA
        {
            get; set;
        }

        public List<decimal?> LeadingSpanB
        {
            get; set;
        }

        public List<decimal?> LaggingSpan
        {
            get; set;
        }

        public IchimokuSerie()
        {
            ConversionLine = new List<decimal?>();
            BaseLine = new List<decimal?>();
            LeadingSpanA = new List<decimal?>();
            LeadingSpanB = new List<decimal?>();
            LaggingSpan = new List<decimal?>();
        }
    }
}