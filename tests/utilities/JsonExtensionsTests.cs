using System;
using System.Text.Json;
using Xunit;
// 확장 메서드 네임스페이스 (JsonExtensions가 전역 namespace라 using 불필요하나 안전차원에서 유지)

namespace CCXT.Collector.Tests.Utilities
{
    public class JsonExtensionsTests
    {
        [Fact]
        public void GetUnixTimeOrDefault_NumberEpochSeconds()
        {
            var epochSeconds = 1_700_000_000L; // seconds
            var json = JsonDocument.Parse($"{{ \"t\": {epochSeconds} }}");
            var value = json.RootElement.GetUnixTimeOrDefault("t");
            Assert.Equal(epochSeconds * 1000, value); // converted to ms
        }

        [Fact]
        public void GetUnixTimeOrDefault_NumberEpochMilliseconds()
        {
            var epochMs = 1_700_000_000_000L; // milliseconds
            var json = JsonDocument.Parse($"{{ \"t\": {epochMs} }}");
            var value = json.RootElement.GetUnixTimeOrDefault("t");
            Assert.Equal(epochMs, value);
        }

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
    }
}
