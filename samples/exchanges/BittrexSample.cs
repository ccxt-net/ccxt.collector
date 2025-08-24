using CCXT.Collector.Samples.Base;
using System;
using System.Linq;
using System.Threading.Tasks;
using CCXT.Collector.Bittrex;
using CCXT.Collector.Core.Abstractions;

namespace CCXT.Collector.Samples.Exchanges
{
    public class BittrexSample : IExchangeSample
    {
        public string ExchangeName => "Bittrex";

        public async Task SampleRun()
        {
            Console.WriteLine($"\n=== {ExchangeName} WebSocket Sample (SignalR Protocol) ===\n");

            IWebSocketClient client = new BittrexWebSocketClient();
            
            try
            {
                // Track connection events
                DateTime connectedAt = DateTime.Now;
                
                // Set up callbacks with connection timing
                client.OnOrderbookReceived += (orderbook) =>
                {
                    var elapsed = (DateTime.Now - connectedAt).TotalSeconds;
                    Console.WriteLine($"[{ExchangeName}] [{elapsed:F1}s] Orderbook: {orderbook.symbol} - Depth: {orderbook.result?.bids.Count + orderbook.result?.asks.Count ?? 0}");
                };

                client.OnTradeReceived += (trades) =>
                {
                    var elapsed = (DateTime.Now - connectedAt).TotalSeconds;
                    Console.WriteLine($"[{ExchangeName}] [{elapsed:F1}s] Trade: {trades.symbol} @ {trades.result?.FirstOrDefault()?.price ?? 0:F2} x {trades.result?.FirstOrDefault()?.quantity ?? 0:F4}");
                };

                client.OnTickerReceived += (ticker) =>
                {
                    var elapsed = (DateTime.Now - connectedAt).TotalSeconds;
                    Console.WriteLine($"[{ExchangeName}] [{elapsed:F1}s] Ticker: {ticker.symbol} - Bid: {ticker.result?.bidPrice ?? 0:F2}, Ask: {ticker.result?.askPrice ?? 0:F2}");
                };

                // Connect (SignalR connection)
                Console.WriteLine($"Connecting to {ExchangeName} via SignalR...");
                bool connected = await client.ConnectAsync();
                
                if (!connected)
                {
                    Console.WriteLine($"Failed to connect to {ExchangeName}");
                    return;
                }

                connectedAt = DateTime.Now;
                Console.WriteLine($"Connected to {ExchangeName} SignalR hub!");

                // Subscribe to a single symbol for cleaner output
                string symbol = "BTC/USDT";
                
                Console.WriteLine($"Subscribing to {symbol}...");
                await client.SubscribeOrderbookAsync(symbol);
                await client.SubscribeTradesAsync(symbol);
                await client.SubscribeTickerAsync(symbol);

                // Run for 15 seconds with status updates
                Console.WriteLine($"\nReceiving SignalR data for 15 seconds...\n");
                
                for (int i = 0; i < 15; i++)
                {
                    await Task.Delay(1000);
                    if (i % 5 == 4)
                    {
                        Console.WriteLine($"... {15 - i - 1} seconds remaining ...");
                    }
                }

                // Disconnect
                Console.WriteLine($"\nDisconnecting from {ExchangeName} SignalR...");
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