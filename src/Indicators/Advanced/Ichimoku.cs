using CCXT.Collector.Service;
using System.Collections.Generic;
using System.Linq;

namespace CCXT.Collector.Indicator
{
    public class Ichimoku : IndicatorCalculatorBase<IchimokuSerie>
    {
        protected override List<SOhlcvItem> OhlcList
        {
            get; set;
        }

        protected int Fast = 9;
        protected int Med = 26;
        protected int Slow = 26;

        public Ichimoku()
        {
        }

        public Ichimoku(int fast, int med, int slow)
        {
            this.Fast = fast;
            this.Med = med;
            this.Slow = slow;
        }

        public override IchimokuSerie Calculate()
        {
            IchimokuSerie ichimokuSerie = new IchimokuSerie();

            var highList = OhlcList.Select(x => x.highPrice).ToList();
            var lowList = OhlcList.Select(x => x.lowPrice).ToList();

            // TurningLine
            var runMaxFast = Statistics.RunMax(highList, Fast);
            var runMinFast = Statistics.RunMin(lowList, Fast);
            var runMaxMed = Statistics.RunMax(highList, Med);
            var runMinMed = Statistics.RunMin(lowList, Med);
            var runMaxSlow = Statistics.RunMax(highList, Slow);
            var runMinSlow = Statistics.RunMin(lowList, Slow);

            for (var i = 0; i < OhlcList.Count; i++)
            {
                if (i >= Fast - 1)
                {
                    ichimokuSerie.ConversionLine.Add((runMaxFast[i] + runMinFast[i]) / 2);
                }
                else
                {
                    ichimokuSerie.ConversionLine.Add(null);
                }

                if (i >= Med - 1)
                {
                    ichimokuSerie.BaseLine.Add((runMaxMed[i] + runMinMed[i]) / 2);
                    ichimokuSerie.LeadingSpanA.Add((ichimokuSerie.BaseLine[i] + ichimokuSerie.ConversionLine[i]) / 2);
                }
                else
                {
                    ichimokuSerie.BaseLine.Add(null);
                    ichimokuSerie.LeadingSpanA.Add(null);
                }

                if (i >= Slow - 1)
                {
                    ichimokuSerie.LeadingSpanB.Add((runMaxSlow[i] + runMinSlow[i]) / 2);
                }
                else
                {
                    ichimokuSerie.LeadingSpanB.Add(null);
                }
            }

            // shift to left Med
            var laggingSpan = new List<decimal?>();//OhlcList.Select(x => x.Close).ToList();//new double?[OhlcList.Count];
            for (var i = 0; i < OhlcList.Count; i++)
            {
                laggingSpan.Add(null);
            }
            for (var i = 0; i < OhlcList.Count; i++)
            {
                if (i >= Med - 1)
                {
                    laggingSpan[i - (Med - 1)] = OhlcList[i].closePrice;
                }
            }
            ichimokuSerie.LaggingSpan = laggingSpan;

            return ichimokuSerie;
        }
    }
}