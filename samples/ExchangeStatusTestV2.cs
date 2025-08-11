using System;
using System.Threading.Tasks;
using CCXT.Collector.Core.Abstractions;
using CCXT.Collector.Binance;
using CCXT.Collector.Bittrex;
using CCXT.Collector.Coinbase;
using CCXT.Collector.Upbit;
using CCXT.Collector.Bithumb;
using CCXT.Collector.Models.WebSocket;

namespace CCXT.Collector.Samples
{
    public class ExchangeStatusTestV2
    {
        public static async Task RunTest()
        {
            Console.WriteLine("=====================================");
            Console.WriteLine("  📊 Exchange Status Management V2");
            Console.WriteLine("     (Self-Contained in Each Client)");
            Console.WriteLine("=====================================\n");

            // Test 1: Check status of various exchanges
            Console.WriteLine("1. Checking Exchange Status:");
            Console.WriteLine("-----------------------------");
            
            var exchanges = new IWebSocketClient[]
            {
                new BinanceWebSocketClient(),
                new UpbitWebSocketClient(),
                new BithumbWebSocketClient(),
                new CoinbaseWebSocketClient(),
                new BittrexWebSocketClient()
            };
            
            foreach (var client in exchanges)
            {
                CheckExchangeStatus(client);
                client.Dispose();
            }
            Console.WriteLine();

            // Test 2: Try to connect to an active exchange (Binance)
            Console.WriteLine("2. Testing Active Exchange (Binance):");
            Console.WriteLine("--------------------------------------");
            await TestExchangeConnection(new BinanceWebSocketClient());
            Console.WriteLine();

            // Test 3: Try to connect to a closed exchange (Bittrex)
            Console.WriteLine("3. Testing Closed Exchange (Bittrex):");
            Console.WriteLine("--------------------------------------");
            await TestExchangeConnection(new BittrexWebSocketClient());
            Console.WriteLine();

            // Test 4: Demonstrate setting maintenance mode
            Console.WriteLine("4. Simulating Maintenance Mode:");
            Console.WriteLine("--------------------------------");
            await TestMaintenanceMode();
            Console.WriteLine();

            Console.WriteLine("✅ Exchange status management V2 test completed!");
            Console.WriteLine("\nPress any key to exit...");
            Console.ReadKey();
        }

        private static void CheckExchangeStatus(IWebSocketClient client)
        {
            var statusIcon = client.Status switch
            {
                ExchangeStatus.Active => "✅",
                ExchangeStatus.Maintenance => "🔧",
                ExchangeStatus.Deprecated => "⚠️",
                ExchangeStatus.Closed => "⛔",
                _ => "❓"
            };
            
            Console.WriteLine($"   {statusIcon} {client.ExchangeName,-15} Status: {client.Status,-12} Active: {client.IsActive}");
            
            if (client.Status == ExchangeStatus.Closed)
            {
                Console.WriteLine($"      Message: {client.StatusMessage}");
                if (client.ClosedDate.HasValue)
                {
                    Console.WriteLine($"      Closed: {client.ClosedDate.Value:yyyy-MM-dd}");
                }
                if (client.AlternativeExchanges?.Count > 0)
                {
                    Console.WriteLine($"      Alternatives: {string.Join(", ", client.AlternativeExchanges)}");
                }
            }
        }

        private static async Task TestExchangeConnection(IWebSocketClient client)
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
                Console.WriteLine($"   Exchange: {client.ExchangeName}");
                Console.WriteLine($"   Status: {client.Status}");
                Console.WriteLine($"   IsActive: {client.IsActive}");
                Console.WriteLine($"   Attempting connection...");
                
                var connected = await client.ConnectAsync();
                
                if (!connected)
                {
                    if (errorReceived)
                    {
                        Console.WriteLine($"   ❌ Connection blocked: {errorMessage}");
                    }
                    else
                    {
                        Console.WriteLine($"   ❌ Failed to connect");
                    }
                }
                else
                {
                    Console.WriteLine($"   ✅ Successfully connected!");
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

        private static async Task TestMaintenanceMode()
        {
            // Create a custom test client that simulates maintenance
            var client = new TestMaintenanceClient();
            
            Console.WriteLine($"   Created test client: {client.ExchangeName}");
            Console.WriteLine($"   Initial status: {client.Status}");
            
            // Set to maintenance
            client.SimulateMaintenanceMode();
            Console.WriteLine($"   Changed status to: {client.Status}");
            Console.WriteLine($"   Status message: {client.StatusMessage}");
            
            // Try to connect
            Console.WriteLine($"   Attempting connection...");
            var connected = await client.ConnectAsync();
            Console.WriteLine($"   Connection result: {(connected ? "Connected" : "Blocked")}");
            
            client.Dispose();
        }
    }

    // Test client to demonstrate maintenance mode
    public class TestMaintenanceClient : WebSocketClientBase
    {
        public override string ExchangeName => "TestExchange";
        protected override string WebSocketUrl => "wss://test.example.com";
        protected override int PingIntervalMs => 30000;

        public void SimulateMaintenanceMode()
        {
            SetExchangeStatus(
                ExchangeStatus.Maintenance,
                "TestExchange is under scheduled maintenance until 14:00 UTC"
            );
        }

        protected override Task ProcessMessageAsync(string message, bool isPrivate = false)
        {
            return Task.CompletedTask;
        }

        public override Task<bool> SubscribeOrderbookAsync(string symbol) => Task.FromResult(true);
        public override Task<bool> SubscribeTradesAsync(string symbol) => Task.FromResult(true);
        public override Task<bool> SubscribeTickerAsync(string symbol) => Task.FromResult(true);
        public override Task<bool> SubscribeCandlesAsync(string symbol, string interval) => Task.FromResult(true);
        public override Task<bool> UnsubscribeAsync(string channel, string symbol) => Task.FromResult(true);
        protected override string CreatePingMessage() => "ping";
        protected override Task ResubscribeAsync(SubscriptionInfo subscription) => Task.CompletedTask;
    }
}