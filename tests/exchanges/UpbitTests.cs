using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CCXT.Collector.Upbit;
using CCXT.Collector.Service;
using Xunit;

namespace CCXT.Collector.Tests.Exchanges
{
    /// <summary>
    /// Comprehensive test suite for Upbit exchange integration
    /// Tests Korean market specific features, KRW pairs, and market analysis
    /// </summary>
    
    [Trait("Category", "Exchange")]
    [Trait("Category", "Upbit")]
    public class UpbitTests : IDisposable
    {
        private UpbitWebSocketClient _client;
        private readonly List<string> _krwPairs = new() { "BTC/KRW", "ETH/KRW", "XRP/KRW" };
        private readonly List<string> _usdtPairs = new() { "BTC/USDT", "ETH/USDT" };
        private readonly int _testDuration = 10000; // 10 seconds per test

        
        public UpbitTests()
        {
            Console.WriteLine("=== Upbit Test Suite Initialization ===");
            _client = new UpbitWebSocketClient();
        }

        
        public void Dispose()
        {
            _client?.Dispose();
            Console.WriteLine("=== Upbit Test Suite Cleanup Complete ===\n");
        }

        #region Connection Tests

        [Fact]
        [Trait("Category", "Connection")]
        public async Task Test_KRW_Market_Connection()
        {
            Console.WriteLine("\n[TEST] KRW Market Connection");
            Console.WriteLine("----------------------------------------");
            
            var connected = false;
            var krwMarketsConnected = new HashSet<string>();
            
            _client.OnConnected += () => connected = true;
            
            await _client.ConnectAsync();
            
            foreach (var pair in _krwPairs)
            {
                await _client.SubscribeTickerAsync(pair);
            }
            
            await Task.Delay(3000);

            Assert.True(connected, "Failed to connect to Upbit");
            Assert.True(krwMarketsConnected.Count > 0, "No KRW markets connected");
            
            Console.WriteLine($"✅ Connected to {krwMarketsConnected.Count} KRW markets");
            foreach (var market in krwMarketsConnected)
            {
                Console.WriteLine($"   - {market}");
            }
        }

        [Fact]
        [Trait("Category", "Connection")]
        public async Task Test_Mixed_Market_Subscription()
        {
            Console.WriteLine("\n[TEST] Mixed Market Subscription (KRW + USDT)");
            Console.WriteLine("----------------------------------------");
            
            var krwData = new Dictionary<string, int>();
            var usdtData = new Dictionary<string, int>();
            
            _client.OnTickerReceived += (ticker) =>
            {
                if (ticker.symbol.EndsWith("/KRW"))
                {
                    krwData[ticker.symbol] = krwData.GetValueOrDefault(ticker.symbol) + 1;
                }
                else if (ticker.symbol.EndsWith("/USDT"))
                {
                    usdtData[ticker.symbol] = usdtData.GetValueOrDefault(ticker.symbol) + 1;
                }
            };

            await _client.ConnectAsync();
            
            // Subscribe to both KRW and USDT pairs
            foreach (var pair in _krwPairs.Concat(_usdtPairs))
            {
                await _client.SubscribeTickerAsync(pair);
            }
            
            await Task.Delay(_testDuration);
            
            Assert.True(krwData.Count > 0, "No KRW market data received");
            Assert.True(usdtData.Count > 0, "No USDT market data received");
            
            Console.WriteLine($"✅ KRW Markets: {krwData.Count} symbols, " +
                $"{krwData.Values.Sum()} updates");
            Console.WriteLine($"✅ USDT Markets: {usdtData.Count} symbols, " +
                $"{usdtData.Values.Sum()} updates");
        }

        #endregion

        #region Korean Market Specific Tests

        [Fact]
        [Trait("Category", "KoreanMarket")]
        public async Task Test_KRW_Premium_Calculation()
        {
            Console.WriteLine("\n[TEST] KRW Premium Calculation");
            Console.WriteLine("----------------------------------------");
            
            var krwPrices = new Dictionary<string, decimal>();
            var usdtPrices = new Dictionary<string, decimal>();
            var usdToKrw = 1300m; // Example exchange rate
            
            _client.OnTickerReceived += (ticker) =>
            {
                var baseSymbol = ticker.symbol.Split('/')[0];
                
                if (ticker.symbol.EndsWith("/KRW"))
                {
                    krwPrices[baseSymbol] = ticker.result.closePrice;
                }
                else if (ticker.symbol.EndsWith("/USDT"))
                {
                    usdtPrices[baseSymbol] = ticker.result.closePrice;
                }
            };

            await _client.ConnectAsync();
            
            // Subscribe to both BTC pairs for premium calculation
            await _client.SubscribeTickerAsync("BTC/KRW");
            await _client.SubscribeTickerAsync("BTC/USDT");
            await _client.SubscribeTickerAsync("ETH/KRW");
            await _client.SubscribeTickerAsync("ETH/USDT");
            
            await Task.Delay(_testDuration);
            
            // Calculate and display premiums
            Console.WriteLine("\n📊 Korea Premium Analysis:");
            foreach (var symbol in krwPrices.Keys.Intersect(usdtPrices.Keys))
            {
                var krwPrice = krwPrices[symbol];
                var usdtPrice = usdtPrices[symbol];
                var expectedKrwPrice = usdtPrice * usdToKrw;
                var premium = ((krwPrice - expectedKrwPrice) / expectedKrwPrice) * 100;
                
                Console.WriteLine($"   {symbol}:");
                Console.WriteLine($"      KRW Price: ₩{krwPrice:N0}");
                Console.WriteLine($"      USDT Price: ${usdtPrice:F2}");
                Console.WriteLine($"      Premium: {premium:F2}%");
                
                Assert.True(Math.Abs(premium) < 20, 
                    $"Unusual premium detected for {symbol}: {premium:F2}%");
            }
            
            Assert.True(krwPrices.Count > 0 && usdtPrices.Count > 0, 
                "Insufficient data for premium calculation");
        }

        [Fact]
        [Trait("Category", "KoreanMarket")]
        public async Task Test_KRW_Volume_Analysis()
        {
            Console.WriteLine("\n[TEST] KRW Market Volume Analysis");
            Console.WriteLine("----------------------------------------");
            
            var volumeData = new Dictionary<string, List<decimal>>();
            
            _client.OnTickerReceived += (ticker) =>
            {
                if (ticker.symbol.EndsWith("/KRW"))
                {
                    if (!volumeData.ContainsKey(ticker.symbol))
                        volumeData[ticker.symbol] = new List<decimal>();
                    
                    volumeData[ticker.symbol].Add(ticker.result.volume);
                }
            };

            await _client.ConnectAsync();
            
            foreach (var pair in _krwPairs)
            {
                await _client.SubscribeTickerAsync(pair);
            }
            
            await Task.Delay(_testDuration);
            
            Console.WriteLine("\n📊 KRW Market Volume Statistics:");
            
            var totalVolume = 0m;
            foreach (var kvp in volumeData)
            {
                var avgVolume = kvp.Value.Average();
                var maxVolume = kvp.Value.Max();
                var minVolume = kvp.Value.Min();
                totalVolume += avgVolume;
                
                Console.WriteLine($"   {kvp.Key}:");
                Console.WriteLine($"      Avg Volume: {avgVolume:F2}");
                Console.WriteLine($"      Max Volume: {maxVolume:F2}");
                Console.WriteLine($"      Min Volume: {minVolume:F2}");
            }
            
            Console.WriteLine($"\n   Total Market Volume: {totalVolume:F2}");
            
            Assert.True(volumeData.Count > 0, "No volume data collected");
            Assert.True(totalVolume > 0, "Zero total volume detected");
        }

        [Fact]
        [Trait("Category", "KoreanMarket")]
        public async Task Test_Payment_Coin_Monitoring()
        {
            Console.WriteLine("\n[TEST] Payment Coin Monitoring");
            Console.WriteLine("----------------------------------------");
            
            // Korean payment coins popular in Upbit
            var paymentCoins = new[] { "XRP/KRW", "ADA/KRW", "DOGE/KRW", "TRX/KRW" };
            var paymentCoinData = new Dictionary<string, PaymentCoinMetrics>();
            
            _client.OnTickerReceived += (ticker) =>
            {
                if (paymentCoins.Contains(ticker.symbol))
                {
                    if (!paymentCoinData.ContainsKey(ticker.symbol))
                    {
                        paymentCoinData[ticker.symbol] = new PaymentCoinMetrics
                        {
                            Symbol = ticker.symbol,
                            PriceHistory = new List<decimal>(),
                            VolumeHistory = new List<decimal>()
                        };
                    }
                    
                    var metrics = paymentCoinData[ticker.symbol];
                    metrics.PriceHistory.Add(ticker.result.closePrice);
                    metrics.VolumeHistory.Add(ticker.result.volume);
                    metrics.UpdateCount++;
                }
            };

            await _client.ConnectAsync();
            
            foreach (var coin in paymentCoins)
            {
                await _client.SubscribeTickerAsync(coin);
            }
            
            await Task.Delay(_testDuration);
            
            Console.WriteLine("\n📊 Payment Coin Analysis:");
            
            foreach (var kvp in paymentCoinData)
            {
                var metrics = kvp.Value;
                var priceVolatility = CalculateVolatility(metrics.PriceHistory);
                var avgVolume = metrics.VolumeHistory.Average();
                
                Console.WriteLine($"   {metrics.Symbol}:");
                Console.WriteLine($"      Updates: {metrics.UpdateCount}");
                Console.WriteLine($"      Price Range: ₩{metrics.PriceHistory.Min():N0} - ₩{metrics.PriceHistory.Max():N0}");
                Console.WriteLine($"      Volatility: {priceVolatility:F2}%");
                Console.WriteLine($"      Avg Volume: {avgVolume:F2}");
            }
            
            Assert.True(paymentCoinData.Count > 0, "No payment coin data collected");
        }

        #endregion

        #region Data Stream Tests

        [Fact]
        [Trait("Category", "DataStream")]
        public async Task Test_SOrderBooks_Depth()
        {
            Console.WriteLine("\n[TEST] SOrderBooks Depth Analysis");
            Console.WriteLine("----------------------------------------");
            
            var orderbookDepths = new Dictionary<string, List<int>>();
            
            _client.OnOrderbookReceived += (orderbook) =>
            {
                if (!orderbookDepths.ContainsKey(orderbook.symbol))
                    orderbookDepths[orderbook.symbol] = new List<int>();
                
                orderbookDepths[orderbook.symbol].Add(orderbook.result.bids.Count);
                
                // Analyze orderbook imbalance
                var bidVolume = orderbook.result.bids.Take(10).Sum(b => b.quantity);
                var askVolume = orderbook.result.asks.Take(10).Sum(a => a.quantity);
                var imbalance = (bidVolume - askVolume) / (bidVolume + askVolume) * 100;
                
                if (orderbookDepths[orderbook.symbol].Count % 10 == 0)
                {
                    Console.WriteLine($"📊 {orderbook.symbol} - Depth: {orderbook.result.bids.Count}, " +
                        $"Imbalance: {imbalance:F2}%");
                }
            };

            await _client.ConnectAsync();
            
            foreach (var pair in _krwPairs)
            {
                await _client.SubscribeOrderbookAsync(pair);
            }
            
            await Task.Delay(_testDuration);
            
            Console.WriteLine("\n📊 SOrderBooks Depth Summary:");
            foreach (var kvp in orderbookDepths)
            {
                var avgDepth = kvp.Value.Average();
                Console.WriteLine($"   {kvp.Key}: Avg Depth = {avgDepth:F1} levels");
            }
            
            Assert.True(orderbookDepths.Count > 0, "No orderbook data received");
        }

        [Fact]
        [Trait("Category", "DataStream")]
        public async Task Test_SCompleteOrders_Flow_Analysis()
        {
            Console.WriteLine("\n[TEST] SCompleteOrders Flow Analysis");
            Console.WriteLine("----------------------------------------");
            
            var tradeFlows = new Dictionary<string, SCompleteOrdersFlow>();
            
            _client.OnTradeReceived += (trade) =>
            {
                if (!tradeFlows.ContainsKey(trade.symbol))
                {
                    tradeFlows[trade.symbol] = new SCompleteOrdersFlow { Symbol = trade.symbol };
                }
                
                var flow = tradeFlows[trade.symbol];
                flow.TotalSCompleteOrderss++;
                
                if (trade.result[0].sideType == SideType.Bid)
                {
                    flow.BuyVolume += trade.result[0].amount * trade.result[0].price;
                    flow.BuyCount++;
                }
                else
                {
                    flow.SellVolume += trade.result[0].amount * trade.result[0].price;
                    flow.SellCount++;
                }
                
                if (flow.TotalSCompleteOrderss % 50 == 0)
                {
                    var netFlow = flow.BuyVolume - flow.SellVolume;
                    Console.WriteLine($"📊 {trade.symbol} - Net Flow: ₩{netFlow:N0}, " +
                        $"B/S Ratio: {(float)flow.BuyCount/flow.SellCount:F2}");
                }
            };

            await _client.ConnectAsync();
            
            foreach (var pair in _krwPairs)
            {
                await _client.SubscribeTradesAsync(pair);
            }
            
            await Task.Delay(_testDuration);
            
            Console.WriteLine("\n📊 SCompleteOrders Flow Summary:");
            foreach (var flow in tradeFlows.Values)
            {
                var netFlow = flow.BuyVolume - flow.SellVolume;
                var flowDirection = netFlow > 0 ? "Bullish" : "Bearish";
                
                Console.WriteLine($"   {flow.Symbol}:");
                Console.WriteLine($"      Total SCompleteOrderss: {flow.TotalSCompleteOrderss}");
                Console.WriteLine($"      Buy Volume: ₩{flow.BuyVolume:N0}");
                Console.WriteLine($"      Sell Volume: ₩{flow.SellVolume:N0}");
                Console.WriteLine($"      Net Flow: ₩{netFlow:N0} ({flowDirection})");
            }
            
            Assert.True(tradeFlows.Count > 0, "No trade flow data collected");
        }

        #endregion

        #region Market Analysis Tests

        [Fact]
        [Trait("Category", "MarketAnalysis")]
        public async Task Test_Top_Movers_Detection()
        {
            Console.WriteLine("\n[TEST] Top Movers Detection");
            Console.WriteLine("----------------------------------------");
            
            var priceChanges = new Dictionary<string, PriceChangeTracker>();
            
            _client.OnTickerReceived += (ticker) =>
            {
                if (!priceChanges.ContainsKey(ticker.symbol))
                {
                    priceChanges[ticker.symbol] = new PriceChangeTracker
                    {
                        Symbol = ticker.symbol,
                        InitialPrice = ticker.result.closePrice,
                        CurrentPrice = ticker.result.closePrice
                    };
                }
                
                priceChanges[ticker.symbol].CurrentPrice = ticker.result.closePrice;
                priceChanges[ticker.symbol].UpdateCount++;
            };

            await _client.ConnectAsync();
            
            // Subscribe to multiple KRW pairs for mover detection
            var extendedPairs = new[] { "BTC/KRW", "ETH/KRW", "XRP/KRW", "ADA/KRW", 
                                        "SOL/KRW", "DOGE/KRW", "AVAX/KRW", "MATIC/KRW" };
            
            foreach (var pair in extendedPairs)
            {
                await _client.SubscribeTickerAsync(pair);
            }
            
            await Task.Delay(_testDuration);
            
            // Calculate and rank price changes
            var movers = priceChanges.Values
                .Where(p => p.UpdateCount > 5)
                .Select(p => new
                {
                    p.Symbol,
                    ChangePercent = ((p.CurrentPrice - p.InitialPrice) / p.InitialPrice) * 100
                })
                .OrderByDescending(p => Math.Abs(p.ChangePercent))
                .ToList();
            
            Console.WriteLine("\n📊 Top Movers:");
            Console.WriteLine("   Top Gainers:");
            foreach (var gainer in movers.Where(m => m.ChangePercent > 0).Take(3))
            {
                Console.WriteLine($"      {gainer.Symbol}: +{gainer.ChangePercent:F3}%");
            }
            
            Console.WriteLine("   Top Losers:");
            foreach (var loser in movers.Where(m => m.ChangePercent < 0).Take(3))
            {
                Console.WriteLine($"      {loser.Symbol}: {loser.ChangePercent:F3}%");
            }
            
            Assert.True(movers.Count > 0, "No price movement data collected");
        }

        [Fact]
        [Trait("Category", "MarketAnalysis")]
        public async Task Test_Market_Correlation()
        {
            Console.WriteLine("\n[TEST] Market Correlation Analysis");
            Console.WriteLine("----------------------------------------");
            
            var priceData = new Dictionary<string, List<decimal>>();
            
            _client.OnTickerReceived += (ticker) =>
            {
                if (ticker.symbol.EndsWith("/KRW"))
                {
                    if (!priceData.ContainsKey(ticker.symbol))
                        priceData[ticker.symbol] = new List<decimal>();
                    
                    priceData[ticker.symbol].Add(ticker.result.closePrice);
                }
            };

            await _client.ConnectAsync();
            
            // Subscribe to major pairs for correlation analysis
            var correlationPairs = new[] { "BTC/KRW", "ETH/KRW", "XRP/KRW" };
            foreach (var pair in correlationPairs)
            {
                await _client.SubscribeTickerAsync(pair);
            }
            
            await Task.Delay(_testDuration);
            
            Console.WriteLine("\n📊 Market Correlation Matrix:");
            
            // Calculate simple correlation indicators
            foreach (var pair1 in priceData.Keys)
            {
                foreach (var pair2 in priceData.Keys)
                {
                    if (pair1.CompareTo(pair2) < 0)
                    {
                        var correlation = CalculateSimpleCorrelation(
                            priceData[pair1], priceData[pair2]);
                        
                        Console.WriteLine($"   {pair1} ↔ {pair2}: {correlation:F2}");
                    }
                }
            }
            
            Assert.True(priceData.Count >= 2, "Insufficient data for correlation analysis");
        }

        #endregion

        #region Performance Tests

        [Fact]
        [Trait("Category", "Performance")]
        public async Task Test_Message_Latency()
        {
            Console.WriteLine("\n[TEST] Message Latency Test");
            Console.WriteLine("----------------------------------------");
            
            var latencies = new List<double>();
            var timestamps = new Dictionary<string, DateTime>();
            
                        
            
            await _client.ConnectAsync();
            
            foreach (var pair in _krwPairs)
            {
                await _client.SubscribeTickerAsync(pair);
                await _client.SubscribeTradesAsync(pair);
            }
            
            await Task.Delay(_testDuration);
            
            if (latencies.Count > 0)
            {
                var avgLatency = latencies.Average();
                var minLatency = latencies.Min();
                var maxLatency = latencies.Max();
                var p95Latency = latencies.OrderBy(l => l)
                    .Skip((int)(latencies.Count * 0.95)).FirstOrDefault();
                
                Console.WriteLine("\n✅ Latency Statistics:");
                Console.WriteLine($"   Average: {avgLatency:F2}ms");
                Console.WriteLine($"   Minimum: {minLatency:F2}ms");
                Console.WriteLine($"   Maximum: {maxLatency:F2}ms");
                Console.WriteLine($"   P95: {p95Latency:F2}ms");
                
                Assert.True(avgLatency < 1000, $"High average latency: {avgLatency:F2}ms");
            }
        }

        [Fact]
        [Trait("Category", "Performance")]
        public async Task Test_Concurrent_Subscriptions()
        {
            Console.WriteLine("\n[TEST] Concurrent Subscription Test");
            Console.WriteLine("----------------------------------------");
            
            var subscriptionCount = 0;
            var messageCount = 0;
            
                        
            _client.OnTickerReceived += (_) => Interlocked.Increment(ref messageCount);
            _client.OnTradeReceived += (_) => Interlocked.Increment(ref messageCount);

            await _client.ConnectAsync();
            
            // Subscribe to many pairs concurrently
            var allPairs = new[] { 
                "BTC/KRW", "ETH/KRW", "XRP/KRW", "ADA/KRW", "SOL/KRW",
                "DOGE/KRW", "AVAX/KRW", "MATIC/KRW", "LINK/KRW", "DOT/KRW"
            };
            
            var tasks = allPairs.SelectMany(pair => new[]
            {
                _client.SubscribeTickerAsync(pair),
                _client.SubscribeTradesAsync(pair)
            }).ToArray();
            
            await Task.WhenAll(tasks);
            await Task.Delay(5000);
            
            var expectedSubscriptions = allPairs.Length * 2; // ticker + trades for each
            var messagesPerSecond = messageCount / 5.0;
            
            Console.WriteLine($"\n✅ Concurrent Subscription Results:");
            Console.WriteLine($"   Subscriptions: {subscriptionCount}/{expectedSubscriptions}");
            Console.WriteLine($"   Messages received: {messageCount}");
            Console.WriteLine($"   Messages/second: {messagesPerSecond:F2}");
            
            Assert.True(subscriptionCount == expectedSubscriptions, "Not all subscriptions successful");
            Assert.True(messageCount > 0, "No messages received");
        }

        #endregion

        #region Helper Methods

        private double CalculateVolatility(List<decimal> prices)
        {
            if (prices.Count < 2) return 0;
            
            var returns = new List<double>();
            for (int i = 1; i < prices.Count; i++)
            {
                var returnValue = (double)((prices[i] - prices[i-1]) / prices[i-1]);
                returns.Add(returnValue);
            }
            
            var avgReturn = returns.Average();
            var variance = returns.Select(r => Math.Pow(r - avgReturn, 2)).Average();
            return Math.Sqrt(variance) * 100;
        }

        private double CalculateSimpleCorrelation(List<decimal> series1, List<decimal> series2)
        {
            var minCount = Math.Min(series1.Count, series2.Count);
            if (minCount < 2) return 0;
            
            var changes1 = new List<double>();
            var changes2 = new List<double>();
            
            for (int i = 1; i < minCount; i++)
            {
                changes1.Add((double)((series1[i] - series1[i-1]) / series1[i-1]));
                changes2.Add((double)((series2[i] - series2[i-1]) / series2[i-1]));
            }
            
            // Simple direction correlation
            var sameDirection = 0;
            for (int i = 0; i < changes1.Count; i++)
            {
                if (Math.Sign(changes1[i]) == Math.Sign(changes2[i]))
                    sameDirection++;
            }
            
            return (double)sameDirection / changes1.Count;
        }

        #endregion

        #region Helper Classes

        private class PaymentCoinMetrics
        {
            public string Symbol { get; set; }
            public List<decimal> PriceHistory { get; set; }
            public List<decimal> VolumeHistory { get; set; }
            public int UpdateCount { get; set; }
        }

        private class SCompleteOrdersFlow
        {
            public string Symbol { get; set; }
            public int TotalSCompleteOrderss { get; set; }
            public int BuyCount { get; set; }
            public int SellCount { get; set; }
            public decimal BuyVolume { get; set; }
            public decimal SellVolume { get; set; }
        }

        private class PriceChangeTracker
        {
            public string Symbol { get; set; }
            public decimal InitialPrice { get; set; }
            public decimal CurrentPrice { get; set; }
            public int UpdateCount { get; set; }
        }

        #endregion
    }
}