using System;
using System.Collections.Generic;

namespace CCXT.Collector.Indicator
{
    public static class Statistics
    {
        public static decimal StandardDeviation(List<decimal> valueList)
        {
            var M = 0.0m;
            var S = 0.0m;

            var k = 1;

            foreach (var value in valueList)
            {
                var tmpM = M;

                M += (value - tmpM) / k;
                S += (value - tmpM) * (value - M);

                k++;
            }

            return (decimal)Math.Sqrt((double)S / (k - 2));
        }

        public static List<decimal?> RunMax(List<decimal> list, int period)
        {
            var maxList = new List<decimal?>();

            for (var i = 0; i < list.Count; i++)
            {
                if (i >= period - 1)
                {
                    var max = 0.0m;
                    for (var j = i - (period - 1); j <= i; j++)
                    {
                        if (list[j] > max)
                        {
                            max = list[j];
                        }
                    }

                    maxList.Add(max);
                }
                else
                {
                    maxList.Add(null);
                }
            }

            return maxList;
        }

        public static List<decimal?> RunMin(List<decimal> list, int period)
        {
            var minList = new List<decimal?>();

            for (var i = 0; i < list.Count; i++)
            {
                if (i >= period - 1)
                {
                    var min = Decimal.MaxValue;
                    for (var j = i - (period - 1); j <= i; j++)
                    {
                        if (list[j] < min)
                        {
                            min = list[j];
                        }
                    }

                    minList.Add(min);
                }
                else
                {
                    minList.Add(null);
                }
            }

            return minList;
        }
    }
}