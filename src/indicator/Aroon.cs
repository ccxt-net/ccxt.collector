﻿using System;
using System.Collections.Generic;

namespace CCXT.Collector.Indicator
{
    /// <summary>
    /// Aroon
    /// </summary>
    public class Aroon : IndicatorCalculatorBase<AroonSerie>
    {
        protected override List<Ohlcv> OhlcList
        {
            get; set;
        }

        protected int Period
        {
            get; set;
        }

        public Aroon(int period)
        {
            Period = period;
        }

        /// <summary>
        /// Aroon up: {((number of periods) - (number of periods since highest high)) / (number of periods)} x 100
        /// Aroon down: {((number of periods) - (number of periods since lowest low)) / (number of periods)} x 100
        /// </summary>
        /// <see cref="http://www.investopedia.com/ask/answers/112814/what-aroon-indicator-formula-and-how-indicator-calculated.asp"/>
        /// <returns></returns>
        public override AroonSerie Calculate()
        {
            var aroonSerie = new AroonSerie();
            for (var i = 0; i < OhlcList.Count; i++)
            {
                if (i >= Period)
                {
                    var aroonUp = CalculateAroonUp(i);
                    var aroonDown = CalculateAroonDown(i);

                    aroonSerie.Down.Add(aroonDown);
                    aroonSerie.Up.Add(aroonUp);
                }
                else
                {
                    aroonSerie.Down.Add(null);
                    aroonSerie.Up.Add(null);
                }
            }

            return aroonSerie;
        }

        private decimal CalculateAroonUp(int i)
        {
            var maxIndex = FindMax(i - Period, i);

            var up = CalcAroon(i - maxIndex);

            return up;
        }

        private decimal CalculateAroonDown(int i)
        {
            var minIndex = FindMin(i - Period, i);

            var down = CalcAroon(i - minIndex);

            return down;
        }

        private decimal CalcAroon(int numOfDays)
        {
            var result = ((Period - numOfDays)) * ((decimal)100.0m / Period);
            return result;
        }

        private int FindMin(int startIndex, int endIndex)
        {
            var min = Decimal.MaxValue;
            var index = startIndex;
            for (var i = startIndex; i <= endIndex; i++)
            {
                if (min < OhlcList[i].lowPrice)
                    continue;

                min = OhlcList[i].lowPrice;
                index = i;
            }
            return index;
        }

        private int FindMax(int startIndex, int endIndex)
        {
            var max = Decimal.MinValue;

            var index = startIndex;
            for (var i = startIndex; i <= endIndex; i++)
            {
                if (max > OhlcList[i].highPrice)
                    continue;

                max = OhlcList[i].highPrice;
                index = i;
            }
            return index;
        }
    }
}