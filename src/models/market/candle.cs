using System;
using System.Collections.Generic;

namespace CCXT.Collector.Service
{
    /// <summary>
    /// Candlestick/K-Line data structure (캔들 데이터)
    /// </summary>
    public class SCandle
    {
        /// <summary>
        /// Exchange name
        /// </summary>
        public string exchange { get; set; }

        /// <summary>
        /// Trading symbol (e.g., "BTC/USDT")
        /// </summary>
        public string symbol { get; set; }

        /// <summary>
        /// Candle interval (1m, 5m, 15m, 30m, 1h, 4h, 1d, 1w, 1M)
        /// </summary>
        public string interval { get; set; }

        /// <summary>
        /// Timestamp when data was received
        /// </summary>
        public long timestamp { get; set; }

        /// <summary>
        /// Candle data list
        /// </summary>
        public List<SCandleItem> result { get; set; }
    }

    /// <summary>
    /// Individual candle data (OHLCV)
    /// </summary>
    public class SCandleItem
    {
        /// <summary>
        /// Candle open time
        /// </summary>
        public long openTime { get; set; }

        /// <summary>
        /// Candle close time
        /// </summary>
        public long closeTime { get; set; }

        /// <summary>
        /// Open price
        /// </summary>
        public decimal open { get; set; }

        /// <summary>
        /// High price
        /// </summary>
        public decimal high { get; set; }

        /// <summary>
        /// Low price
        /// </summary>
        public decimal low { get; set; }

        /// <summary>
        /// Close price
        /// </summary>
        public decimal close { get; set; }

        /// <summary>
        /// Volume in base currency
        /// </summary>
        public decimal volume { get; set; }

        /// <summary>
        /// Volume in quote currency
        /// </summary>
        public decimal quoteVolume { get; set; }

        /// <summary>
        /// Number of trades
        /// </summary>
        public long tradeCount { get; set; }

        /// <summary>
        /// Is this candle closed/complete
        /// </summary>
        public bool isClosed { get; set; }

        /// <summary>
        /// Buy volume (taker buy)
        /// </summary>
        public decimal buyVolume { get; set; }

        /// <summary>
        /// Buy quote volume
        /// </summary>
        public decimal buyQuoteVolume { get; set; }
    }

    /// <summary>
    /// Candle interval enum
    /// </summary>
    public enum CandleInterval
    {
        OneMinute,      // 1m
        ThreeMinutes,   // 3m
        FiveMinutes,    // 5m
        FifteenMinutes, // 15m
        ThirtyMinutes,  // 30m
        OneHour,        // 1h
        TwoHours,       // 2h
        FourHours,      // 4h
        SixHours,       // 6h
        EightHours,     // 8h
        TwelveHours,    // 12h
        OneDay,         // 1d
        ThreeDays,      // 3d
        OneWeek,        // 1w
        OneMonth        // 1M
    }

    /// <summary>
    /// Helper class for candle interval conversion
    /// </summary>
    public static class CandleIntervalHelper
    {
        /// <summary>
        /// Convert enum to string representation
        /// </summary>
        public static string ToString(CandleInterval interval)
        {
            return interval switch
            {
                CandleInterval.OneMinute => "1m",
                CandleInterval.ThreeMinutes => "3m",
                CandleInterval.FiveMinutes => "5m",
                CandleInterval.FifteenMinutes => "15m",
                CandleInterval.ThirtyMinutes => "30m",
                CandleInterval.OneHour => "1h",
                CandleInterval.TwoHours => "2h",
                CandleInterval.FourHours => "4h",
                CandleInterval.SixHours => "6h",
                CandleInterval.EightHours => "8h",
                CandleInterval.TwelveHours => "12h",
                CandleInterval.OneDay => "1d",
                CandleInterval.ThreeDays => "3d",
                CandleInterval.OneWeek => "1w",
                CandleInterval.OneMonth => "1M",
                _ => "1h"
            };
        }

        /// <summary>
        /// Parse string to enum
        /// </summary>
        public static CandleInterval Parse(string interval)
        {
            return interval?.ToLower() switch
            {
                "1m" or "1min" => CandleInterval.OneMinute,
                "3m" or "3min" => CandleInterval.ThreeMinutes,
                "5m" or "5min" => CandleInterval.FiveMinutes,
                "15m" or "15min" => CandleInterval.FifteenMinutes,
                "30m" or "30min" => CandleInterval.ThirtyMinutes,
                "1h" or "60m" or "1hour" => CandleInterval.OneHour,
                "2h" or "2hour" => CandleInterval.TwoHours,
                "4h" or "4hour" => CandleInterval.FourHours,
                "6h" or "6hour" => CandleInterval.SixHours,
                "8h" or "8hour" => CandleInterval.EightHours,
                "12h" or "12hour" => CandleInterval.TwelveHours,
                "1d" or "24h" or "1day" => CandleInterval.OneDay,
                "3d" or "3day" => CandleInterval.ThreeDays,
                "1w" or "7d" or "1week" => CandleInterval.OneWeek,
                "1M" or "30d" or "1month" => CandleInterval.OneMonth,
                _ => CandleInterval.OneHour
            };
        }

        /// <summary>
        /// Get interval in milliseconds
        /// </summary>
        public static long ToMilliseconds(CandleInterval interval)
        {
            return interval switch
            {
                CandleInterval.OneMinute => 60000,
                CandleInterval.ThreeMinutes => 180000,
                CandleInterval.FiveMinutes => 300000,
                CandleInterval.FifteenMinutes => 900000,
                CandleInterval.ThirtyMinutes => 1800000,
                CandleInterval.OneHour => 3600000,
                CandleInterval.TwoHours => 7200000,
                CandleInterval.FourHours => 14400000,
                CandleInterval.SixHours => 21600000,
                CandleInterval.EightHours => 28800000,
                CandleInterval.TwelveHours => 43200000,
                CandleInterval.OneDay => 86400000,
                CandleInterval.ThreeDays => 259200000,
                CandleInterval.OneWeek => 604800000,
                CandleInterval.OneMonth => 2592000000,
                _ => 3600000
            };
        }
    }
}