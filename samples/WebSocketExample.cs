using System;
using System.Threading.Tasks;
using CCXT.Collector.Binance;
using CCXT.Collector.Upbit;
using CCXT.Collector.Bithumb;
using CCXT.Collector.Library;
using CCXT.Collector.Service;

namespace CCXT.Collector.Samples
{
    /// <summary>
    /// Example of using WebSocket clients with callback functions
    /// </summary>
    public class WebSocketExample
    {
        public static async Task Main(string[] args)
        {
            Console.WriteLine("CCXT.Collector WebSocket Example");
            Console.WriteLine("=================================\n");

            Console.WriteLine("Select Exchange:");
            Console.WriteLine("1. Binance");
            Console.WriteLine("2. Upbit");
            Console.WriteLine("3. Bithumb");
            Console.WriteLine("4. Multi-Exchange");
            
            Console.Write("Enter choice: ");
            var choice = Console.ReadLine();

            switch (choice)
            {
                case "1":
                    await RunBinanceExample();
                    break;
                case "2":
                    await RunUpbitExample();
                    break;
                case "3":
                    await RunBithumbExample();
                    break;
                case "4":
                    await RunMultiExchangeExample();
                    break;
                default:
                    Console.WriteLine("Invalid choice");
                    break;
            }
        }

        private static async Task RunBinanceExample()
        {
            Console.WriteLine("\n=== Binance WebSocket Example ===\n");

            using var client = new BinanceWebSocketClient();

            // Register callbacks
            client.OnConnected += () => Console.WriteLine("✅ Connected to Binance");
            client.OnDisconnected += () => Console.WriteLine("❌ Disconnected from Binance");
            client.OnError += (error) => Console.WriteLine($"⚠️ Error: {error}");

            // Orderbook callback
            client.OnOrderbookReceived += (orderbook) =>
            {
                Console.WriteLine($"📊 Orderbook {orderbook.symbol}:");
                if (orderbook.result.bids.Count > 0 && orderbook.result.asks.Count > 0)
                {
                    var bestBid = orderbook.result.bids[0];
                    var bestAsk = orderbook.result.asks[0];
                    var spread = bestAsk.price - bestBid.price;
                    var spreadPercent = (spread / bestBid.price) * 100;
                    
                    Console.WriteLine($"   Best Bid: {bestBid.price:F2} x {bestBid.quantity:F8}");
                    Console.WriteLine($"   Best Ask: {bestAsk.price:F2} x {bestAsk.quantity:F8}");
                    Console.WriteLine($"   Spread: {spread:F2} ({spreadPercent:F3}%)");
                }
            };

            // Trade callback
            client.OnTradeReceived += (trade) =>
            {
                var side = trade.result.sideType == SideType.Bid ? "BUY" : "SELL";
                Console.WriteLine($"💰 Trade {trade.symbol}: {side} {trade.result.quantity:F8} @ {trade.result.price:F2}");
            };

            // Ticker callback
            client.OnTickerReceived += (ticker) =>
            {
                Console.WriteLine($"📈 Ticker {ticker.symbol}:");
                Console.WriteLine($"   Price: {ticker.result.closePrice:F2} ({ticker.result.percentage:+0.00;-0.00}%)");
                Console.WriteLine($"   24h Volume: {ticker.result.volume:F2}");
            };

            // Connect and subscribe
            if (await client.ConnectAsync())
            {
                await client.SubscribeOrderbookAsync("BTC/USDT");
                await client.SubscribeTradesAsync("BTC/USDT");
                await client.SubscribeTickerAsync("BTC/USDT");

                Console.WriteLine("\nStreaming BTC/USDT data. Press any key to stop...");
                Console.ReadKey();
            }

            await client.DisconnectAsync();
        }

        private static async Task RunUpbitExample()
        {
            Console.WriteLine("\n=== Upbit WebSocket Example ===\n");

            using var client = new UpbitWebSocketClient();

            // Register callbacks
            client.OnConnected += () => Console.WriteLine("✅ Connected to Upbit");
            client.OnDisconnected += () => Console.WriteLine("❌ Disconnected from Upbit");
            client.OnError += (error) => Console.WriteLine($"⚠️ Error: {error}");

            // Orderbook callback - KRW market specific
            client.OnOrderbookReceived += (orderbook) =>
            {
                if (orderbook.symbol.EndsWith("/KRW"))
                {
                    Console.WriteLine($"📊 Orderbook {orderbook.symbol} (KRW Market):");
                    if (orderbook.result.bids.Count > 0 && orderbook.result.asks.Count > 0)
                    {
                        var bestBid = orderbook.result.bids[0];
                        var bestAsk = orderbook.result.asks[0];
                        Console.WriteLine($"   Best Bid: ₩{bestBid.price:N0}");
                        Console.WriteLine($"   Best Ask: ₩{bestAsk.price:N0}");
                        Console.WriteLine($"   Spread: ₩{bestAsk.price - bestBid.price:N0}");
                    }
                }
            };

            // Trade callback
            client.OnTradeReceived += (trade) =>
            {
                var side = trade.result.sideType == SideType.Bid ? "매수" : "매도";
                if (trade.symbol.EndsWith("/KRW"))
                {
                    Console.WriteLine($"💰 체결 {trade.symbol}: {side} {trade.result.quantity:F8} @ ₩{trade.result.price:N0}");
                }
            };

            // Ticker callback with KRW formatting
            client.OnTickerReceived += (ticker) =>
            {
                if (ticker.symbol.EndsWith("/KRW"))
                {
                    Console.WriteLine($"📈 시세 {ticker.symbol}:");
                    Console.WriteLine($"   현재가: ₩{ticker.result.closePrice:N0} ({ticker.result.percentage:+0.00;-0.00}%)");
                    Console.WriteLine($"   24시간 거래량: {ticker.result.volume:F2}");
                }
            };

            // Connect and subscribe to KRW markets
            if (await client.ConnectAsync())
            {
                await client.SubscribeOrderbookAsync("BTC/KRW");
                await client.SubscribeTradesAsync("BTC/KRW");
                await client.SubscribeTickerAsync("BTC/KRW");
                
                // Also subscribe to ETH
                await client.SubscribeTickerAsync("ETH/KRW");

                Console.WriteLine("\nStreaming KRW market data. Press any key to stop...");
                Console.ReadKey();
            }

            await client.DisconnectAsync();
        }

        private static async Task RunBithumbExample()
        {
            Console.WriteLine("\n=== Bithumb WebSocket Example ===\n");

            using var client = new BithumbWebSocketClient();

            // Track payment coins
            var paymentCoins = new[] { "XRP/KRW", "ADA/KRW", "DOGE/KRW", "TRX/KRW" };

            // Register callbacks
            client.OnConnected += () => Console.WriteLine("✅ Connected to Bithumb");
            client.OnDisconnected += () => Console.WriteLine("❌ Disconnected from Bithumb");
            client.OnError += (error) => Console.WriteLine($"⚠️ Error: {error}");

            // Payment coin specific tracking
            client.OnTickerReceived += (ticker) =>
            {
                if (Array.Exists(paymentCoins, pc => pc == ticker.symbol))
                {
                    Console.WriteLine($"🪙 Payment Coin {ticker.symbol}:");
                    Console.WriteLine($"   Price: ₩{ticker.result.closePrice:N0}");
                    Console.WriteLine($"   Volume: {ticker.result.volume:F2}");
                    Console.WriteLine($"   Change: {ticker.result.percentage:+0.00;-0.00}%");
                }
            };

            // Trade flow analysis
            var buyVolume = 0m;
            var sellVolume = 0m;
            client.OnTradeReceived += (trade) =>
            {
                if (trade.result.sideType == SideType.Bid)
                    buyVolume += trade.result.amount;
                else
                    sellVolume += trade.result.amount;

                var netFlow = buyVolume - sellVolume;
                var flowDirection = netFlow > 0 ? "🟢 Bullish" : "🔴 Bearish";
                
                Console.WriteLine($"💰 Trade {trade.symbol}: {trade.result.quantity:F8} @ ₩{trade.result.price:N0}");
                Console.WriteLine($"   Net Flow: ₩{netFlow:N0} {flowDirection}");
            };

            // Connect and subscribe to payment coins
            if (await client.ConnectAsync())
            {
                foreach (var coin in paymentCoins)
                {
                    await client.SubscribeTickerAsync(coin);
                    await client.SubscribeTradesAsync(coin);
                }

                Console.WriteLine("\nStreaming Bithumb payment coin data. Press any key to stop...");
                Console.ReadKey();
            }

            await client.DisconnectAsync();
        }

        private static async Task RunMultiExchangeExample()
        {
            Console.WriteLine("\n=== Multi-Exchange WebSocket Example ===\n");

            // Create clients for each exchange
            var binanceClient = new BinanceWebSocketClient();
            var upbitClient = new UpbitWebSocketClient();
            var bithumbClient = new BithumbWebSocketClient();

            // Track prices across exchanges for arbitrage detection
            var prices = new System.Collections.Concurrent.ConcurrentDictionary<string, decimal>();

            // Binance BTC/USDT
            binanceClient.OnTickerReceived += (ticker) =>
            {
                if (ticker.symbol == "BTC/USDT")
                {
                    prices["Binance_BTC_USD"] = ticker.result.closePrice;
                    CheckArbitrage();
                }
            };

            // Upbit BTC/KRW
            upbitClient.OnTickerReceived += (ticker) =>
            {
                if (ticker.symbol == "BTC/KRW")
                {
                    prices["Upbit_BTC_KRW"] = ticker.result.closePrice;
                    CheckArbitrage();
                }
            };

            // Bithumb BTC/KRW
            bithumbClient.OnTickerReceived += (ticker) =>
            {
                if (ticker.symbol == "BTC/KRW")
                {
                    prices["Bithumb_BTC_KRW"] = ticker.result.closePrice;
                    CheckArbitrage();
                }
            };

            void CheckArbitrage()
            {
                // Simple arbitrage check between Korean exchanges
                if (prices.ContainsKey("Upbit_BTC_KRW") && prices.ContainsKey("Bithumb_BTC_KRW"))
                {
                    var upbitPrice = prices["Upbit_BTC_KRW"];
                    var bithumbPrice = prices["Bithumb_BTC_KRW"];
                    var diff = Math.Abs(upbitPrice - bithumbPrice);
                    var diffPercent = (diff / Math.Min(upbitPrice, bithumbPrice)) * 100;

                    if (diffPercent > 0.1m) // 0.1% difference
                    {
                        Console.WriteLine($"\n🎯 Arbitrage Opportunity Detected!");
                        Console.WriteLine($"   Upbit: ₩{upbitPrice:N0}");
                        Console.WriteLine($"   Bithumb: ₩{bithumbPrice:N0}");
                        Console.WriteLine($"   Difference: ₩{diff:N0} ({diffPercent:F2}%)");
                        
                        if (upbitPrice > bithumbPrice)
                            Console.WriteLine($"   Strategy: Buy on Bithumb, Sell on Upbit");
                        else
                            Console.WriteLine($"   Strategy: Buy on Upbit, Sell on Bithumb");
                    }
                }

                // Display current prices
                Console.WriteLine($"\n📊 Current Prices:");
                foreach (var kvp in prices)
                {
                    var parts = kvp.Key.Split('_');
                    var exchange = parts[0];
                    var pair = $"{parts[1]}/{parts[2]}";
                    
                    if (pair.EndsWith("/KRW"))
                        Console.WriteLine($"   {exchange} {pair}: ₩{kvp.Value:N0}");
                    else
                        Console.WriteLine($"   {exchange} {pair}: ${kvp.Value:F2}");
                }
            }

            // Connect all exchanges
            var connectTasks = new[]
            {
                binanceClient.ConnectAsync(),
                upbitClient.ConnectAsync(),
                bithumbClient.ConnectAsync()
            };

            await Task.WhenAll(connectTasks);

            // Subscribe to BTC markets
            await binanceClient.SubscribeTickerAsync("BTC/USDT");
            await upbitClient.SubscribeTickerAsync("BTC/KRW");
            await bithumbClient.SubscribeTickerAsync("BTC/KRW");

            Console.WriteLine("\nMonitoring cross-exchange arbitrage. Press any key to stop...");
            Console.ReadKey();

            // Disconnect all
            await Task.WhenAll(
                binanceClient.DisconnectAsync(),
                upbitClient.DisconnectAsync(),
                bithumbClient.DisconnectAsync()
            );
        }
    }
}