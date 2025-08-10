using System;
using System.Threading.Tasks;
using CCXT.Collector.Kucoin;
using CCXT.Collector.Library;
using CCXT.Collector.Service;

namespace CCXT.Collector.Samples.Exchanges
{
    /// <summary>
    /// KuCoin WebSocket sample - Global exchange with diverse altcoins
    /// </summary>
    public class KucoinExample
    {
        public static async Task RunSample()
        {
            Console.WriteLine("\n=== KuCoin WebSocket Sample ===");
            Console.WriteLine("Connecting to KuCoin (The People's Exchange)...\n");

            var client = new KucoinWebSocketClient();
            
            // Event handlers
            client.OnConnected += () => Console.WriteLine("[Connected] KuCoin WebSocket connected");
            client.OnDisconnected += () => Console.WriteLine("[Disconnected] KuCoin WebSocket disconnected");
            client.OnError += (error) => Console.WriteLine($"[Error] {error}");

            // Market data handlers
            client.OnOrderbookReceived += (orderbook) =>
            {
                if (orderbook.result.bids.Count > 0 && orderbook.result.asks.Count > 0)
                {
                    Console.WriteLine($"[Level2] {orderbook.symbol}");
                    Console.WriteLine($"  Best Bid: ${orderbook.result.bids[0].price:F4} x {orderbook.result.bids[0].quantity:F6}");
                    Console.WriteLine($"  Best Ask: ${orderbook.result.asks[0].price:F4} x {orderbook.result.asks[0].quantity:F6}");
                    Console.WriteLine($"  Spread: ${orderbook.result.asks[0].price - orderbook.result.bids[0].price:F6}");
                }
            };

            client.OnTradeReceived += (trade) =>
            {
                if (trade.result.Count > 0)
                {
                    var t = trade.result[0];
                    var side = t.sideType == SideType.Bid ? "BUY" : "SELL";
                    Console.WriteLine($"[Match] {trade.symbol} - ${t.price:F4} x {t.quantity:F6} [{side}]");
                }
            };

            client.OnTickerReceived += (ticker) =>
            {
                Console.WriteLine($"[Ticker] {ticker.symbol}");
                Console.WriteLine($"  Price: ${ticker.result.closePrice:F4}");
                Console.WriteLine($"  24h Change: {ticker.result.percentage:+0.00;-0.00}%");
                Console.WriteLine($"  24h Vol: {ticker.result.volume:F2}");
                Console.WriteLine($"  24h Vol Value: ${ticker.result.quoteVolume:N0}");
            };

            client.OnCandleReceived += (candle) =>
            {
                if (candle.result.Count > 0)
                {
                    var c = candle.result[0];
                    Console.WriteLine($"[Candles] {candle.symbol} {candle.interval}");
                    Console.WriteLine($"  OHLCV: ${c.open:F4} / ${c.high:F4} / ${c.low:F4} / ${c.close:F4} / {c.volume:F2}");
                }
            };

            try
            {
                // Connect to WebSocket
                await client.ConnectAsync();
                
                // Subscribe to BTC-USDT market (KuCoin uses hyphen)
                var btcMarket = new Market("BTC", "USDT");
                
                Console.WriteLine("Subscribing to BTC-USDT market data...");
                await client.SubscribeOrderbookAsync(btcMarket);
                await client.SubscribeTradesAsync(btcMarket);
                await client.SubscribeTickerAsync(btcMarket);
                await client.SubscribeCandlesAsync(btcMarket, "1min");
                
                // Also subscribe to ETH-USDT
                var ethMarket = new Market("ETH", "USDT");
                Console.WriteLine("Subscribing to ETH-USDT market data...");
                await client.SubscribeOrderbookAsync(ethMarket);
                await client.SubscribeTickerAsync(ethMarket);
                
                // Also subscribe to KCS-USDT (KuCoin Token)
                var kcsMarket = new Market("KCS", "USDT");
                Console.WriteLine("Subscribing to KCS-USDT market data...");
                await client.SubscribeTickerAsync(kcsMarket);                // Run for 10 seconds
                Console.WriteLine("\nCollecting data for 10 seconds...\n");
                await SampleHelper.WaitForDurationOrEsc(10000);
                
                // Properly disconnect with cleanup
                await SampleHelper.SafeDisconnectAsync(client, "KuCoin");
                Console.WriteLine("\nKuCoin sample completed!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }
    }
}