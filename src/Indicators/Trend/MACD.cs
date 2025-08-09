using CCXT.Collector.Service;
using System.Collections.Generic;
using System.Linq;

namespace CCXT.Collector.Indicator
{
    public class MACD : IndicatorCalculatorBase<MACDSerie>
    {
        protected override List<SOhlcvItem> OhlcList
        {
            get; set;
        }

        protected int Fast = 12;
        protected int Slow = 26;
        protected int Signal = 9;
        protected bool Percent = false;

        public MACD()
        {
            OhlcList = new List<SOhlcvItem>();
        }

        public MACD(bool percent) : this()
        {
            this.Percent = percent;
        }

        public MACD(int fast, int slow, int signal) : this()
        {
            this.Fast = fast;
            this.Slow = slow;
            this.Signal = signal;
        }

        public MACD(int fast, int slow, int signal, bool percent) : this()
        {
            this.Fast = fast;
            this.Slow = slow;
            this.Signal = signal;
            this.Percent = percent;
        }

        public override MACDSerie Calculate()
        {
            var macdSerie = new MACDSerie();

            var _ema = new EMA(Fast, false);
            _ema.Load(OhlcList);
            
            var fastEmaValues = _ema.Calculate().Values;

            _ema = new EMA(Slow, false);
            _ema.Load(OhlcList);

            var slowEmaValues = _ema.Calculate().Values;

            for (var i = 0; i < OhlcList.Count; i++)
            {
                // MACD Line
                if (fastEmaValues[i].HasValue && slowEmaValues[i].HasValue)
                {
                    if (!Percent)
                    {
                        macdSerie.MACDLine.Add(fastEmaValues[i] - slowEmaValues[i]);
                    }
                    else
                    {
                        // macd <- 100 * ( mavg.fast / mavg.slow - 1 )
                        macdSerie.MACDLine.Add(100 * ((fastEmaValues[i] / slowEmaValues[i]) - 1));
                    }
                    OhlcList[i].closePrice = macdSerie.MACDLine[i].Value;
                }
                else
                {
                    macdSerie.MACDLine.Add(null);
                    OhlcList[i].closePrice = 0.0m;
                }
            }

            int zeroCount = macdSerie.MACDLine.Where(x => x == null).Count();
            _ema = new EMA(Signal, false);
            _ema.Load(OhlcList.Skip(zeroCount).ToList());
            var signalEmaValues = _ema.Calculate().Values;
            for (var i = 0; i < zeroCount; i++)
            {
                signalEmaValues.Insert(0, null);
            }

            // Fill Signal and MACD Histogram lists
            for (var i = 0; i < signalEmaValues.Count; i++)
            {
                macdSerie.Signal.Add(signalEmaValues[i]);

                macdSerie.MACDHistogram.Add(macdSerie.MACDLine[i] - macdSerie.Signal[i]);
            }

            return macdSerie;
        }
    }
}