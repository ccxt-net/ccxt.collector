using CCXT.Collector.Service;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CCXT.Collector.Indicator
{
    public class ADX : IndicatorCalculatorBase<ADXSerie>
    {
        protected override List<SOhlcvItem> OhlcList
        {
            get; set;
        }

        protected int Period = 14;

        public ADX()
        {
        }

        public ADX(int period)
        {
            this.Period = period;
        }

        public override ADXSerie Calculate()
        {
            var adxSerie = new ADXSerie();

            var tempOhlcList = new List<SOhlcvItem>();
            for (var i = 0; i < OhlcList.Count; i++)
            {
                var tempOhlc = new SOhlcvItem() { closePrice = OhlcList[i].highPrice };
                tempOhlcList.Add(tempOhlc);
            }
            var momentum = new Momentum();
            momentum.Load(tempOhlcList);
            var highMomentums = momentum.Calculate().Values;

            tempOhlcList = new List<SOhlcvItem>();
            for (var i = 0; i < OhlcList.Count; i++)
            {
                var tempOhlc = new SOhlcvItem() { closePrice = OhlcList[i].lowPrice };
                tempOhlcList.Add(tempOhlc);
            }
            momentum = new Momentum();
            momentum.Load(tempOhlcList);
            var lowMomentums = momentum.Calculate().Values;
            for (var i = 0; i < lowMomentums.Count; i++)
            {
                if (lowMomentums[i].HasValue)
                {
                    lowMomentums[i] *= -1;
                }
            }

            //DMIp <- ifelse( dH==dL | (dH< 0 & dL< 0), 0, ifelse( dH >dL, dH, 0 ) )
            var DMIPositives = new List<decimal?>() { null };
            // DMIn <- ifelse( dH==dL | (dH< 0 & dL< 0), 0, ifelse( dH <dL, dL, 0 ) )
            var DMINegatives = new List<decimal?>() { null };
            for (var i = 1; i < OhlcList.Count; i++)
            {
                if (highMomentums[i] == lowMomentums[i] || (highMomentums[i] < 0 & lowMomentums[i] < 0))
                {
                    DMIPositives.Add(0);
                }
                else
                {
                    if (highMomentums[i] > lowMomentums[i])
                    {
                        DMIPositives.Add(highMomentums[i]);
                    }
                    else
                    {
                        DMIPositives.Add(0);
                    }
                }

                if (highMomentums[i] == lowMomentums[i] || (highMomentums[i] < 0 & lowMomentums[i] < 0))
                {
                    DMINegatives.Add(0);
                }
                else
                {
                    if (highMomentums[i] < lowMomentums[i])
                    {
                        DMINegatives.Add(lowMomentums[i]);
                    }
                    else
                    {
                        DMINegatives.Add(0);
                    }
                }
            }

            ATR atr = new ATR();
            atr.Load(OhlcList);
            var trueRanges = atr.Calculate().TrueRange;
            adxSerie.TrueRange = trueRanges;

            var trSum = wilderSum(trueRanges);

            // DIp <- 100 * wilderSum(DMIp, n=n) / TRsum
            var DIPositives = new List<decimal?>();
            var wilderSumOfDMIp = wilderSum(DMIPositives);
            for (var i = 0; i < wilderSumOfDMIp.Count; i++)
            {
                if (wilderSumOfDMIp[i].HasValue)
                {
                    DIPositives.Add(wilderSumOfDMIp[i].Value * 100 / trSum[i].Value);
                }
                else
                {
                    DIPositives.Add(null);
                }
            }
            adxSerie.DIPositive = DIPositives;

            // DIn <- 100 * wilderSum(DMIn, n=n) / TRsum
            var DINegatives = new List<decimal?>();
            var wilderSumOfDMIn = wilderSum(DMINegatives);
            for (var i = 0; i < wilderSumOfDMIn.Count; i++)
            {
                if (wilderSumOfDMIn[i].HasValue)
                {
                    DINegatives.Add(wilderSumOfDMIn[i].Value * 100 / trSum[i].Value);
                }
                else
                {
                    DINegatives.Add(null);
                }
            }
            adxSerie.DINegative = DINegatives;

            // DX  <- 100 * ( abs(DIp - DIn) / (DIp + DIn) )
            var DX = new List<decimal?>();
            for (var i = 0; i < OhlcList.Count; i++)
            {
                if (DIPositives[i].HasValue)
                {
                    var dx = 100 * (Math.Abs(DIPositives[i].Value - DINegatives[i].Value) / (DIPositives[i].Value + DINegatives[i].Value));
                    DX.Add(dx);
                }
                else
                {
                    DX.Add(null);
                }
            }
            adxSerie.DX = DX;

            for (var i = 0; i < OhlcList.Count; i++)
            {
                if (DX[i].HasValue)
                {
                    OhlcList[i].closePrice = DX[i].Value;
                }
                else
                {
                    OhlcList[i].closePrice = 0.0m;
                }
            }

            EMA ema = new EMA(Period, true);
            ema.Load(OhlcList.Skip(Period).ToList());
            var emaValues = ema.Calculate().Values;
            for (var i = 0; i < Period; i++)
            {
                emaValues.Insert(0, null);
            }
            adxSerie.ADX = emaValues;

            return adxSerie;
        }

        private List<decimal?> wilderSum(List<decimal?> values)
        {
            var wilderSumsArray = new decimal?[values.Count];
            var valueArr = values.ToArray();

            int beg = Period - 1;
            var sum = 0.0m;

            var i = 0;
            for (i = 0; i < beg; i++)
            {
                /* Account for leading NAs in input */
                if (!valueArr[i].HasValue)
                {
                    wilderSumsArray[i] = null;
                    beg++;
                    wilderSumsArray[beg] = 0;
                    continue;
                }
                /* Set leading NAs in output */
                if (i < beg)
                {
                    wilderSumsArray[i] = null;
                }
                /* Calculate raw sum to start */
                sum += valueArr[i].Value;
            }

            wilderSumsArray[beg] = valueArr[i] + sum * (Period - 1) / Period;

            /* Loop over non-NA input values */
            for (i = beg + 1; i < values.Count; i++)
            {
                wilderSumsArray[i] = valueArr[i] + wilderSumsArray[i - 1] * (Period - 1) / Period;
            }

            return wilderSumsArray.ToList();
        }
    }
}