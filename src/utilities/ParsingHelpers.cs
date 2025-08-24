using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace CCXT.Collector.Core.Infrastructure
{
    /// <summary>
    /// Common exchange parsing/formatting helpers.
    /// Centralizes symbol/interval/order-status parsing logic shared by exchange WebSocketClient implementations.
    /// </summary>
    public static class ParsingHelpers
    {
        private static readonly HashSet<string> CommonQuoteCurrencies = new(StringComparer.OrdinalIgnoreCase)
        { "USDT", "USD", "USDC", "BTC", "ETH", "BNB", "BUSD", "KRW" };

        /// <summary>
        /// Normalize a symbol to the standard uppercase form with '/' delimiter (e.g., "BTC/USDT").
        /// </summary>
        public static string NormalizeSymbol(string symbol)
        {
            if (string.IsNullOrWhiteSpace(symbol)) return symbol;
            symbol = symbol.Trim().Replace("-", "/").ToUpperInvariant();
            if (symbol.IndexOf('/') >= 0) return symbol;
            // Attempt to split base/quote for no-delimiter cases (e.g., BTCUSDT)
            foreach (var quote in CommonQuoteCurrencies)
            {
                if (symbol.EndsWith(quote, StringComparison.OrdinalIgnoreCase) && symbol.Length > quote.Length)
                {
                    var @base = symbol.Substring(0, symbol.Length - quote.Length);
                    return @$"{@base}/{quote}".ToUpperInvariant();
                }
            }
            return symbol;
        }

        /// <summary>
        /// Convert "BTC/USDT" -> "BTCUSDT" (e.g., for Binance APIs).
        /// </summary>
        public static string RemoveDelimiter(string symbol)
        {
            return NormalizeSymbol(symbol).Replace("/", string.Empty);
        }

        /// <summary>
        /// Upbit format: "BTC/KRW" -> "KRW-BTC".
        /// </summary>
        public static string ToUpbitCode(string symbol)
        {
            var norm = NormalizeSymbol(symbol);
            var parts = norm.Split('/');
            return parts.Length == 2 ? $"{parts[1]}-{parts[0]}" : symbol;
        }

        /// <summary>
        /// Upbit code to standard: "KRW-BTC" -> "BTC/KRW".
        /// </summary>
        public static string FromUpbitCode(string code)
        {
            if (string.IsNullOrWhiteSpace(code)) return code;
            var parts = code.Split('-');
            return parts.Length == 2 ? $"{parts[1]}/{parts[0]}" : code;
        }

        /// <summary>
        /// Normalize interval to a common lowercase standard; treat equivalents like 1h == 60m.
        /// </summary>
        public static string NormalizeInterval(string interval)
        {
            if (string.IsNullOrWhiteSpace(interval)) return interval;
            interval = interval.Trim().ToLowerInvariant();
            return interval switch
            {
                "60m" => "1h",
                "24h" => "1d",
                "7d" => "1w",
                "30d" => "1M",
                _ => interval
            };
        }

        /// <summary>
        /// Binance interval to standard.
        /// </summary>
        public static string FromBinanceInterval(string val) => NormalizeInterval(val);

        /// <summary>
        /// Standard to Binance interval (mostly identical; month remains 1M).
        /// </summary>
        public static string ToBinanceInterval(string val)
        {
            val = NormalizeInterval(val);
            return val;
        }

        /// <summary>
        /// Standard interval to Upbit unit code.
        /// </summary>
        public static string ToUpbitIntervalUnit(string interval)
        {
            interval = NormalizeInterval(interval);
            return interval switch
            {
                "1m" => "1",
                "3m" => "3",
                "5m" => "5",
                "10m" => "10",
                "15m" => "15",
                "30m" => "30",
                "1h" => "60",
                "4h" => "240",
                "1d" => "D",
                "1w" => "W",
                "1M" => "M",
                _ => "60"
            };
        }

        /// <summary>
        /// Upbit unit code to standard interval.
        /// </summary>
        public static string FromUpbitIntervalUnit(string unit) => unit switch
        {
            "1" => "1m",
            "3" => "3m",
            "5" => "5m",
            "10" => "10m",
            "15" => "15m",
            "30" => "30m",
            "60" => "1h",
            "240" => "4h",
            "D" => "1d",
            "W" => "1w",
            "M" => "1M",
            _ => "1h"
        };

        /// <summary>
        /// Standard symbol -> dash-delimited symbol (BTC/USDT -> BTC-USDT)
        /// </summary>
        public static string ToDashSymbol(string symbol)
            => NormalizeSymbol(symbol).Replace('/', '-');

        /// <summary>
        /// Dash-delimited symbol -> standard symbol (BTC-USDT -> BTC/USDT)
        /// </summary>
        public static string FromDashSymbol(string symbol)
            => symbol?.Replace('-', '/') ?? symbol;

        /// <summary>
        /// Bitget channel interval (standard -> channel suffix)
        /// </summary>
        public static string ToBitgetChannelInterval(string interval)
        {
            interval = NormalizeInterval(interval);
            return interval switch
            {
                "1m" => "1m",
                "5m" => "5m",
                "15m" => "15m",
                "30m" => "30m",
                "1h" => "1H",
                "4h" => "4H",
                "1d" => "1D",
                "1w" => "1W",
                _ => "1m"
            };
        }

        /// <summary>
    /// Parse standard interval from a Bitget channel string (e.g., candle1H).
        /// </summary>
        public static string FromBitgetChannelInterval(string channel)
        {
            if (string.IsNullOrEmpty(channel)) return "1m";
            var raw = channel.Replace("candle", "");
            return raw switch
            {
                "1m" => "1m",
                "5m" => "5m",
                "15m" => "15m",
                "30m" => "30m",
                "1H" => "1h",
                "4H" => "4h",
                "1D" => "1d",
                "1W" => "1w",
                _ => "1m"
            };
        }

        /// <summary>
    /// Convert an interval to milliseconds.
        /// </summary>
        public static long IntervalToMilliseconds(string interval)
        {
            interval = NormalizeInterval(interval);
            return interval switch
            {
                "1m" => 60_000L,
                "3m" => 180_000L,
                "5m" => 300_000L,
                "10m" => 600_000L,
                "15m" => 900_000L,
                "30m" => 1_800_000L,
                "1h" => 3_600_000L,
                "2h" => 7_200_000L,
                "4h" => 14_400_000L,
                "6h" => 21_600_000L,
                "8h" => 28_800_000L,
                "12h" => 43_200_000L,
                "1d" => 86_400_000L,
                "3d" => 259_200_000L,
                "1w" => 604_800_000L,
        "1M" => 2_592_000_000L, // approx. 30 days
                _ => 3_600_000L
            };
        }

        /// <summary>
    /// Map common order type string to enum (defaults to Limit).
    /// Assumes the enum is defined under CCXT.Collector.Service namespace.
    /// String-based mapping, no reflection.
        /// </summary>
        public static CCXT.Collector.Service.OrderType ParseGenericOrderType(string type)
        {
            if (string.IsNullOrWhiteSpace(type)) return CCXT.Collector.Service.OrderType.Limit;
            return type.ToUpperInvariant() switch
            {
                "LIMIT" => CCXT.Collector.Service.OrderType.Limit,
                "MARKET" => CCXT.Collector.Service.OrderType.Market,
                "STOP" or "STOP_LOSS" => CCXT.Collector.Service.OrderType.Stop,
                "STOP_LIMIT" or "STOP_LOSS_LIMIT" => CCXT.Collector.Service.OrderType.StopLimit,
                "TAKE_PROFIT" => CCXT.Collector.Service.OrderType.TakeProfit,
                "TAKE_PROFIT_LIMIT" => CCXT.Collector.Service.OrderType.TakeProfitLimit,
                _ => CCXT.Collector.Service.OrderType.Limit
            };
        }

        /// <summary>
    /// Map common order status string to enum.
        /// </summary>
        public static CCXT.Collector.Service.OrderStatus ParseGenericOrderStatus(string status)
        {
            if (string.IsNullOrWhiteSpace(status)) return CCXT.Collector.Service.OrderStatus.New;
            status = status.ToUpperInvariant();
            return status switch
            {
                "LIVE" => CCXT.Collector.Service.OrderStatus.Open,
                "CREATED" => CCXT.Collector.Service.OrderStatus.New,
                "SUBMITTED" => CCXT.Collector.Service.OrderStatus.Open,
                "PARTIAL-FILLED" => CCXT.Collector.Service.OrderStatus.PartiallyFilled,
                "PARTIAL_FILLED" => CCXT.Collector.Service.OrderStatus.PartiallyFilled,
                "PARTIAL-CANCELED" => CCXT.Collector.Service.OrderStatus.Canceled,
                "PARTIAL_CANCELED" => CCXT.Collector.Service.OrderStatus.Canceled,
                "CANCELLED" => CCXT.Collector.Service.OrderStatus.Canceled,
                "NEW" => CCXT.Collector.Service.OrderStatus.New,
                "OPEN" or "WAIT" or "WATCH" => CCXT.Collector.Service.OrderStatus.Open,
                "PARTIALLY_FILLED" or "PARTIAL" => CCXT.Collector.Service.OrderStatus.PartiallyFilled,
                "FILLED" or "DONE" => CCXT.Collector.Service.OrderStatus.Filled,
                "CANCELED" or "CANCEL" => CCXT.Collector.Service.OrderStatus.Canceled,
                "REJECTED" => CCXT.Collector.Service.OrderStatus.Rejected,
                "EXPIRED" => CCXT.Collector.Service.OrderStatus.Expired,
                _ => CCXT.Collector.Service.OrderStatus.Open
            };
        }

        // OKX interval helpers (standard <-> channel piece)
        public static string ToOkxInterval(string interval)
        {
            interval = NormalizeInterval(interval);
            return interval switch
            {
                "1m" => "1m",
                "3m" => "3m",
                "5m" => "5m",
                "15m" => "15m",
                "30m" => "30m",
                "1h" => "1H",
                "2h" => "2H",
                "4h" => "4H",
                "6h" => "6H",
                "12h" => "12H",
                "1d" => "1D",
                "1w" => "1W",
                "1M" => "1M",
                _ => "1m"
            };
        }

        public static string FromOkxInterval(string channel)
        {
            if (string.IsNullOrEmpty(channel)) return "1m";
            var raw = channel.Replace("candle", "");
            return raw switch
            {
                "1m" => "1m",
                "3m" => "3m",
                "5m" => "5m",
                "15m" => "15m",
                "30m" => "30m",
                "1H" => "1h",
                "2H" => "2h",
                "4H" => "4h",
                "6H" => "6h",
                "12H" => "12h",
                "1D" => "1d",
                "1W" => "1w",
                "1M" => "1M",
                _ => "1m"
            };
        }

        // Bybit interval helpers (standard <-> code)
        public static string ToBybitIntervalCode(string interval)
        {
            interval = NormalizeInterval(interval);
            return interval switch
            {
                "1m" => "1",
                "3m" => "3",
                "5m" => "5",
                "15m" => "15",
                "30m" => "30",
                "1h" => "60",
                "2h" => "120",
                "4h" => "240",
                "6h" => "360",
                "12h" => "720",
                "1d" => "D",
                "1w" => "W",
                "1M" => "M",
                _ => "1"
            };
        }

        public static string FromBybitIntervalCode(string code)
        {
            return code switch
            {
                "1" => "1m",
                "3" => "3m",
                "5" => "5m",
                "15" => "15m",
                "30" => "30m",
                "60" => "1h",
                "120" => "2h",
                "240" => "4h",
                "360" => "6h",
                "720" => "12h",
                "D" => "1d",
                "W" => "1w",
                "M" => "1M",
                _ => "1m"
            };
        }

        // Huobi interval helpers
        public static string ToHuobiInterval(string interval)
        {
            interval = NormalizeInterval(interval);
            return interval switch
            {
                "1m" => "1min",
                "5m" => "5min",
                "15m" => "15min",
                "30m" => "30min",
                "1h" => "60min",
                "4h" => "4hour",
                "1d" => "1day",
                "1w" => "1week",
                "1M" => "1mon",
                _ => "1min"
            };
        }

        public static string FromHuobiInterval(string val)
        {
            return val switch
            {
                "1min" => "1m",
                "5min" => "5m",
                "15min" => "15m",
                "30min" => "30m",
                "60min" => "1h",
                "4hour" => "4h",
                "1day" => "1d",
                "1week" => "1w",
                "1mon" => "1M",
                _ => "1m"
            };
        }

        // Bittrex interval helpers (standard <-> code)
        public static string ToBittrexInterval(string interval)
        {
            interval = NormalizeInterval(interval);
            return interval switch
            {
                "1m" => "MINUTE_1",
                "5m" => "MINUTE_5",
                "15m" => "MINUTE_15",
                "30m" => "MINUTE_30",
                "1h" => "HOUR_1",
                "4h" => "HOUR_4",
                "1d" => "DAY_1",
                _ => "MINUTE_1"
            };
        }

        public static string FromBittrexInterval(string code) => code switch
        {
            "MINUTE_1" => "1m",
            "MINUTE_5" => "5m",
            "MINUTE_15" => "15m",
            "MINUTE_30" => "30m",
            "HOUR_1" => "1h",
            "HOUR_4" => "4h",
            "DAY_1" => "1d",
            _ => "1m"
        };

        // Gate.io intervals (mostly identical, passthrough for validation)
        public static string ToGateioInterval(string interval) => NormalizeInterval(interval);
        public static string FromGateioInterval(string interval) => NormalizeInterval(interval);

        // Crypto.com interval helpers
        public static string ToCryptoInterval(string interval)
        {
            interval = NormalizeInterval(interval);
            return interval switch
            {
                "1m" => "1M",
                "5m" => "5M",
                "15m" => "15M",
                "30m" => "30M",
                "1h" => "1H",
                "4h" => "4H",
                "6h" => "6H",
                "12h" => "12H",
                "1d" => "1D",
                "1w" => "7D",
                "2w" => "14D",
                "1M" => "1M",
                _ => "1M"
            };
        }

        public static string FromCryptoInterval(string code) => code switch
        {
            "1M" => "1m",
            "5M" => "5m",
            "15M" => "15m",
            "30M" => "30m",
            "1H" => "1h",
            "4H" => "4h",
            "6H" => "6h",
            "12H" => "12h",
            "1D" => "1d",
            "7D" => "1w",
            "14D" => "2w",
            _ => "1m"
        };

        // Underscore symbol helpers (e.g., BTC_USDT)
        public static string ToUnderscoreSymbol(string symbol) => NormalizeSymbol(symbol).Replace('/', '_');
        public static string FromUnderscoreSymbol(string symbol) => symbol?.Replace('_', '/') ?? symbol;
    }
}
