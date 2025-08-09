using CCXT.Collector.Service;
using System.Collections.Generic;

namespace CCXT.Collector.Indicator
{
    /// <summary>
    /// Price Volume Trend (PVT)
    /// </summary>
    public class PVT : IndicatorCalculatorBase<SingleDoubleSerie>
    {
        protected override List<SOhlcvItem> OhlcList
        {
            get; set;
        }

        /// <summary>
        /// PVT = [((CurrentClose - PreviousClose) / PreviousClose) x Volume] + PreviousPVT
        /// </summary>
        /// <see cref="https://www.tradingview.com/stock-charts-support/index.php/Price_Volume_Trend_(PVT)"/>
        /// <returns></returns>
        public override SingleDoubleSerie Calculate()
        {
            var pvtSerie = new SingleDoubleSerie();
            pvtSerie.Values.Add(null);

            for (var i = 1; i < OhlcList.Count; i++)
            {
                pvtSerie.Values.Add((((OhlcList[i].closePrice - OhlcList[i - 1].closePrice) / OhlcList[i - 1].closePrice) * OhlcList[i].baseVolume) + pvtSerie.Values[i - 1]);
            }

            return pvtSerie;
        }
    }
}