using CCXT.Collector.Samples.Base;
using System;
using System.Linq;
using System.Threading.Tasks;
using CCXT.Collector.Coinone;
using CCXT.Collector.Core.Abstractions;

namespace CCXT.Collector.Samples.Exchanges
{
    public class CoinoneSample : IExchangeSample
    {
        public string ExchangeName => "Coinone";

        public async Task SampleRun()
        {
            Console.WriteLine($"\n=== {ExchangeName} WebSocket Sample (Korean Exchange v2 API) ===\n");

            IWebSocketClient client = new CoinoneWebSocketClient();
            
            try
            {
                // Set up callbacks for Korean market
                client.OnOrderbookReceived += (orderbook) =>
                {
                    Console.WriteLine($"[{ExchangeName}] {orderbook.symbol} - Orderbook depth: {orderbook.result?.bids.Count + orderbook.result?.asks.Count ?? 0}");
                };

                client.OnTradeReceived += (trades) =>
                {
                    Console.WriteLine($"[{ExchangeName}] {trades.symbol} - Trade: ₩{trades.result?.FirstOrDefault()?.price ?? 0:N0} x {trades.result?.FirstOrDefault()?.quantity ?? 0:F4}");
                };

                client.OnTickerReceived += (ticker) =>
                {
                    decimal change = ticker.result?.closePrice ?? 0 - ticker.result?.prevClosePrice ?? 0;
                    string changeStr = change >= 0 ? $"+₩{change:N0}" : $"-₩{Math.Abs(change):N0}";
                    Console.WriteLine($"[{ExchangeName}] {ticker.symbol} - ₩{ticker.result?.closePrice ?? 0:N0} ({changeStr})");
                };

                // Connect with v2 API
                Console.WriteLine($"Connecting to {ExchangeName} v2 API...");
                bool connected = await client.ConnectAsync();
                
                if (!connected)
                {
                    Console.WriteLine($"Failed to connect to {ExchangeName}");
                    return;
                }

                Console.WriteLine($"Connected to {ExchangeName} v2!");

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