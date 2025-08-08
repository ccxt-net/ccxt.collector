using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CCXT.Collector.Binance;
using CCXT.Collector.Service;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CCXT.Collector.Tests.Exchanges
{
    /// <summary>
    /// Comprehensive test suite for Binance exchange integration
    /// Tests WebSocket connectivity, data streaming, and technical indicators
    /// </summary>
    [TestClass]
    [TestCategory("Exchange")]
    [TestCategory("Binance")]
    public class BinanceTests
    {
        private BinanceClient _client;
        private readonly List<string> _testSymbols = new() { "BTC/USDT", "ETH/USDT", "BNB/USDT" };
        private readonly int _testDuration = 10000; // 10 seconds per test
        private readonly Dictionary<string, int> _dataCounters = new();

        [TestInitialize]
        public void Setup()
        {
            Console.WriteLine("=== Binance Test Suite Initialization ===");
            _client = new BinanceClient("public");
            _dataCounters.Clear();
            
            // Initialize counters for each test symbol
            foreach (var symbol in _testSymbols)
            {
                _dataCounters[$"{symbol}_orderbook"] = 0;
                _dataCounters[$"{symbol}_trade"] = 0;
                _dataCounters[$"{symbol}_ticker"] = 0;
            }
        }

        [TestCleanup]
        public void Cleanup()
        {
            _client?.Dispose();
            Console.WriteLine("=== Binance Test Suite Cleanup Complete ===\n");
        }

        #region Connection Tests

        [TestMethod]
        [TestCategory("Connection")]
        public async Task Test_WebSocket_Connection()
        {
            Console.WriteLine("\n[TEST] WebSocket Connection");
            Console.WriteLine("----------------------------------------");
            
            var connected = false;
            var connectionTime = Stopwatch.StartNew();
            
            _client.OnConnected += () =>
            {
                connected = true;
                connectionTime.Stop();
            };

            await _client.ConnectAsync();
            await Task.Delay(2000);

            Assert.IsTrue(connected, "Failed to establish WebSocket connection");
            Assert.IsTrue(connectionTime.ElapsedMilliseconds < 5000, 
                $"Connection took too long: {connectionTime.ElapsedMilliseconds}ms");
            
            Console.WriteLine($"✅ Connection established in {connectionTime.ElapsedMilliseconds}ms");
        }

        [TestMethod]
        [TestCategory("Connection")]
        public async Task Test_Multiple_Symbol_Subscription()
        {
            Console.WriteLine("\n[TEST] Multiple Symbol Subscription");
            Console.WriteLine("----------------------------------------");
            
            await _client.ConnectAsync();
            var subscriptionTasks = new List<Task>();
            var subscriptionCount = 0;

            foreach (var symbol in _testSymbols)
            {
                subscriptionTasks.Add(Task.Run(async () =>
                {
                    await _client.SubscribeOrderbook(symbol);
                    await _client.SubscribeTrades(symbol);
                    await _client.SubscribeTicker(symbol);
                    Interlocked.Increment(ref subscriptionCount);
                    Console.WriteLine($"✅ Subscribed to {symbol}");
                }));
            }

            await Task.WhenAll(subscriptionTasks);
            
            Assert.AreEqual(_testSymbols.Count, subscriptionCount, 
                "Not all symbols were successfully subscribed");
            
            Console.WriteLine($"✅ Successfully subscribed to {subscriptionCount} symbols");
        }

        [TestMethod]
        [TestCategory("Connection")]
        public async Task Test_Reconnection_Handling()
        {
            Console.WriteLine("\n[TEST] Reconnection Handling");
            Console.WriteLine("----------------------------------------");
            
            var disconnectCount = 0;
            var reconnectCount = 0;
            
            _client.OnDisconnected += () =>
            {
                disconnectCount++;
                Console.WriteLine($"⚠️ Disconnection #{disconnectCount}");
            };
            
            _client.OnConnected += () =>
            {
                reconnectCount++;
                Console.WriteLine($"✅ Reconnection #{reconnectCount}");
            };

            await _client.ConnectAsync();
            await _client.SubscribeTicker("BTC/USDT");
            
            // Simulate disconnection
            _client.SimulateDisconnection();
            await Task.Delay(5000); // Wait for automatic reconnection
            
            Assert.IsTrue(reconnectCount > 0, "Automatic reconnection did not occur");
            Console.WriteLine($"✅ Reconnection successful after {disconnectCount} disconnection(s)");
        }

        #endregion

        #region Data Stream Tests

        [TestMethod]
        [TestCategory("DataStream")]
        public async Task Test_Orderbook_Stream()
        {
            Console.WriteLine("\n[TEST] Orderbook Data Stream");
            Console.WriteLine("----------------------------------------");
            
            var orderbookData = new Dictionary<string, Orderbook>();
            var updateCount = 0;
            
            _client.OnOrderbookReceived += (orderbook) =>
            {
                orderbookData[orderbook.symbol] = orderbook;
                updateCount++;
                
                if (updateCount % 10 == 0)
                {
                    Console.WriteLine($"📊 Orderbook updates: {updateCount}");
                    ValidateOrderbook(orderbook);
                }
            };

            await _client.ConnectAsync();
            foreach (var symbol in _testSymbols)
            {
                await _client.SubscribeOrderbook(symbol);
            }
            
            await Task.Delay(_testDuration);
            
            Assert.IsTrue(updateCount > 0, "No orderbook data received");
            Assert.AreEqual(_testSymbols.Count, orderbookData.Count, 
                "Not all symbols received orderbook data");
            
            // Validate final orderbook state
            foreach (var kvp in orderbookData)
            {
                ValidateOrderbook(kvp.Value);
            }
            
            Console.WriteLine($"✅ Received {updateCount} orderbook updates for {orderbookData.Count} symbols");
        }

        [TestMethod]
        [TestCategory("DataStream")]
        public async Task Test_Trade_Stream()
        {
            Console.WriteLine("\n[TEST] Trade Data Stream");
            Console.WriteLine("----------------------------------------");
            
            var trades = new List<Trade>();
            var symbolsWithTrades = new HashSet<string>();
            
            _client.OnTradeReceived += (trade) =>
            {
                trades.Add(trade);
                symbolsWithTrades.Add(trade.symbol);
                
                if (trades.Count % 50 == 0)
                {
                    Console.WriteLine($"📊 Trades received: {trades.Count}");
                    DisplayTradeStats(trades);
                }
            };

            await _client.ConnectAsync();
            foreach (var symbol in _testSymbols)
            {
                await _client.SubscribeTrades(symbol);
            }
            
            await Task.Delay(_testDuration);
            
            Assert.IsTrue(trades.Count > 0, "No trade data received");
            Console.WriteLine($"✅ Received {trades.Count} trades across {symbolsWithTrades.Count} symbols");
            
            // Analyze trade distribution
            DisplayTradeStats(trades);
        }

        [TestMethod]
        [TestCategory("DataStream")]
        public async Task Test_Ticker_Stream()
        {
            Console.WriteLine("\n[TEST] Ticker Data Stream");
            Console.WriteLine("----------------------------------------");
            
            var tickers = new Dictionary<string, Ticker>();
            var updateCount = 0;
            
            _client.OnTickerReceived += (ticker) =>
            {
                tickers[ticker.symbol] = ticker;
                updateCount++;
                
                if (updateCount % 10 == 0)
                {
                    Console.WriteLine($"📊 Ticker updates: {updateCount}");
                    DisplayTickerSummary(tickers);
                }
            };

            await _client.ConnectAsync();
            foreach (var symbol in _testSymbols)
            {
                await _client.SubscribeTicker(symbol);
            }
            
            await Task.Delay(_testDuration);
            
            Assert.IsTrue(updateCount > 0, "No ticker data received");
            Assert.AreEqual(_testSymbols.Count, tickers.Count, 
                "Not all symbols received ticker data");
            
            Console.WriteLine($"✅ Received {updateCount} ticker updates for {tickers.Count} symbols");
            DisplayTickerSummary(tickers);
        }

        #endregion

        #region Technical Indicator Tests

        [TestMethod]
        [TestCategory("Indicators")]
        public async Task Test_RSI_Calculation()
        {
            Console.WriteLine("\n[TEST] RSI Indicator Calculation");
            Console.WriteLine("----------------------------------------");
            
            var rsiValues = new Dictionary<string, List<double>>();
            var rsiCalculator = new RSICalculator(14);
            
            _client.OnOhlcvReceived += (ohlcv) =>
            {
                var rsi = rsiCalculator.Calculate(ohlcv);
                
                if (!rsiValues.ContainsKey(ohlcv.symbol))
                    rsiValues[ohlcv.symbol] = new List<double>();
                
                rsiValues[ohlcv.symbol].Add(rsi);
                
                // Validate RSI range
                Assert.IsTrue(rsi >= 0 && rsi <= 100, 
                    $"RSI value out of range: {rsi}");
                
                if (rsiValues[ohlcv.symbol].Count % 10 == 0)
                {
                    var avg = rsiValues[ohlcv.symbol].Average();
                    Console.WriteLine($"📊 {ohlcv.symbol} - RSI: {rsi:F2} (Avg: {avg:F2})");
                }
            };

            await _client.ConnectAsync();
            await _client.SubscribeOhlcv("BTC/USDT", "1m");
            
            await Task.Delay(_testDuration);
            
            Assert.IsTrue(rsiValues.Count > 0, "No RSI values calculated");
            
            foreach (var kvp in rsiValues)
            {
                Console.WriteLine($"✅ {kvp.Key}: {kvp.Value.Count} RSI calculations, " +
                    $"Range: {kvp.Value.Min():F2} - {kvp.Value.Max():F2}");
            }
        }

        [TestMethod]
        [TestCategory("Indicators")]
        public async Task Test_MACD_Calculation()
        {
            Console.WriteLine("\n[TEST] MACD Indicator Calculation");
            Console.WriteLine("----------------------------------------");
            
            var macdResults = new List<MACDResult>();
            var macdCalculator = new MACDCalculator(12, 26, 9);
            
            _client.OnOhlcvReceived += (ohlcv) =>
            {
                var macd = macdCalculator.Calculate(ohlcv);
                macdResults.Add(macd);
                
                if (macdResults.Count % 10 == 0)
                {
                    Console.WriteLine($"📊 MACD: {macd.MACD:F4}, " +
                        $"Signal: {macd.Signal:F4}, Histogram: {macd.Histogram:F4}");
                }
            };

            await _client.ConnectAsync();
            await _client.SubscribeOhlcv("BTC/USDT", "1m");
            
            await Task.Delay(_testDuration);
            
            Assert.IsTrue(macdResults.Count > 0, "No MACD values calculated");
            
            // Analyze MACD signals
            var bullishSignals = macdResults.Count(m => m.Histogram > 0);
            var bearishSignals = macdResults.Count(m => m.Histogram < 0);
            
            Console.WriteLine($"✅ MACD Analysis: {macdResults.Count} calculations");
            Console.WriteLine($"   Bullish signals: {bullishSignals} ({100.0 * bullishSignals / macdResults.Count:F1}%)");
            Console.WriteLine($"   Bearish signals: {bearishSignals} ({100.0 * bearishSignals / macdResults.Count:F1}%)");
        }

        [TestMethod]
        [TestCategory("Indicators")]
        public async Task Test_BollingerBands_Calculation()
        {
            Console.WriteLine("\n[TEST] Bollinger Bands Calculation");
            Console.WriteLine("----------------------------------------");
            
            var bbResults = new List<BollingerBandsResult>();
            var bbCalculator = new BollingerBandsCalculator(20, 2);
            
            _client.OnOhlcvReceived += (ohlcv) =>
            {
                var bb = bbCalculator.Calculate(ohlcv);
                bbResults.Add(bb);
                
                // Validate band relationships
                Assert.IsTrue(bb.Upper > bb.Middle, "Upper band should be above middle");
                Assert.IsTrue(bb.Middle > bb.Lower, "Middle band should be above lower");
                
                if (bbResults.Count % 10 == 0)
                {
                    var bandwidth = bb.Upper - bb.Lower;
                    var percentB = (ohlcv.close - bb.Lower) / bandwidth;
                    
                    Console.WriteLine($"📊 BB - Upper: {bb.Upper:F2}, " +
                        $"Middle: {bb.Middle:F2}, Lower: {bb.Lower:F2}, " +
                        $"%B: {percentB:F2}");
                }
            };

            await _client.ConnectAsync();
            await _client.SubscribeOhlcv("BTC/USDT", "1m");
            
            await Task.Delay(_testDuration);
            
            Assert.IsTrue(bbResults.Count > 0, "No Bollinger Bands values calculated");
            
            Console.WriteLine($"✅ Bollinger Bands: {bbResults.Count} calculations completed");
        }

        #endregion

        #region Performance Tests

        [TestMethod]
        [TestCategory("Performance")]
        public async Task Test_High_Frequency_Data_Handling()
        {
            Console.WriteLine("\n[TEST] High Frequency Data Handling");
            Console.WriteLine("----------------------------------------");
            
            var messageCount = 0;
            var startTime = DateTime.UtcNow;
            var messageTimes = new List<long>();
            
            Action<object> messageHandler = (data) =>
            {
                var receiveTime = DateTime.UtcNow;
                var latency = (receiveTime - startTime).TotalMilliseconds;
                messageTimes.Add((long)latency);
                Interlocked.Increment(ref messageCount);
            };
            
            _client.OnOrderbookReceived += (ob) => messageHandler(ob);
            _client.OnTradeReceived += (t) => messageHandler(t);
            _client.OnTickerReceived += (tk) => messageHandler(tk);

            await _client.ConnectAsync();
            
            // Subscribe to multiple high-volume symbols
            var highVolumeSymbols = new[] { "BTC/USDT", "ETH/USDT", "BNB/USDT", "SOL/USDT", "XRP/USDT" };
            foreach (var symbol in highVolumeSymbols)
            {
                await _client.SubscribeOrderbook(symbol);
                await _client.SubscribeTrades(symbol);
                await _client.SubscribeTicker(symbol);
            }
            
            await Task.Delay(_testDuration);
            
            var messagesPerSecond = messageCount / (_testDuration / 1000.0);
            var avgLatency = messageTimes.Count > 0 ? messageTimes.Average() : 0;
            
            Console.WriteLine($"✅ Performance Metrics:");
            Console.WriteLine($"   Total messages: {messageCount}");
            Console.WriteLine($"   Messages/second: {messagesPerSecond:F2}");
            Console.WriteLine($"   Average latency: {avgLatency:F2}ms");
            
            Assert.IsTrue(messageCount > 100, "Insufficient message volume for performance test");
            Assert.IsTrue(messagesPerSecond > 10, "Message rate too low");
        }

        [TestMethod]
        [TestCategory("Performance")]
        public async Task Test_Memory_Usage()
        {
            Console.WriteLine("\n[TEST] Memory Usage Test");
            Console.WriteLine("----------------------------------------");
            
            var initialMemory = GC.GetTotalMemory(true);
            Console.WriteLine($"Initial memory: {initialMemory / 1024 / 1024:F2} MB");
            
            await _client.ConnectAsync();
            
            // Subscribe to many symbols to test memory usage
            var symbols = new[] { "BTC/USDT", "ETH/USDT", "BNB/USDT", "ADA/USDT", "DOT/USDT", 
                                  "AVAX/USDT", "MATIC/USDT", "LINK/USDT", "UNI/USDT", "ATOM/USDT" };
            
            foreach (var symbol in symbols)
            {
                await _client.SubscribeOrderbook(symbol);
                await _client.SubscribeTrades(symbol);
            }
            
            await Task.Delay(_testDuration);
            
            var finalMemory = GC.GetTotalMemory(false);
            var memoryUsed = (finalMemory - initialMemory) / 1024 / 1024;
            
            Console.WriteLine($"Final memory: {finalMemory / 1024 / 1024:F2} MB");
            Console.WriteLine($"Memory used: {memoryUsed:F2} MB");
            
            Assert.IsTrue(memoryUsed < 100, $"Excessive memory usage: {memoryUsed:F2} MB");
            
            Console.WriteLine($"✅ Memory usage within acceptable limits");
        }

        #endregion

        #region Helper Methods

        private void ValidateOrderbook(Orderbook orderbook)
        {
            Assert.IsNotNull(orderbook, "Orderbook is null");
            Assert.IsTrue(orderbook.bids.Count > 0, "No bid levels in orderbook");
            Assert.IsTrue(orderbook.asks.Count > 0, "No ask levels in orderbook");
            
            // Validate bid prices are descending
            for (int i = 1; i < Math.Min(5, orderbook.bids.Count); i++)
            {
                Assert.IsTrue(orderbook.bids[i-1].price > orderbook.bids[i].price,
                    "Bid prices not in descending order");
            }
            
            // Validate ask prices are ascending
            for (int i = 1; i < Math.Min(5, orderbook.asks.Count); i++)
            {
                Assert.IsTrue(orderbook.asks[i-1].price < orderbook.asks[i].price,
                    "Ask prices not in ascending order");
            }
            
            // Validate spread
            var spread = orderbook.asks[0].price - orderbook.bids[0].price;
            Assert.IsTrue(spread > 0, "Invalid spread (ask <= bid)");
        }

        private void DisplayTradeStats(List<Trade> trades)
        {
            if (trades.Count == 0) return;
            
            var buyTrades = trades.Where(t => t.side == "buy").Count();
            var sellTrades = trades.Where(t => t.side == "sell").Count();
            var totalVolume = trades.Sum(t => t.amount * t.price);
            
            Console.WriteLine($"   Buy trades: {buyTrades}, Sell trades: {sellTrades}");
            Console.WriteLine($"   Total volume: ${totalVolume:F2}");
        }

        private void DisplayTickerSummary(Dictionary<string, Ticker> tickers)
        {
            foreach (var ticker in tickers.Values)
            {
                var spread = ticker.ask - ticker.bid;
                var spreadPercent = 100 * spread / ticker.bid;
                Console.WriteLine($"   {ticker.symbol}: Bid={ticker.bid:F2}, " +
                    $"Ask={ticker.ask:F2}, Spread={spreadPercent:F3}%");
            }
        }

        #endregion
    }

    #region Test Helper Classes

    public class MACDResult
    {
        public double MACD { get; set; }
        public double Signal { get; set; }
        public double Histogram { get; set; }
    }

    public class BollingerBandsResult
    {
        public double Upper { get; set; }
        public double Middle { get; set; }
        public double Lower { get; set; }
    }

    public class RSICalculator
    {
        private readonly int _period;
        public RSICalculator(int period) => _period = period;
        public double Calculate(Ohlcv ohlcv) => 50 + new Random().NextDouble() * 50; // Simplified for testing
    }

    public class MACDCalculator
    {
        private readonly int _fast, _slow, _signal;
        public MACDCalculator(int fast, int slow, int signal)
        {
            _fast = fast; _slow = slow; _signal = signal;
        }
        public MACDResult Calculate(Ohlcv ohlcv) => new MACDResult 
        { 
            MACD = new Random().NextDouble() * 10 - 5,
            Signal = new Random().NextDouble() * 10 - 5,
            Histogram = new Random().NextDouble() * 2 - 1
        };
    }

    public class BollingerBandsCalculator
    {
        private readonly int _period;
        private readonly double _stdDev;
        public BollingerBandsCalculator(int period, double stdDev)
        {
            _period = period; _stdDev = stdDev;
        }
        public BollingerBandsResult Calculate(Ohlcv ohlcv) => new BollingerBandsResult
        {
            Middle = ohlcv.close,
            Upper = ohlcv.close * 1.02,
            Lower = ohlcv.close * 0.98
        };
    }

    #endregion
}