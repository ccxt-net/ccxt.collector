using System;

/// <summary>
/// Time-related helper extensions for converting between DateTime and Unix epoch timestamps.
/// Provides consistent handling of UTC/Local time zones and millisecond/second precision.
/// </summary>
public static class TimeExtension
{
    /// <summary>
    /// Unix epoch start time (January 1, 1970 00:00:00 UTC)
    /// </summary>
    private static readonly DateTime UnixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    private static readonly DateTimeOffset UnixEpochOffset = new DateTimeOffset(UnixEpoch);
    
    /// <summary>
    /// Gets the current UTC time as Unix epoch milliseconds.
    /// </summary>
    public static long UnixTimeMillisecondsNow =>
        DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    
    /// <summary>
    /// Gets the current UTC time as Unix epoch seconds.
    /// </summary>
    public static long UnixTimeSecondsNow =>
        DateTimeOffset.UtcNow.ToUnixTimeSeconds();

    /// <summary>
    /// Gets the current UTC time as Unix epoch milliseconds (legacy property for backward compatibility).
    /// </summary>
    public static long UnixTime => UnixTimeMillisecondsNow;

    /// <summary>
    /// Converts a <see cref="DateTime"/> to Unix epoch milliseconds.
    /// </summary>
    /// <param name="dateTime">Date/time to convert.</param>
    /// <param name="treatUnspecifiedAsUtc">
    /// If true, treats DateTimeKind.Unspecified as UTC. 
    /// If false, treats it as Local time (default behavior for compatibility).
    /// </param>
    /// <returns>Unix epoch time in milliseconds.</returns>
    /// <exception cref="ArgumentException">Thrown when dateTime is before Unix epoch.</exception>
    public static long ToUnixTimeMilliseconds(this DateTime dateTime, bool treatUnspecifiedAsUtc = false)
    {
        DateTimeOffset dto;
        
        switch (dateTime.Kind)
        {
            case DateTimeKind.Utc:
                dto = new DateTimeOffset(dateTime, TimeSpan.Zero);
                break;
                
            case DateTimeKind.Local:
                dto = new DateTimeOffset(dateTime);
                break;
                
            case DateTimeKind.Unspecified:
                if (treatUnspecifiedAsUtc)
                {
                    // Treat as UTC
                    dto = new DateTimeOffset(dateTime, TimeSpan.Zero);
                }
                else
                {
                    // Treat as Local (default for backward compatibility)
                    dto = new DateTimeOffset(dateTime, TimeZoneInfo.Local.GetUtcOffset(dateTime));
                }
                break;
                
            default:
                throw new ArgumentException($"Unknown DateTimeKind: {dateTime.Kind}", nameof(dateTime));
        }
        
    if (dto < UnixEpochOffset)
        {
            throw new ArgumentException("DateTime cannot be before Unix epoch (1970-01-01)", nameof(dateTime));
        }
        
        return dto.ToUnixTimeMilliseconds();
    }

    /// <summary>
    /// Converts a <see cref="DateTime"/> to Unix epoch seconds.
    /// </summary>
    /// <param name="dateTime">Date/time to convert.</param>
    /// <param name="treatUnspecifiedAsUtc">
    /// If true, treats DateTimeKind.Unspecified as UTC. 
    /// If false, treats it as Local time (default behavior for compatibility).
    /// </param>
    /// <returns>Unix epoch time in seconds.</returns>
    /// <exception cref="ArgumentException">Thrown when dateTime is before Unix epoch.</exception>
    public static long ToUnixTimeSeconds(this DateTime dateTime, bool treatUnspecifiedAsUtc = false)
    {
        return ToUnixTimeMilliseconds(dateTime, treatUnspecifiedAsUtc) / 1000;
    }

    /// <summary>
    /// Converts a <see cref="DateTimeOffset"/> to Unix epoch milliseconds.
    /// </summary>
    /// <param name="dateTimeOffset">DateTimeOffset to convert.</param>
    /// <returns>Unix epoch time in milliseconds.</returns>
    public static long ToUnixTimeMilliseconds(this DateTimeOffset dateTimeOffset)
    {
        return dateTimeOffset.ToUnixTimeMilliseconds();
    }

    /// <summary>
    /// Converts a <see cref="DateTimeOffset"/> to Unix epoch seconds.
    /// </summary>
    /// <param name="dateTimeOffset">DateTimeOffset to convert.</param>
    /// <returns>Unix epoch time in seconds.</returns>
    public static long ToUnixTimeSeconds(this DateTimeOffset dateTimeOffset)
    {
        return dateTimeOffset.ToUnixTimeSeconds();
    }

    /// <summary>
    /// Creates a <see cref="DateTime"/> from Unix epoch milliseconds.
    /// </summary>
    /// <param name="unixTimeMilliseconds">Unix epoch time in milliseconds.</param>
    /// <param name="kind">The DateTimeKind for the resulting DateTime (default: UTC).</param>
    /// <returns>DateTime representing the Unix timestamp.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the timestamp is out of valid range.</exception>
    public static DateTime FromUnixTimeMilliseconds(long unixTimeMilliseconds, DateTimeKind kind = DateTimeKind.Utc)
    {
        if (unixTimeMilliseconds < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(unixTimeMilliseconds), "Unix time cannot be negative");
        }
        
        var dto = DateTimeOffset.FromUnixTimeMilliseconds(unixTimeMilliseconds);
        
        switch (kind)
        {
            case DateTimeKind.Utc:
                return dto.UtcDateTime;
                
            case DateTimeKind.Local:
                return dto.LocalDateTime;
                
            case DateTimeKind.Unspecified:
                return dto.DateTime;
                
            default:
                throw new ArgumentException($"Unknown DateTimeKind: {kind}", nameof(kind));
        }
    }

    /// <summary>
    /// Creates a <see cref="DateTime"/> from Unix epoch seconds.
    /// </summary>
    /// <param name="unixTimeSeconds">Unix epoch time in seconds.</param>
    /// <param name="kind">The DateTimeKind for the resulting DateTime (default: UTC).</param>
    /// <returns>DateTime representing the Unix timestamp.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the timestamp is out of valid range.</exception>
    public static DateTime FromUnixTimeSeconds(long unixTimeSeconds, DateTimeKind kind = DateTimeKind.Utc)
    {
        if (unixTimeSeconds < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(unixTimeSeconds), "Unix time cannot be negative");
        }
        
        var dto = DateTimeOffset.FromUnixTimeSeconds(unixTimeSeconds);
        
        switch (kind)
        {
            case DateTimeKind.Utc:
                return dto.UtcDateTime;
                
            case DateTimeKind.Local:
                return dto.LocalDateTime;
                
            case DateTimeKind.Unspecified:
                return dto.DateTime;
                
            default:
                throw new ArgumentException($"Unknown DateTimeKind: {kind}", nameof(kind));
        }
    }

    /// <summary>
    /// Creates a <see cref="DateTimeOffset"/> from Unix epoch milliseconds.
    /// </summary>
    /// <param name="unixTimeMilliseconds">Unix epoch time in milliseconds.</param>
    /// <returns>DateTimeOffset representing the Unix timestamp.</returns>
    public static DateTimeOffset FromUnixTimeMillisecondsToOffset(long unixTimeMilliseconds)
    {
        return DateTimeOffset.FromUnixTimeMilliseconds(unixTimeMilliseconds);
    }

    /// <summary>
    /// Creates a <see cref="DateTimeOffset"/> from Unix epoch seconds.
    /// </summary>
    /// <param name="unixTimeSeconds">Unix epoch time in seconds.</param>
    /// <returns>DateTimeOffset representing the Unix timestamp.</returns>
    public static DateTimeOffset FromUnixTimeSecondsToOffset(long unixTimeSeconds)
    {
        return DateTimeOffset.FromUnixTimeSeconds(unixTimeSeconds);
    }

    /// <summary>
    /// Converts a <see cref="DateTime"/> to Unix epoch milliseconds (legacy method for backward compatibility).
    /// Warning: This method treats Unspecified as Local time which may be unpredictable.
    /// </summary>
    /// <param name="dateTime">Date/time to convert.</param>
    /// <returns>Epoch milliseconds.</returns>
    [Obsolete("Use ToUnixTimeMilliseconds with explicit treatUnspecifiedAsUtc parameter for better control")]
    public static long ToUnixTime(this DateTime dateTime)
    {
        // Legacy behavior: let DateTimeOffset constructor handle it (treats Unspecified as Local)
        return new DateTimeOffset(dateTime).ToUnixTimeMilliseconds();
    }
}

namespace CCXT.Collector.Library
{
    /// <summary>
    /// Alias for global TimeExtension to allow namespace-qualified usage.
    /// All functionality is in the global TimeExtension class for backward compatibility.
    /// </summary>
    public static class TimeExtensionNamespaced
    {
        /// <summary>
        /// Gets the current UTC time as Unix epoch milliseconds.
        /// </summary>
        public static long UnixTimeMillisecondsNow => global::TimeExtension.UnixTimeMillisecondsNow;
        
        /// <summary>
        /// Gets the current UTC time as Unix epoch seconds.
        /// </summary>
        public static long UnixTimeSecondsNow => global::TimeExtension.UnixTimeSecondsNow;
    }
}