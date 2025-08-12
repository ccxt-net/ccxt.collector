using CCXT.Collector.Service;
using System.Collections.Generic;
using System.Linq;

namespace CCXT.Collector.Indicator
{
    public class SAR : IndicatorCalculatorBase<SingleDoubleSerie>
    {
        protected override List<SOhlcvItem> OhlcList
        {
            get;
            set;
        }

        private decimal AccelerationFactor = 0.02m;
        protected decimal MaximumAccelerationFactor = 0.2m;

        public SAR()
        {
        }

        public SAR(decimal accelerationFactor, decimal maximumAccelerationFactor)
        {
            this.AccelerationFactor = accelerationFactor;
            this.MaximumAccelerationFactor = maximumAccelerationFactor;
        }

        /// <summary>
        ///
        /// </summary>
        /// <returns></returns>
        public override SingleDoubleSerie Calculate()
        {
            var _sar_serie = new SingleDoubleSerie();

            // Difference of High and Low
            var differences = new List<decimal>();
            for (var i = 0; i < OhlcList.Count; i++)
            {
                decimal difference = OhlcList[i].highPrice - OhlcList[i].lowPrice;
                differences.Add(difference);
            }

            // STDEV of differences
            var stDev = Statistics.StandardDeviation(differences);

            var sarArr = new decimal?[OhlcList.Count];

            var highList = OhlcList.Select(x => x.highPrice).ToArray();
            var lowList = OhlcList.Select(x => x.lowPrice).ToArray();

            /* Find first non-NA value */
            var beg = 1;
            for (var i = 0; i < OhlcList.Count; i++)
            {
                if (OhlcList[i].highPrice == 0 || OhlcList[i].lowPrice == 0)
                {
                    sarArr[i] = 0;
                    beg++;
                }
                else
                {
                    break;
                }
            }

            // TODO: needs attention, understand better and replace variable names with meaningful ones.
            /* Initialize values needed by the routine */
            int sig0 = 1, sig1 = 0;
            decimal xpt0 = highList[beg - 1], xpt1 = 0;
            decimal af0 = AccelerationFactor, af1 = 0;
            decimal lmin, lmax;
            sarArr[beg - 1] = lowList[beg - 1] - stDev;

            for (var i = beg; i < OhlcList.Count; i++)
            {
                /* Increment signal, extreme point, and acceleration factor */
                sig1 = sig0;
                xpt1 = xpt0;
                af1 = af0;

                /* Local extrema */
                lmin = (lowList[i - 1] > lowList[i]) ? lowList[i] : lowList[i - 1];
                lmax = (highList[i - 1] > highList[i]) ? highList[i - 1] : highList[i];

                /* Create signal and extreme price vectors */
                if (sig1 == 1)
                {  /* Previous buy signal */
                    sig0 = (lowList[i] > sarArr[i - 1]) ? 1 : -1;  /* New signal */
                    xpt0 = (lmax > xpt1) ? lmax : xpt1;             /* New extreme price */
                }
                else
                {           /* Previous sell signal */
                    sig0 = (highList[i] < sarArr[i - 1]) ? -1 : 1;  /* New signal */
                    xpt0 = (lmin > xpt1) ? xpt1 : lmin;             /* New extreme price */
                }

                /*
                    * Calculate acceleration factor (af)
                    * and stop-and-reverse (sar) vector
                */

                /* No signal change */
                if (sig0 == sig1)
                {
                    /* Initial calculations */
                    sarArr[i] = sarArr[i - 1] + (xpt1 - sarArr[i - 1]) * af1;
                    af0 = (af1 == MaximumAccelerationFactor) ? MaximumAccelerationFactor : (AccelerationFactor + af1);
                    /* Current buy signal */
                    if (sig0 == 1)
                    {
                        af0 = (xpt0 > xpt1) ? af0 : af1;  /* Update acceleration factor */
                        sarArr[i] = (sarArr[i] > lmin) ? lmin : sarArr[i];  /* Determine sar value */
                    }
                    /* Current sell signal */
                    else
                    {
                        af0 = (xpt0 < xpt1) ? af0 : af1;  /* Update acceleration factor */
                        sarArr[i] = (sarArr[i] > lmax) ? sarArr[i] : lmax;   /* Determine sar value */
                    }
                }
                else /* New signal */
                {
                    af0 = AccelerationFactor;    /* reset acceleration factor */
                    sarArr[i] = xpt0;  /* set sar value */
                }
            }

            _sar_serie.Values = sarArr.ToList();

            return _sar_serie;
        }
    }
}