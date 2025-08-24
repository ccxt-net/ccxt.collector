using System;
using System.Linq;
using System.Threading.Tasks;
using CCXT.Collector.Bithumb;
using CCXT.Collector.Core.Abstractions;
using CCXT.Collector.Samples.Base;

namespace CCXT.Collector.Samples.Exchanges
{
    public class BithumbSample : IExchangeSample
    {
        public string ExchangeName => "Bithumb";

        public async Task SampleRun()
        {
            Console.WriteLine($"\n=== {ExchangeName} WebSocket Sample (Korean Exchange) ===\n");

            IWebSocketClient client = new BithumbWebSocketClient();
            
            try
            {
                // Set up callbacks - focus on KRW pairs
                client.OnOrderbookReceived += (orderbook) =>
                {
                    var bestBid = orderbook.result?.bids.FirstOrDefault();
                    var bestAsk = orderbook.result?.asks.FirstOrDefault();
                    
                    if (bestBid != null && bestAsk != null)
                    {
                        Console.WriteLine($"[{ExchangeName}] {orderbook.symbol} Spread: {bestAsk.price - bestBid.price:F0} KRW (Bid: {bestBid.price:F0}, Ask: {bestAsk.price:F0})");
                    }
                };

                client.OnTradeReceived += (trades) =>
                {
                    string side = (trades.result?.FirstOrDefault()?.side ?? "") == "buy" ? "BUY " : "SELL";
                    Console.WriteLine($"[{ExchangeName}] {trades.symbol} {side} - Price: {trades.result?.FirstOrDefault()?.price ?? 0:F0} KRW, Amount: {trades.result?.FirstOrDefault()?.quantity ?? 0:F8} {trades.symbol.Split('/')[0]}");
                };

                client.OnTickerReceived += (ticker) =>
                {
                    decimal changePercent = (ticker.result?.closePrice ?? 0 - ticker.result?.prevClosePrice ?? 0) / ticker.result?.prevClosePrice ?? 0 * 100;
                    string changeStr = changePercent >= 0 ? $"+{changePercent:F2}%" : $"{changePercent:F2}%";
                    Console.WriteLine($"[{ExchangeName}] {ticker.symbol} - {ticker.result?.closePrice ?? 0:F0} KRW ({changeStr}), 24h Vol: {ticker.result?.volume ?? 0:F2}");
                };

                // Connect
                Console.WriteLine($"Connecting to {ExchangeName} (Korea)...");
                bool connected = await client.ConnectAsync();
                
                if (!connected)
                {
                    Console.WriteLine($"Failed to connect to {ExchangeName}");
                    return;
                }

                Console.WriteLine($"Connected to {ExchangeName}!");

                // Subscribe to major KRW pairs
                string[] symbols = { "BTC/KRW", "ETH/KRW", "XRP/KRW" };
                
                foreach (var symbol in symbols)
                {
                    Console.WriteLine($"Subscribing to {symbol}...");
                    
                    await client.SubscribeOrderbookAsync(symbol);
                    await client.SubscribeTradesAsync(symbol);
                    await client.SubscribeTickerAsync(symbol);
                    
                    await Task.Delay(300);
                }

                // Run for 25 seconds
                Console.WriteLine($"\nReceiving Korean market data for 25 seconds...\n");
                await Task.Delay(25000);

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