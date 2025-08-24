using CCXT.Collector.Samples.Base;
using System;
using System.Linq;
using System.Threading.Tasks;
using CCXT.Collector.Korbit;
using CCXT.Collector.Core.Abstractions;

namespace CCXT.Collector.Samples.Exchanges
{
    public class KorbitSample : IExchangeSample
    {
        public string ExchangeName => "Korbit";

        public async Task SampleRun()
        {
            Console.WriteLine($"\n=== {ExchangeName} WebSocket Sample (Korean Exchange) ===\n");

            IWebSocketClient client = new KorbitWebSocketClient();
            
            try
            {
                // Set up callbacks with KRW formatting
                client.OnOrderbookReceived += (orderbook) =>
                {
                    var bestBid = orderbook.result?.bids.OrderByDescending(o => o.price).FirstOrDefault();
                    var bestAsk = orderbook.result?.asks.OrderBy(o => o.price).FirstOrDefault();
                    
                    if (bestBid != null && bestAsk != null)
                    {
                        Console.WriteLine($"[{ExchangeName}] {orderbook.symbol} - Best Bid: ₩{bestBid.price:N0}, Best Ask: ₩{bestAsk.price:N0}");
                    }
                };

                client.OnTradeReceived += (trades) =>
                {
                    Console.WriteLine($"[{ExchangeName}] {trades.symbol} - {(trades.result?.FirstOrDefault()?.side ?? "").ToUpper()} {trades.result?.FirstOrDefault()?.quantity ?? 0:F8} @ ₩{trades.result?.FirstOrDefault()?.price ?? 0:N0}");
                };

                client.OnTickerReceived += (ticker) =>
                {
                    decimal changePercent = ((ticker.result?.closePrice ?? 0 - ticker.result?.prevClosePrice ?? 0) / ticker.result?.prevClosePrice ?? 0) * 100;
                    Console.WriteLine($"[{ExchangeName}] {ticker.symbol} - ₩{ticker.result?.closePrice ?? 0:N0} ({changePercent:+0.00;-0.00;0}%)");
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
                string[] symbols = { "BTC/KRW", "ETH/KRW", "XRP/KRW" };
                
                foreach (var symbol in symbols)
                {
                    Console.WriteLine($"Subscribing to {symbol}...");
                    
                    await client.SubscribeOrderbookAsync(symbol);
                    await client.SubscribeTradesAsync(symbol);
                    await client.SubscribeTickerAsync(symbol);
                    
                    await Task.Delay(300);
                }

                // Run for 18 seconds
                Console.WriteLine($"\nReceiving Korean market data for 18 seconds...\n");
                await Task.Delay(18000);

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