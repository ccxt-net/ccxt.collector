using System.Collections.Generic;

namespace CCXT.Collector.Indicator
{
    /// <summary>
    /// On Balance Volume (OBV)
    /// </summary>
    public class OBV : IndicatorCalculatorBase<SingleDoubleSerie>
    {
        protected override List<Ohlcv> OhlcList
        {
            get; set;
        }

        /// <summary>
        /// If today’s close is greater than yesterday’s close then:
        /// OBV(i) = OBV(i-1)+VOLUME(i)
        /// If today’s close is less than yesterday’s close then:
        /// OBV(i) = OBV(i-1)-VOLUME(i)
        /// If today’s close is equal to yesterday’s close then:
        /// OBV(i) = OBV(i-1)
        /// </summary>
        /// <see cref="http://ta.mql4.com/indicators/volumes/on_balance_volume"/>
        /// <returns></returns>
        public override SingleDoubleSerie Calculate()
        {
            var obvSerie = new SingleDoubleSerie();
            obvSerie.Values.Add(OhlcList[0].volume);

            for (var i = 1; i < OhlcList.Count; i++)
            {
                var value = 0.0m;
                if (OhlcList[i].closePrice > OhlcList[i - 1].closePrice)
                {
                    value = obvSerie.Values[i - 1].Value + OhlcList[i].volume;
                }
                else if (OhlcList[i].closePrice < OhlcList[i - 1].closePrice)
                {
                    value = obvSerie.Values[i - 1].Value - OhlcList[i].volume;
                }
                else if (OhlcList[i].closePrice == OhlcList[i - 1].closePrice)
                {
                    value = obvSerie.Values[i - 1].Value;
                }

                obvSerie.Values.Add(value);
            }

            return obvSerie;
        }
    }
}