using CCXT.Collector.Core.Abstractions;

namespace CCXT.Collector.Samples.Utilities
{
    /// <summary>
    /// Test runner for multi-market data reception
    /// </summary>
    public static class MultiMarketTestRunner
    {
        /// <summary>
        /// Test multi-market data reception for all exchanges
        /// </summary>
        public static async Task TestMultiMarketDataReception()
        {
            Console.WriteLine("Testing multi-market data reception...\n");

            var exchanges = ExchangeRegistry.GetExchangeList();
            var results = new List<MultiMarketTestResult>();

            foreach (var exchange in exchanges)
            {
                Console.Write($"Testing {exchange.Name,-15} multi-market... ");
                var symbols = ExchangeRegistry.GetMultipleSymbols(exchange.Name);
                var result = await TestMultiMarket(exchange.Name, exchange.CreateClient, symbols);
                results.Add(result);

                Console.WriteLine(result.Success ? "✅ PASS" : "❌ FAIL");
                if (result.Success)
                {
                    Console.WriteLine($"  Markets: {string.Join(", ", result.MarketData.Select(m => $"{m.Key}: {m.Value}"))}");
                }
                else
                {
                    Console.WriteLine($"  Error: {result.Message}");
                }
            }

            // Display summary
            Console.WriteLine("\n=== Multi-Market Test Summary ===");
            var successCount = results.Count(r => r.Success);
            Console.WriteLine($"Passed: {successCount}/{results.Count}");

            if (successCount < results.Count)
            {
                Console.WriteLine("\nFailed exchanges:");
                foreach (var failed in results.Where(r => !r.Success))
                {
                    Console.WriteLine($"  • {failed.Name}: {failed.Message}");
                }
            }

            Console.WriteLine("\nPress any key to continue...");
            Console.ReadKey();
            Console.Clear();
        }

        /// <summary>
        /// Test multi-market data reception for a single exchange
        /// </summary>
        private static async Task<MultiMarketTestResult> TestMultiMarket(string name, Func<IWebSocketClient> createClient, string[] symbols)
        {
            var result = new MultiMarketTestResult { Name = name };
            IWebSocketClient? client = null;

            try
            {
                client = createClient();
                var marketData = new Dictionary<string, int>();

                // Initialize counters for each symbol
                foreach (var symbol in symbols)
                {
                    marketData[symbol] = 0;
                }

                // Set up callbacks
                client.OnOrderbookReceived += (orderbook) =>
                {
                    if (marketData.ContainsKey(orderbook.symbol))
                        marketData[orderbook.symbol]++;
                };

                client.OnTradeReceived += (trades) =>
                {
                    if (marketData.ContainsKey(trades.symbol))
                        marketData[trades.symbol]++;
                };

                client.OnTickerReceived += (ticker) =>
                {
                    if (marketData.ContainsKey(ticker.symbol))
                        marketData[ticker.symbol]++;
                };

                // Connect
                var connected = await client.ConnectAsync();
                if (!connected)
                {
                    result.Message = "Failed to connect";
                    return result;
                }

                // Subscribe to multiple markets
                foreach (var symbol in symbols)
                {
                    await client.SubscribeOrderbookAsync(symbol);
                    await client.SubscribeTradesAsync(symbol);
                    await client.SubscribeTickerAsync(symbol);
                    await Task.Delay(200); // Small delay between subscriptions
                }

                // Wait for data (max 7 seconds)
                var timeout = DateTime.Now.AddSeconds(7);
                while (DateTime.Now < timeout)
                {
                    if (marketData.All(m => m.Value > 0))
                        break;
                    await Task.Delay(100);
                }

                result.MarketData = marketData;
                result.Success = marketData.Any(m => m.Value > 0);

                if (!result.Success)
                {
                    result.Message = "No data received from any market";
                }
                else if (marketData.Any(m => m.Value == 0))
                {
                    var missingMarkets = marketData.Where(m => m.Value == 0).Select(m => m.Key);
                    result.Message = $"No data from: {string.Join(", ", missingMarkets)}";
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