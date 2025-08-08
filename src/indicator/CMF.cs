using System.Collections.Generic;

namespace CCXT.Collector.Indicator
{
    /// <summary>
    /// Chaikin Money Flow
    /// </summary>
    public class CMF : IndicatorCalculatorBase<SingleDoubleSerie>
    {
        protected override List<Ohlcv> OhlcList
        {
            get; set;
        }

        protected int Period = 20;

        public CMF()
        {
        }

        public CMF(int period)
        {
            this.Period = period;
        }

        /// <summary>
        /// Chaikin Money Flow
        /// Money Flow Multiplier = [(Close  -  Low) - (High - Close)] /(High - Low)
        /// Money Flow Volume = Money Flow Multiplier x Volume for the Period
        /// 20-period CMF = 20-period Sum of Money Flow Volume / 20 period Sum of Volume
        /// </summary>
        /// <see cref="http://stockcharts.com/school/doku.php?id=chart_school:technical_indicators:chaikin_money_flow_cmf"/>
        /// <returns></returns>
        public override SingleDoubleSerie Calculate()
        {
            var cmfSerie = new SingleDoubleSerie();

            var moneyFlowVolumeList = new List<decimal>();

            for (var i = 0; i < OhlcList.Count; i++)
            {
                var moneyFlowMultiplier = ((OhlcList[i].closePrice - OhlcList[i].lowPrice) - (OhlcList[i].highPrice - OhlcList[i].closePrice)) / (OhlcList[i].highPrice - OhlcList[i].lowPrice);

                moneyFlowVolumeList.Add(moneyFlowMultiplier * OhlcList[i].volume);

                if (i >= Period - 1)
                {
                    decimal sumOfMoneyFlowVolume = 0.0m, sumOfVolume = 0.0m;
                    for (var j = i; j >= i - (Period - 1); j--)
                    {
                        sumOfMoneyFlowVolume += moneyFlowVolumeList[j];
                        sumOfVolume += OhlcList[j].volume;
                    }
                    cmfSerie.Values.Add(sumOfMoneyFlowVolume / sumOfVolume);
                }
                else
                {
                    cmfSerie.Values.Add(null);
                }
            }

            return cmfSerie;
        }
    }
}