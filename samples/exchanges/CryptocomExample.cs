using System;
using System.Threading.Tasks;
using CCXT.Collector.Crypto;
using CCXT.Collector.Library;
using CCXT.Collector.Service;

namespace CCXT.Collector.Samples.Exchanges
{
    /// <summary>
    /// Crypto.com WebSocket sample - Global exchange with multiple trading pairs
    /// </summary>
    public class CryptocomExample
    {
        public static async Task RunSample()
        {
            Console.WriteLine("\n=== Crypto.com WebSocket Sample ===");
            Console.WriteLine("Connecting to Crypto.com Exchange...\n");

            var client = new CryptoWebSocketClient();
            
            // Event handlers
            client.OnConnected += () => Console.WriteLine("[Connected] Crypto.com WebSocket connected");
            client.OnDisconnected += () => Console.WriteLine("[Disconnected] Crypto.com WebSocket disconnected");
            client.OnError += (error) => Console.WriteLine($"[Error] {error}");

            // Market data handlers
            client.OnOrderbookReceived += (orderbook) =>
            {
                if (orderbook.result.bids.Count > 0 && orderbook.result.asks.Count > 0)
                {
                    Console.WriteLine($"[Book] {orderbook.symbol}");
                    Console.WriteLine($"  Bid: ${orderbook.result.bids[0].price:F2} x {orderbook.result.bids[0].quantity:F6}");
                    Console.WriteLine($"  Ask: ${orderbook.result.asks[0].price:F2} x {orderbook.result.asks[0].quantity:F6}");
                    Console.WriteLine($"  Levels: {orderbook.result.bids.Count}");
                }
            };

            client.OnTradeReceived += (trade) =>
            {
                if (trade.result.Count > 0)
                {
                    foreach (var t in trade.result.Take(3)) // Show first 3 trades
                    {
                        Console.WriteLine($"[Trade] {trade.symbol} - ${t.price:F2} x {t.quantity:F6} ({t.sideType})");
                    }
                }
            };

            client.OnTickerReceived += (ticker) =>
            {
                Console.WriteLine($"[Ticker] {ticker.symbol}");
                Console.WriteLine($"  Price: ${ticker.result.closePrice:F2} ({ticker.result.percentage:+0.00;-0.00}%)");
                Console.WriteLine($"  24h Vol: {ticker.result.volume:F2}");
                Console.WriteLine($"  High/Low: ${ticker.result.highPrice:F2} / ${ticker.result.lowPrice:F2}");
            };

            client.OnCandleReceived += (candle) =>
            {
                if (candle.result.Count > 0)
                {
                    var c = candle.result[0];
                    Console.WriteLine($"[K-Line] {candle.symbol} {candle.interval}");
                    Console.WriteLine($"  O: ${c.open:F2} H: ${c.high:F2} L: ${c.low:F2} C: ${c.close:F2}");
                }
            };

            try
            {
                // Connect to WebSocket
                await client.ConnectAsync();
                
                // Subscribe to BTC_USDT market (Crypto.com uses underscore)
                var btcMarket = new Market("BTC", "USDT");
                
                Console.WriteLine("Subscribing to BTC_USDT market data...");
                await client.SubscribeOrderbookAsync(btcMarket);
                await client.SubscribeTradesAsync(btcMarket);
                await client.SubscribeTickerAsync(btcMarket);
                await client.SubscribeCandlesAsync(btcMarket, "1m");
                
                // Also subscribe to CRO_USDT (native token)
                var croMarket = new Market("CRO", "USDT");
                Console.WriteLine("Subscribing to CRO_USDT market data...");
                await client.SubscribeTickerAsync(croMarket);                // Run for 10 seconds
                Console.WriteLine("\nCollecting data for 10 seconds...\n");
                await SampleHelper.WaitForDurationOrEsc(10000);
                
                // Properly disconnect with cleanup
                await SampleHelper.SafeDisconnectAsync(client, "Crypto.com");
                Console.WriteLine("\nCrypto.com sample completed!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }
    }
}