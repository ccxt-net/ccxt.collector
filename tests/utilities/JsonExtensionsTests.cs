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
