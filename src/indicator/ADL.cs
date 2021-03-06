﻿using System.Collections.Generic;

namespace CCXT.Collector.Indicator
{
    /// <summary>
    /// Accumulation / Distribution Line
    /// </summary>
    public class ADL : IndicatorCalculatorBase<SingleDoubleSerie>
    {
        protected override List<Ohlcv> OhlcList
        {
            get; set;
        }

        /// <summary>
        /// Acc/Dist = ((Close – Low) – (High – Close)) / (High – Low) * Period's volume
        /// </summary>
        /// <see cref="http://www.investopedia.com/terms/a/accumulationdistribution.asp"/>
        /// <returns></returns>
        public override SingleDoubleSerie Calculate()
        {
            var adlSerie = new SingleDoubleSerie();
            foreach (var ohlc in OhlcList)
            {
                var value = ((ohlc.closePrice - ohlc.lowPrice) - (ohlc.highPrice - ohlc.closePrice)) / (ohlc.highPrice - ohlc.lowPrice) * ohlc.volume;
                adlSerie.Values.Add(value);
            }

            return adlSerie;
        }
    }
}