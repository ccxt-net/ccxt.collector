using System.Collections.Generic;

namespace CCXT.Collector.Indicator
{
    public class Momentum : IndicatorCalculatorBase<SingleDoubleSerie>
    {
        protected override List<Ohlcv> OhlcList
        {
            get; set;
        }

        public override SingleDoubleSerie Calculate()
        {
            var momentumSerie = new SingleDoubleSerie();
            momentumSerie.Values.Add(null);

            for (var i = 1; i < OhlcList.Count; i++)
            {
                momentumSerie.Values.Add(OhlcList[i].closePrice - OhlcList[i - 1].closePrice);
            }

            return momentumSerie;
        }

        public SingleDoubleSerie Calculate(List<decimal> values)
        {
            var momentumSerie = new SingleDoubleSerie();
            momentumSerie.Values.Add(null);

            for (var i = 1; i < values.Count; i++)
            {
                momentumSerie.Values.Add(values[i] - values[i - 1]);
            }

            return momentumSerie;
        }
    }
}