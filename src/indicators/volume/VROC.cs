using CCXT.Collector.Service;
using System.Collections.Generic;

namespace CCXT.Collector.Indicator
{
    /// <summary>
    /// Volume Rate of Change (VROC)
    /// </summary>
    public class VROC : IndicatorCalculatorBase<SingleDoubleSerie>
    {
        protected override List<SOhlcvItem> OhlcList
        {
            get; set;
        }

        protected int Period
        {
            get; set;
        }

        public VROC(int period)
        {
            this.Period = period;
        }

        /// <summary>
        /// VROC = ((VOLUME (i) - VOLUME (i - n)) / VOLUME (i - n)) * 100
        /// </summary>
        /// <see href="https://ta.mql4.com/indicators/volumes/rate_of_change"/>
        /// <returns></returns>
        public override SingleDoubleSerie Calculate()
        {
            var rocSerie = new SingleDoubleSerie();

            for (var i = 0; i < OhlcList.Count; i++)
            {
                if (i >= this.Period)
                {
                    rocSerie.Values.Add(((OhlcList[i].baseVolume - OhlcList[i - this.Period].baseVolume) / OhlcList[i - this.Period].baseVolume) * 100);
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