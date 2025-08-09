using CCXT.Collector.Service;
using System.Collections.Generic;

namespace CCXT.Collector.Indicator
{
    /// <summary>
    /// Moving Average Envelopes
    /// </summary>
    public class Envelope : IndicatorCalculatorBase<EnvelopeSerie>
    {
        protected override List<SOhlcvItem> OhlcList
        {
            get; set;
        }

        protected int Period = 20;
        protected decimal Factor = 0.025m;

        public Envelope()
        {
        }

        public Envelope(int period, decimal factor)
        {
            this.Period = period;
            this.Factor = factor;
        }

        /// <summary>
        /// Upper Envelope: 20-day SMA + (20-day SMA x .025)
        /// Lower Envelope: 20-day SMA - (20-day SMA x .025)
        /// </summary>
        /// <see cref="http://stockcharts.com/school/doku.php?id=chart_school:technical_indicators:moving_average_envelopes"/>
        /// <returns></returns>
        public override EnvelopeSerie Calculate()
        {
            var envelopeSerie = new EnvelopeSerie();

            SMA sma = new SMA(Period);
            sma.Load(OhlcList);
            var smaList = sma.Calculate().Values;

            for (var i = 0; i < OhlcList.Count; i++)
            {
                if (smaList[i].HasValue)
                {
                    envelopeSerie.Lower.Add(smaList[i].Value - (smaList[i].Value * Factor));
                    envelopeSerie.Upper.Add(smaList[i].Value + (smaList[i].Value * Factor));
                }
                else
                {
                    envelopeSerie.Lower.Add(null);
                    envelopeSerie.Upper.Add(null);
                }
            }

            return envelopeSerie;
        }
    }
}