using CCXT.Collector.Core.Abstractions;
using CCXT.Collector.Binance;
using CCXT.Collector.Bitget;
using CCXT.Collector.Bithumb;
using CCXT.Collector.Bittrex;
using CCXT.Collector.Bybit;
using CCXT.Collector.Coinbase;
using CCXT.Collector.Coinone;
using CCXT.Collector.Crypto;
using CCXT.Collector.Gateio;
using CCXT.Collector.Gopax;
using CCXT.Collector.Huobi;
using CCXT.Collector.Korbit;
using CCXT.Collector.Kucoin;
using CCXT.Collector.Okx;
using CCXT.Collector.Upbit;
using CCXT.Collector.Samples.Base;
using CCXT.Collector.Samples.Exchanges;

namespace CCXT.Collector.Samples.Utilities
{
    /// <summary>
    /// Central registry for all exchange configurations and factory methods
    /// </summary>
    public static class ExchangeRegistry
    {
        /// <summary>
        /// Get list of all exchanges with their factory methods and default symbols
        /// </summary>
        public static List<(string Name, Func<IWebSocketClient> CreateClient, string Symbol)> GetExchangeList()
        {
            return new List<(string, Func<IWebSocketClient>, string)>
            {
                ("Binance", () => new BinanceWebSocketClient(), "BTC/USDT"),
                ("Bitget", () => new BitgetWebSocketClient(), "BTC/USDT"),
                ("Bithumb", () => new BithumbWebSocketClient(), "BTC/KRW"),
                ("Bittrex", () => new BittrexWebSocketClient(), "BTC/USDT"),
                ("Bybit", () => new BybitWebSocketClient(), "BTC/USDT"),
                ("Coinbase", () => new CoinbaseWebSocketClient(), "BTC/USD"),
                ("Coinone", () => new CoinoneWebSocketClient(), "BTC/KRW"),
                ("Crypto.com", () => new CryptoWebSocketClient(), "BTC/USDT"),
                ("Gate.io", () => new GateioWebSocketClient(), "BTC/USDT"),
                ("Gopax", () => new GopaxWebSocketClient(), "BTC/KRW"),
                ("Huobi", () => new HuobiWebSocketClient(), "BTC/USDT"),
                ("Korbit", () => new KorbitWebSocketClient(), "BTC/KRW"),
                ("Kucoin", () => new KucoinWebSocketClient(), "BTC/USDT"),
                ("OKX", () => new OkxWebSocketClient(), "BTC/USDT"),
                ("Upbit", () => new UpbitWebSocketClient(), "BTC/KRW")
            };
        }

        /// <summary>
        /// Get list of exchanges that support batch subscriptions
        /// </summary>
        public static List<(string Name, Func<IWebSocketClient> CreateClient, string[] Symbols)> GetBatchSupportedExchanges()
        {
            return new List<(string, Func<IWebSocketClient>, string[])>
            {
                ("Upbit", () => new UpbitWebSocketClient(), new[] { "BTC/KRW", "ETH/KRW" }),
                ("Bithumb", () => new BithumbWebSocketClient(), new[] { "BTC/KRW", "ETH/KRW" }),
                ("Binance", () => new BinanceWebSocketClient(), new[] { "BTC/USDT", "ETH/USDT" }),
                ("Bitget", () => new BitgetWebSocketClient(), new[] { "BTC/USDT", "ETH/USDT" }),
                ("Coinbase", () => new CoinbaseWebSocketClient(), new[] { "BTC/USD", "ETH/USD" }),
                ("OKX", () => new OkxWebSocketClient(), new[] { "BTC/USDT", "ETH/USDT" }),
                ("Huobi", () => new HuobiWebSocketClient(), new[] { "BTC/USDT", "ETH/USDT" }),
                ("Crypto.com", () => new CryptoWebSocketClient(), new[] { "BTC/USDT", "ETH/USDT" }),
                ("Gate.io", () => new GateioWebSocketClient(), new[] { "BTC/USDT", "ETH/USDT" }),
                ("Korbit", () => new KorbitWebSocketClient(), new[] { "BTC/KRW", "ETH/KRW" }),
                ("Coinone", () => new CoinoneWebSocketClient(), new[] { "BTC/KRW", "ETH/KRW" })
            };
        }

        /// <summary>
        /// Get multiple symbols for multi-market testing based on exchange
        /// </summary>
        public static string[] GetMultipleSymbols(string exchangeName)
        {
            return exchangeName switch
            {
                "Upbit" or "Bithumb" or "Korbit" or "Coinone" or "Gopax" => new[] { "BTC/KRW", "ETH/KRW", "XRP/KRW" },
                "Coinbase" => new[] { "BTC/USD", "ETH/USD", "SOL/USD" },
                _ => new[] { "BTC/USDT", "ETH/USDT", "SOL/USDT" }
            };
        }

        /// <summary>
        /// Get list of all exchange sample implementations
        /// </summary>
        public static List<IExchangeSample> GetExchangeSamples()
        {
            return new List<IExchangeSample>
            {
                new BinanceSample(),
                new BitgetSample(),
                new BithumbSample(),
                new BittrexSample(),
                new BybitSample(),
                new CoinbaseSample(),
                new CoinoneSample(),
                new CryptoSample(),
                new GateioSample(),
                new GopaxSample(),
                new HuobiSample(),
                new KorbitSample(),
                new KucoinSample(),
                new OkxSample(),
                new UpbitSample()
            };
        }
    }
}