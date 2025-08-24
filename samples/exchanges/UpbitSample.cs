using CCXT.Collector.Samples.Base;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using CCXT.Collector.Upbit;
using CCXT.Collector.Core.Abstractions;

namespace CCXT.Collector.Samples.Exchanges
{
    public class UpbitSample : IExchangeSample
    {
        public string ExchangeName => "Upbit";

        public async Task SampleRun()
        {
            Console.WriteLine($"\n=== {ExchangeName} WebSocket Sample (Korea's Largest Exchange) ===\n");

            IWebSocketClient client = new UpbitWebSocketClient();
            
            try
            {
                // Track statistics
                var stats = new Dictionary<string, int>();
                DateTime startTime = DateTime.Now;
                
                // Set up callbacks with statistics
                client.OnOrderbookReceived += (orderbook) =>
                {
                    stats[$"{orderbook.symbol}-orderbook"] = stats.GetValueOrDefault($"{orderbook.symbol}-orderbook", 0) + 1;
                    
                    // Show first few messages
                    if (stats[$"{orderbook.symbol}-orderbook"] <= 2)
                    {
                        var bestBid = orderbook.result?.bids.OrderByDescending(o => o.price).FirstOrDefault();
                        var bestAsk = orderbook.result?.asks.OrderBy(o => o.price).FirstOrDefault();
                        
                        if (bestBid != null && bestAsk != null)
                        {
                            Console.WriteLine($"[{ExchangeName}] {orderbook.symbol} - Spread: ₩{bestAsk.price - bestBid.price:N0}");
                        }
                    }
                };

                client.OnTradeReceived += (trades) =>
                {
                    stats[$"{trades.symbol}-trades"] = stats.GetValueOrDefault($"{trades.symbol}-trades", 0) + 1;
                    
                    // Show first few trades
                    if (stats[$"{trades.symbol}-trades"] <= 2)
                    {
                        Console.WriteLine($"[{ExchangeName}] {trades.symbol} - {(trades.result?.FirstOrDefault()?.side ?? "").ToUpper()} ₩{trades.result?.FirstOrDefault()?.price ?? 0:N0} x {trades.result?.FirstOrDefault()?.quantity ?? 0:F6}");
                    }
                };

                client.OnTickerReceived += (ticker) =>
                {
                    stats[$"{ticker.symbol}-ticker"] = stats.GetValueOrDefault($"{ticker.symbol}-ticker", 0) + 1;
                    
                    // Show first few tickers
                    if (stats[$"{ticker.symbol}-ticker"] <= 2)
                    {
                        decimal changeRate = ((ticker.result?.closePrice ?? 0 - ticker.result?.prevClosePrice ?? 0) / ticker.result?.prevClosePrice ?? 0) * 100;
                        Console.WriteLine($"[{ExchangeName}] {ticker.symbol} - ₩{ticker.result?.closePrice ?? 0:N0} ({changeRate:+0.00;-0.00;0}%)");
                    }
                };

                // Use batch subscription for Upbit
                Console.WriteLine($"Preparing batch subscriptions for {ExchangeName}...");
                
                string[] symbols = { "BTC/KRW", "ETH/KRW", "XRP/KRW", "SOL/KRW" };
                foreach (var symbol in symbols)
                {
                    client.AddSubscription("orderbook", symbol);
                    client.AddSubscription("trades", symbol);
                    client.AddSubscription("ticker", symbol);
                }
                
                Console.WriteLine($"Connecting to {ExchangeName} with batch mode...");
                bool connected = await client.ConnectAndSubscribeAsync();
                
                if (!connected)
                {
                    Console.WriteLine($"Failed to connect to {ExchangeName}");
                    return;
                }

                Console.WriteLine($"Connected to {ExchangeName}!");
                Console.WriteLine($"Subscribed to {symbols.Length} symbols x 3 channels = {symbols.Length * 3} subscriptions\n");

                // Run for 25 seconds
                Console.WriteLine("Receiving Korean market data for 25 seconds...");
                Console.WriteLine("(Showing first 2 messages per channel)\n");
                
                await Task.Delay(25000);

                // Calculate and display statistics
                var elapsed = (DateTime.Now - startTime).TotalSeconds;
                int totalMessages = stats.Values.Sum();
                
                Console.WriteLine($"\n=== {ExchangeName} Statistics ===");
                Console.WriteLine($"Duration: {elapsed:F1} seconds");
                Console.WriteLine($"Total messages: {totalMessages}");
                Console.WriteLine($"Average rate: {totalMessages / elapsed:F1} msg/sec");
                Console.WriteLine($"\nBreakdown by channel:");
                
                foreach (var symbol in symbols)
                {
                    Console.WriteLine($"  {symbol}:");
                    Console.WriteLine($"    Orderbooks: {stats.GetValueOrDefault($"{symbol}-orderbook", 0)}");
                    Console.WriteLine($"    Trades: {stats.GetValueOrDefault($"{symbol}-trades", 0)}");
                    Console.WriteLine($"    Tickers: {stats.GetValueOrDefault($"{symbol}-ticker", 0)}");
                }

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