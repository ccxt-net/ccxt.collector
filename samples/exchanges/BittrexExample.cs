using System;
using System.Threading.Tasks;
using CCXT.Collector.Bittrex;
using CCXT.Collector.Library;
using CCXT.Collector.Service;

namespace CCXT.Collector.Samples.Exchanges
{
    /// <summary>
    /// Bittrex WebSocket sample - Demonstrates SignalR-based real-time data streaming
    /// </summary>
    public class BittrexExample
    {
        public static async Task RunSample()
        {
            Console.WriteLine("\n=== Bittrex WebSocket Sample ===");
            Console.WriteLine("Connecting to Bittrex via SignalR protocol...\n");

            var client = new BittrexWebSocketClient();
            
            // Event handlers
            client.OnConnected += () => Console.WriteLine("[Connected] Bittrex SignalR connected");
            client.OnDisconnected += () => Console.WriteLine("[Disconnected] Bittrex SignalR disconnected");
            client.OnError += (error) => Console.WriteLine($"[Error] {error}");

            // Market data handlers
            client.OnOrderbookReceived += (orderbook) =>
            {
                if (orderbook.result.bids.Count > 0 && orderbook.result.asks.Count > 0)
                {
                    Console.WriteLine($"[Orderbook] {orderbook.symbol}");
                    Console.WriteLine($"  Best Bid: ${orderbook.result.bids[0].price:F2} x {orderbook.result.bids[0].quantity:F8}");
                    Console.WriteLine($"  Best Ask: ${orderbook.result.asks[0].price:F2} x {orderbook.result.asks[0].quantity:F8}");
                }
            };

            client.OnTradeReceived += (trade) =>
            {
                if (trade.result.Count > 0)
                {
                    var t = trade.result[0];
                    Console.WriteLine($"[Trade] {trade.symbol} - ${t.price:F2} x {t.quantity:F8} @ {new DateTime(1970, 1, 1).AddMilliseconds(t.timestamp):HH:mm:ss}");
                }
            };

            client.OnTickerReceived += (ticker) =>
            {
                Console.WriteLine($"[Ticker] {ticker.symbol} - ${ticker.result.closePrice:F2} ({ticker.result.percentage:+0.00;-0.00}%)");
            };

            try
            {
                // Connect to WebSocket
                await client.ConnectAsync();
                
                // Subscribe to BTC-USDT market (Bittrex uses hyphen separator)
                var market = new Market("BTC", "USDT");
                
                Console.WriteLine("Subscribing to BTC-USDT market data...");
                await client.SubscribeOrderbookAsync(market);
                await client.SubscribeTradesAsync(market);
                await client.SubscribeTickerAsync(market);
                
                // Also subscribe to ETH-USDT
                var ethMarket = new Market("ETH", "USDT");
                Console.WriteLine("Subscribing to ETH-USDT market data...");
                await client.SubscribeTickerAsync(ethMarket);                // Run for 10 seconds
                Console.WriteLine("\nCollecting data for 10 seconds...\n");
                await SampleHelper.WaitForDurationOrEsc(10000);
                
                // Properly disconnect with cleanup
                await SampleHelper.SafeDisconnectAsync(client, "Bittrex");
                Console.WriteLine("\nBittrex sample completed!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }
    }
}