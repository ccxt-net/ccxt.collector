using System;
using System.Threading.Tasks;
using CCXT.Collector.Bybit;
using CCXT.Collector.Library;
using CCXT.Collector.Service;

namespace CCXT.Collector.Samples.Exchanges
{
    /// <summary>
    /// Bybit WebSocket sample - Demonstrates derivatives and spot trading data
    /// </summary>
    public class BybitExample
    {
        public static async Task RunSample()
        {
            Console.WriteLine("\n=== Bybit WebSocket Sample ===");
            Console.WriteLine("Connecting to Bybit derivatives & spot markets...\n");

            var client = new BybitWebSocketClient();
            
            // Event handlers
            client.OnConnected += () => Console.WriteLine("[Connected] Bybit WebSocket connected");
            client.OnDisconnected += () => Console.WriteLine("[Disconnected] Bybit WebSocket disconnected");
            client.OnError += (error) => Console.WriteLine($"[Error] {error}");

            // Market data handlers
            client.OnOrderbookReceived += (orderbook) =>
            {
                if (orderbook.result.bids.Count > 0 && orderbook.result.asks.Count > 0)
                {
                    Console.WriteLine($"[Orderbook] {orderbook.symbol}");
                    Console.WriteLine($"  Best Bid: ${orderbook.result.bids[0].price:F2} x {orderbook.result.bids[0].quantity:F4}");
                    Console.WriteLine($"  Best Ask: ${orderbook.result.asks[0].price:F2} x {orderbook.result.asks[0].quantity:F4}");
                    Console.WriteLine($"  Mid Price: ${(orderbook.result.bids[0].price + orderbook.result.asks[0].price) / 2:F2}");
                }
            };

            client.OnTradeReceived += (trade) =>
            {
                if (trade.result.Count > 0)
                {
                    var t = trade.result[0];
                    var side = t.sideType == SideType.Bid ? "BUY" : "SELL";
                    Console.WriteLine($"[Trade] {trade.symbol} - ${t.price:F2} x {t.quantity:F4} [{side}]");
                }
            };

            client.OnTickerReceived += (ticker) =>
            {
                Console.WriteLine($"[Ticker] {ticker.symbol}");
                Console.WriteLine($"  Last: ${ticker.result.closePrice:F2}");
                Console.WriteLine($"  24h Change: {ticker.result.percentage:+0.00;-0.00}%");
                Console.WriteLine($"  24h Volume: ${ticker.result.quoteVolume:N0}");
            };

            client.OnCandleReceived += (candle) =>
            {
                if (candle.result.Count > 0)
                {
                    var c = candle.result[0];
                    Console.WriteLine($"[Candle] {candle.symbol} {candle.interval}");
                    Console.WriteLine($"  OHLC: ${c.open:F2} / ${c.high:F2} / ${c.low:F2} / ${c.close:F2}");
                    Console.WriteLine($"  Volume: {c.volume:F4}");
                }
            };

            try
            {
                // Connect to WebSocket
                await client.ConnectAsync();
                
                // Subscribe to BTCUSDT spot market
                var spotMarket = new Market("BTC", "USDT");
                
                Console.WriteLine("Subscribing to BTCUSDT spot market...");
                await client.SubscribeOrderbookAsync(spotMarket);
                await client.SubscribeTradesAsync(spotMarket);
                await client.SubscribeTickerAsync(spotMarket);
                await client.SubscribeCandlesAsync(spotMarket, "1m");
                
                // Also subscribe to ETHUSDT
                var ethMarket = new Market("ETH", "USDT");
                Console.WriteLine("Subscribing to ETHUSDT market...");
                await client.SubscribeTickerAsync(ethMarket);                // Run for 10 seconds
                Console.WriteLine("\nCollecting data for 10 seconds...\n");
                await SampleHelper.WaitForDurationOrEsc(10000);
                
                // Properly disconnect with cleanup
                await SampleHelper.SafeDisconnectAsync(client, "Bybit");
                Console.WriteLine("\nBybit sample completed!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }
    }
}