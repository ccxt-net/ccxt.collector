using System.Collections.Generic;
using System.Linq;

namespace CCXT.Collector.Indicator
{
    public class ZLEMA : IndicatorCalculatorBase<SingleDoubleSerie>
    {
        protected override List<Ohlcv> OhlcList
        {
            get;
            set;
        }

        protected int Period = 10;

        public ZLEMA()
        {
        }

        public ZLEMA(int period)
        {
            this.Period = period;
        }

        /// <summary>
        ///
        /// </summary>
        /// <returns></returns>
        public override SingleDoubleSerie Calculate()
        {
            var _zlema_serie = new SingleDoubleSerie();

            var ratio = 2.0m / (decimal)(Period + 1);
            var lag = 1 / ratio;
            var wt = lag - ((int)lag / 1.0m) * 1.0m; //DMOD( lag, 1.0D0 )
            var meanOfFirstPeriod = OhlcList.Take(Period).Select(x => x.closePrice).Sum() / Period;

            for (var i = 0; i < OhlcList.Count; i++)
            {
                if (i > Period - 1)
                {
                    var loc = (int)(i - lag);

                    var zlema = ratio * (2 * OhlcList[i].closePrice - (OhlcList[loc].closePrice * (1 - wt) + OhlcList[loc + 1].closePrice * wt)) + (1 - ratio) * _zlema_serie.Values[i - 1].Value;
                    _zlema_serie.Values.Add(zlema);
                }
                else if (i == Period - 1)
                {
                    _zlema_serie.Values.Add(meanOfFirstPeriod);
                }
                else
                {
                    _zlema_serie.Values.Add(null);
                }
            }

            return _zlema_serie;
        }
    }
}