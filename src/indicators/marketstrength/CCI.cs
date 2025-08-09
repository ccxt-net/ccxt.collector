using CCXT.Collector.Service;
using System;
using System.Collections.Generic;

namespace CCXT.Collector.Indicator
{
    /// <summary>
    /// Commodity Channel Index (CCI)
    /// </summary>
    public class CCI : IndicatorCalculatorBase<SingleDoubleSerie>
    {
        protected override List<SOhlcvItem> OhlcList
        {
            get; set;
        }

        protected int Period = 20;
        protected decimal Factor = 0.015m;

        public CCI()
        {
        }

        public CCI(int period, int factor)
        {
            this.Period = period;
            this.Factor = factor;
        }

        /// <summary>
        /// Commodity Channel Index (CCI)
        /// tp = (high + low + close) / 3
        /// cci = (tp - SMA(tp)) / (Factor * meanDeviation(tp))
        /// </summary>
        /// <see cref="http://www.fmlabs.com/reference/default.htm?url=CCI.htm"/>
        /// <see cref="http://stockcharts.com/school/doku.php?id=chart_school:technical_indicators:commodity_channel_index_cci"/>
        /// <returns></returns>
        public override SingleDoubleSerie Calculate()
        {
            var cciSerie = new SingleDoubleSerie();

            for (var i = 0; i < OhlcList.Count; i++)
            {
                OhlcList[i].closePrice = (OhlcList[i].highPrice + OhlcList[i].lowPrice + OhlcList[i].closePrice) / 3;
            }

            SMA sma = new SMA(Period);
            sma.Load(OhlcList);
            var smaList = sma.Calculate().Values;

            var meanDeviationList = new List<decimal?>();
            for (var i = 0; i < OhlcList.Count; i++)
            {
                if (i >= Period - 1)
                {
                    var total = 0.0m;
                    for (var j = i; j >= i - (Period - 1); j--)
                    {
                        total += Math.Abs(smaList[i].Value - OhlcList[j].closePrice);
                    }
                    meanDeviationList.Add(total / (decimal)Period);

                    var cci = (OhlcList[i].closePrice - smaList[i].Value) / (Factor * meanDeviationList[i].Value);
                    cciSerie.Values.Add(cci);
                }
                else
                {
                    meanDeviationList.Add(null);
                    cciSerie.Values.Add(null);
                }
            }

            return cciSerie;
        }
    }
}