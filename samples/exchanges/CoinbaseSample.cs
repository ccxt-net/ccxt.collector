using CCXT.Collector.Samples.Base;
using System;
using System.Linq;
using System.Threading.Tasks;
using CCXT.Collector.Coinbase;
using CCXT.Collector.Core.Abstractions;

namespace CCXT.Collector.Samples.Exchanges
{
    public class CoinbaseSample : IExchangeSample
    {
        public string ExchangeName => "Coinbase";

        public async Task SampleRun()
        {
            Console.WriteLine($"\n=== {ExchangeName} WebSocket Sample (USD Market) ===\n");

            IWebSocketClient client = new CoinbaseWebSocketClient();
            
            try
            {
                // Set up callbacks with USD formatting
                client.OnOrderbookReceived += (orderbook) =>
                {
                    var topBids = orderbook.result?.bids.Take(3);
                    var topAsks = orderbook.result?.asks.Take(3);

                    if (topBids != null && topAsks != null)
                    {
                        Console.WriteLine($"[{ExchangeName}] {orderbook.symbol} Orderbook Update:");
                        Console.WriteLine($"  Top Bids: {string.Join(", ", topBids.Select(b => $"${b.price:F2}"))}");
                        Console.WriteLine($"  Top Asks: {string.Join(", ", topAsks.Select(a => $"${a.price:F2}"))}");
                    }
                };

                client.OnTradeReceived += (trades) =>
                {
                    string marker = (trades.result?.FirstOrDefault()?.side ?? "") == "buy" ? "↑" : "↓";
                    Console.WriteLine($"[{ExchangeName}] {trades.symbol} Trade {marker} ${trades.result?.FirstOrDefault()?.price ?? 0:F2} x {trades.result?.FirstOrDefault()?.quantity ?? 0:F6}");
                };

                client.OnTickerReceived += (ticker) =>
                {
                    Console.WriteLine($"[{ExchangeName}] {ticker.symbol} - Last: ${ticker.result?.closePrice ?? 0:F2}, 24h High: ${ticker.result?.highPrice ?? 0:F2}, Low: ${ticker.result?.lowPrice ?? 0:F2}");
                };

                // Connect
                Console.WriteLine($"Connecting to {ExchangeName} (level2_batch channel)...");
                bool connected = await client.ConnectAsync();
                
                if (!connected)
                {
                    Console.WriteLine($"Failed to connect to {ExchangeName}");
                    return;
                }

                Console.WriteLine($"Connected to {ExchangeName}!");

                // Subscribe to USD pairs
                string[] symbols = { "BTC/USD", "ETH/USD", "SOL/USD" };
                
                foreach (var symbol in symbols)
                {
                    Console.WriteLine($"Subscribing to {symbol}...");
                    
                    await client.SubscribeOrderbookAsync(symbol);
                    await client.SubscribeTradesAsync(symbol);
                    await client.SubscribeTickerAsync(symbol);
                    
                    await Task.Delay(200);
                }

                // Run for 20 seconds
                Console.WriteLine($"\nReceiving USD market data for 20 seconds...\n");
                await Task.Delay(20000);

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