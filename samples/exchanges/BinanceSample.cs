using System;
using System.Threading.Tasks;
using CCXT.Collector.Binance;
using CCXT.Collector.Library;
using CCXT.Collector.Service;

namespace CCXT.Collector.Samples.Exchanges
{
    /// <summary>
    /// Binance WebSocket sample - World's largest cryptocurrency exchange
    /// </summary>
    public class BinanceSample
    {
        public static async Task RunSample()
        {
            Console.WriteLine("\n=== Binance WebSocket Sample ===");
            Console.WriteLine("Connecting to Binance...\n");

            var client = new BinanceWebSocketClient();
            
            // Event handlers
            client.OnConnected += () => Console.WriteLine("[Connected] Binance WebSocket connected");
            client.OnDisconnected += () => Console.WriteLine("[Disconnected] Binance WebSocket disconnected");
            client.OnError += (error) => Console.WriteLine($"[Error] {error}");

            // Market data handlers
            client.OnOrderbookReceived += (orderbook) =>
            {
                if (orderbook.result.bids.Count > 0 && orderbook.result.asks.Count > 0)
                {
                    Console.WriteLine($"[Orderbook] {orderbook.symbol}");
                    Console.WriteLine($"  Best Bid: ${orderbook.result.bids[0].price:F2} x {orderbook.result.bids[0].quantity:F8}");
                    Console.WriteLine($"  Best Ask: ${orderbook.result.asks[0].price:F2} x {orderbook.result.asks[0].quantity:F8}");
                    Console.WriteLine($"  Spread: ${orderbook.result.asks[0].price - orderbook.result.bids[0].price:F2}");
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
                Console.WriteLine($"  Price: ${ticker.result.closePrice:F2} ({ticker.result.percentage:+0.00;-0.00}%)");
                Console.WriteLine($"  24h Volume: {ticker.result.volume:F2}");
            };

            try
            {
                // Connect to WebSocket
                await client.ConnectAsync();
                
                // Subscribe to BTC/USDT market
                var market = new Market("BTC", "USDT");
                
                Console.WriteLine("Subscribing to BTC/USDT market data...");
                await client.SubscribeOrderbookAsync(market);
                await client.SubscribeTradesAsync(market);
                await client.SubscribeTickerAsync(market);
                
                // Also subscribe to ETH/USDT
                var ethMarket = new Market("ETH", "USDT");
                Console.WriteLine("Subscribing to ETH/USDT market data...");
                await client.SubscribeTickerAsync(ethMarket);
                
                // Run for 10 seconds
                Console.WriteLine("\nCollecting data for 10 seconds...\n");
                await SampleHelper.WaitForDurationOrEsc(10000);
                
                // Properly disconnect with cleanup
                await SampleHelper.SafeDisconnectAsync(client, "Binance");
                Console.WriteLine("\nBinance sample completed!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }
    }
}