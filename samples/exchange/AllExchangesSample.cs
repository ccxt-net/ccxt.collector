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
using CCXT.Collector.Service;
using CCXT.Collector.Samples.Exchanges;

namespace CCXT.Collector.Samples
{
    /// <summary>
    /// Sample runner for all 15 completed exchanges
    /// </summary>
    public class AllExchangesSample
    {
        public static async Task RunMenu()
        {
            while (true)
            {
                Console.Clear();
                Console.WriteLine("==============================================");
                Console.WriteLine("   CCXT.Collector - All Exchanges Sample");
                Console.WriteLine("==============================================");
                Console.WriteLine("\n📊 Select Exchange to Test:\n");
                
                // Global Exchanges
                Console.WriteLine("🌍 Global Exchanges:");
                Console.WriteLine("  1. Binance      - World's largest exchange");
                Console.WriteLine("  2. OKX          - Leading derivatives platform");
                Console.WriteLine("  3. Gate.io      - Diverse altcoin selection");
                Console.WriteLine("  4. KuCoin       - The People's Exchange");
                Console.WriteLine("  5. Huobi        - Major Asian exchange");
                Console.WriteLine("  6. Bybit        - Derivatives specialist");
                Console.WriteLine("  7. Bitget       - Copy trading leader");
                
                // US Exchanges
                Console.WriteLine("\n🇺🇸 US Exchanges:");
                Console.WriteLine("  8. Coinbase     - US regulated exchange");
                Console.WriteLine("  9. Crypto.com   - All-in-one platform");
                Console.WriteLine(" 10. Bittrex      - US exchange (SignalR)");
                
                // Korean Exchanges
                Console.WriteLine("\n🇰🇷 Korean Exchanges:");
                Console.WriteLine(" 11. Upbit        - Korea's largest");
                Console.WriteLine(" 12. Bithumb      - High volume KRW");
                Console.WriteLine(" 13. Coinone      - Korean pioneer");
                Console.WriteLine(" 14. Korbit       - Korea's first");
                
                // Special Options
                Console.WriteLine("\n⚡ Special Tests:");
                Console.WriteLine(" 15. Multi-Exchange - Test 3 exchanges simultaneously");
                Console.WriteLine(" 16. All Korean     - Test all KRW markets");
                Console.WriteLine(" 17. Top 5 Global   - Test top 5 by volume");
                Console.WriteLine(" 18. 🔌 Connectivity Test - Test all 15 exchanges connectivity");
                Console.WriteLine(" 19. 📊 Live Monitor - Monitor all exchanges real-time");
                
                Console.WriteLine("\n  0. Back to Main Menu");
                Console.WriteLine("\n==============================================");
                Console.Write("\nEnter your choice (0-19): ");
                
                // Clear any buffered input before reading
                while (Console.KeyAvailable)
                {
                    Console.ReadKey(true);
                }
                
                var choice = Console.ReadLine();
                
                try
                {
                    switch (choice)
                    {
                        case "1":
                            await BinanceSample.RunSample();
                            break;
                        case "2":
                            await OkxExample.RunSample();
                            break;
                        case "3":
                            await GateioExample.RunSample();
                            break;
                        case "4":
                            await KucoinExample.RunSample();
                            break;
                        case "5":
                            await HuobiExample.RunSample();
                            break;
                        case "6":
                            await BybitExample.RunSample();
                            break;
                        case "7":
                            await BitgetExample.RunSample();
                            break;
                        case "8":
                            await CoinbaseExample.RunSample();
                            break;
                        case "9":
                            await CryptocomExample.RunSample();
                            break;
                        case "10":
                            await BittrexExample.RunSample();
                            break;
                        case "11":
                            await UpbitSample.RunSample();
                            break;
                        case "12":
                            await BithumbSample.RunSample();
                            break;
                        case "13":
                            await CoinoneExample.RunSample();
                            break;
                        case "14":
                            await KorbitExample.RunSample();
                            break;
                        case "15":
                            await RunMultiExchangeTest();
                            break;
                        case "16":
                            await RunAllKoreanExchanges();
                            break;
                        case "17":
                            await RunTop5Global();
                            break;
                        case "18":
                            await RunConnectivityTest();
                            break;
                        case "19":
                            await RunLiveMonitor();
                            break;
                        case "0":
                            return;
                        default:
                            Console.WriteLine("\n❌ Invalid choice. Please try again.");
                            break;
                    }
                    
                    if (choice != "0")
                    {
                        Console.WriteLine("\n✅ Test completed. Press any key to continue...");
                        
                        // Clear any buffered input before waiting for key
                        while (Console.KeyAvailable)
                        {
                            Console.ReadKey(true);
                        }
                        
                        Console.ReadKey();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"\n❌ Error: {ex.Message}");
                    Console.WriteLine("Press any key to continue...");
                    
                    // Clear any buffered input before waiting for key
                    while (Console.KeyAvailable)
                    {
                        Console.ReadKey(true);
                    }
                    
                    Console.ReadKey();
                }
            }
        }
        
        private static async Task RunMultiExchangeTest()
        {
            Console.WriteLine("\n=== Multi-Exchange Test ===");
            Console.WriteLine("Testing Binance, OKX, and Coinbase simultaneously...\n");
            
            var tasks = new Task[]
            {
                Task.Run(async () => 
                {
                    Console.WriteLine("[1/3] Starting Binance...");
                    await BinanceSample.RunSample();
                }),
                Task.Run(async () => 
                {
                    Console.WriteLine("[2/3] Starting OKX...");
                    await OkxExample.RunSample();
                }),
                Task.Run(async () => 
                {
                    Console.WriteLine("[3/3] Starting Coinbase...");
                    await CoinbaseExample.RunSample();
                })
            };
            
            await Task.WhenAll(tasks);
            Console.WriteLine("\n✅ Multi-exchange test completed!");
        }
        
        private static async Task RunAllKoreanExchanges()
        {
            Console.WriteLine("\n=== All Korean Exchanges Test ===");
            Console.WriteLine("Testing all KRW markets...\n");
            
            Console.WriteLine("[1/4] Testing Upbit...");
            await UpbitSample.RunSample();
            
            Console.WriteLine("\n[2/4] Testing Bithumb...");
            await BithumbSample.RunSample();
            
            Console.WriteLine("\n[3/4] Testing Coinone...");
            await CoinoneExample.RunSample();
            
            Console.WriteLine("\n[4/4] Testing Korbit...");
            await KorbitExample.RunSample();
            
            Console.WriteLine("\n✅ All Korean exchanges tested!");
        }
        
        private static async Task RunTop5Global()
        {
            Console.WriteLine("\n=== Top 5 Global Exchanges Test ===");
            Console.WriteLine("Testing top exchanges by volume...\n");
            
            Console.WriteLine("[1/5] Testing Binance...");
            await BinanceSample.RunSample();
            
            Console.WriteLine("\n[2/5] Testing OKX...");
            await OkxExample.RunSample();
            
            Console.WriteLine("\n[3/5] Testing Bybit...");
            await BybitExample.RunSample();
            
            Console.WriteLine("\n[4/5] Testing Gate.io...");
            await GateioExample.RunSample();
            
            Console.WriteLine("\n[5/5] Testing KuCoin...");
            await KucoinExample.RunSample();
            
            Console.WriteLine("\n✅ Top 5 global exchanges tested!");
        }

        private static async Task RunConnectivityTest()
        {
            Console.Clear();
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

            var results = new List<(string Name, bool Success, string Status)>();

            foreach (var exchange in exchanges)
            {
                Console.Write($"Testing {exchange.Key,-15} ");
                
                var success = await TestExchangeConnectivity(exchange.Key, exchange.Value.Create, exchange.Value.Symbol);
                var status = success ? "✅ SUCCESS" : "❌ FAILED";
                results.Add((exchange.Key, success, status));
                
                Console.WriteLine(status);
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
                    Console.WriteLine($"  • {failed.Name}");
                }
            }
        }

        private static async Task<bool> TestExchangeConnectivity(string name, Func<IWebSocketClient> createClient, string symbol)
        {
            IWebSocketClient? client = null;
            var cts = new CancellationTokenSource(TimeSpan.FromSeconds(20));
            var orderbookCount = 0;
            var tradeCount = 0;
            var tickerCount = 0;
            var connected = false;
            
            try
            {
                client = createClient();
                
                client.OnConnected += () => connected = true;
                client.OnOrderbookReceived += (_) => orderbookCount++;
                client.OnTradeReceived += (_) => tradeCount++;
                client.OnTickerReceived += (_) => tickerCount++;
                
                await client.ConnectAsync();
                await Task.Delay(500);
                
                if (!connected) return false;
                
                await client.SubscribeOrderbookAsync(symbol);
                await client.SubscribeTradesAsync(symbol);
                await client.SubscribeTickerAsync(symbol);
                
                // Wait for packets
                var startTime = DateTime.Now;
                while ((DateTime.Now - startTime).TotalSeconds < 15)
                {
                    if (orderbookCount >= 2 && tradeCount >= 2 && tickerCount >= 2)
                        return true;
                    await Task.Delay(500);
                }
                
                return false;
            }
            catch
            {
                return false;
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

        private static async Task RunLiveMonitor()
        {
            Console.Clear();
            Console.WriteLine("=====================================");
            Console.WriteLine("  📊 Live Monitor - All Exchanges");
            Console.WriteLine("=====================================");
            Console.WriteLine("Connecting to all exchanges...");
            Console.WriteLine("Press 'Q' to quit\n");
            
            var exchanges = new Dictionary<string, (Func<IWebSocketClient> Create, string Symbol)>
            {
                { "Binance", (() => new BinanceWebSocketClient(), "BTC/USDT") },
                { "OKX", (() => new OkxWebSocketClient(), "BTC/USDT") },
                { "Bybit", (() => new BybitWebSocketClient(), "BTC/USDT") },
                { "Gate.io", (() => new GateioWebSocketClient(), "BTC/USDT") },
                { "Huobi", (() => new HuobiWebSocketClient(), "BTC/USDT") },
                { "Coinbase", (() => new CoinbaseWebSocketClient(), "BTC/USD") },
                { "Upbit", (() => new UpbitWebSocketClient(), "BTC/KRW") },
                { "Bithumb", (() => new BithumbWebSocketClient(), "BTC/KRW") }
            };
            
            var clients = new Dictionary<string, (IWebSocketClient Client, decimal LastPrice, DateTime LastUpdate)>();
            var cts = new CancellationTokenSource();
            
            try
            {
                // Connect to exchanges
                foreach (var exchange in exchanges)
                {
                    try
                    {
                        var client = exchange.Value.Create();
                        var exchangeName = exchange.Key;
                        
                        client.OnTickerReceived += (ticker) =>
                        {
                            if (clients.ContainsKey(exchangeName))
                            {
                                var current = clients[exchangeName];
                                clients[exchangeName] = (current.Client, ticker.result.closePrice, DateTime.Now);
                            }
                        };
                        
                        await client.ConnectAsync();
                        await client.SubscribeTickerAsync(exchange.Value.Symbol);
                        
                        clients[exchangeName] = (client, 0, DateTime.Now);
                        Console.WriteLine($"✓ Connected to {exchangeName}");
                    }
                    catch
                    {
                        Console.WriteLine($"✗ Failed to connect to {exchange.Key}");
                    }
                }
                
                // Display loop
                _ = Task.Run(async () =>
                {
                    while (!cts.Token.IsCancellationRequested)
                    {
                        Console.SetCursorPosition(0, 12);
                        Console.WriteLine($"\n{"Exchange",-15} {"Symbol",-10} {"Price",12} {"Status",-10}");
                        Console.WriteLine(new string('-', 50));
                        
                        foreach (var kvp in clients.OrderBy(x => x.Key))
                        {
                            var timeSinceUpdate = (DateTime.Now - kvp.Value.LastUpdate).TotalSeconds;
                            var status = timeSinceUpdate < 5 ? "🟢 Active" : "🔴 Inactive";
                            var symbol = exchanges[kvp.Key].Symbol;
                            
                            Console.WriteLine($"{kvp.Key,-15} {symbol,-10} {kvp.Value.LastPrice,12:F2} {status,-10}");
                        }
                        
                        await Task.Delay(1000);
                    }
                });
                
                // Wait for user to quit
                while (true)
                {
                    var key = Console.ReadKey(true);
                    if (key.KeyChar == 'q' || key.KeyChar == 'Q')
                    {
                        cts.Cancel();
                        break;
                    }
                }
            }
            finally
            {
                foreach (var client in clients.Values)
                {
                    try
                    {
                        await client.Client.DisconnectAsync();
                        client.Client.Dispose();
                    }
                    catch { }
                }
                cts.Dispose();
            }
        }
    }
}