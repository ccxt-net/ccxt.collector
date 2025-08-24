using CCXT.Collector.Samples.Base;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using CCXT.Collector.Bybit;
using CCXT.Collector.Core.Abstractions;

namespace CCXT.Collector.Samples.Exchanges
{
    public class BybitSample : IExchangeSample
    {
        public string ExchangeName => "Bybit";

        public async Task SampleRun()
        {
            Console.WriteLine($"\n=== {ExchangeName} WebSocket Sample (Batch Subscription) ===\n");

            IWebSocketClient client = new BybitWebSocketClient();
            
            try
            {
                // Track message statistics
                var messageStats = new Dictionary<string, int>();
                
                client.OnOrderbookReceived += (orderbook) =>
                {
                    string key = $"{orderbook.symbol}-orderbook";
                    messageStats[key] = messageStats.GetValueOrDefault(key, 0) + 1;
                    
                    if (messageStats[key] == 1) // First message only
                    {
                        Console.WriteLine($"[{ExchangeName}] ??Orderbook stream started for {orderbook.symbol}");
                    }
                };

                client.OnTradeReceived += (trades) =>
                {
                    string key = $"{trades.symbol}-trades";
                    messageStats[key] = messageStats.GetValueOrDefault(key, 0) + 1;
                    
                    if (messageStats[key] == 1) // First message only
                    {
                        Console.WriteLine($"[{ExchangeName}] ??Trade stream started for {trades.symbol}");
                    }
                };

                client.OnTickerReceived += (ticker) =>
                {
                    string key = $"{ticker.symbol}-ticker";
                    messageStats[key] = messageStats.GetValueOrDefault(key, 0) + 1;
                    
                    if (messageStats[key] == 1) // First message only
                    {
                        Console.WriteLine($"[{ExchangeName}] ??Ticker stream started for {ticker.symbol}");
                    }
                };

                // Use batch subscription
                Console.WriteLine($"Preparing batch subscriptions for {ExchangeName}...");
                
                string[] symbols = { "BTC/USDT", "ETH/USDT", "SOL/USDT" };
                foreach (var symbol in symbols)
                {
                    client.AddSubscription("orderbook", symbol);
                    client.AddSubscription("trades", symbol);
                    client.AddSubscription("ticker", symbol);
                }
                
                Console.WriteLine($"Connecting and subscribing to {ExchangeName} (batch mode)...");
                bool connected = await client.ConnectAndSubscribeAsync();
                
                if (!connected)
                {
                    Console.WriteLine($"Failed to connect to {ExchangeName}");
                    return;
                }

                Console.WriteLine($"Connected to {ExchangeName} with batch subscriptions!");
                Console.WriteLine($"Subscribed to {symbols.Length} symbols x 3 channels = {symbols.Length * 3} total subscriptions\n");

                // Monitor for 20 seconds
                Console.WriteLine("Monitoring data streams for 20 seconds...\n");
                await Task.Delay(20000);

                // Display statistics
                Console.WriteLine($"\n=== {ExchangeName} Statistics ===");
                foreach (var stat in messageStats.OrderBy(s => s.Key))
                {
                    Console.WriteLine($"  {stat.Key}: {stat.Value} messages");
                }
                
                int totalMessages = messageStats.Values.Sum();
                Console.WriteLine($"\nTotal messages received: {totalMessages}");
                Console.WriteLine($"Average rate: {totalMessages / 20.0:F1} messages/second");

                // Disconnect
                Console.WriteLine($"\nDisconnecting from {ExchangeName}...");
                await client.DisconnectAsync();
                Console.WriteLine($"Disconnected from {ExchangeName}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in {ExchangeName} sample: {ex.Message}");
            }
            finally
            {
                if (client != null)
                {
                    try { await client.DisconnectAsync(); } catch { }
                }
            }

            Console.WriteLine($"\n{ExchangeName} sample completed.");
        }
    }
}