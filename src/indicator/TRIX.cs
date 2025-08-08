using System.Collections.Generic;
using System.Linq;

namespace CCXT.Collector.Indicator
{
    /// <summary>
    /// Triple Smoothed Exponential Oscillator
    /// </summary>
    public class TRIX : IndicatorCalculatorBase<SingleDoubleSerie>
    {
        protected override List<Ohlcv> OhlcList
        {
            get;
            set;
        }

        protected int Period = 20;
        protected bool CalculatePercentage = true;

        public TRIX()
        {
        }

        public TRIX(int period, bool calculatePercentage)
        {
            this.Period = period;
            this.CalculatePercentage = calculatePercentage;
        }

        /// <summary>
        /// 1 - EMA of Close prices [EMA(Close)]
        /// 2 - Double smooth [EMA(EMA(Close))]
        /// 3 - Triple smooth [EMA(EMA(EMA(Close)))]
        /// 4 - a) Calculation with percentage: [ROC(EMA(EMA(EMA(Close))))]
        /// 4 - b) Calculation with percentage: [Momentum(EMA(EMA(EMA(Close))))]
        /// </summary>
        /// <see cref="http://stockcharts.com/school/doku.php?id=chart_school:technical_indicators:trix"/>
        /// <see cref="http://www.fmlabs.com/reference/default.htm?url=TRIX.htm"/>
        /// <returns></returns>
        public override SingleDoubleSerie Calculate()
        {
            // EMA calculation
            var _ema = new EMA(Period, false);
            _ema.Load(OhlcList);

            var emaValues = _ema.Calculate().Values;
            for (var i = 0; i < OhlcList.Count; i++)
            {
                OhlcList[i].closePrice = emaValues[i].HasValue ? emaValues[i].Value : 0.0m;
            }

            // Double smooth
            _ema.Load(OhlcList.Skip(Period - 1).ToList());
            var doubleSmoothValues = _ema.Calculate().Values;
            for (var i = 0; i < Period - 1; i++)
            {
                doubleSmoothValues.Insert(0, null);
            }

            for (var i = 0; i < OhlcList.Count; i++)
            {
                OhlcList[i].closePrice = doubleSmoothValues[i].HasValue ? doubleSmoothValues[i].Value : 0.0m;
            }

            // Triple smooth
            _ema.Load(OhlcList.Skip(2 * (Period - 1)).ToList());
            var tripleSmoothValues = _ema.Calculate().Values;
            for (var i = 0; i < (2 * (Period - 1)); i++)
            {
                tripleSmoothValues.Insert(0, null);
            }

            for (var i = 0; i < OhlcList.Count; i++)
            {
                OhlcList[i].closePrice = tripleSmoothValues[i].HasValue ? tripleSmoothValues[i].Value : 0.0m;
            }

            // Last step
            var trixSerie = new SingleDoubleSerie();

            if (CalculatePercentage)
            {
                ROC roc = new ROC(1);
                roc.Load(OhlcList.Skip(3 * (Period - 1)).ToList());
                trixSerie = roc.Calculate();
            }
            else
            {
                Momentum momentum = new Momentum();
                momentum.Load(OhlcList.Skip(3 * (Period - 1)).ToList());
                trixSerie = momentum.Calculate();
            }

            for (var i = 0; i < (3 * (Period - 1)); i++)
            {
                trixSerie.Values.Insert(0, null);
            }

            return trixSerie;
        }
    }
}