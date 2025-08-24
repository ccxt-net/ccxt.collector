using CCXT.Collector.Samples.Base;
using System;
using System.Linq;
using System.Threading.Tasks;
using CCXT.Collector.Okx;
using CCXT.Collector.Core.Abstractions;

namespace CCXT.Collector.Samples.Exchanges
{
    public class OkxSample : IExchangeSample
    {
        public string ExchangeName => "OKX";

        public async Task SampleRun()
        {
            Console.WriteLine($"\n=== {ExchangeName} WebSocket Sample (formerly OKEx) ===\n");

            IWebSocketClient client = new OkxWebSocketClient();
            
            try
            {
                // Set up callbacks with multi-market support
                client.OnOrderbookReceived += (orderbook) =>
                {
                    var depth = orderbook.result?.bids.Count + orderbook.result?.asks.Count ?? 0;
                    Console.WriteLine($"[{ExchangeName}] {orderbook.symbol} - Orderbook depth: {depth} levels");
                };

                client.OnTradeReceived += (trades) =>
                {
                    string direction = (trades.result?.FirstOrDefault()?.side ?? "") == "buy" ? "↑" : "↓";
                    Console.WriteLine($"[{ExchangeName}] {trades.symbol} {direction} Trade: {trades.result?.FirstOrDefault()?.quantity ?? 0:F4} @ ${trades.result?.FirstOrDefault()?.price ?? 0:F2}");
                };

                client.OnTickerReceived += (ticker) =>
                {
                    Console.WriteLine($"[{ExchangeName}] {ticker.symbol} - ${ticker.result?.closePrice ?? 0:F2} (24h Vol: ${ticker.result?.volume ?? 0 * ticker.result?.closePrice ?? 0:F0})");
                };

                client.OnCandleReceived += (candle) =>
                {
                    string trend = (candle.result?.FirstOrDefault()?.close ?? 0) > (candle.result?.FirstOrDefault()?.open ?? 0) ? "🟢" : "🔴";
                    Console.WriteLine($"[{ExchangeName}] {candle.symbol} - {trend} Candle: ${candle.result?.FirstOrDefault()?.close ?? 0:F2}");
                };

                // Connect
                Console.WriteLine($"Connecting to {ExchangeName} (OKEx rebrand)...");
                bool connected = await client.ConnectAsync();
                
                if (!connected)
                {
                    Console.WriteLine($"Failed to connect to {ExchangeName}");
                    return;
                }

                Console.WriteLine($"Connected to {ExchangeName}!");

                // Subscribe to pairs including OKB token
                string[] symbols = { "BTC/USDT", "ETH/USDT", "OKB/USDT" };
                
                foreach (var symbol in symbols)
                {
                    Console.WriteLine($"Subscribing to {symbol}...");
                    
                    await client.SubscribeOrderbookAsync(symbol);
                    await client.SubscribeTradesAsync(symbol);
                    await client.SubscribeTickerAsync(symbol);
                    // await client.SubscribeCandleAsync(symbol, "1m"); // Not implemented yet
                    
                    await Task.Delay(200);
                }

                // Run for 22 seconds
                Console.WriteLine($"\nReceiving data for 22 seconds...\n");
                await Task.Delay(22000);

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