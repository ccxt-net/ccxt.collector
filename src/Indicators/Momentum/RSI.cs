using CCXT.Collector.Service;
using System.Collections.Generic;
using System.Linq;

namespace CCXT.Collector.Indicator
{
    /// <summary>
    /// Relative Strength Index (RSI)
    /// </summary>
    public class RSI : IndicatorCalculatorBase<RSISerie>
    {
        protected override List<SOhlcvItem> OhlcList
        {
            get;
            set;
        }

        /// <summary>
        ///
        /// </summary>
        protected int Period
        {
            get;
            set;
        }

        private List<decimal?> change = new List<decimal?>();

        /// <summary>
        ///
        /// </summary>
        /// <param name="period"></param>
        public RSI(int period)
        {
            this.Period = period;
        }

        /// <summary>
        ///    RS = Average Gain / Average Loss
        ///
        ///                  100
        ///    RSI = 100 - --------
        ///                 1 + RS
        /// </summary>
        /// <see cref="http://stockcharts.com/school/doku.php?id=chart_school:technical_indicators:relative_strength_index_rsi"/>
        /// <returns></returns>
        public override RSISerie Calculate()
        {
            var _rsi_serie = new RSISerie();

            // Add null values for first item, iteration will start from second item of OhlcList
            _rsi_serie.RS.Add(null);
            _rsi_serie.RSI.Add(null);
            change.Add(null);

            for (var i = 1; i < OhlcList.Count; i++)
            {
                if (i >= this.Period)
                {
                    var averageGain = change.Where(x => x > 0).Sum() / change.Count;
                    var averageLoss = change.Where(x => x < 0).Sum() * (-1) / change.Count;

                    var rs = averageGain / averageLoss;
                    _rsi_serie.RS.Add(rs);

                    var rsi = 100 - (100 / (1 + rs));
                    _rsi_serie.RSI.Add(rsi);

                    // assign change for item
                    change.Add(OhlcList[i].closePrice - OhlcList[i - 1].closePrice);
                }
                else
                {
                    _rsi_serie.RS.Add(null);
                    _rsi_serie.RSI.Add(null);

                    // assign change for item
                    change.Add(OhlcList[i].closePrice - OhlcList[i - 1].closePrice);
                }
            }

            return _rsi_serie;
        }
    }
}