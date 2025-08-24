using System;
using System.Linq;
using System.Threading.Tasks;
using CCXT.Collector.Bitget;
using CCXT.Collector.Core.Abstractions;
using CCXT.Collector.Samples.Base;

namespace CCXT.Collector.Samples.Exchanges
{
    public class BitgetSample : IExchangeSample
    {
        public string ExchangeName => "Bitget";

        public async Task SampleRun()
        {
            Console.WriteLine($"\n=== {ExchangeName} WebSocket Sample ===\n");

            IWebSocketClient client = new BitgetWebSocketClient();
            
            try
            {
                // Set up callbacks with data counters
                int orderbookCount = 0, tradeCount = 0, tickerCount = 0;

                client.OnOrderbookReceived += (orderbook) =>
                {
                    orderbookCount++;
                    if (orderbookCount <= 3) // Show first 3 messages
                    {
                        Console.WriteLine($"[{ExchangeName}] Orderbook #{orderbookCount}: {orderbook.symbol} - Bids: {orderbook.result?.bids.Count ?? 0}, Asks: {orderbook.result?.asks.Count ?? 0}");
                    }
                };

                client.OnTradeReceived += (trades) =>
                {
                    tradeCount++;
                    if (tradeCount <= 3) // Show first 3 messages
                    {
                        Console.WriteLine($"[{ExchangeName}] Trade #{tradeCount}: {trades.symbol} - Price: {trades.result?.FirstOrDefault()?.price ?? 0:F2}, Amount: {trades.result?.FirstOrDefault()?.quantity ?? 0:F4}");
                    }
                };

                client.OnTickerReceived += (ticker) =>
                {
                    tickerCount++;
                    if (tickerCount <= 3) // Show first 3 messages
                    {
                        Console.WriteLine($"[{ExchangeName}] Ticker #{tickerCount}: {ticker.symbol} - Last: {ticker.result?.closePrice ?? 0:F2}, 24h Vol: {ticker.result?.volume ?? 0:F2}");
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

                // Subscribe to BTC/USDT and ETH/USDT
                string[] symbols = { "BTC/USDT", "ETH/USDT" };
                
                foreach (var symbol in symbols)
                {
                    Console.WriteLine($"Subscribing to {symbol}...");
                    
                    await client.SubscribeOrderbookAsync(symbol);
                    await client.SubscribeTradesAsync(symbol);
                    await client.SubscribeTickerAsync(symbol);
                    
                    await Task.Delay(200);
                }

                // Let it run for 20 seconds
                Console.WriteLine($"\nReceiving data from {ExchangeName} for 20 seconds...");
                Console.WriteLine("(Showing first 3 messages of each type)\n");

                await Task.Delay(20000);

                // Show summary
                Console.WriteLine($"\n=== {ExchangeName} Summary ===");
                Console.WriteLine($"Orderbooks received: {orderbookCount}");
                Console.WriteLine($"Trades received: {tradeCount}");
                Console.WriteLine($"Tickers received: {tickerCount}");
                Console.WriteLine($"Total messages: {orderbookCount + tradeCount + tickerCount}");

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