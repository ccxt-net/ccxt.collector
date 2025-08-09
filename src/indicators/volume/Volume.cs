using System.Collections.Generic;
using CCXT.Collector.Service;

namespace CCXT.Collector.Indicator
{
    public class Volume : IndicatorCalculatorBase<SingleDoubleSerie>
    {
        protected override List<SOhlcvItem> OhlcList
        {
            get; set;
        }

        public override SingleDoubleSerie Calculate()
        {
            var volumeSerie = new SingleDoubleSerie();

            foreach (var item in OhlcList)
            {
                volumeSerie.Values.Add(item.baseVolume);
            }

            return volumeSerie;
        }
    }
}