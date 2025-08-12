using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace CCXT.Collector.Core.Infrastructure
{
    /// <summary>
    /// 공통 거래소 파싱/포맷 도우미 집합.
    /// 개별 거래소 WebSocketClient 구현에서 반복되는 심볼/인터벌/주문상태 파싱 로직을 중앙화.
    /// </summary>
    public static class ParsingHelpers
    {
        private static readonly HashSet<string> CommonQuoteCurrencies = new(StringComparer.OrdinalIgnoreCase)
        { "USDT", "USD", "USDC", "BTC", "ETH", "BNB", "BUSD", "KRW" };

        /// <summary>
        /// "BTC/USDT" 형태를 표준(대문자, 구분자 '/')으로 정규화.
        /// </summary>
        public static string NormalizeSymbol(string symbol)
        {
            if (string.IsNullOrWhiteSpace(symbol)) return symbol;
            symbol = symbol.Trim().Replace("-", "/").ToUpperInvariant();
            if (symbol.Contains('/')) return symbol;
            // Delimiter 없는 케이스 (예: BTCUSDT) 분리 시도
            foreach (var quote in CommonQuoteCurrencies)
            {
                if (symbol.EndsWith(quote, StringComparison.OrdinalIgnoreCase) && symbol.Length > quote.Length)
                {
                    var @base = symbol[..^quote.Length];
                    return @$"{@base}/{quote}".ToUpperInvariant();
                }
            }
            return symbol;
        }

        /// <summary>
        /// "BTC/USDT" -> "BTCUSDT" (Binance 등) 변환.
        /// </summary>
        public static string RemoveDelimiter(string symbol)
        {
            return NormalizeSymbol(symbol).Replace("/", string.Empty);
        }

        /// <summary>
        /// Upbit 형식 변환: "BTC/ KRW" -> "KRW-BTC".
        /// </summary>
        public static string ToUpbitCode(string symbol)
        {
            var norm = NormalizeSymbol(symbol);
            var parts = norm.Split('/');
            return parts.Length == 2 ? $"{parts[1]}-{parts[0]}" : symbol;
        }

        /// <summary>
        /// Upbit 코드 역변환: "KRW-BTC" -> "BTC/KRW".
        /// </summary>
        public static string FromUpbitCode(string code)
        {
            if (string.IsNullOrWhiteSpace(code)) return code;
            var parts = code.Split('-');
            return parts.Length == 2 ? $"{parts[1]}/{parts[0]}" : code;
        }

        /// <summary>
        /// 공통 인터벌 표준화 (모두 소문자). 1h, 60m 등 동등 처리.
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
        /// Binance Interval -> 표준.
        /// </summary>
        public static string FromBinanceInterval(string val) => NormalizeInterval(val);

        /// <summary>
        /// 표준 -> Binance Interval (대부분 동일, 월=1M 그대로 유지).
        /// </summary>
        public static string ToBinanceInterval(string val)
        {
            val = NormalizeInterval(val);
            return val;
        }

        /// <summary>
        /// Upbit interval 표준 -> Upbit unit.
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
        /// Upbit unit -> 표준 interval.
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
        /// 표준 심볼 -> DASH 심볼 (BTC/USDT -> BTC-USDT)
        /// </summary>
        public static string ToDashSymbol(string symbol)
            => NormalizeSymbol(symbol).Replace('/', '-');

        /// <summary>
        /// DASH 심볼 -> 표준 심볼 (BTC-USDT -> BTC/USDT)
        /// </summary>
        public static string FromDashSymbol(string symbol)
            => symbol?.Replace('-', '/') ?? symbol;

        /// <summary>
        /// Bitget 채널 인터벌 변환 (표준 -> 채널 suffix)
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
        /// Bitget channel 문자열(candle1H 등)에서 표준 인터벌 추출.
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
        /// 인터벌을 밀리초로.
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
                "1M" => 2_592_000_000L, // 30d 근사
                _ => 3_600_000L
            };
        }

        /// <summary>
        /// 공통 OrderType 문자열 -> Enum 매핑 (기본 Limit).
        /// 실제 Enum은 CCXT.Collector.Service 네임스페이스에 존재한다고 가정.
        /// reflection 없이 문자열 기준.
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
        /// 공통 OrderStatus 문자열 -> Enum 매핑.
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
