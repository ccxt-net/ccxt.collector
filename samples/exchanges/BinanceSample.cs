using System;
using System.Linq;
using System.Threading.Tasks;
using CCXT.Collector.Binance;
using CCXT.Collector.Core.Abstractions;
using CCXT.Collector.Samples.Base;

namespace CCXT.Collector.Samples.Exchanges
{
    public class BinanceSample : IExchangeSample
    {
        public string ExchangeName => "Binance";

        public async Task SampleRun()
        {
            Console.WriteLine($"\n=== {ExchangeName} WebSocket Sample ===\n");

            IWebSocketClient client = new BinanceWebSocketClient();
            
            try
            {
                // Set up callbacks
                client.OnOrderbookReceived += (orderbook) =>
                {
                    if (orderbook.result != null)
                    {
                        Console.WriteLine($"[{ExchangeName}] Orderbook: {orderbook.symbol} - Bids: {orderbook.result.bids.Count}, Asks: {orderbook.result.asks.Count}");
                    }
                };

                client.OnTradeReceived += (trades) =>
                {
                    if (trades.result != null && trades.result.Count > 0)
                    {
                        var trade = trades.result.First();
                        Console.WriteLine($"[{ExchangeName}] Trade: {trades.symbol} - Price: {trade.price}, Amount: {trade.quantity}");
                    }
                };

                client.OnTickerReceived += (ticker) =>
                {
                    if (ticker.result != null)
                    {
                        Console.WriteLine($"[{ExchangeName}] Ticker: {ticker.symbol} - Last: {ticker.result.closePrice}, Volume: {ticker.result.volume}");
                    }
                };

                client.OnCandleReceived += (candle) =>
                {
                    if (candle.result != null && candle.result.Count > 0)
                    {
                        var candleItem = candle.result.First();
                        Console.WriteLine($"[{ExchangeName}] Candle: {candle.symbol} - Open: {candleItem.open}, Close: {candleItem.close}");
                    }
                };

                // Connect to WebSocket
                Console.WriteLine($"Connecting to {ExchangeName}...");
                bool connected = await client.ConnectAsync();
                
                if (!connected)
                {
                    Console.WriteLine($"Failed to connect to {ExchangeName}");
                    return;
                }

                Console.WriteLine($"Connected to {ExchangeName}!");

                // Subscribe to multiple symbols
                string[] symbols = { "BTC/USDT", "ETH/USDT", "BNB/USDT" };
                
                foreach (var symbol in symbols)
                {
                    Console.WriteLine($"Subscribing to {symbol}...");
                    
                    await client.SubscribeOrderbookAsync(symbol);
                    await client.SubscribeTradesAsync(symbol);
                    await client.SubscribeTickerAsync(symbol);
                    // await client.SubscribeCandleAsync(symbol, "1m"); // Not implemented yet
                    
                    await Task.Delay(100); // Small delay between subscriptions
                }

                // Let it run for 30 seconds
                Console.WriteLine($"\nReceiving data from {ExchangeName} for 30 seconds...");
                Console.WriteLine("Press 'Q' to quit early\n");

                var endTime = DateTime.Now.AddSeconds(30);
                while (DateTime.Now < endTime)
                {
                    if (Console.KeyAvailable)
                    {
                        var key = Console.ReadKey(true);
                        if (key.Key == ConsoleKey.Q)
                        {
                            Console.WriteLine("\nQuitting early...");
                            break;
                        }
                    }
                    await Task.Delay(100);
                }

                // Unsubscribe and disconnect
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