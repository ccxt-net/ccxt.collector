using System;
using System.Threading.Tasks;
using CCXT.Collector.Okx;
using CCXT.Collector.Library;
using CCXT.Collector.Service;

namespace CCXT.Collector.Samples.Exchanges
{
    /// <summary>
    /// OKX WebSocket sample - Leading derivatives and spot exchange
    /// </summary>
    public class OkxExample
    {
        public static async Task RunSample()
        {
            Console.WriteLine("\n=== OKX WebSocket Sample ===");
            Console.WriteLine("Connecting to OKX (formerly OKEx)...\n");

            var client = new OkxWebSocketClient();
            
            // Event handlers
            client.OnConnected += () => Console.WriteLine("[Connected] OKX WebSocket connected");
            client.OnDisconnected += () => Console.WriteLine("[Disconnected] OKX WebSocket disconnected");
            client.OnError += (error) => Console.WriteLine($"[Error] {error}");

            // Market data handlers
            client.OnOrderbookReceived += (orderbook) =>
            {
                if (orderbook.result.bids.Count > 0 && orderbook.result.asks.Count > 0)
                {
                    Console.WriteLine($"[Books] {orderbook.symbol}");
                    Console.WriteLine($"  Bid: ${orderbook.result.bids[0].price:F2} x {orderbook.result.bids[0].quantity:F6}");
                    Console.WriteLine($"  Ask: ${orderbook.result.asks[0].price:F2} x {orderbook.result.asks[0].quantity:F6}");
                    Console.WriteLine($"  Spread: {((orderbook.result.asks[0].price - orderbook.result.bids[0].price) / orderbook.result.bids[0].price * 100):F4}%");
                }
            };

            client.OnTradeReceived += (trade) =>
            {
                if (trade.result.Count > 0)
                {
                    var t = trade.result[0];
                    var side = t.sideType == SideType.Bid ? "BUY" : "SELL";
                    Console.WriteLine($"[Trades] {trade.symbol} - ${t.price:F2} x {t.quantity:F6} [{side}]");
                }
            };

            client.OnTickerReceived += (ticker) =>
            {
                Console.WriteLine($"[Tickers] {ticker.symbol}");
                Console.WriteLine($"  Last: ${ticker.result.closePrice:F2}");
                Console.WriteLine($"  24h Chg: {ticker.result.percentage:+0.00;-0.00}%");
                Console.WriteLine($"  24h High: ${ticker.result.highPrice:F2}");
                Console.WriteLine($"  24h Low: ${ticker.result.lowPrice:F2}");
                Console.WriteLine($"  24h Vol: {ticker.result.volume:F2} BTC");
                Console.WriteLine($"  24h Vol(USDT): ${ticker.result.quoteVolume:N0}");
            };

            client.OnCandleReceived += (candle) =>
            {
                if (candle.result.Count > 0)
                {
                    var c = candle.result[0];
                    var trend = c.close >= c.open ? "↑" : "↓";
                    Console.WriteLine($"[Candle] {candle.symbol} {candle.interval} {trend}");
                    Console.WriteLine($"  O: ${c.open:F2} H: ${c.high:F2} L: ${c.low:F2} C: ${c.close:F2}");
                    Console.WriteLine($"  Volume: {c.volume:F4} BTC");
                }
            };

            try
            {
                // Connect to WebSocket
                await client.ConnectAsync();
                
                // Subscribe to BTC-USDT spot market
                var btcMarket = new Market("BTC", "USDT");
                
                Console.WriteLine("Subscribing to BTC-USDT spot market...");
                await client.SubscribeOrderbookAsync(btcMarket);
                await client.SubscribeTradesAsync(btcMarket);
                await client.SubscribeTickerAsync(btcMarket);
                await client.SubscribeCandlesAsync(btcMarket, "1m");
                
                // Also subscribe to ETH-USDT
                var ethMarket = new Market("ETH", "USDT");
                Console.WriteLine("Subscribing to ETH-USDT spot market...");
                await client.SubscribeOrderbookAsync(ethMarket);
                await client.SubscribeTickerAsync(ethMarket);
                
                // Also subscribe to OKB-USDT (OKX Token)
                var okbMarket = new Market("OKB", "USDT");
                Console.WriteLine("Subscribing to OKB-USDT market...");
                await client.SubscribeTickerAsync(okbMarket);
                
                // Run for 10 seconds

                
                Console.WriteLine("\nCollecting data for 10 seconds...\n");

                
                await SampleHelper.WaitForDurationOrEsc(10000);
                
                // Properly disconnect with cleanup
                await SampleHelper.SafeDisconnectAsync(client, "OKX");
                Console.WriteLine("\nOKX sample completed!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }
    }
}