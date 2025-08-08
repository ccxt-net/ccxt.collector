using System.Collections.Generic;

namespace CCXT.Collector.Indicator
{
    public class Volume : IndicatorCalculatorBase<SingleDoubleSerie>
    {
        protected override List<Ohlcv> OhlcList
        {
            get; set;
        }

        public override SingleDoubleSerie Calculate()
        {
            var volumeSerie = new SingleDoubleSerie();

            foreach (var item in OhlcList)
            {
                volumeSerie.Values.Add(item.volume);
            }

            return volumeSerie;
        }
    }
}