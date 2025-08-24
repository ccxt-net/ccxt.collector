using CCXT.Collector.Samples.Base;
using System;
using System.Linq;
using System.Threading.Tasks;
using CCXT.Collector.Kucoin;
using CCXT.Collector.Core.Abstractions;

namespace CCXT.Collector.Samples.Exchanges
{
    public class KucoinSample : IExchangeSample
    {
        public string ExchangeName => "Kucoin";

        public async Task SampleRun()
        {
            Console.WriteLine($"\n=== {ExchangeName} WebSocket Sample (Token Required) ===\n");

            IWebSocketClient client = new KucoinWebSocketClient();
            
            try
            {
                // Set up callbacks
                client.OnOrderbookReceived += (orderbook) =>
                {
                    Console.WriteLine($"[{ExchangeName}] {orderbook.symbol} - Orderbook snapshot/update received");
                };

                client.OnTradeReceived += (trades) =>
                {
                    Console.WriteLine($"[{ExchangeName}] {trades.symbol} - Trade: {trades.result?.FirstOrDefault()?.side ?? ""} {trades.result?.FirstOrDefault()?.quantity ?? 0:F4} @ {trades.result?.FirstOrDefault()?.price ?? 0:F2} USDT");
                };

                client.OnTickerReceived += (ticker) =>
                {
                    Console.WriteLine($"[{ExchangeName}] {ticker.symbol} - Price: {ticker.result?.closePrice ?? 0:F2}, 24h Change: {((ticker.result?.closePrice ?? 0 - ticker.result?.prevClosePrice ?? 0) / ticker.result?.prevClosePrice ?? 0 * 100):+0.00;-0.00;0}%");
                };

                client.OnCandleReceived += (candle) =>
                {
                    Console.WriteLine($"[{ExchangeName}] {candle.symbol} - Candle Close: {candle.result?.FirstOrDefault()?.close ?? 0:F2}, Volume: {candle.result?.FirstOrDefault()?.volume ?? 0:F2}");
                };

                // Connect (requires token endpoint)
                Console.WriteLine($"Connecting to {ExchangeName} (retrieving WebSocket token)...");
                bool connected = await client.ConnectAsync();
                
                if (!connected)
                {
                    Console.WriteLine($"Failed to connect to {ExchangeName} (token endpoint may be required)");
                    return;
                }

                Console.WriteLine($"Connected to {ExchangeName}!");

                // Subscribe to pairs
                string[] symbols = { "BTC/USDT", "ETH/USDT", "KCS/USDT" };
                
                foreach (var symbol in symbols)
                {
                    Console.WriteLine($"Subscribing to {symbol}...");
                    
                    await client.SubscribeOrderbookAsync(symbol);
                    await client.SubscribeTradesAsync(symbol);
                    await client.SubscribeTickerAsync(symbol);
                    // await client.SubscribeCandleAsync(symbol, "1min"); // Not implemented yet
                    
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
                if (ex.Message.Contains("token"))
                {
                    Console.WriteLine("Note: KuCoin requires a WebSocket token from their REST API");
                }
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