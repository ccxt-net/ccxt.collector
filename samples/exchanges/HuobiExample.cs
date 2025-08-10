using System;
using System.Threading.Tasks;
using CCXT.Collector.Huobi;
using CCXT.Collector.Library;
using CCXT.Collector.Service;

namespace CCXT.Collector.Samples.Exchanges
{
    /// <summary>
    /// Huobi WebSocket sample - Major Chinese exchange real-time data
    /// </summary>
    public class HuobiExample
    {
        public static async Task RunSample()
        {
            Console.WriteLine("\n=== Huobi WebSocket Sample ===");
            Console.WriteLine("Connecting to Huobi Global...\n");

            var client = new HuobiWebSocketClient();
            
            // Event handlers
            client.OnConnected += () => Console.WriteLine("[Connected] Huobi WebSocket connected");
            client.OnDisconnected += () => Console.WriteLine("[Disconnected] Huobi WebSocket disconnected");
            client.OnError += (error) => Console.WriteLine($"[Error] {error}");

            // Market data handlers
            client.OnOrderbookReceived += (orderbook) =>
            {
                if (orderbook.result.bids.Count > 0 && orderbook.result.asks.Count > 0)
                {
                    Console.WriteLine($"[Depth] {orderbook.symbol}");
                    Console.WriteLine($"  Bid1: ${orderbook.result.bids[0].price:F2} x {orderbook.result.bids[0].quantity:F6}");
                    Console.WriteLine($"  Ask1: ${orderbook.result.asks[0].price:F2} x {orderbook.result.asks[0].quantity:F6}");
                    Console.WriteLine($"  Spread: ${orderbook.result.asks[0].price - orderbook.result.bids[0].price:F4}");
                }
            };

            client.OnTradeReceived += (trade) =>
            {
                if (trade.result.Count > 0)
                {
                    var t = trade.result[0];
                    var direction = t.sideType == SideType.Bid ? "BUY" : "SELL";
                    Console.WriteLine($"[Trade] {trade.symbol} - ${t.price:F2} x {t.quantity:F6} [{direction}]");
                }
            };

            client.OnTickerReceived += (ticker) =>
            {
                Console.WriteLine($"[Market] {ticker.symbol}");
                Console.WriteLine($"  Price: ${ticker.result.closePrice:F2}");
                Console.WriteLine($"  Change: {ticker.result.percentage:+0.00;-0.00}%");
                Console.WriteLine($"  24h High: ${ticker.result.highPrice:F2}");
                Console.WriteLine($"  24h Low: ${ticker.result.lowPrice:F2}");
                Console.WriteLine($"  24h Amount: {ticker.result.volume:F2}");
                Console.WriteLine($"  24h Volume: ${ticker.result.quoteVolume:N0}");
            };

            client.OnCandleReceived += (candle) =>
            {
                if (candle.result.Count > 0)
                {
                    var c = candle.result[0];
                    Console.WriteLine($"[Kline] {candle.symbol} {candle.interval}");
                    Console.WriteLine($"  Open: ${c.open:F2}, High: ${c.high:F2}");
                    Console.WriteLine($"  Low: ${c.low:F2}, Close: ${c.close:F2}");
                    Console.WriteLine($"  Volume: {c.volume:F4}");
                }
            };

            try
            {
                // Connect to WebSocket
                await client.ConnectAsync();
                
                // Subscribe to btcusdt market
                var btcMarket = new Market("BTC", "USDT");
                
                Console.WriteLine("Subscribing to btcusdt market data...");
                await client.SubscribeOrderbookAsync(btcMarket);
                await client.SubscribeTradesAsync(btcMarket);
                await client.SubscribeTickerAsync(btcMarket);
                await client.SubscribeCandlesAsync(btcMarket, "1min");
                
                // Also subscribe to ethusdt
                var ethMarket = new Market("ETH", "USDT");
                Console.WriteLine("Subscribing to ethusdt market data...");
                await client.SubscribeOrderbookAsync(ethMarket);
                await client.SubscribeTickerAsync(ethMarket);
                
                // Also subscribe to HT (Huobi Token)
                var htMarket = new Market("HT", "USDT");
                Console.WriteLine("Subscribing to htusdt market data...");
                await client.SubscribeTickerAsync(htMarket);                // Run for 10 seconds
                Console.WriteLine("\nCollecting data for 10 seconds...\n");
                await SampleHelper.WaitForDurationOrEsc(10000);
                
                // Properly disconnect with cleanup
                await SampleHelper.SafeDisconnectAsync(client, "Huobi");
                Console.WriteLine("\nHuobi sample completed!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }
    }
}