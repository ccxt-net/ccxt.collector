using CCXT.Collector.Service;
using System.Collections.Generic;

namespace CCXT.Collector.Indicator
{
    /// <summary>
    /// Chande Momentum Oscillator (CMO)
    /// </summary>
    public class CMO : IndicatorCalculatorBase<SingleDoubleSerie>
    {
        protected override List<SOhlcvItem> OhlcList
        {
            get; set;
        }

        protected int Period = 14;

        public CMO()
        {
        }

        public CMO(int period)
        {
            this.Period = period;
        }

        /// <summary>
        /// Chande Momentum Oscillator (CMO)
        /// </summary>
        /// <see cref="http://www.fmlabs.com/reference/default.htm?url=CMO.htm"/>
        /// <returns></returns>
        public override SingleDoubleSerie Calculate()
        {
            var cmoSerie = new SingleDoubleSerie();
            cmoSerie.Values.Add(null);

            var upValues = new List<decimal>();
            upValues.Add(0);
            var downValues = new List<decimal>();
            downValues.Add(0);

            for (var i = 1; i < OhlcList.Count; i++)
            {
                if (OhlcList[i].closePrice > OhlcList[i - 1].closePrice)
                {
                    upValues.Add(OhlcList[i].closePrice - OhlcList[i - 1].closePrice);
                    downValues.Add(0);
                }
                else if (OhlcList[i].closePrice < OhlcList[i - 1].closePrice)
                {
                    upValues.Add(0);
                    downValues.Add(OhlcList[i - 1].closePrice - OhlcList[i].closePrice);
                }
                else
                {
                    upValues.Add(0);
                    downValues.Add(0);
                }

                if (i >= Period)
                {
                    decimal upTotal = 0.0m, downTotal = 0.0m;
                    for (var j = i; j >= i - (Period - 1); j--)
                    {
                        upTotal += upValues[j];
                        downTotal += downValues[j];
                    }

                    var cmo = 100 * (upTotal - downTotal) / (upTotal + downTotal);
                    cmoSerie.Values.Add(cmo);
                }
                else
                {
                    cmoSerie.Values.Add(null);
                }
            }

            return cmoSerie;
        }
    }
}