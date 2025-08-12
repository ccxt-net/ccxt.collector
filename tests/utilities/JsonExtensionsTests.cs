using System;
using System.Text.Json;
using Xunit;
// 확장 메서드 네임스페이스 (JsonExtensions가 전역 namespace라 using 불필요하나 안전차원에서 유지)

namespace CCXT.Collector.Tests.Utilities
{
    public class JsonExtensionsTests
    {
        #region Existing Tests
        
        [Fact]
        public void GetUnixTimeOrDefault_NumberEpochSeconds()
        {
            var epochSeconds = 1_700_000_000L; // seconds (2023-11-14)
            var json = JsonDocument.Parse($"{{ \"t\": {epochSeconds} }}");
            var value = json.RootElement.GetUnixTimeOrDefault("t");
            Assert.Equal(epochSeconds * 1000, value); // converted to ms
        }

        [Fact]
        public void GetUnixTimeOrDefault_NumberEpochMilliseconds()
        {
            var epochMs = 1_700_000_000_000L; // milliseconds (2023-11-14)
            var json = JsonDocument.Parse($"{{ \"t\": {epochMs} }}");
            var value = json.RootElement.GetUnixTimeOrDefault("t");
            Assert.Equal(epochMs, value);
        }

        #endregion

        #region New Tests for Improved Epoch Detection

        [Theory]
        [InlineData(946684800, 946684800000)]      // 2000-01-01 in seconds
        [InlineData(1609459200, 1609459200000)]    // 2021-01-01 in seconds
        [InlineData(1735689600, 1735689600000)]    // 2025-01-01 in seconds
        [InlineData(2051222400, 2051222400000)]    // 2035-01-01 in seconds (after 2033 boundary)
        [InlineData(2524608000, 2524608000000)]    // 2050-01-01 in seconds
        [InlineData(3155760000, 3155760000000)]    // 2070-01-01 in seconds
        public void GetUnixTimeOrDefault_FutureEpochSeconds_ConvertsCorrectly(long epochSeconds, long expectedMs)
        {
            var json = JsonDocument.Parse($"{{ \"timestamp\": {epochSeconds} }}");
            var value = json.RootElement.GetUnixTimeOrDefault("timestamp");
            Assert.Equal(expectedMs, value);
        }

        [Theory]
        [InlineData(946684800000, 946684800000)]      // 2000-01-01 in milliseconds
        [InlineData(1735689600000, 1735689600000)]    // 2025-01-01 in milliseconds
        [InlineData(2051222400000, 2051222400000)]    // 2035-01-01 in milliseconds
        [InlineData(4102444800000, 4102444800000)]    // 2100-01-01 in milliseconds
        public void GetUnixTimeOrDefault_FutureEpochMilliseconds_PreservesValue(long epochMs, long expectedMs)
        {
            var json = JsonDocument.Parse($"{{ \"timestamp\": {epochMs} }}");
            var value = json.RootElement.GetUnixTimeOrDefault("timestamp");
            Assert.Equal(expectedMs, value);
        }

        [Fact]
        public void GetDateTimeOffsetOrDefault_Year2035_HandlesCorrectly()
        {
            // Test year 2035 when seconds will be 11 digits (2051222400)
            var epochSec2035 = 2051222400L;
            var jsonSec = JsonDocument.Parse($"{{ \"date\": {epochSec2035} }}");
            var dateSec = jsonSec.RootElement.GetDateTimeOffsetOrDefault("date");
            
            var expectedDate = new DateTime(2035, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            Assert.Equal(expectedDate, dateSec.UtcDateTime);

            // Test milliseconds version
            var epochMs2035 = 2051222400000L;
            var jsonMs = JsonDocument.Parse($"{{ \"date\": {epochMs2035} }}");
            var dateMs = jsonMs.RootElement.GetDateTimeOffsetOrDefault("date");
            Assert.Equal(expectedDate, dateMs.UtcDateTime);
        }

        [Fact]
        public void GetUnixTimeOrDefault_EdgeCase_VeryOldDates()
        {
            // Test dates before year 2000 (should still work)
            var epoch1990Sec = 631152000L;  // 1990-01-01 in seconds
            var json = JsonDocument.Parse($"{{ \"old\": {epoch1990Sec} }}");
            var value = json.RootElement.GetUnixTimeOrDefault("old");
            
            // Should detect as seconds since it's outside reasonable millisecond range
            // and within digit count threshold
            Assert.Equal(epoch1990Sec * 1000, value);
        }

        [Fact]
        public void GetUnixTimeOrDefault_EdgeCase_VeryFutureDates()
        {
            // Test dates far in the future (year 2200+)
            var epoch2200Sec = 7258118400L;  // 2200-01-01 in seconds (10 digits)
            var json = JsonDocument.Parse($"{{ \"future\": {epoch2200Sec} }}");
            var value = json.RootElement.GetUnixTimeOrDefault("future");
            
            // Should use digit count fallback since it's outside reasonable range
            Assert.Equal(epoch2200Sec * 1000, value);
        }

        #endregion

        [Fact]
        public void TryGetArrayAllowEmpty_ReturnsTrueForEmpty()
        {
            var json = JsonDocument.Parse("{ \"arr\": [] }");
            var ok = json.RootElement.TryGetArrayAllowEmpty("arr", out var arr);
            Assert.True(ok);
            Assert.Equal(0, arr.GetArrayLength());
        }

        [Fact]
        public void TryGetNonEmptyArray_FailsForEmpty()
        {
            var json = JsonDocument.Parse("{ \"arr\": [] }");
            var ok = json.RootElement.TryGetNonEmptyArray("arr", out _);
            Assert.False(ok);
        }

        [Fact]
        public void FirstOrUndefined_IsDefinedElement_Works()
        {
            var json = JsonDocument.Parse("{ \"arr\": [1,2,3] }");
            var first = json.RootElement.GetProperty("arr").FirstOrUndefined();
            Assert.True(first.IsDefinedElement());
        }

        [Fact]
        public void FirstOrUndefined_EmptyArray_NotDefined()
        {
            var json = JsonDocument.Parse("{ \"arr\": [] }");
            var first = json.RootElement.GetProperty("arr").FirstOrUndefined();
            Assert.False(first.IsDefinedElement());
        }

        [Fact]
        public void GetUnixTimeOrDefault_StringEpochSeconds()
        {
            var sec = 1_700_000_100L;
            var json = JsonDocument.Parse($"{{ \"t\": \"{sec}\" }}");
            var ms = json.RootElement.GetUnixTimeOrDefault("t");
            Assert.Equal(sec * 1000, ms);
        }

        [Fact]
        public void GetUnixTimeOrDefault_StringIso()
        {
            var dto = DateTimeOffset.UtcNow;
            var iso = dto.ToString("O");
            var json = JsonDocument.Parse($"{{ \"t\": \"{iso}\" }}");
            var ms = json.RootElement.GetUnixTimeOrDefault("t");
            Assert.Equal(dto.ToUnixTimeMilliseconds(), ms);
        }
    }
}
