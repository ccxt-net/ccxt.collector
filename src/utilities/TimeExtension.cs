using System;
using System.Globalization;
using System.Linq;
using System.Text.Json;

public static class TimeExtension
{
    public static Int64 UnixTime =>
        DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
   
    public static Int64 ToUnixTime(this DateTime dateTime)
    {
        return new DateTimeOffset(dateTime).ToUnixTimeMilliseconds();
    }
}