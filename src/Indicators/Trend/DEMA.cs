using CCXT.Collector.Service;
using System.Collections.Generic;
using System.Linq;

namespace CCXT.Collector.Indicator
{
    /// <summary>
    /// Double Exponential Moving Average (DEMA)
    /// </summary>
    public class DEMA : IndicatorCalculatorBase<SingleDoubleSerie>
    {
        protected override List<SOhlcvItem> OhlcList
        {
            get; set;
        }

        protected int Period
        {
            get; set;
        }

        public DEMA(int period)
        {
            this.Period = period;
        }

        /// <summary>
        /// DEMA = 2 * EMA - EMA of EMA
        /// </summary>
        /// <see cref="http://forex-indicators.net/trend-indicators/dema"/>
        /// <returns></returns>
        public override SingleDoubleSerie Calculate()
        {
            var demaSerie = new SingleDoubleSerie();
            EMA ema = new EMA(Period, false);
            ema.Load(OhlcList);
            var emaValues = ema.Calculate().Values;

            // assign EMA values to Close price
            for (var i = 0; i < OhlcList.Count; i++)
            {
                OhlcList[i].closePrice = emaValues[i].HasValue ? emaValues[i].Value : 0.0m;
            }

            ema.Load(OhlcList.Skip(Period - 1).ToList());
            // EMA(EMA(value))
            var emaEmaValues = ema.Calculate().Values;
            for (var i = 0; i < Period - 1; i++)
            {
                emaEmaValues.Insert(0, null);
            }

            // Calculate DEMA
            for (var i = 0; i < OhlcList.Count; i++)
            {
                if (i >= 2 * Period - 2)
                {
                    var dema = 2 * emaValues[i] - emaEmaValues[i];
                    demaSerie.Values.Add(dema);
                }
                else
                {
                    demaSerie.Values.Add(null);
                }
            }

            return demaSerie;
        }
    }
}