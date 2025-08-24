using CCXT.Collector.Core.Abstractions;

namespace CCXT.Collector.Samples.Utilities
{
    /// <summary>
    /// Test runner for basic exchange connectivity and data reception tests
    /// </summary>
    public static class ExchangeTestRunner
    {
        /// <summary>
        /// Test all exchanges for basic connectivity and data reception
        /// </summary>
        public static async Task TestAllExchanges()
        {
            Console.WriteLine("Testing all 15 exchanges...\n");

            var exchanges = ExchangeRegistry.GetExchangeList();
            var results = new List<(string Name, bool Success, string Message)>();

            foreach (var exchange in exchanges)
            {
                Console.Write($"Testing {exchange.Name,-15} ");
                var result = await TestExchange(exchange.Name, exchange.CreateClient, exchange.Symbol);
                results.Add((exchange.Name, result.Success, result.Message));
                
                Console.WriteLine(result.Success ? "✅ PASS" : "❌ FAIL");
                if (!result.Success)
                {
                    Console.WriteLine($"  Error: {result.Message}");
                }
            }

            // Display summary
            Console.WriteLine("\n=== Test Summary ===");
            var successCount = results.FindAll(r => r.Success).Count;
            Console.WriteLine($"Passed: {successCount}/{results.Count}");
            
            if (successCount < results.Count)
            {
                Console.WriteLine("\nFailed exchanges:");
                foreach (var failed in results.FindAll(r => !r.Success))
                {
                    Console.WriteLine($"  • {failed.Name}: {failed.Message}");
                }
            }

            Console.WriteLine("\nPress any key to continue...");
            Console.ReadKey();
            Console.Clear();
        }

        /// <summary>
        /// Test a single exchange selected by user
        /// </summary>
        public static async Task TestSingleExchange()
        {
            var exchanges = ExchangeRegistry.GetExchangeList();
            
            Console.WriteLine("Available exchanges:");
            for (int i = 0; i < exchanges.Count; i++)
            {
                Console.WriteLine($"{i + 1,2}. {exchanges[i].Name}");
            }

            Console.Write("\nSelect exchange number: ");
            if (int.TryParse(Console.ReadLine(), out int selection) && 
                selection > 0 && selection <= exchanges.Count)
            {
                var exchange = exchanges[selection - 1];
                Console.WriteLine($"\nTesting {exchange.Name}...");
                
                var result = await TestExchange(exchange.Name, exchange.CreateClient, exchange.Symbol);
                
                if (result.Success)
                {
                    Console.WriteLine("✅ Connection successful!");
                    Console.WriteLine($"  • Orderbook: {result.OrderbookReceived}");
                    Console.WriteLine($"  • Trades: {result.TradesReceived}");
                    Console.WriteLine($"  • Ticker: {result.TickerReceived}");
                }
                else
                {
                    Console.WriteLine($"❌ Connection failed: {result.Message}");
                }
            }
            else
            {
                Console.WriteLine("Invalid selection.");
            }

            Console.WriteLine("\nPress any key to continue...");
            Console.ReadKey();
            Console.Clear();
        }

        /// <summary>
        /// Quick connectivity check for all exchanges
        /// </summary>
        public static async Task QuickConnectivityCheck()
        {
            Console.WriteLine("Running quick connectivity check...\n");

            var exchanges = ExchangeRegistry.GetExchangeList();
            var tasks = new List<Task<(string Name, bool Success)>>();

            foreach (var exchange in exchanges)
            {
                tasks.Add(Task.Run(async () =>
                {
                    try
                    {
                        var client = exchange.CreateClient();
                        var connected = await client.ConnectAsync();
                        if (connected)
                        {
                            await client.DisconnectAsync();
                            return (exchange.Name, true);
                        }
                        return (exchange.Name, false);
                    }
                    catch
                    {
                        return (exchange.Name, false);
                    }
                }));
            }

            var results = await Task.WhenAll(tasks);

            foreach (var result in results)
            {
                Console.WriteLine($"{result.Name,-15} {(result.Success ? "✅ Online" : "❌ Offline")}");
            }

            var successCount = results.Count(r => r.Success);
            Console.WriteLine($"\nOnline: {successCount}/{results.Length}");

            Console.WriteLine("\nPress any key to continue...");
            Console.ReadKey();
            Console.Clear();
        }

        /// <summary>
        /// Test exchange connectivity and data reception
        /// </summary>
        private static async Task<TestResult> TestExchange(string name, Func<IWebSocketClient> createClient, string symbol)
        {
            var result = new TestResult { Name = name };
            IWebSocketClient? client = null;

            try
            {
                client = createClient();
                
                // Set up callbacks
                bool orderbookReceived = false;
                bool tradeReceived = false;
                bool tickerReceived = false;

                client.OnOrderbookReceived += (_) => orderbookReceived = true;
                client.OnTradeReceived += (_) => tradeReceived = true;
                client.OnTickerReceived += (_) => tickerReceived = true;

                // Connect
                var connected = await client.ConnectAsync();
                if (!connected)
                {
                    result.Message = "Failed to connect";
                    return result;
                }

                // Subscribe to channels
                await client.SubscribeOrderbookAsync(symbol);
                await client.SubscribeTradesAsync(symbol);
                await client.SubscribeTickerAsync(symbol);

                // Wait for data (max 5 seconds)
                var timeout = DateTime.Now.AddSeconds(5);
                while (DateTime.Now < timeout)
                {
                    if (orderbookReceived && tradeReceived && tickerReceived)
                        break;
                    await Task.Delay(100);
                }

                result.Success = orderbookReceived || tradeReceived || tickerReceived;
                result.OrderbookReceived = orderbookReceived;
                result.TradesReceived = tradeReceived;
                result.TickerReceived = tickerReceived;

                if (!result.Success)
                {
                    result.Message = "No data received within timeout";
                }
            }
            catch (Exception ex)
            {
                result.Message = ex.Message;
            }
            finally
            {
                if (client != null)
                {
                    try { await client.DisconnectAsync(); } catch { }
                }
            }

            return result;
        }
    }
}