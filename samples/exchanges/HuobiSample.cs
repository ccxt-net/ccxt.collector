using CCXT.Collector.Samples.Base;
using System;
using System.Linq;
using System.Threading.Tasks;
using CCXT.Collector.Huobi;
using CCXT.Collector.Core.Abstractions;

namespace CCXT.Collector.Samples.Exchanges
{
    public class HuobiSample : IExchangeSample
    {
        public string ExchangeName => "Huobi";

        public async Task SampleRun()
        {
            Console.WriteLine($"\n=== {ExchangeName} WebSocket Sample (Compressed Data) ===\n");

            IWebSocketClient client = new HuobiWebSocketClient();
            
            try
            {
                // Track compression stats
                int compressedMessages = 0;
                
                // Set up callbacks
                client.OnOrderbookReceived += (orderbook) =>
                {
                    compressedMessages++;
                    if (compressedMessages <= 5)
                    {
                        Console.WriteLine($"[{ExchangeName}] {orderbook.symbol} - Orderbook (compressed msg #{compressedMessages})");
                    }
                };

                client.OnTradeReceived += (trades) =>
                {
                    Console.WriteLine($"[{ExchangeName}] {trades.symbol} - {trades.result?.FirstOrDefault()?.side ?? ""} trade: {trades.result?.FirstOrDefault()?.quantity ?? 0:F4} @ ${trades.result?.FirstOrDefault()?.price ?? 0:F2}");
                };

                client.OnTickerReceived += (ticker) =>
                {
                    Console.WriteLine($"[{ExchangeName}] {ticker.symbol} - Ticker: ${ticker.result?.closePrice ?? 0:F2} (Vol: {ticker.result?.volume ?? 0:F0})");
                };

                client.OnCandleReceived += (candle) =>
                {
                    Console.WriteLine($"[{ExchangeName}] {candle.symbol} - 1m Candle: O:{candle.result?.FirstOrDefault()?.open ?? 0:F2} H:{candle.result?.FirstOrDefault()?.high ?? 0:F2} L:{candle.result?.FirstOrDefault()?.low ?? 0:F2} C:{candle.result?.FirstOrDefault()?.close ?? 0:F2}");
                };

                // Connect
                Console.WriteLine($"Connecting to {ExchangeName} (with gzip compression)...");
                bool connected = await client.ConnectAsync();
                
                if (!connected)
                {
                    Console.WriteLine($"Failed to connect to {ExchangeName}");
                    return;
                }

                Console.WriteLine($"Connected to {ExchangeName}!");
                Console.WriteLine("Note: Huobi uses gzip compression for WebSocket messages\n");

                // Subscribe to pairs
                string[] symbols = { "BTC/USDT", "ETH/USDT" };
                
                foreach (var symbol in symbols)
                {
                    Console.WriteLine($"Subscribing to {symbol}...");
                    
                    await client.SubscribeOrderbookAsync(symbol);
                    await client.SubscribeTradesAsync(symbol);
                    await client.SubscribeTickerAsync(symbol);
                    // await client.SubscribeCandleAsync(symbol, "1m"); // Not implemented yet
                    
                    await Task.Delay(200);
                }

                // Run for 20 seconds
                Console.WriteLine($"\nReceiving compressed data for 20 seconds...\n");
                await Task.Delay(20000);

                Console.WriteLine($"\nTotal compressed messages processed: {compressedMessages}");

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