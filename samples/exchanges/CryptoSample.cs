using CCXT.Collector.Samples.Base;
using System;
using System.Linq;
using System.Threading.Tasks;
using CCXT.Collector.Crypto;
using CCXT.Collector.Core.Abstractions;

namespace CCXT.Collector.Samples.Exchanges
{
    public class CryptoSample : IExchangeSample
    {
        public string ExchangeName => "Crypto.com";

        public async Task SampleRun()
        {
            Console.WriteLine($"\n=== {ExchangeName} WebSocket Sample ===\n");

            IWebSocketClient client = new CryptoWebSocketClient();
            
            try
            {
                // Set up callbacks with formatted output
                client.OnOrderbookReceived += (orderbook) =>
                {
                    var spread = (orderbook.result?.asks.Min(a => a.price) ?? 0) -
                                (orderbook.result?.bids.Max(b => b.price) ?? 0);
                    Console.WriteLine($"[{ExchangeName}] {orderbook.symbol} - Spread: {Math.Abs(spread):F2} USDT");
                };

                client.OnTradeReceived += (trades) =>
                {
                    Console.WriteLine($"[{ExchangeName}] {trades.symbol} - {trades.result?.FirstOrDefault()?.side ?? "".ToUpper()} {trades.result?.FirstOrDefault()?.quantity ?? 0:F4} @ {trades.result?.FirstOrDefault()?.price ?? 0:F2}");
                };

                client.OnTickerReceived += (ticker) =>
                {
                    Console.WriteLine($"[{ExchangeName}] {ticker.symbol} - ${ticker.result?.closePrice ?? 0:F2} (24h Vol: {ticker.result?.volume ?? 0:F0})");
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

                // Subscribe to popular pairs
                string[] symbols = { "BTC/USDT", "ETH/USDT", "CRO/USDT" };
                
                foreach (var symbol in symbols)
                {
                    Console.WriteLine($"Subscribing to {symbol}...");
                    
                    await client.SubscribeOrderbookAsync(symbol);
                    await client.SubscribeTradesAsync(symbol);
                    await client.SubscribeTickerAsync(symbol);
                    
                    await Task.Delay(200);
                }

                // Run for 18 seconds
                Console.WriteLine($"\nReceiving data for 18 seconds...\n");
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