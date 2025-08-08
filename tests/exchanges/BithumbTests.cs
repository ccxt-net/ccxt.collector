using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CCXT.Collector.Bithumb;
using CCXT.Collector.Service;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CCXT.Collector.Tests.Exchanges
{
    /// <summary>
    /// Comprehensive test suite for Bithumb exchange integration
    /// Tests payment coins, market depth, arbitrage opportunities, and Korean market features
    /// </summary>
    [TestClass]
    [TestCategory("Exchange")]
    [TestCategory("Bithumb")]
    public class BithumbTests
    {
        private BithumbClient _client;
        private readonly List<string> _majorPairs = new() { "BTC/KRW", "ETH/KRW", "XRP/KRW" };
        private readonly List<string> _paymentCoins = new() { "XRP/KRW", "TRX/KRW", "ADA/KRW", "DOGE/KRW" };
        private readonly int _testDuration = 10000; // 10 seconds per test

        [TestInitialize]
        public void Setup()
        {
            Console.WriteLine("=== Bithumb Test Suite Initialization ===");
            _client = new BithumbClient("public");
        }

        [TestCleanup]
        public void Cleanup()
        {
            _client?.Dispose();
            Console.WriteLine("=== Bithumb Test Suite Cleanup Complete ===\n");
        }

        #region Connection Tests

        [TestMethod]
        [TestCategory("Connection")]
        public async Task Test_Basic_Connection()
        {
            Console.WriteLine("\n[TEST] Basic WebSocket Connection");
            Console.WriteLine("----------------------------------------");
            
            var connected = false;
            var connectionAttempts = 0;
            var connectionTime = Stopwatch.StartNew();
            
            _client.OnConnectionAttempt += () => connectionAttempts++;
            _client.OnConnected += () =>
            {
                connected = true;
                connectionTime.Stop();
            };

            await _client.ConnectAsync();
            await Task.Delay(3000);

            Assert.IsTrue(connected, "Failed to establish connection to Bithumb");
            Assert.AreEqual(1, connectionAttempts, "Multiple connection attempts detected");
            
            Console.WriteLine($"✅ Connected successfully in {connectionTime.ElapsedMilliseconds}ms");
            Console.WriteLine($"   Connection attempts: {connectionAttempts}");
        }

        [TestMethod]
        [TestCategory("Connection")]
        public async Task Test_Payment_Coin_Subscription()
        {
            Console.WriteLine("\n[TEST] Payment Coin Channel Subscription");
            Console.WriteLine("----------------------------------------");
            
            var subscribedChannels = new HashSet<string>();
            var dataReceived = new Dictionary<string, int>();
            
            _client.OnChannelSubscribed += (channel) =>
            {
                subscribedChannels.Add(channel);
                Console.WriteLine($"✅ Subscribed to channel: {channel}");
            };
            
            _client.OnTickerReceived += (ticker) =>
            {
                if (_paymentCoins.Contains(ticker.symbol))
                {
                    dataReceived[ticker.symbol] = dataReceived.GetValueOrDefault(ticker.symbol) + 1;
                }
            };

            await _client.ConnectAsync();
            
            foreach (var coin in _paymentCoins)
            {
                await _client.SubscribeTicker(coin);
            }
            
            await Task.Delay(_testDuration);
            
            Assert.IsTrue(subscribedChannels.Count > 0, "No channels subscribed");
            Assert.IsTrue(dataReceived.Count > 0, "No payment coin data received");
            
            Console.WriteLine($"\n📊 Payment Coin Data Summary:");
            foreach (var kvp in dataReceived)
            {
                Console.WriteLine($"   {kvp.Key}: {kvp.Value} updates");
            }
        }

        [TestMethod]
        [TestCategory("Connection")]
        public async Task Test_Error_Recovery()
        {
            Console.WriteLine("\n[TEST] Error Recovery Mechanism");
            Console.WriteLine("----------------------------------------");
            
            var errorCount = 0;
            var recoveryCount = 0;
            var errorMessages = new List<string>();
            
            _client.OnError += (error) =>
            {
                errorCount++;
                errorMessages.Add(error);
                Console.WriteLine($"❌ Error #{errorCount}: {error}");
            };
            
            _client.OnRecovery += () =>
            {
                recoveryCount++;
                Console.WriteLine($"✅ Recovery #{recoveryCount} successful");
            };

            await _client.ConnectAsync();
            
            // Subscribe to invalid symbol to trigger error
            try
            {
                await _client.SubscribeTicker("INVALID/KRW");
            }
            catch { }
            
            // Subscribe to valid symbol to test recovery
            await _client.SubscribeTicker("BTC/KRW");
            
            await Task.Delay(5000);
            
            if (errorCount > 0)
            {
                Assert.AreEqual(errorCount, recoveryCount, 
                    "Not all errors were recovered");
                Console.WriteLine($"✅ All {errorCount} errors recovered successfully");
            }
            else
            {
                Console.WriteLine("✅ No errors encountered during test");
            }
        }

        #endregion

        #region Payment Coin Tests

        [TestMethod]
        [TestCategory("PaymentCoins")]
        public async Task Test_Payment_Coin_Analysis()
        {
            Console.WriteLine("\n[TEST] Payment Coin Comprehensive Analysis");
            Console.WriteLine("----------------------------------------");
            
            var paymentCoinData = new Dictionary<string, PaymentCoinAnalysis>();
            
            _client.OnTickerReceived += (ticker) =>
            {
                if (_paymentCoins.Contains(ticker.symbol))
                {
                    if (!paymentCoinData.ContainsKey(ticker.symbol))
                    {
                        paymentCoinData[ticker.symbol] = new PaymentCoinAnalysis
                        {
                            Symbol = ticker.symbol,
                            Prices = new List<decimal>(),
                            Volumes = new List<decimal>(),
                            Spreads = new List<decimal>()
                        };
                    }
                    
                    var analysis = paymentCoinData[ticker.symbol];
                    analysis.Prices.Add(ticker.last);
                    analysis.Volumes.Add(ticker.baseVolume);
                    analysis.Spreads.Add(ticker.ask - ticker.bid);
                    analysis.UpdateCount++;
                }
            };

            await _client.ConnectAsync();
            
            foreach (var coin in _paymentCoins)
            {
                await _client.SubscribeTicker(coin);
            }
            
            await Task.Delay(_testDuration);
            
            Console.WriteLine("\n📊 Payment Coin Analysis Results:");
            
            foreach (var kvp in paymentCoinData)
            {
                var analysis = kvp.Value;
                var avgPrice = analysis.Prices.Average();
                var priceVolatility = CalculateVolatility(analysis.Prices);
                var avgVolume = analysis.Volumes.Average();
                var avgSpread = analysis.Spreads.Average();
                var spreadPercent = (avgSpread / avgPrice) * 100;
                
                Console.WriteLine($"\n   {analysis.Symbol}:");
                Console.WriteLine($"      Updates: {analysis.UpdateCount}");
                Console.WriteLine($"      Avg Price: ₩{avgPrice:N2}");
                Console.WriteLine($"      Volatility: {priceVolatility:F3}%");
                Console.WriteLine($"      Avg Volume: {avgVolume:F2}");
                Console.WriteLine($"      Avg Spread: ₩{avgSpread:F2} ({spreadPercent:F3}%)");
                
                // Classify coin based on metrics
                var classification = ClassifyPaymentCoin(priceVolatility, avgVolume, spreadPercent);
                Console.WriteLine($"      Classification: {classification}");
            }
            
            Assert.IsTrue(paymentCoinData.Count > 0, "No payment coin data collected");
        }

        [TestMethod]
        [TestCategory("PaymentCoins")]
        public async Task Test_Payment_Coin_Correlation()
        {
            Console.WriteLine("\n[TEST] Payment Coin Correlation Analysis");
            Console.WriteLine("----------------------------------------");
            
            var priceMovements = new Dictionary<string, List<double>>();
            var previousPrices = new Dictionary<string, decimal>();
            
            _client.OnTickerReceived += (ticker) =>
            {
                if (_paymentCoins.Contains(ticker.symbol))
                {
                    if (previousPrices.ContainsKey(ticker.symbol))
                    {
                        var movement = (double)((ticker.last - previousPrices[ticker.symbol]) 
                            / previousPrices[ticker.symbol] * 100);
                        
                        if (!priceMovements.ContainsKey(ticker.symbol))
                            priceMovements[ticker.symbol] = new List<double>();
                        
                        priceMovements[ticker.symbol].Add(movement);
                    }
                    
                    previousPrices[ticker.symbol] = ticker.last;
                }
            };

            await _client.ConnectAsync();
            
            foreach (var coin in _paymentCoins)
            {
                await _client.SubscribeTicker(coin);
            }
            
            await Task.Delay(_testDuration);
            
            Console.WriteLine("\n📊 Payment Coin Movement Correlation:");
            
            // Calculate correlation between payment coins
            var correlationMatrix = CalculateCorrelationMatrix(priceMovements);
            
            foreach (var kvp in correlationMatrix)
            {
                Console.WriteLine($"   {kvp.Key}: {kvp.Value:F3}");
                
                if (kvp.Value > 0.7)
                    Console.WriteLine($"      → Strong positive correlation");
                else if (kvp.Value < -0.7)
                    Console.WriteLine($"      → Strong negative correlation");
            }
            
            Assert.IsTrue(priceMovements.Count >= 2, 
                "Insufficient data for correlation analysis");
        }

        #endregion

        #region Market Depth Tests

        [TestMethod]
        [TestCategory("MarketDepth")]
        public async Task Test_Orderbook_Depth_Analysis()
        {
            Console.WriteLine("\n[TEST] Market Depth Analysis");
            Console.WriteLine("----------------------------------------");
            
            var depthAnalysis = new Dictionary<string, MarketDepthAnalysis>();
            
            _client.OnOrderbookReceived += (orderbook) =>
            {
                if (!depthAnalysis.ContainsKey(orderbook.symbol))
                {
                    depthAnalysis[orderbook.symbol] = new MarketDepthAnalysis
                    {
                        Symbol = orderbook.symbol,
                        BidDepths = new List<decimal>(),
                        AskDepths = new List<decimal>(),
                        Imbalances = new List<double>()
                    };
                }
                
                var analysis = depthAnalysis[orderbook.symbol];
                
                // Calculate total bid/ask depth (top 10 levels)
                var bidDepth = orderbook.bids.Take(10).Sum(b => b.quantity * b.price);
                var askDepth = orderbook.asks.Take(10).Sum(a => a.quantity * a.price);
                var imbalance = (double)((bidDepth - askDepth) / (bidDepth + askDepth));
                
                analysis.BidDepths.Add(bidDepth);
                analysis.AskDepths.Add(askDepth);
                analysis.Imbalances.Add(imbalance);
                analysis.UpdateCount++;
                
                if (analysis.UpdateCount % 10 == 0)
                {
                    Console.WriteLine($"📊 {orderbook.symbol} - " +
                        $"Bid: ₩{bidDepth:N0}, Ask: ₩{askDepth:N0}, " +
                        $"Imbalance: {imbalance:F3}");
                }
            };

            await _client.ConnectAsync();
            
            foreach (var pair in _majorPairs)
            {
                await _client.SubscribeOrderbook(pair);
            }
            
            await Task.Delay(_testDuration);
            
            Console.WriteLine("\n📊 Market Depth Summary:");
            
            foreach (var kvp in depthAnalysis)
            {
                var analysis = kvp.Value;
                var avgBidDepth = analysis.BidDepths.Average();
                var avgAskDepth = analysis.AskDepths.Average();
                var avgImbalance = analysis.Imbalances.Average();
                
                Console.WriteLine($"\n   {analysis.Symbol}:");
                Console.WriteLine($"      Updates: {analysis.UpdateCount}");
                Console.WriteLine($"      Avg Bid Depth: ₩{avgBidDepth:N0}");
                Console.WriteLine($"      Avg Ask Depth: ₩{avgAskDepth:N0}");
                Console.WriteLine($"      Avg Imbalance: {avgImbalance:F3}");
                
                var marketCondition = avgImbalance > 0.1 ? "Bullish" : 
                                     avgImbalance < -0.1 ? "Bearish" : "Neutral";
                Console.WriteLine($"      Market Condition: {marketCondition}");
            }
            
            Assert.IsTrue(depthAnalysis.Count > 0, "No market depth data collected");
        }

        [TestMethod]
        [TestCategory("MarketDepth")]
        public async Task Test_Liquidity_Analysis()
        {
            Console.WriteLine("\n[TEST] Liquidity Analysis");
            Console.WriteLine("----------------------------------------");
            
            var liquidityData = new Dictionary<string, LiquidityMetrics>();
            
            _client.OnOrderbookReceived += (orderbook) =>
            {
                if (!liquidityData.ContainsKey(orderbook.symbol))
                {
                    liquidityData[orderbook.symbol] = new LiquidityMetrics
                    {
                        Symbol = orderbook.symbol,
                        SpreadHistory = new List<decimal>(),
                        DepthHistory = new List<int>(),
                        SlippageEstimates = new List<decimal>()
                    };
                }
                
                var metrics = liquidityData[orderbook.symbol];
                
                // Calculate spread
                var spread = orderbook.asks[0].price - orderbook.bids[0].price;
                metrics.SpreadHistory.Add(spread);
                
                // Calculate depth
                var totalLevels = orderbook.bids.Count + orderbook.asks.Count;
                metrics.DepthHistory.Add(totalLevels);
                
                // Estimate slippage for a standard order size
                var orderSize = 1.0m; // 1 BTC equivalent
                var slippage = EstimateSlippage(orderbook, orderSize);
                metrics.SlippageEstimates.Add(slippage);
                
                metrics.UpdateCount++;
            };

            await _client.ConnectAsync();
            
            foreach (var pair in _majorPairs)
            {
                await _client.SubscribeOrderbook(pair);
            }
            
            await Task.Delay(_testDuration);
            
            Console.WriteLine("\n📊 Liquidity Analysis Results:");
            
            foreach (var kvp in liquidityData)
            {
                var metrics = kvp.Value;
                var avgSpread = metrics.SpreadHistory.Average();
                var avgDepth = metrics.DepthHistory.Average();
                var avgSlippage = metrics.SlippageEstimates.Average();
                
                Console.WriteLine($"\n   {metrics.Symbol}:");
                Console.WriteLine($"      Avg Spread: ₩{avgSpread:F2}");
                Console.WriteLine($"      Avg Depth: {avgDepth:F1} levels");
                Console.WriteLine($"      Avg Slippage (1 unit): {avgSlippage:F3}%");
                
                var liquidityScore = CalculateLiquidityScore(avgSpread, avgDepth, avgSlippage);
                Console.WriteLine($"      Liquidity Score: {liquidityScore}/10");
            }
            
            Assert.IsTrue(liquidityData.Count > 0, "No liquidity data collected");
        }

        #endregion

        #region Arbitrage Tests

        [TestMethod]
        [TestCategory("Arbitrage")]
        public async Task Test_Cross_Exchange_Arbitrage()
        {
            Console.WriteLine("\n[TEST] Cross-Exchange Arbitrage Detection");
            Console.WriteLine("----------------------------------------");
            
            var bithumbPrices = new Dictionary<string, decimal>();
            var arbitrageOpportunities = new List<ArbitrageOpportunity>();
            
            // Simulated external exchange prices for testing
            var externalPrices = new Dictionary<string, decimal>
            {
                { "BTC/KRW", 50000000m },
                { "ETH/KRW", 3000000m },
                { "XRP/KRW", 1000m }
            };
            
            _client.OnTickerReceived += (ticker) =>
            {
                bithumbPrices[ticker.symbol] = ticker.last;
                
                if (externalPrices.ContainsKey(ticker.symbol))
                {
                    var priceDiff = ticker.last - externalPrices[ticker.symbol];
                    var percentDiff = (priceDiff / externalPrices[ticker.symbol]) * 100;
                    
                    if (Math.Abs(percentDiff) > 0.5m) // 0.5% threshold
                    {
                        var opportunity = new ArbitrageOpportunity
                        {
                            Symbol = ticker.symbol,
                            BithumbPrice = ticker.last,
                            ExternalPrice = externalPrices[ticker.symbol],
                            PriceDifference = priceDiff,
                            PercentDifference = percentDiff,
                            Timestamp = DateTime.UtcNow
                        };
                        
                        arbitrageOpportunities.Add(opportunity);
                        
                        Console.WriteLine($"🎯 Arbitrage Opportunity Detected!");
                        Console.WriteLine($"   Symbol: {ticker.symbol}");
                        Console.WriteLine($"   Bithumb: ₩{ticker.last:N0}");
                        Console.WriteLine($"   External: ₩{externalPrices[ticker.symbol]:N0}");
                        Console.WriteLine($"   Difference: {percentDiff:F2}%");
                    }
                }
            };

            await _client.ConnectAsync();
            
            foreach (var pair in _majorPairs)
            {
                await _client.SubscribeTicker(pair);
            }
            
            await Task.Delay(_testDuration);
            
            Console.WriteLine($"\n📊 Arbitrage Summary:");
            Console.WriteLine($"   Total opportunities: {arbitrageOpportunities.Count}");
            
            if (arbitrageOpportunities.Count > 0)
            {
                var avgProfit = arbitrageOpportunities.Average(a => Math.Abs(a.PercentDifference));
                Console.WriteLine($"   Average profit potential: {avgProfit:F2}%");
            }
            
            Assert.IsTrue(bithumbPrices.Count > 0, "No price data collected");
        }

        [TestMethod]
        [TestCategory("Arbitrage")]
        public async Task Test_Triangular_Arbitrage()
        {
            Console.WriteLine("\n[TEST] Triangular Arbitrage Detection");
            Console.WriteLine("----------------------------------------");
            
            var prices = new Dictionary<string, decimal>();
            var triangularOpportunities = new List<TriangularArbitrage>();
            
            _client.OnTickerReceived += (ticker) =>
            {
                prices[ticker.symbol] = ticker.last;
                
                // Check for triangular arbitrage opportunity
                // Example: KRW -> BTC -> ETH -> KRW
                if (prices.ContainsKey("BTC/KRW") && 
                    prices.ContainsKey("ETH/KRW") && 
                    prices.ContainsKey("ETH/BTC"))
                {
                    var btcKrw = prices["BTC/KRW"];
                    var ethKrw = prices["ETH/KRW"];
                    var ethBtc = prices.GetValueOrDefault("ETH/BTC", ethKrw / btcKrw);
                    
                    // Calculate triangular arbitrage
                    var startAmount = 1000000m; // Start with 1M KRW
                    var btcAmount = startAmount / btcKrw;
                    var ethAmount = btcAmount / ethBtc;
                    var finalKrw = ethAmount * ethKrw;
                    
                    var profit = finalKrw - startAmount;
                    var profitPercent = (profit / startAmount) * 100;
                    
                    if (profitPercent > 0.1m) // 0.1% threshold
                    {
                        var opportunity = new TriangularArbitrage
                        {
                            Path = "KRW -> BTC -> ETH -> KRW",
                            StartAmount = startAmount,
                            FinalAmount = finalKrw,
                            Profit = profit,
                            ProfitPercent = profitPercent,
                            Timestamp = DateTime.UtcNow
                        };
                        
                        triangularOpportunities.Add(opportunity);
                        
                        Console.WriteLine($"🎯 Triangular Arbitrage Found!");
                        Console.WriteLine($"   Path: {opportunity.Path}");
                        Console.WriteLine($"   Profit: ₩{profit:N0} ({profitPercent:F3}%)");
                    }
                }
            };

            await _client.ConnectAsync();
            
            var triangularPairs = new[] { "BTC/KRW", "ETH/KRW", "XRP/KRW" };
            foreach (var pair in triangularPairs)
            {
                await _client.SubscribeTicker(pair);
            }
            
            await Task.Delay(_testDuration);
            
            Console.WriteLine($"\n📊 Triangular Arbitrage Summary:");
            Console.WriteLine($"   Opportunities found: {triangularOpportunities.Count}");
            
            if (triangularOpportunities.Count > 0)
            {
                var maxProfit = triangularOpportunities.Max(t => t.ProfitPercent);
                Console.WriteLine($"   Max profit potential: {maxProfit:F3}%");
            }
            
            Assert.IsTrue(prices.Count > 0, "No price data collected");
        }

        #endregion

        #region Performance Tests

        [TestMethod]
        [TestCategory("Performance")]
        public async Task Test_Data_Throughput()
        {
            Console.WriteLine("\n[TEST] Data Throughput Test");
            Console.WriteLine("----------------------------------------");
            
            var messageCount = 0;
            var byteCount = 0L;
            var startTime = DateTime.UtcNow;
            
            _client.OnRawMessage += (message) =>
            {
                Interlocked.Increment(ref messageCount);
                Interlocked.Add(ref byteCount, message.Length);
            };

            await _client.ConnectAsync();
            
            // Subscribe to all test pairs
            var allPairs = _majorPairs.Concat(_paymentCoins).Distinct();
            foreach (var pair in allPairs)
            {
                await _client.SubscribeOrderbook(pair);
                await _client.SubscribeTicker(pair);
                await _client.SubscribeTrades(pair);
            }
            
            await Task.Delay(_testDuration);
            
            var duration = (DateTime.UtcNow - startTime).TotalSeconds;
            var messagesPerSecond = messageCount / duration;
            var mbPerSecond = (byteCount / 1024.0 / 1024.0) / duration;
            
            Console.WriteLine("\n✅ Throughput Metrics:");
            Console.WriteLine($"   Total messages: {messageCount}");
            Console.WriteLine($"   Messages/second: {messagesPerSecond:F2}");
            Console.WriteLine($"   Data rate: {mbPerSecond:F3} MB/s");
            Console.WriteLine($"   Avg message size: {byteCount / Math.Max(1, messageCount)} bytes");
            
            Assert.IsTrue(messageCount > 0, "No messages received");
            Assert.IsTrue(messagesPerSecond > 5, "Throughput too low");
        }

        [TestMethod]
        [TestCategory("Performance")]
        public async Task Test_CPU_Usage()
        {
            Console.WriteLine("\n[TEST] CPU Usage Test");
            Console.WriteLine("----------------------------------------");
            
            var process = Process.GetCurrentProcess();
            var startCpuTime = process.TotalProcessorTime;
            var startTime = DateTime.UtcNow;
            
            await _client.ConnectAsync();
            
            // Heavy subscription load
            foreach (var pair in _majorPairs.Concat(_paymentCoins))
            {
                await _client.SubscribeOrderbook(pair);
                await _client.SubscribeTicker(pair);
                await _client.SubscribeTrades(pair);
            }
            
            await Task.Delay(_testDuration);
            
            var endCpuTime = process.TotalProcessorTime;
            var endTime = DateTime.UtcNow;
            
            var cpuUsedMs = (endCpuTime - startCpuTime).TotalMilliseconds;
            var totalMs = (endTime - startTime).TotalMilliseconds;
            var cpuUsagePercent = (cpuUsedMs / totalMs) * 100;
            
            Console.WriteLine($"\n✅ CPU Usage Metrics:");
            Console.WriteLine($"   CPU time used: {cpuUsedMs:F2}ms");
            Console.WriteLine($"   Wall time: {totalMs:F2}ms");
            Console.WriteLine($"   CPU usage: {cpuUsagePercent:F2}%");
            
            Assert.IsTrue(cpuUsagePercent < 50, 
                $"Excessive CPU usage: {cpuUsagePercent:F2}%");
        }

        #endregion

        #region Helper Methods

        private double CalculateVolatility(List<decimal> prices)
        {
            if (prices.Count < 2) return 0;
            
            var returns = new List<double>();
            for (int i = 1; i < prices.Count; i++)
            {
                returns.Add((double)((prices[i] - prices[i-1]) / prices[i-1]));
            }
            
            var mean = returns.Average();
            var variance = returns.Select(r => Math.Pow(r - mean, 2)).Average();
            return Math.Sqrt(variance) * 100;
        }

        private string ClassifyPaymentCoin(double volatility, decimal volume, decimal spreadPercent)
        {
            if (volatility > 5 && volume > 1000000)
                return "High Activity - High Risk";
            else if (volatility > 5 && volume <= 1000000)
                return "High Volatility - Low Volume";
            else if (volatility <= 5 && volume > 1000000)
                return "Stable - High Volume";
            else
                return "Low Activity";
        }

        private Dictionary<string, double> CalculateCorrelationMatrix(
            Dictionary<string, List<double>> priceMovements)
        {
            var correlations = new Dictionary<string, double>();
            var symbols = priceMovements.Keys.ToList();
            
            for (int i = 0; i < symbols.Count; i++)
            {
                for (int j = i + 1; j < symbols.Count; j++)
                {
                    var correlation = CalculateCorrelation(
                        priceMovements[symbols[i]], 
                        priceMovements[symbols[j]]);
                    
                    correlations[$"{symbols[i]} ↔ {symbols[j]}"] = correlation;
                }
            }
            
            return correlations;
        }

        private double CalculateCorrelation(List<double> series1, List<double> series2)
        {
            var minCount = Math.Min(series1.Count, series2.Count);
            if (minCount < 2) return 0;
            
            var sameDirection = 0;
            for (int i = 0; i < minCount; i++)
            {
                if (Math.Sign(series1[i]) == Math.Sign(series2[i]))
                    sameDirection++;
            }
            
            return (double)sameDirection / minCount;
        }

        private decimal EstimateSlippage(Orderbook orderbook, decimal orderSize)
        {
            var totalCost = 0m;
            var remaining = orderSize;
            
            foreach (var ask in orderbook.asks)
            {
                if (remaining <= 0) break;
                
                var fillAmount = Math.Min(remaining, ask.quantity);
                totalCost += fillAmount * ask.price;
                remaining -= fillAmount;
            }
            
            var avgPrice = totalCost / orderSize;
            var bestPrice = orderbook.asks[0].price;
            return ((avgPrice - bestPrice) / bestPrice) * 100;
        }

        private double CalculateLiquidityScore(decimal spread, double depth, decimal slippage)
        {
            var spreadScore = Math.Max(0, 10 - (double)spread / 100);
            var depthScore = Math.Min(10, depth / 10);
            var slippageScore = Math.Max(0, 10 - (double)slippage);
            
            return (spreadScore + depthScore + slippageScore) / 3;
        }

        #endregion

        #region Helper Classes

        private class PaymentCoinAnalysis
        {
            public string Symbol { get; set; }
            public List<decimal> Prices { get; set; }
            public List<decimal> Volumes { get; set; }
            public List<decimal> Spreads { get; set; }
            public int UpdateCount { get; set; }
        }

        private class MarketDepthAnalysis
        {
            public string Symbol { get; set; }
            public List<decimal> BidDepths { get; set; }
            public List<decimal> AskDepths { get; set; }
            public List<double> Imbalances { get; set; }
            public int UpdateCount { get; set; }
        }

        private class LiquidityMetrics
        {
            public string Symbol { get; set; }
            public List<decimal> SpreadHistory { get; set; }
            public List<int> DepthHistory { get; set; }
            public List<decimal> SlippageEstimates { get; set; }
            public int UpdateCount { get; set; }
        }

        private class ArbitrageOpportunity
        {
            public string Symbol { get; set; }
            public decimal BithumbPrice { get; set; }
            public decimal ExternalPrice { get; set; }
            public decimal PriceDifference { get; set; }
            public decimal PercentDifference { get; set; }
            public DateTime Timestamp { get; set; }
        }

        private class TriangularArbitrage
        {
            public string Path { get; set; }
            public decimal StartAmount { get; set; }
            public decimal FinalAmount { get; set; }
            public decimal Profit { get; set; }
            public decimal ProfitPercent { get; set; }
            public DateTime Timestamp { get; set; }
        }

        #endregion
    }
}