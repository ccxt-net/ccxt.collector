using System;
using System.Threading.Tasks;
using CCXT.Collector.Gateio;
using CCXT.Collector.Library;
using CCXT.Collector.Service;

namespace CCXT.Collector.Samples.Exchanges
{
    /// <summary>
    /// Gate.io WebSocket sample - Demonstrates spot market real-time data
    /// </summary>
    public class GateioExample
    {
        public static async Task RunSample()
        {
            Console.WriteLine("\n=== Gate.io WebSocket Sample ===");
            Console.WriteLine("Connecting to Gate.io spot market...\n");

            var client = new GateioWebSocketClient();
            
            // Event handlers
            client.OnConnected += () => Console.WriteLine("[Connected] Gate.io WebSocket connected");
            client.OnDisconnected += () => Console.WriteLine("[Disconnected] Gate.io WebSocket disconnected");
            client.OnError += (error) => Console.WriteLine($"[Error] {error}");

            // Market data handlers
            client.OnOrderbookReceived += (orderbook) =>
            {
                if (orderbook.result.bids.Count > 0 && orderbook.result.asks.Count > 0)
                {
                    Console.WriteLine($"[Orderbook] {orderbook.symbol}");
                    Console.WriteLine($"  Best Bid: ${orderbook.result.bids[0].price:F2} x {orderbook.result.bids[0].quantity:F8}");
                    Console.WriteLine($"  Best Ask: ${orderbook.result.asks[0].price:F2} x {orderbook.result.asks[0].quantity:F8}");
                    Console.WriteLine($"  Mid Price: ${(orderbook.result.bids[0].price + orderbook.result.asks[0].price) / 2:F2}");
                }
            };

            client.OnTradeReceived += (trade) =>
            {
                if (trade.result.Count > 0)
                {
                    var t = trade.result[0];
                    Console.WriteLine($"[Trade] {trade.symbol} - ${t.price:F2} x {t.quantity:F8} ({t.sideType})");
                }
            };

            client.OnTickerReceived += (ticker) =>
            {
                Console.WriteLine($"[Ticker] {ticker.symbol}");
                Console.WriteLine($"  Last: ${ticker.result.closePrice:F2}");
                Console.WriteLine($"  Change: {ticker.result.percentage:+0.00;-0.00}%");
                Console.WriteLine($"  Base Vol: {ticker.result.volume:F4}");
                Console.WriteLine($"  Quote Vol: ${ticker.result.quoteVolume:N0}");
            };

            client.OnCandleReceived += (candle) =>
            {
                if (candle.result.Count > 0)
                {
                    var c = candle.result[0];
                    var change = c.close - c.open;
                    var changePercent = (change / c.open) * 100;
                    Console.WriteLine($"[Candle] {candle.symbol} {candle.interval}");
                    Console.WriteLine($"  OHLC: ${c.open:F2} / ${c.high:F2} / ${c.low:F2} / ${c.close:F2}");
                    Console.WriteLine($"  Change: ${change:F2} ({changePercent:+0.00;-0.00}%)");
                }
            };

            try
            {
                // Connect to WebSocket
                await client.ConnectAsync();
                
                // Subscribe to BTC_USDT market (Gate.io uses underscore)
                var btcMarket = new Market("BTC", "USDT");
                
                Console.WriteLine("Subscribing to BTC_USDT spot market...");
                await client.SubscribeOrderbookAsync(btcMarket);
                await client.SubscribeTradesAsync(btcMarket);
                await client.SubscribeTickerAsync(btcMarket);
                await client.SubscribeCandlesAsync(btcMarket, "1m");
                
                // Also subscribe to ETH_USDT
                var ethMarket = new Market("ETH", "USDT");
                Console.WriteLine("Subscribing to ETH_USDT spot market...");
                await client.SubscribeOrderbookAsync(ethMarket);
                await client.SubscribeTickerAsync(ethMarket);
                
                // Also subscribe to GT_USDT (Gate Token)
                var gtMarket = new Market("GT", "USDT");
                Console.WriteLine("Subscribing to GT_USDT market...");
                await client.SubscribeTickerAsync(gtMarket);
                
                // Run for 10 seconds

                
                Console.WriteLine("\nCollecting data for 10 seconds...\n");

                
                await SampleHelper.WaitForDurationOrEsc(10000);
                
                // Properly disconnect with cleanup
                await SampleHelper.SafeDisconnectAsync(client, "Gate.io");
                Console.WriteLine("\nGate.io sample completed!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }
    }
}