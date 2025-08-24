using CCXT.Collector.Samples.Base;
using System;
using System.Linq;
using System.Threading.Tasks;
using CCXT.Collector.Gopax;
using CCXT.Collector.Core.Abstractions;

namespace CCXT.Collector.Samples.Exchanges
{
    public class GopaxSample : IExchangeSample
    {
        public string ExchangeName => "Gopax";

        public async Task SampleRun()
        {
            Console.WriteLine($"\n=== {ExchangeName} WebSocket Sample (Korean Exchange) ===\n");

            IWebSocketClient client = new GopaxWebSocketClient();
            
            try
            {
                // Set up callbacks
                client.OnOrderbookReceived += (orderbook) =>
                {
                    Console.WriteLine($"[{ExchangeName}] {orderbook.symbol} - Orderbook updated with {orderbook.result?.bids.Count + orderbook.result?.asks.Count ?? 0} levels");
                };

                client.OnTradeReceived += (trades) =>
                {
                    Console.WriteLine($"[{ExchangeName}] {trades.symbol} - {trades.result?.FirstOrDefault()?.side ?? ""} {trades.result?.FirstOrDefault()?.quantity ?? 0:F6} @ ₩{trades.result?.FirstOrDefault()?.price ?? 0:N0}");
                };

                client.OnTickerReceived += (ticker) =>
                {
                    Console.WriteLine($"[{ExchangeName}] {ticker.symbol} - ₩{ticker.result?.closePrice ?? 0:N0} (24h: ₩{ticker.result?.lowPrice ?? 0:N0} - ₩{ticker.result?.highPrice ?? 0:N0})");
                };

                // Connect
                Console.WriteLine($"Connecting to {ExchangeName}...");
                bool connected = await client.ConnectAsync();
                
                if (!connected)
                {
                    Console.WriteLine($"Failed to connect to {ExchangeName}");
                    return;
                }

                Console.WriteLine($"Connected to {ExchangeName}!");

                // Subscribe to KRW pairs
                string[] symbols = { "BTC/KRW", "ETH/KRW" };
                
                foreach (var symbol in symbols)
                {
                    Console.WriteLine($"Subscribing to {symbol}...");
                    
                    await client.SubscribeOrderbookAsync(symbol);
                    await client.SubscribeTradesAsync(symbol);
                    await client.SubscribeTickerAsync(symbol);
                    
                    await Task.Delay(300);
                }

                // Run for 15 seconds
                Console.WriteLine($"\nReceiving Korean market data for 15 seconds...\n");
                await Task.Delay(15000);

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