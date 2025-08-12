using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using CCXT.Collector.Indicator;

namespace CCXT.Collector.Tests.Utilities
{
    public class StatisticsTests
    {
        #region StandardDeviation Tests

        [Fact]
        public void StandardDeviationSample_EmptyList_ReturnsZero()
        {
            var result = Statistics.StandardDeviationSample(new List<decimal>());
            Assert.Equal(0m, result);
        }

        [Fact]
        public void StandardDeviationSample_SingleValue_ReturnsZero()
        {
            var result = Statistics.StandardDeviationSample(new List<decimal> { 5m });
            Assert.Equal(0m, result);
        }

        [Fact]
        public void StandardDeviationSample_TwoValues_CalculatesCorrectly()
        {
            var values = new List<decimal> { 1m, 3m };
            var result = Statistics.StandardDeviationSample(values);
            
            // Sample std dev = sqrt(((1-2)^2 + (3-2)^2) / (2-1)) = sqrt(2/1) = 1.414...
            Assert.Equal(1.414m, Math.Round(result, 3));
        }

        [Fact]
        public void StandardDeviationPopulation_BasicValues_CalculatesCorrectly()
        {
            var values = new List<decimal> { 2m, 4m, 6m, 8m, 10m };
            var result = Statistics.StandardDeviationPopulation(values);
            
            // Mean = 6, Population std dev = sqrt(8) = 2.828...
            Assert.Equal(2.828m, Math.Round(result, 3));
        }

        [Fact]
        public void StandardDeviationSample_vs_Population_DifferentResults()
        {
            var values = new List<decimal> { 1m, 2m, 3m, 4m, 5m };
            
            var sample = Statistics.StandardDeviationSample(values);
            var population = Statistics.StandardDeviationPopulation(values);
            
            // Sample should be larger than population
            Assert.True(sample > population);
            
            // Verify specific values
            Assert.Equal(1.581m, Math.Round(sample, 3));      // sqrt(2.5)
            Assert.Equal(1.414m, Math.Round(population, 3));  // sqrt(2)
        }

        #endregion

        #region RunMax Tests

        [Fact]
        public void RunMax_BasicCase_CalculatesCorrectly()
        {
            var values = new List<decimal> { 1m, 3m, 2m, 5m, 4m };
            var period = 3;
            
            var result = Statistics.RunMax(values, period);
            
            Assert.Equal(5, result.Count);
            Assert.Null(result[0]); // Not enough values
            Assert.Null(result[1]); // Not enough values
            Assert.Equal(3m, result[2]); // max(1,3,2)
            Assert.Equal(5m, result[3]); // max(3,2,5)
            Assert.Equal(5m, result[4]); // max(2,5,4)
        }

        [Fact]
        public void RunMax_LargePeriod_PerformanceTest()
        {
            // Generate large dataset
            var values = new List<decimal>();
            var random = new Random(42);
            for (int i = 0; i < 10000; i++)
            {
                values.Add((decimal)random.NextDouble() * 100);
            }
            
            var period = 100;
            
            // Measure performance
            var startTime = DateTime.Now;
            var result = Statistics.RunMax(values, period);
            var elapsed = (DateTime.Now - startTime).TotalMilliseconds;
            
            // Should complete quickly with O(n) algorithm
            Assert.True(elapsed < 100, $"RunMax took {elapsed}ms, should be under 100ms");
            Assert.Equal(values.Count, result.Count);
            
            // Verify correctness for a few windows
            for (int i = period - 1; i < Math.Min(period + 10, values.Count); i++)
            {
                var expectedMax = values.Skip(i - period + 1).Take(period).Max();
                Assert.Equal(expectedMax, result[i]);
            }
        }

        #endregion

        #region RunMin Tests

        [Fact]
        public void RunMin_BasicCase_CalculatesCorrectly()
        {
            var values = new List<decimal> { 5m, 3m, 4m, 1m, 2m };
            var period = 3;
            
            var result = Statistics.RunMin(values, period);
            
            Assert.Equal(5, result.Count);
            Assert.Null(result[0]); // Not enough values
            Assert.Null(result[1]); // Not enough values
            Assert.Equal(3m, result[2]); // min(5,3,4)
            Assert.Equal(1m, result[3]); // min(3,4,1)
            Assert.Equal(1m, result[4]); // min(4,1,2)
        }

        [Fact]
        public void RunMin_EdgeCases()
        {
            // Empty list
            var result1 = Statistics.RunMin(new List<decimal>(), 3);
            Assert.Empty(result1);
            
            // Period = 0
            var result2 = Statistics.RunMin(new List<decimal> { 1m, 2m }, 0);
            Assert.Empty(result2);
            
            // Period > list size
            var result3 = Statistics.RunMin(new List<decimal> { 1m, 2m }, 5);
            Assert.Equal(2, result3.Count);
            Assert.All(result3, r => Assert.Null(r));
        }

        #endregion

        #region Stream Tests

        [Fact]
        public void RunMaxStream_BasicCase_ProducesCorrectResults()
        {
            var values = new[] { 1m, 3m, 2m, 5m, 4m };
            var period = 3;
            
            var results = Statistics.RunMaxStream(values, period).ToList();
            
            Assert.Equal(5, results.Count);
            Assert.Equal((null, false), results[0]);
            Assert.Equal((null, false), results[1]);
            Assert.Equal((3m, true), results[2]);
            Assert.Equal((5m, true), results[3]);
            Assert.Equal((5m, true), results[4]);
        }

        [Fact]
        public void RunMinStream_StreamingBehavior()
        {
            var values = new[] { 5m, 3m, 4m, 1m, 2m };
            var period = 2;
            
            var results = new List<(decimal? value, bool isReady)>();
            
            // Simulate streaming data
            foreach (var result in Statistics.RunMinStream(values, period))
            {
                results.Add(result);
                
                // Can process each value as it arrives
                if (result.isReady)
                {
                    Assert.NotNull(result.value);
                }
            }
            
            Assert.Equal(5, results.Count);
            Assert.False(results[0].isReady);
            Assert.True(results[1].isReady);
            Assert.Equal(3m, results[1].value);
        }

        #endregion

        #region Additional Statistics Tests

        [Fact]
        public void Mean_CalculatesCorrectly()
        {
            var values = new List<decimal> { 1m, 2m, 3m, 4m, 5m };
            var result = Statistics.Mean(values);
            Assert.Equal(3m, result);
        }

        [Fact]
        public void VarianceSample_CalculatesCorrectly()
        {
            var values = new List<decimal> { 2m, 4m, 6m };
            var result = Statistics.VarianceSample(values);
            
            // Mean = 4, Variance = ((2-4)^2 + (4-4)^2 + (6-4)^2) / (3-1) = 8/2 = 4
            Assert.Equal(4m, result);
        }

        [Fact]
        public void VariancePopulation_CalculatesCorrectly()
        {
            var values = new List<decimal> { 2m, 4m, 6m };
            var result = Statistics.VariancePopulation(values);
            
            // Mean = 4, Variance = ((2-4)^2 + (4-4)^2 + (6-4)^2) / 3 = 8/3 = 2.666...
            Assert.Equal(2.667m, Math.Round(result, 3));
        }

        #endregion

        #region Monotonic Deque Algorithm Verification

        [Fact]
        public void RunMax_MonotonicDeque_MaintainsCorrectness()
        {
            // Test that the monotonic deque maintains correctness
            // with values that would break a naive implementation
            var values = new List<decimal> { 1m, 5m, 3m, 2m, 6m, 4m };
            var period = 3;
            
            var result = Statistics.RunMax(values, period);
            
            // Manually verify each window
            Assert.Equal(5m, result[2]); // max(1,5,3) = 5
            Assert.Equal(5m, result[3]); // max(5,3,2) = 5
            Assert.Equal(6m, result[4]); // max(3,2,6) = 6
            Assert.Equal(6m, result[5]); // max(2,6,4) = 6
        }

        [Fact]
        public void RunMin_MonotonicDeque_HandlesDecreasingSequence()
        {
            // Decreasing sequence tests deque efficiency
            var values = new List<decimal> { 10m, 9m, 8m, 7m, 6m, 5m };
            var period = 3;
            
            var result = Statistics.RunMin(values, period);
            
            Assert.Equal(8m, result[2]); // min(10,9,8)
            Assert.Equal(7m, result[3]); // min(9,8,7)
            Assert.Equal(6m, result[4]); // min(8,7,6)
            Assert.Equal(5m, result[5]); // min(7,6,5)
        }

        #endregion
    }
}