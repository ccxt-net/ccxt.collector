using System;
using System.Threading.Tasks;
using CCXT.Collector.Upbit;
using CCXT.Collector.Library;
using CCXT.Collector.Service;

namespace CCXT.Collector.Samples.Exchanges
{
    /// <summary>
    /// Upbit WebSocket sample - Korea's largest cryptocurrency exchange
    /// </summary>
    public class UpbitSample
    {
        public static async Task RunSample()
        {
            Console.WriteLine("\n=== Upbit WebSocket Sample ===");
            Console.WriteLine("Connecting to Upbit (Korea)...\n");

            var client = new UpbitWebSocketClient();
            
            // Event handlers
            client.OnConnected += () => Console.WriteLine("[Connected] Upbit WebSocket connected");
            client.OnDisconnected += () => Console.WriteLine("[Disconnected] Upbit WebSocket disconnected");
            client.OnError += (error) => Console.WriteLine($"[Error] {error}");

            // Market data handlers
            client.OnOrderbookReceived += (orderbook) =>
            {
                if (orderbook.result.bids.Count > 0 && orderbook.result.asks.Count > 0)
                {
                    Console.WriteLine($"[Orderbook] {orderbook.symbol}");
                    Console.WriteLine($"  Best Bid: ₩{orderbook.result.bids[0].price:N0} x {orderbook.result.bids[0].quantity:F8}");
                    Console.WriteLine($"  Best Ask: ₩{orderbook.result.asks[0].price:N0} x {orderbook.result.asks[0].quantity:F8}");
                    Console.WriteLine($"  Spread: ₩{orderbook.result.asks[0].price - orderbook.result.bids[0].price:N0}");
                }
            };

            client.OnTradeReceived += (trade) =>
            {
                if (trade.result.Count > 0)
                {
                    var t = trade.result[0];
                    var side = t.sideType == SideType.Bid ? "BUY" : "SELL";
                    Console.WriteLine($"[Trade] {trade.symbol} - ₩{t.price:N0} x {t.quantity:F8} [{side}]");
                }
            };

            client.OnTickerReceived += (ticker) =>
            {
                Console.WriteLine($"[Ticker] {ticker.symbol}");
                Console.WriteLine($"  Price: ₩{ticker.result.closePrice:N0}");
                Console.WriteLine($"  Change: {ticker.result.percentage:+0.00;-0.00}%");
                Console.WriteLine($"  Volume: {ticker.result.volume:F4} BTC");
            };

            try
            {
                // Connect to WebSocket
                await client.ConnectAsync();
                
                // Subscribe to BTC/KRW market
                var market = new Market("BTC", "KRW");
                
                Console.WriteLine("Subscribing to BTC/KRW market...");
                await client.SubscribeOrderbookAsync(market);
                await client.SubscribeTradesAsync(market);
                await client.SubscribeTickerAsync(market);
                
                // Also subscribe to ETH/KRW
                var ethMarket = new Market("ETH", "KRW");
                Console.WriteLine("Subscribing to ETH/KRW market...");
                await client.SubscribeTickerAsync(ethMarket);
                
                // Run for 10 seconds
                Console.WriteLine("\nCollecting data for 10 seconds...\n");
                await SampleHelper.WaitForDurationOrEsc(10000);
                
                // Properly disconnect with cleanup
                await SampleHelper.SafeDisconnectAsync(client, "Upbit");
                Console.WriteLine("\nUpbit sample completed!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }
    }
}