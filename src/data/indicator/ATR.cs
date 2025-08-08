using System.Collections.Generic;
using System.Linq;

namespace CCXT.Collector.Indicator
{
    /// <summary>
    /// True Range / Average True Range
    /// </summary>
    public class ATR : IndicatorCalculatorBase<ATRSerie>
    {
        protected override List<Ohlcv> OhlcList
        {
            get; set;
        }

        protected int Period = 14;

        public ATR()
        {
        }

        public ATR(int period)
        {
            this.Period = period;
        }

        /// <summary>
        /// TrueHigh = Highest of high[0] or close[-1]
        /// TrueLow = Highest of low[0] or close[-1]
        /// TR = TrueHigh - TrueLow
        /// ATR = EMA(TR)
        /// </summary>
        /// <see cref="http://www.fmlabs.com/reference/default.htm?url=TR.htm"/>
        /// <see cref="http://www.fmlabs.com/reference/default.htm?url=ATR.htm"/>
        /// <returns></returns>
        public override ATRSerie Calculate()
        {
            var _atr_serie = new ATRSerie();
            _atr_serie.TrueHigh.Add(null);
            _atr_serie.TrueLow.Add(null);
            _atr_serie.TrueRange.Add(null);
            _atr_serie.ATR.Add(null);

            for (var i = 1; i < OhlcList.Count; i++)
            {
                var trueHigh = OhlcList[i].highPrice >= OhlcList[i - 1].closePrice ? OhlcList[i].highPrice : OhlcList[i - 1].closePrice;
                _atr_serie.TrueHigh.Add(trueHigh);

                var trueLow = OhlcList[i].lowPrice <= OhlcList[i - 1].closePrice ? OhlcList[i].lowPrice : OhlcList[i - 1].closePrice;
                _atr_serie.TrueLow.Add(trueLow);

                var trueRange = trueHigh - trueLow;
                _atr_serie.TrueRange.Add(trueRange);
            }

            for (var i = 1; i < OhlcList.Count; i++)
            {
                OhlcList[i].closePrice = _atr_serie.TrueRange[i].Value;
            }

            var _ema = new EMA(Period, true);
            _ema.Load(OhlcList.Skip(1).ToList());

            var atrList = _ema.Calculate().Values;
            foreach (var atr in atrList)
            {
                _atr_serie.ATR.Add(atr);
            }

            return _atr_serie;
        }
    }
}