using System;
using System.Threading.Tasks;
using CCXT.Collector.Coinbase;
using CCXT.Collector.Library;
using CCXT.Collector.Service;

namespace CCXT.Collector.Samples.Exchanges
{
    /// <summary>
    /// Coinbase WebSocket sample - Demonstrates US-regulated exchange data streaming
    /// </summary>
    public class CoinbaseExample
    {
        public static async Task RunSample()
        {
            Console.WriteLine("\n=== Coinbase WebSocket Sample ===");
            Console.WriteLine("Connecting to Coinbase Pro/Advanced Trade...\n");

            var client = new CoinbaseWebSocketClient();
            
            // Event handlers
            client.OnConnected += () => Console.WriteLine("[Connected] Coinbase WebSocket connected");
            client.OnDisconnected += () => Console.WriteLine("[Disconnected] Coinbase WebSocket disconnected");
            client.OnError += (error) => Console.WriteLine($"[Error] {error}");

            // Market data handlers
            client.OnOrderbookReceived += (orderbook) =>
            {
                if (orderbook.result.bids.Count > 0 && orderbook.result.asks.Count > 0)
                {
                    Console.WriteLine($"[L2 Orderbook] {orderbook.symbol}");
                    Console.WriteLine($"  Best Bid: ${orderbook.result.bids[0].price:F2} x {orderbook.result.bids[0].quantity:F8} BTC");
                    Console.WriteLine($"  Best Ask: ${orderbook.result.asks[0].price:F2} x {orderbook.result.asks[0].quantity:F8} BTC");
                    Console.WriteLine($"  Spread: ${orderbook.result.asks[0].price - orderbook.result.bids[0].price:F2}");
                    Console.WriteLine($"  Depth: {orderbook.result.bids.Count} bids, {orderbook.result.asks.Count} asks");
                }
            };

            client.OnTradeReceived += (trade) =>
            {
                if (trade.result.Count > 0)
                {
                    var t = trade.result[0];
                    var side = t.sideType == SideType.Bid ? "BUY" : "SELL";
                    Console.WriteLine($"[Match] {trade.symbol} - ${t.price:F2} x {t.quantity:F8} BTC [{side}] @ {new DateTime(1970, 1, 1).AddMilliseconds(t.timestamp):HH:mm:ss.fff}");
                }
            };

            client.OnTickerReceived += (ticker) =>
            {
                Console.WriteLine($"[Ticker] {ticker.symbol}");
                Console.WriteLine($"  Price: ${ticker.result.closePrice:F2}");
                Console.WriteLine($"  24h High: ${ticker.result.highPrice:F2}");
                Console.WriteLine($"  24h Low: ${ticker.result.lowPrice:F2}");
                Console.WriteLine($"  24h Volume: {ticker.result.volume:F4} BTC");
            };

            try
            {
                // Connect to WebSocket
                await client.ConnectAsync();
                
                // Subscribe to BTC-USD market
                var btcMarket = new Market("BTC", "USD");
                
                Console.WriteLine("Subscribing to BTC-USD market data...");
                await client.SubscribeOrderbookAsync(btcMarket);
                await client.SubscribeTradesAsync(btcMarket);
                await client.SubscribeTickerAsync(btcMarket);
                
                // Also subscribe to ETH-USD
                var ethMarket = new Market("ETH", "USD");
                Console.WriteLine("Subscribing to ETH-USD market data...");
                await client.SubscribeOrderbookAsync(ethMarket);
                await client.SubscribeTickerAsync(ethMarket);
                
                // Run for 10 seconds

                
                Console.WriteLine("\nCollecting data for 10 seconds...\n");

                
                await SampleHelper.WaitForDurationOrEsc(10000);
                
                // Properly disconnect with cleanup
                await SampleHelper.SafeDisconnectAsync(client, "Coinbase");
                Console.WriteLine("\nCoinbase sample completed!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }
    }
}