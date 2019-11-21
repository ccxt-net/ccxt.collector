using System.Collections.Generic;

namespace CCXT.Collector.Indicator
{
    /// <summary>
    /// Simple Moving Average
    /// </summary>
    public class SMA : IndicatorCalculatorBase<SingleDoubleSerie>
    {
        protected override List<Ohlcv> OhlcList
        {
            get;
            set;
        }

        protected int Period
        {
            get;
            set;
        }

        public SMA(int period)
        {
            this.Period = period;
        }

        /// <summary>
        /// Daily Closing Prices: 11,12,13,14,15,16,17
        /// First day of 5-day SMA: (11 + 12 + 13 + 14 + 15) / 5 = 13
        /// Second day of 5-day SMA: (12 + 13 + 14 + 15 + 16) / 5 = 14
        /// Third day of 5-day SMA: (13 + 14 + 15 + 16 + 17) / 5 = 15
        /// </summary>
        /// <see cref="http://stockcharts.com/school/doku.php?id=chart_school:technical_indicators:moving_averages"/>
        /// <returns></returns>
        public override SingleDoubleSerie Calculate()
        {
            var _sma_serie = new SingleDoubleSerie();

            for (var i = 0; i < OhlcList.Count; i++)
            {
                if (i >= Period - 1)
                {
                    var sum = 0.0m;
                    for (var j = i; j >= i - (Period - 1); j--)
                    {
                        sum += OhlcList[j].closePrice;
                    }

                    var avg = sum / Period;
                    _sma_serie.Values.Add(avg);
                }
                else
                {
                    _sma_serie.Values.Add(null);
                }
            }

            return _sma_serie;
        }
    }
}