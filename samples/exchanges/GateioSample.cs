using CCXT.Collector.Samples.Base;
using System;
using System.Linq;
using System.Threading.Tasks;
using CCXT.Collector.Gateio;
using CCXT.Collector.Core.Abstractions;

namespace CCXT.Collector.Samples.Exchanges
{
    public class GateioSample : IExchangeSample
    {
        public string ExchangeName => "Gate.io";

        public async Task SampleRun()
        {
            Console.WriteLine($"\n=== {ExchangeName} WebSocket Sample (JSON Protocol) ===\n");

            IWebSocketClient client = new GateioWebSocketClient();
            
            try
            {
                // Set up callbacks
                client.OnOrderbookReceived += (orderbook) =>
                {
                    Console.WriteLine($"[{ExchangeName}] {orderbook.symbol} - Bids: {orderbook.result?.bids.Count ?? 0}, Asks: {orderbook.result?.asks.Count ?? 0}");
                };

                client.OnTradeReceived += (trades) =>
                {
                    string timeStr = DateTimeOffset.FromUnixTimeMilliseconds(trades.result?.FirstOrDefault()?.timestamp ?? 0).ToString("HH:mm:ss");
                    Console.WriteLine($"[{ExchangeName}] {timeStr} - {trades.symbol} Trade: {trades.result?.FirstOrDefault()?.quantity ?? 0:F4} @ {trades.result?.FirstOrDefault()?.price ?? 0:F2}");
                };

                client.OnTickerReceived += (ticker) =>
                {
                    decimal changePercent = ((ticker.result?.closePrice ?? 0 - ticker.result?.prevClosePrice ?? 0) / ticker.result?.prevClosePrice ?? 0) * 100;
                    Console.WriteLine($"[{ExchangeName}] {ticker.symbol} - ${ticker.result?.closePrice ?? 0:F2} ({changePercent:+0.00;-0.00;0}%)");
                };

                // Connect
                Console.WriteLine($"Connecting to {ExchangeName} (JSON-RPC style)...");
                bool connected = await client.ConnectAsync();
                
                if (!connected)
                {
                    Console.WriteLine($"Failed to connect to {ExchangeName}");
                    return;
                }

                Console.WriteLine($"Connected to {ExchangeName}!");

                // Subscribe to pairs
                string[] symbols = { "BTC/USDT", "ETH/USDT", "GT/USDT" };
                
                foreach (var symbol in symbols)
                {
                    Console.WriteLine($"Subscribing to {symbol}...");
                    
                    await client.SubscribeOrderbookAsync(symbol);
                    await client.SubscribeTradesAsync(symbol);
                    await client.SubscribeTickerAsync(symbol);
                    
                    await Task.Delay(250);
                }

                // Run for 20 seconds
                Console.WriteLine($"\nReceiving data for 20 seconds...\n");
                await Task.Delay(20000);

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