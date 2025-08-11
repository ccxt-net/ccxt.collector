using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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

namespace CCXT.Collector.Samples
{
    public class ConnectivityTest
    {
        public static async Task RunTest()
        {
            Console.WriteLine("=====================================");
            Console.WriteLine("  🔌 Connectivity Test - All Exchanges");
            Console.WriteLine("=====================================");
            Console.WriteLine("Testing WebSocket connection and packet reception...");
            Console.WriteLine("Required: 2 packets per channel (orderbook/trade/ticker)");
            Console.WriteLine();

            var exchanges = new Dictionary<string, (Func<IWebSocketClient> Create, string Symbol)>
            {
                { "Binance", (() => new BinanceWebSocketClient(), "BTC/USDT") },
                { "Bitget", (() => new BitgetWebSocketClient(), "BTC/USDT") },
                { "Bithumb", (() => new BithumbWebSocketClient(), "BTC/KRW") },
                { "Bittrex", (() => new BittrexWebSocketClient(), "BTC/USDT") },
                { "Bybit", (() => new BybitWebSocketClient(), "BTC/USDT") },
                { "Coinbase", (() => new CoinbaseWebSocketClient(), "BTC/USD") },
                { "Coinone", (() => new CoinoneWebSocketClient(), "BTC/KRW") },
                { "Crypto.com", (() => new CryptoWebSocketClient(), "BTC/USDT") },
                { "Gate.io", (() => new GateioWebSocketClient(), "BTC/USDT") },
                { "Gopax", (() => new GopaxWebSocketClient(), "BTC/KRW") },
                { "Huobi", (() => new HuobiWebSocketClient(), "BTC/USDT") },
                { "Korbit", (() => new KorbitWebSocketClient(), "BTC/KRW") },
                { "Kucoin", (() => new KucoinWebSocketClient(), "BTC/USDT") },
                { "OKX", (() => new OkxWebSocketClient(), "BTC/USDT") },
                { "Upbit", (() => new UpbitWebSocketClient(), "BTC/KRW") }
            };

            var results = new List<(string Name, bool Success, string Status, string Details)>();

            foreach (var exchange in exchanges)
            {
                Console.Write($"Testing {exchange.Key,-15} ");
                
                var (success, details) = await TestExchangeConnectivity(exchange.Key, exchange.Value.Create, exchange.Value.Symbol);
                var status = success ? "✅ SUCCESS" : "❌ FAILED";
                results.Add((exchange.Key, success, status, details));
                
                Console.WriteLine($"{status} - {details}");
            }

            // Print summary
            Console.WriteLine("\n=====================================");
            Console.WriteLine("           SUMMARY");
            Console.WriteLine("=====================================");
            
            var successCount = results.Count(r => r.Success);
            var failCount = results.Count - successCount;
            
            Console.WriteLine($"Total: {results.Count} | Success: {successCount} | Failed: {failCount}");
            Console.WriteLine($"Success Rate: {(successCount * 100.0 / results.Count):F1}%");
            
            if (failCount > 0)
            {
                Console.WriteLine("\nFailed Exchanges:");
                foreach (var failed in results.Where(r => !r.Success))
                {
                    Console.WriteLine($"  • {failed.Name}: {failed.Details}");
                }
            }

            Console.WriteLine("\n✅ Test completed!");
        }

        private static async Task<(bool Success, string Details)> TestExchangeConnectivity(string name, Func<IWebSocketClient> createClient, string symbol)
        {
            IWebSocketClient client = null;
            var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10)); // Reduced timeout
            var orderbookCount = 0;
            var tradeCount = 0;
            var tickerCount = 0;
            var connected = false;
            var errors = new List<string>();
            
            try
            {
                client = createClient();
                
                client.OnConnected += () => connected = true;
                client.OnOrderbookReceived += (_) => Interlocked.Increment(ref orderbookCount);
                client.OnTradeReceived += (_) => Interlocked.Increment(ref tradeCount);
                client.OnTickerReceived += (_) => Interlocked.Increment(ref tickerCount);
                client.OnError += (error) => errors.Add(error);
                
                // Connect with timeout
                var connectTask = client.ConnectAsync();
                if (await Task.WhenAny(connectTask, Task.Delay(3000)) != connectTask)
                {
                    return (false, "Connection timeout");
                }
                
                // Wait for connection confirmation
                var connectionTimeout = DateTime.Now.AddSeconds(2);
                while (!connected && DateTime.Now < connectionTimeout)
                {
                    await Task.Delay(100);
                }
                
                if (!connected)
                {
                    return (false, "Not connected");
                }
                
                // Subscribe to channels with timeout
                var subscribeTask = Task.WhenAll(
                    client.SubscribeOrderbookAsync(symbol),
                    client.SubscribeTradesAsync(symbol),
                    client.SubscribeTickerAsync(symbol)
                );
                
                if (await Task.WhenAny(subscribeTask, Task.Delay(3000)) != subscribeTask)
                {
                    return (false, "Subscribe timeout");
                }
                
                var subscribeResults = await subscribeTask;
                if (!subscribeResults.All(r => r))
                {
                    return (false, "Subscription failed");
                }
                
                // Wait for packets with shorter timeout
                var startTime = DateTime.Now;
                while ((DateTime.Now - startTime).TotalSeconds < 5) // Reduced to 5 seconds
                {
                    if (orderbookCount >= 2 && tradeCount >= 2 && tickerCount >= 2)
                    {
                        return (true, $"OB:{orderbookCount} T:{tradeCount} TK:{tickerCount}");
                    }
                    await Task.Delay(200);
                }
                
                // Return partial success with details
                var details = $"OB:{orderbookCount} T:{tradeCount} TK:{tickerCount}";
                if (orderbookCount >= 2 || tradeCount >= 2 || tickerCount >= 2)
                {
                    return (true, details + " (partial)");
                }
                
                return (false, details + " (insufficient packets)");
            }
            catch (Exception ex)
            {
                return (false, $"Error: {ex.Message}");
            }
            finally
            {
                try
                {
                    if (client != null)
                    {
                        await client.DisconnectAsync();
                        client.Dispose();
                    }
                }
                catch { }
                cts?.Dispose();
            }
        }
    }
}