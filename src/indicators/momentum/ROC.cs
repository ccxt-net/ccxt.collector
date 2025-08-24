using CCXT.Collector.Service;
using System.Collections.Generic;

namespace CCXT.Collector.Indicator
{
    /// <summary>
    /// Rate of Change (ROC)
    /// </summary>
    public class ROC : IndicatorCalculatorBase<SingleDoubleSerie>
    {
        protected override List<SOhlcvItem> OhlcList
        {
            get; set;
        }

        protected int Period
        {
            get; set;
        }

        public ROC(int period)
        {
            this.Period = period;
        }

        /// <summary>
        /// ROC = [(Close - Close n periods ago) / (Close n periods ago)] * 100
        /// </summary>
        /// <see href="https://stockcharts.com/school/doku.php?id=chart_school:technical_indicators:rate_of_change_roc_and_momentum"/>
        /// <returns></returns>
        public override SingleDoubleSerie Calculate()
        {
            var rocSerie = new SingleDoubleSerie();

            for (var i = 0; i < OhlcList.Count; i++)
            {
                if (i >= this.Period)
                {
                    rocSerie.Values.Add(((OhlcList[i].closePrice - OhlcList[i - this.Period].closePrice) / OhlcList[i - this.Period].closePrice) * 100);
                }
                else
                {
                    rocSerie.Values.Add(null);
                }
            }

            return rocSerie;
        }
    }
}