using CCXT.Collector.Service;
using System.Collections.Generic;

namespace CCXT.Collector.Indicator
{
    /// <summary>
    /// Weighted Moving Average
    /// </summary>
    public class WMA : IndicatorCalculatorBase<SingleDoubleSerie>
    {
        protected override List<SOhlcvItem> OhlcList
        {
            get;
            set;
        }

        protected int Period
        {
            get;
            set;
        }

        public WMA(int period)
        {
            this.Period = period;
        }

        /// <summary>
        /// Therefore the 5 Day WMA is 83(5/15) + 81(4/15) + 79(3/15) + 79(2/15) + 77(1/15) = 80.7
        /// Day	     1	2	3	4	5 (current)
        /// Price	77	79	79	81	83
        /// WMA	 	 	 	 	    80.7
        /// </summary>
        /// <see cref="http://fxtrade.oanda.com/learn/forex-indicators/weighted-moving-average"/>
        /// <returns></returns>
        public override SingleDoubleSerie Calculate()
        {
            // karşılaştırma için tutarlar ezilebilir. Bağlantı: http://fxtrade.oanda.com/learn/forex-indicators/weighted-moving-average
            //OhlcList[0].Close = 77;
            //OhlcList[1].Close = 79;
            //OhlcList[2].Close = 79;
            //OhlcList[3].Close = 81;
            //OhlcList[4].Close = 83;

            var _wma_serie = new SingleDoubleSerie();

            var weightSum = 0;
            for (var i = 1; i <= Period; i++)
            {
                weightSum += i;
            }

            for (var i = 0; i < OhlcList.Count; i++)
            {
                if (i >= Period - 1)
                {
                    var wma = 0.0m;

                    var weight = 1;
                    for (var j = i - (Period - 1); j <= i; j++)
                    {
                        wma += ((decimal)weight / weightSum) * OhlcList[j].closePrice;
                        weight++;
                    }
                    _wma_serie.Values.Add(wma);
                }
                else
                {
                    _wma_serie.Values.Add(null);
                }
            }

            return _wma_serie;
        }
    }
}