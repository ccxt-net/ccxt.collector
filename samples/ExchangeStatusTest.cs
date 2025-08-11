using System;
using System.Threading.Tasks;
using CCXT.Collector.Core.Abstractions;
using CCXT.Collector.Core.Infrastructure;
using CCXT.Collector.Binance;
using CCXT.Collector.Bittrex;
using CCXT.Collector.Coinbase;

namespace CCXT.Collector.Samples
{
    public class ExchangeStatusTest
    {
        public static async Task RunTest()
        {
            Console.WriteLine("=====================================");
            Console.WriteLine("  📊 Exchange Status Management Test");
            Console.WriteLine("=====================================\n");

            // Initialize the exchange registry
            ExchangeRegistry.Initialize();

            // Test 1: Check active exchanges
            Console.WriteLine("1. Active Exchanges:");
            Console.WriteLine("--------------------");
            var activeExchanges = ExchangeRegistry.GetActiveExchanges();
            foreach (var exchange in activeExchanges)
            {
                Console.WriteLine($"   ✅ {exchange}");
            }
            Console.WriteLine($"   Total: {activeExchanges.Count} active exchanges\n");

            // Test 2: Check closed exchanges
            Console.WriteLine("2. Closed Exchanges:");
            Console.WriteLine("--------------------");
            var closedExchanges = ExchangeRegistry.GetClosedExchanges();
            foreach (var exchange in closedExchanges)
            {
                Console.WriteLine($"   ⛔ {exchange.Name} - Closed on {exchange.ClosedDate:yyyy-MM-dd}");
                Console.WriteLine($"      Message: {exchange.StatusMessage}");
                if (exchange.AlternativeExchanges?.Count > 0)
                {
                    Console.WriteLine($"      Alternatives: {string.Join(", ", exchange.AlternativeExchanges)}");
                }
            }
            Console.WriteLine();

            // Test 3: Try to connect to an active exchange (Binance)
            Console.WriteLine("3. Testing Active Exchange (Binance):");
            Console.WriteLine("-------------------------------------");
            await TestExchangeConnection(new BinanceWebSocketClient(), "BTC/USDT");
            Console.WriteLine();

            // Test 4: Try to connect to a closed exchange (Bittrex)
            Console.WriteLine("4. Testing Closed Exchange (Bittrex):");
            Console.WriteLine("--------------------------------------");
            await TestExchangeConnection(new BittrexWebSocketClient(), "BTC/USDT");
            Console.WriteLine();

            // Test 5: Check specific exchange status
            Console.WriteLine("5. Checking Specific Exchange Status:");
            Console.WriteLine("--------------------------------------");
            CheckExchangeStatus("Coinbase");
            CheckExchangeStatus("Bittrex");
            CheckExchangeStatus("FTX");
            CheckExchangeStatus("UnknownExchange");
            Console.WriteLine();

            // Test 6: Simulate maintenance mode
            Console.WriteLine("6. Simulating Maintenance Mode:");
            Console.WriteLine("--------------------------------");
            ExchangeRegistry.UpdateExchangeStatus("Coinbase", ExchangeStatus.Maintenance, 
                "Coinbase is undergoing scheduled maintenance. Expected to be back online at 14:00 UTC.");
            
            Console.WriteLine("   Updated Coinbase to maintenance mode");
            await TestExchangeConnection(new CoinbaseWebSocketClient(), "BTC/USD");
            
            // Restore to active
            ExchangeRegistry.UpdateExchangeStatus("Coinbase", ExchangeStatus.Active);
            Console.WriteLine("   Restored Coinbase to active status\n");

            Console.WriteLine("✅ Exchange status management test completed!");
            Console.WriteLine("\nPress any key to exit...");
            Console.ReadKey();
        }

        private static async Task TestExchangeConnection(IWebSocketClient client, string symbol)
        {
            var errorReceived = false;
            var errorMessage = "";
            
            client.OnError += (msg) => {
                errorReceived = true;
                errorMessage = msg;
            };
            
            client.OnConnected += () => {
                Console.WriteLine($"   ✅ Connected to {client.ExchangeName}");
            };

            try
            {
                var connected = await client.ConnectAsync();
                
                if (!connected)
                {
                    if (errorReceived)
                    {
                        Console.WriteLine($"   ❌ Connection blocked: {errorMessage}");
                    }
                    else
                    {
                        Console.WriteLine($"   ❌ Failed to connect to {client.ExchangeName}");
                    }
                }
                else
                {
                    Console.WriteLine($"   ✅ Successfully connected to {client.ExchangeName}");
                    await client.DisconnectAsync();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"   ❌ Exception: {ex.Message}");
            }
            finally
            {
                client.Dispose();
            }
        }

        private static void CheckExchangeStatus(string exchangeName)
        {
            var info = ExchangeRegistry.GetExchangeInfo(exchangeName);
            var statusIcon = info.Status switch
            {
                ExchangeStatus.Active => "✅",
                ExchangeStatus.Maintenance => "🔧",
                ExchangeStatus.Deprecated => "⚠️",
                ExchangeStatus.Closed => "⛔",
                _ => "❓"
            };
            
            Console.WriteLine($"   {statusIcon} {exchangeName}: {info.Status} - {info.StatusMessage}");
        }
    }
}