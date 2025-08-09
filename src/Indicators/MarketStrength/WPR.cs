using CCXT.Collector.Service;
using System;
using System.Collections.Generic;

namespace CCXT.Collector.Indicator
{
    /// <summary>
    /// William %R
    /// </summary>
    public class WPR : IndicatorCalculatorBase<SingleDoubleSerie>
    {
        protected override List<SOhlcvItem> OhlcList
        {
            get;
            set;
        }

        protected int Period = 14;

        public WPR()
        {
        }

        public WPR(int period)
        {
            this.Period = period;
        }

        /// <summary>
        /// %R = (Highest High - Close)/(Highest High - Lowest Low) * 100
        /// Lowest Low = lowest low for the look-back period
        /// Highest High = highest high for the look-back period
        /// %R is multiplied by -100 correct the inversion and move the decimal.
        /// </summary>
        /// <see cref="http://www.fmlabs.com/reference/default.htm?url=WilliamsR.htm"/>
        /// <returns></returns>
        public override SingleDoubleSerie Calculate()
        {
            var _wpr_serie = new SingleDoubleSerie();

            for (var i = 0; i < OhlcList.Count; i++)
            {
                if (i >= Period - 1)
                {
                    var highestHigh = HighestHigh(i);
                    var lowestLow = LowestLow(i);

                    var wpr = (highestHigh - OhlcList[i].closePrice) / (highestHigh - lowestLow) * (100);
                    _wpr_serie.Values.Add(wpr);
                }
                else
                {
                    _wpr_serie.Values.Add(null);
                }
            }

            return _wpr_serie;
        }

        private decimal HighestHigh(int index)
        {
            var startIndex = index - (Period - 1);
            var endIndex = index;

            var highestHigh = 0.0m;
            for (var i = startIndex; i <= endIndex; i++)
            {
                if (OhlcList[i].highPrice > highestHigh)
                {
                    highestHigh = OhlcList[i].highPrice;
                }
            }

            return highestHigh;
        }

        private decimal LowestLow(int index)
        {
            var startIndex = index - (Period - 1);
            var endIndex = index;

            var lowestLow = Decimal.MaxValue;
            for (var i = startIndex; i <= endIndex; i++)
            {
                if (OhlcList[i].lowPrice < lowestLow)
                {
                    lowestLow = OhlcList[i].lowPrice;
                }
            }

            return lowestLow;
        }
    }
}