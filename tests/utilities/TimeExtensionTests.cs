using System;
using Xunit;

namespace CCXT.Collector.Tests.Utilities
{
    public class TimeExtensionTests
    {
        #region Test Constants
        
        // Known Unix timestamps for testing
        private const long UnixEpochMilliseconds = 0L;
        private const long UnixEpochSeconds = 0L;
        
        // January 1, 2000 00:00:00 UTC
        private const long Year2000UnixMilliseconds = 946684800000L;
        private const long Year2000UnixSeconds = 946684800L;
        
        // January 1, 2025 00:00:00 UTC
        private const long Year2025UnixMilliseconds = 1735689600000L;
        private const long Year2025UnixSeconds = 1735689600L;
        
        private static readonly DateTime UnixEpochDateTime = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        private static readonly DateTime Year2000DateTime = new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        private static readonly DateTime Year2025DateTime = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        
        #endregion
        
        #region Current Time Properties Tests
        
        [Fact]
        public void UnixTimeMillisecondsNow_ReturnsValidCurrentTime()
        {
            var before = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var actual = TimeExtension.UnixTimeMillisecondsNow;
            var after = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            
            Assert.InRange(actual, before - 100, after + 100); // Allow 100ms tolerance
        }
        
        [Fact]
        public void UnixTimeSecondsNow_ReturnsValidCurrentTime()
        {
            var before = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var actual = TimeExtension.UnixTimeSecondsNow;
            var after = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            
            Assert.InRange(actual, before - 1, after + 1); // Allow 1 second tolerance
        }
        
        #endregion
        
        #region ToUnixTimeMilliseconds Tests
        
        [Fact]
        public void ToUnixTimeMilliseconds_UtcDateTime_ReturnsCorrectValue()
        {
            var result = Year2000DateTime.ToUnixTimeMilliseconds();
            Assert.Equal(Year2000UnixMilliseconds, result);
        }
        
        [Fact]
        public void ToUnixTimeMilliseconds_LocalDateTime_ReturnsCorrectValue()
        {
            var localTime = new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Local);
            var expected = new DateTimeOffset(localTime).ToUnixTimeMilliseconds();
            
            var result = localTime.ToUnixTimeMilliseconds();
            
            Assert.Equal(expected, result);
        }
        
        [Fact]
        public void ToUnixTimeMilliseconds_UnspecifiedAsLocal_ReturnsLocalValue()
        {
            var unspecifiedTime = new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Unspecified);
            var localOffset = TimeZoneInfo.Local.GetUtcOffset(unspecifiedTime);
            var expected = new DateTimeOffset(unspecifiedTime, localOffset).ToUnixTimeMilliseconds();
            
            var result = unspecifiedTime.ToUnixTimeMilliseconds(treatUnspecifiedAsUtc: false);
            
            Assert.Equal(expected, result);
        }
        
        [Fact]
        public void ToUnixTimeMilliseconds_UnspecifiedAsUtc_ReturnsUtcValue()
        {
            var unspecifiedTime = new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Unspecified);
            
            var result = unspecifiedTime.ToUnixTimeMilliseconds(treatUnspecifiedAsUtc: true);
            
            Assert.Equal(Year2000UnixMilliseconds, result);
        }
        
        [Fact]
        public void ToUnixTimeMilliseconds_BeforeEpoch_ThrowsException()
        {
            var beforeEpoch = new DateTime(1969, 12, 31, 23, 59, 59, DateTimeKind.Utc);
            
            var ex = Assert.Throws<ArgumentException>(() => beforeEpoch.ToUnixTimeMilliseconds());
            Assert.Contains("before Unix epoch", ex.Message);
        }
        
        [Fact]
        public void ToUnixTimeMilliseconds_UnixEpoch_ReturnsZero()
        {
            var result = UnixEpochDateTime.ToUnixTimeMilliseconds();
            Assert.Equal(0L, result);
        }
        
        #endregion
        
        #region ToUnixTimeSeconds Tests
        
        [Fact]
        public void ToUnixTimeSeconds_UtcDateTime_ReturnsCorrectValue()
        {
            var result = Year2000DateTime.ToUnixTimeSeconds();
            Assert.Equal(Year2000UnixSeconds, result);
        }
        
        [Fact]
        public void ToUnixTimeSeconds_UnspecifiedAsUtc_ReturnsUtcValue()
        {
            var unspecifiedTime = new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Unspecified);
            
            var result = unspecifiedTime.ToUnixTimeSeconds(treatUnspecifiedAsUtc: true);
            
            Assert.Equal(Year2000UnixSeconds, result);
        }
        
        #endregion
        
        #region FromUnixTimeMilliseconds Tests
        
        [Fact]
        public void FromUnixTimeMilliseconds_ValidTimestamp_ReturnsUtcDateTime()
        {
            var result = TimeExtension.FromUnixTimeMilliseconds(Year2000UnixMilliseconds);
            
            Assert.Equal(Year2000DateTime, result);
            Assert.Equal(DateTimeKind.Utc, result.Kind);
        }
        
        [Fact]
        public void FromUnixTimeMilliseconds_ZeroTimestamp_ReturnsEpoch()
        {
            var result = TimeExtension.FromUnixTimeMilliseconds(0);
            
            Assert.Equal(UnixEpochDateTime, result);
            Assert.Equal(DateTimeKind.Utc, result.Kind);
        }
        
        [Fact]
        public void FromUnixTimeMilliseconds_WithLocalKind_ReturnsLocalDateTime()
        {
            var result = TimeExtension.FromUnixTimeMilliseconds(Year2000UnixMilliseconds, DateTimeKind.Local);
            
            Assert.Equal(DateTimeKind.Local, result.Kind);
            Assert.Equal(new DateTimeOffset(Year2000DateTime, TimeSpan.Zero).LocalDateTime, result);
        }
        
        [Fact]
        public void FromUnixTimeMilliseconds_WithUnspecifiedKind_ReturnsUnspecifiedDateTime()
        {
            var result = TimeExtension.FromUnixTimeMilliseconds(Year2000UnixMilliseconds, DateTimeKind.Unspecified);
            
            Assert.Equal(DateTimeKind.Unspecified, result.Kind);
        }
        
        [Fact]
        public void FromUnixTimeMilliseconds_NegativeTimestamp_ThrowsException()
        {
            var ex = Assert.Throws<ArgumentOutOfRangeException>(() => 
                TimeExtension.FromUnixTimeMilliseconds(-1));
            Assert.Contains("cannot be negative", ex.Message);
        }
        
        #endregion
        
        #region FromUnixTimeSeconds Tests
        
        [Fact]
        public void FromUnixTimeSeconds_ValidTimestamp_ReturnsUtcDateTime()
        {
            var result = TimeExtension.FromUnixTimeSeconds(Year2000UnixSeconds);
            
            Assert.Equal(Year2000DateTime, result);
            Assert.Equal(DateTimeKind.Utc, result.Kind);
        }
        
        [Fact]
        public void FromUnixTimeSeconds_ZeroTimestamp_ReturnsEpoch()
        {
            var result = TimeExtension.FromUnixTimeSeconds(0);
            
            Assert.Equal(UnixEpochDateTime, result);
            Assert.Equal(DateTimeKind.Utc, result.Kind);
        }
        
        [Fact]
        public void FromUnixTimeSeconds_NegativeTimestamp_ThrowsException()
        {
            var ex = Assert.Throws<ArgumentOutOfRangeException>(() => 
                TimeExtension.FromUnixTimeSeconds(-1));
            Assert.Contains("cannot be negative", ex.Message);
        }
        
        #endregion
        
        #region DateTimeOffset Extension Tests
        
        [Fact]
        public void ToUnixTimeMilliseconds_DateTimeOffset_ReturnsCorrectValue()
        {
            var dto = new DateTimeOffset(Year2000DateTime, TimeSpan.Zero);
            
            var result = dto.ToUnixTimeMilliseconds();
            
            Assert.Equal(Year2000UnixMilliseconds, result);
        }
        
        [Fact]
        public void ToUnixTimeSeconds_DateTimeOffset_ReturnsCorrectValue()
        {
            var dto = new DateTimeOffset(Year2000DateTime, TimeSpan.Zero);
            
            var result = dto.ToUnixTimeSeconds();
            
            Assert.Equal(Year2000UnixSeconds, result);
        }
        
        [Fact]
        public void FromUnixTimeMillisecondsToOffset_ValidTimestamp_ReturnsCorrectOffset()
        {
            var result = TimeExtension.FromUnixTimeMillisecondsToOffset(Year2000UnixMilliseconds);
            
            Assert.Equal(Year2000DateTime, result.UtcDateTime);
        }
        
        [Fact]
        public void FromUnixTimeSecondsToOffset_ValidTimestamp_ReturnsCorrectOffset()
        {
            var result = TimeExtension.FromUnixTimeSecondsToOffset(Year2000UnixSeconds);
            
            Assert.Equal(Year2000DateTime, result.UtcDateTime);
        }
        
        #endregion
        
        #region Round-trip Conversion Tests
        
        [Fact]
        public void RoundTrip_MillisecondsConversion_PreservesValue()
        {
            var original = Year2025DateTime;
            
            var unixTime = original.ToUnixTimeMilliseconds();
            var converted = TimeExtension.FromUnixTimeMilliseconds(unixTime);
            
            Assert.Equal(original, converted);
        }
        
        [Fact]
        public void RoundTrip_SecondsConversion_PreservesValueWithinSecondPrecision()
        {
            var original = new DateTime(2025, 1, 1, 12, 30, 45, DateTimeKind.Utc);
            
            var unixTime = original.ToUnixTimeSeconds();
            var converted = TimeExtension.FromUnixTimeSeconds(unixTime);
            
            // Should be equal to the second (milliseconds lost in conversion)
            Assert.Equal(original.Date, converted.Date);
            Assert.Equal(original.Hour, converted.Hour);
            Assert.Equal(original.Minute, converted.Minute);
            Assert.Equal(original.Second, converted.Second);
        }
        
        #endregion
        
        #region Edge Cases and Boundary Tests
        
        [Fact]
        public void ToUnixTimeMilliseconds_MaxDateTime_DoesNotOverflow()
        {
            var maxDate = new DateTime(9999, 12, 31, 23, 59, 59, 999, DateTimeKind.Utc);
            
            var result = maxDate.ToUnixTimeMilliseconds();
            
            Assert.True(result > 0);
            Assert.True(result < long.MaxValue);
        }
        
        [Fact]
        public void ToUnixTimeMilliseconds_Year2038_HandlesCorrectly()
        {
            // The Year 2038 problem for 32-bit systems
            var year2038 = new DateTime(2038, 1, 19, 3, 14, 7, DateTimeKind.Utc);
            
            var result = year2038.ToUnixTimeMilliseconds();
            
            Assert.Equal(2147483647000L, result); // Max 32-bit signed int in seconds * 1000
        }
        
        [Fact]
        public void ToUnixTimeMilliseconds_DifferentTimeZones_ProduceDifferentResults()
        {
            var localTime = new DateTime(2025, 6, 15, 12, 0, 0, DateTimeKind.Local);
            var utcTime = new DateTime(2025, 6, 15, 12, 0, 0, DateTimeKind.Utc);
            
            var localResult = localTime.ToUnixTimeMilliseconds();
            var utcResult = utcTime.ToUnixTimeMilliseconds();
            
            // Unless you're in UTC timezone, these should be different
            if (TimeZoneInfo.Local.BaseUtcOffset != TimeSpan.Zero)
            {
                Assert.NotEqual(localResult, utcResult);
            }
        }
        
        #endregion
        
        #region Legacy Support Tests
        
        [Fact]
        public void LegacyUnixTime_StillWorks_ButIsObsolete()
        {
            #pragma warning disable CS0618 // Type or member is obsolete
            var result = TimeExtension.UnixTime;
            #pragma warning restore CS0618
            
            var expected = TimeExtension.UnixTimeMillisecondsNow;
            
            // Should be very close (within a few milliseconds)
            Assert.InRange(result, expected - 100, expected + 100);
        }
        
        [Fact]
        public void LegacyToUnixTime_StillWorks_ButIsObsolete()
        {
            #pragma warning disable CS0618 // Type or member is obsolete
            var result = Year2000DateTime.ToUnixTime();
            #pragma warning restore CS0618
            
            Assert.Equal(Year2000UnixMilliseconds, result);
        }
        
        #endregion
    }
}