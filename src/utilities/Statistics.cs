using System;
using System.Collections.Generic;
using System.Linq;

namespace CCXT.Collector.Indicator
{
    /// <summary>
    /// Statistical calculation utilities for technical indicators
    /// </summary>
    public static class Statistics
    {
        /// <summary>
        /// Calculates the sample standard deviation using Welford's online algorithm
        /// </summary>
        /// <param name="valueList">List of values</param>
        /// <returns>Sample standard deviation (uses n-1 denominator)</returns>
        public static decimal StandardDeviationSample(List<decimal> valueList)
        {
            if (valueList == null || valueList.Count == 0)
                return 0m;

            if (valueList.Count == 1)
                return 0m; // Single value has no variance

            // Welford's online algorithm for numerical stability
            var M = 0.0m;  // Mean
            var S = 0.0m;  // Sum of squared differences
            var k = 1;

            foreach (var value in valueList)
            {
                var tmpM = M;
                M += (value - tmpM) / k;
                S += (value - tmpM) * (value - M);
                k++;
            }

            // Sample variance uses n-1 (Bessel's correction)
            var variance = S / (valueList.Count - 1);
            return (decimal)Math.Sqrt((double)variance);
        }

        /// <summary>
        /// Calculates the population standard deviation
        /// </summary>
        /// <param name="valueList">List of values</param>
        /// <returns>Population standard deviation (uses n denominator)</returns>
        public static decimal StandardDeviationPopulation(List<decimal> valueList)
        {
            if (valueList == null || valueList.Count == 0)
                return 0m;

            // Welford's online algorithm
            var M = 0.0m;  // Mean
            var S = 0.0m;  // Sum of squared differences
            var k = 1;

            foreach (var value in valueList)
            {
                var tmpM = M;
                M += (value - tmpM) / k;
                S += (value - tmpM) * (value - M);
                k++;
            }

            // Population variance uses n
            var variance = S / valueList.Count;
            return (decimal)Math.Sqrt((double)variance);
        }


        /// <summary>
        /// Calculates running maximum values over a sliding window using monotonic deque
        /// O(n) time complexity, O(period) space complexity
        /// </summary>
        /// <param name="list">Input values</param>
        /// <param name="period">Window size</param>
        /// <returns>List of maximum values for each window</returns>
        public static List<decimal?> RunMax(List<decimal> list, int period)
        {
            if (list == null || list.Count == 0 || period <= 0)
                return new List<decimal?>();

            var result = new List<decimal?>(list.Count);
            var deque = new LinkedList<(int index, decimal value)>(); // Monotonic decreasing deque

            for (int i = 0; i < list.Count; i++)
            {
                // Remove elements outside the window
                while (deque.Count > 0 && deque.First.Value.index <= i - period)
                {
                    deque.RemoveFirst();
                }

                // Maintain monotonic decreasing property
                while (deque.Count > 0 && deque.Last.Value.value <= list[i])
                {
                    deque.RemoveLast();
                }

                deque.AddLast((i, list[i]));

                // Add result (null if window not yet full)
                if (i >= period - 1)
                {
                    result.Add(deque.First.Value.value);
                }
                else
                {
                    result.Add(null);
                }
            }

            return result;
        }

        /// <summary>
        /// Calculates running minimum values over a sliding window using monotonic deque
        /// O(n) time complexity, O(period) space complexity
        /// </summary>
        /// <param name="list">Input values</param>
        /// <param name="period">Window size</param>
        /// <returns>List of minimum values for each window</returns>
        public static List<decimal?> RunMin(List<decimal> list, int period)
        {
            if (list == null || list.Count == 0 || period <= 0)
                return new List<decimal?>();

            var result = new List<decimal?>(list.Count);
            var deque = new LinkedList<(int index, decimal value)>(); // Monotonic increasing deque

            for (int i = 0; i < list.Count; i++)
            {
                // Remove elements outside the window
                while (deque.Count > 0 && deque.First.Value.index <= i - period)
                {
                    deque.RemoveFirst();
                }

                // Maintain monotonic increasing property
                while (deque.Count > 0 && deque.Last.Value.value >= list[i])
                {
                    deque.RemoveLast();
                }

                deque.AddLast((i, list[i]));

                // Add result (null if window not yet full)
                if (i >= period - 1)
                {
                    result.Add(deque.First.Value.value);
                }
                else
                {
                    result.Add(null);
                }
            }

            return result;
        }

        /// <summary>
        /// Calculates running maximum values with streaming support
        /// </summary>
        public static IEnumerable<(decimal? value, bool isReady)> RunMaxStream(IEnumerable<decimal> stream, int period)
        {
            if (stream == null || period <= 0)
                yield break;

            var deque = new LinkedList<(int index, decimal value)>();
            int currentIndex = 0;

            foreach (var value in stream)
            {
                // Remove elements outside the window
                while (deque.Count > 0 && deque.First.Value.index <= currentIndex - period)
                {
                    deque.RemoveFirst();
                }

                // Maintain monotonic decreasing property
                while (deque.Count > 0 && deque.Last.Value.value <= value)
                {
                    deque.RemoveLast();
                }

                deque.AddLast((currentIndex, value));

                // Yield result
                if (currentIndex >= period - 1)
                {
                    yield return (deque.First.Value.value, true);
                }
                else
                {
                    yield return (null, false);
                }

                currentIndex++;
            }
        }

        /// <summary>
        /// Calculates running minimum values with streaming support
        /// </summary>
        public static IEnumerable<(decimal? value, bool isReady)> RunMinStream(IEnumerable<decimal> stream, int period)
        {
            if (stream == null || period <= 0)
                yield break;

            var deque = new LinkedList<(int index, decimal value)>();
            int currentIndex = 0;

            foreach (var value in stream)
            {
                // Remove elements outside the window
                while (deque.Count > 0 && deque.First.Value.index <= currentIndex - period)
                {
                    deque.RemoveFirst();
                }

                // Maintain monotonic increasing property
                while (deque.Count > 0 && deque.Last.Value.value >= value)
                {
                    deque.RemoveLast();
                }

                deque.AddLast((currentIndex, value));

                // Yield result
                if (currentIndex >= period - 1)
                {
                    yield return (deque.First.Value.value, true);
                }
                else
                {
                    yield return (null, false);
                }

                currentIndex++;
            }
        }

        /// <summary>
        /// Calculates the mean (average) of values
        /// </summary>
        public static decimal Mean(List<decimal> valueList)
        {
            if (valueList == null || valueList.Count == 0)
                return 0m;

            return valueList.Sum() / valueList.Count;
        }

        /// <summary>
        /// Calculates the variance (sample)
        /// </summary>
        public static decimal VarianceSample(List<decimal> valueList)
        {
            if (valueList == null || valueList.Count <= 1)
                return 0m;

            var mean = Mean(valueList);
            var sumOfSquares = valueList.Sum(v => (v - mean) * (v - mean));
            return sumOfSquares / (valueList.Count - 1);
        }

        /// <summary>
        /// Calculates the variance (population)
        /// </summary>
        public static decimal VariancePopulation(List<decimal> valueList)
        {
            if (valueList == null || valueList.Count == 0)
                return 0m;

            var mean = Mean(valueList);
            var sumOfSquares = valueList.Sum(v => (v - mean) * (v - mean));
            return sumOfSquares / valueList.Count;
        }
    }
}