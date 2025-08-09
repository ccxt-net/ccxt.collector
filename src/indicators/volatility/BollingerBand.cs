using CCXT.Collector.Service;
using System;
using System.Collections.Generic;

namespace CCXT.Collector.Indicator
{
    /// <summary>
    /// Bollinger Bands
    /// </summary>
    public class BollingerBand : IndicatorCalculatorBase<BollingerBandSerie>
    {
        protected override List<SOhlcvItem> OhlcList
        {
            get; set;
        }

        protected int Period = 20;
        protected int Factor = 2;

        public BollingerBand()
        {
        }

        public BollingerBand(int period, int factor)
        {
            this.Period = period;
            this.Factor = factor;
        }

        /// <summary>
        /// tp = (high + low + close) / 3
        /// MidBand = SMA(TP)
        /// UpperBand = MidBand + Factor * Stdev(tp)
        /// LowerBand = MidBand - Factor * Stdev(tp)
        /// BandWidth = (UpperBand - LowerBand) / MidBand
        /// </summary>
        /// <see cref="http://www.fmlabs.com/reference/default.htm?url=Bollinger.htm"/>
        /// <see cref="http://stockcharts.com/school/doku.php?id=chart_school:technical_indicators:bollinger_bands"/>
        /// <see cref="http://stockcharts.com/school/doku.php?id=chart_school:technical_indicators:bollinger_band_width"/>
        /// <returns></returns>
        public override BollingerBandSerie Calculate()
        {
            var _bollinger_band_serie = new BollingerBandSerie();

            var _total_average = 0.0m;
            var _total_squares = 0.0m;

            for (var i = 0; i < OhlcList.Count; i++)
            {
                OhlcList[i].closePrice = (OhlcList[i].highPrice + OhlcList[i].lowPrice + OhlcList[i].closePrice) / 3;

                _total_average += OhlcList[i].closePrice;
                _total_squares += (decimal)Math.Pow((double)OhlcList[i].closePrice, 2);

                if (i >= Period - 1)
                {
                    var _average = _total_average / Period;
                    var stdev = (decimal)Math.Sqrt(((double)_total_squares - Math.Pow((double)_total_average, 2.0) / Period) / Period);

                    _bollinger_band_serie.MidBand.Add(_average);
                    var up = _average + Factor * stdev;
                    _bollinger_band_serie.UpperBand.Add(up);
                    var down = _average - Factor * stdev;
                    _bollinger_band_serie.LowerBand.Add(down);
                    var bandWidth = (up - down) / _average;
                    _bollinger_band_serie.BandWidth.Add(bandWidth);
                    var bPercent = (OhlcList[i].closePrice - down) / (up - down);
                    _bollinger_band_serie.BPercent.Add(bPercent);

                    _total_average -= OhlcList[i - Period + 1].closePrice;
                    _total_squares -= (decimal)Math.Pow((double)OhlcList[i - Period + 1].closePrice, 2);
                }
                else
                {
                    _bollinger_band_serie.MidBand.Add(null);
                    _bollinger_band_serie.UpperBand.Add(null);
                    _bollinger_band_serie.LowerBand.Add(null);
                    _bollinger_band_serie.BandWidth.Add(null);
                    _bollinger_band_serie.BPercent.Add(null);
                }
            }

            return _bollinger_band_serie;
        }
    }
}