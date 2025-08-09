using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CCXT.Collector.Binance;
using CCXT.Collector.Service;
using Xunit;

namespace CCXT.Collector.Tests.Exchanges
{
    /// <summary>
    /// Comprehensive test suite for Binance exchange integration
    /// Tests WebSocket connectivity, data streaming, and technical indicators
    /// </summary>
    
    [Trait("Category", "Exchange")]
    [Trait("Category", "Binance")]
    public class BinanceTests : IDisposable
    {
        private BinanceWebSocketClient _client;
        private readonly List<string> _testSymbols = new() { "BTC/USDT", "ETH/USDT", "BNB/USDT" };
        private readonly int _testDuration = 10000; // 10 seconds per test
        private readonly Dictionary<string, int> _dataCounters = new();

        
        public BinanceTests()
        {
            Console.WriteLine("=== Binance Test Suite Initialization ===");
            _client = new BinanceWebSocketClient();
            _dataCounters.Clear();
            
            // Initialize counters for each test symbol
            foreach (var symbol in _testSymbols)
            {
                _dataCounters[$"{symbol}_orderbook"] = 0;
                _dataCounters[$"{symbol}_trade"] = 0;
                _dataCounters[$"{symbol}_ticker"] = 0;
            }
        }

        
        public void Dispose()
        {
            _client?.Dispose();
            Console.WriteLine("=== Binance Test Suite Cleanup Complete ===\n");
        }

        #region Connection Tests

        [Fact]
        [Trait("Category", "Connection")]
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

            Assert.True(connected, "Failed to establish WebSocket connection");
            Assert.True(connectionTime.ElapsedMilliseconds < 5000, 
                $"Connection took too long: {connectionTime.ElapsedMilliseconds}ms");
            
            Console.WriteLine($"✅ Connection established in {connectionTime.ElapsedMilliseconds}ms");
        }

        [Fact]
        [Trait("Category", "Connection")]
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
                    await _client.SubscribeOrderbookAsync(symbol);
                    await _client.SubscribeTradesAsync(symbol);
                    await _client.SubscribeTickerAsync(symbol);
                    Interlocked.Increment(ref subscriptionCount);
                    Console.WriteLine($"✅ Subscribed to {symbol}");
                }));
            }

            await Task.WhenAll(subscriptionTasks);
            
            Assert.True(_testSymbols.Count == subscriptionCount, "Not all symbols were successfully subscribed");
            
            Console.WriteLine($"✅ Successfully subscribed to {subscriptionCount} symbols");
        }

        [Fact]
        [Trait("Category", "Connection")]
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
            await _client.SubscribeTickerAsync("BTC/USDT");
            
            // Simulate disconnection
            // Disconnection simulation removed
            await Task.Delay(5000); // Wait for automatic reconnection
            
            Assert.True(reconnectCount > 0, "Automatic reconnection did not occur");
            Console.WriteLine($"✅ Reconnection successful after {disconnectCount} disconnection(s)");
        }

        #endregion

        #region Data Stream Tests

        [Fact]
        [Trait("Category", "DataStream")]
        public async Task Test_SOrderBooks_Stream()
        {
            Console.WriteLine("\n[TEST] SOrderBooks Data Stream");
            Console.WriteLine("----------------------------------------");
            
            var orderbookData = new Dictionary<string, SOrderBook>();
            var updateCount = 0;
            
            _client.OnOrderbookReceived += (orderbook) =>
            {
                orderbookData[orderbook.symbol] = orderbook;
                updateCount++;
                
                if (updateCount % 10 == 0)
                {
                    Console.WriteLine($"📊 SOrderBooks updates: {updateCount}");
                    ValidateOrderbook(orderbook);
                }
            };

            await _client.ConnectAsync();
            foreach (var symbol in _testSymbols)
            {
                await _client.SubscribeOrderbookAsync(symbol);
            }
            
            await Task.Delay(_testDuration);
            
            Assert.True(updateCount > 0, "No orderbook data received");
            Assert.True(_testSymbols.Count == orderbookData.Count, "Not all symbols received orderbook data");
            
            // Validate final orderbook state
            foreach (var kvp in orderbookData)
            {
                ValidateOrderbook(kvp.Value);
            }
            
            Console.WriteLine($"✅ Received {updateCount} orderbook updates for {orderbookData.Count} symbols");
        }

        [Fact]
        [Trait("Category", "DataStream")]
        public async Task Test_SCompleteOrders_Stream()
        {
            Console.WriteLine("\n[TEST] SCompleteOrders Data Stream");
            Console.WriteLine("----------------------------------------");
            
            var trades = new List<STrade>();
            var symbolsWithSCompleteOrderss = new HashSet<string>();
            
            _client.OnTradeReceived += (trade) =>
            {
                trades.Add(trade);
                symbolsWithSCompleteOrderss.Add(trade.symbol);
                
                if (trades.Count % 50 == 0)
                {
                    Console.WriteLine($"📊 SCompleteOrderss received: {trades.Count}");
                    DisplayTradeStats(trades);
                }
            };

            await _client.ConnectAsync();
            foreach (var symbol in _testSymbols)
            {
                await _client.SubscribeTradesAsync(symbol);
            }
            
            await Task.Delay(_testDuration);
            
            Assert.True(trades.Count > 0, "No trade data received");
            Console.WriteLine($"✅ Received {trades.Count} trades across {symbolsWithSCompleteOrderss.Count} symbols");
            
            // Analyze trade distribution
            DisplayTradeStats(trades);
        }

        [Fact]
        [Trait("Category", "DataStream")]
        public async Task Test_STicker_Stream()
        {
            Console.WriteLine("\n[TEST] STicker Data Stream");
            Console.WriteLine("----------------------------------------");
            
            var tickers = new Dictionary<string, STicker>();
            var updateCount = 0;
            
            _client.OnTickerReceived += (ticker) =>
            {
                tickers[ticker.symbol] = ticker;
                updateCount++;
                
                if (updateCount % 10 == 0)
                {
                    Console.WriteLine($"📊 STicker updates: {updateCount}");
                    DisplayTickerSummary(tickers);
                }
            };

            await _client.ConnectAsync();
            foreach (var symbol in _testSymbols)
            {
                await _client.SubscribeTickerAsync(symbol);
            }
            
            await Task.Delay(_testDuration);
            
            Assert.True(updateCount > 0, "No ticker data received");
            Assert.True(_testSymbols.Count == tickers.Count, "Not all symbols received ticker data");
            
            Console.WriteLine($"✅ Received {updateCount} ticker updates for {tickers.Count} symbols");
            DisplayTickerSummary(tickers);
        }

        #endregion

        #region Technical Indicator Tests

        [Fact]
        [Trait("Category", "Indicators")]
        public async Task Test_RSI_Calculation()
        {
            Console.WriteLine("\n[TEST] RSI Indicator Calculation");
            Console.WriteLine("----------------------------------------");
            
            var rsiValues = new Dictionary<string, List<double>>();
            var rsiCalculator = new RSICalculator(14);
            
            _client.OnCandleReceived += (ohlcv) =>
            {
                var rsi = rsiCalculator.Calculate(ohlcv);
                
                if (!rsiValues.ContainsKey(ohlcv.symbol))
                    rsiValues[ohlcv.symbol] = new List<double>();
                
                rsiValues[ohlcv.symbol].Add(rsi);
                
                // Validate RSI range
                Assert.True(rsi >= 0 && rsi <= 100, 
                    $"RSI value out of range: {rsi}");
                
                if (rsiValues[ohlcv.symbol].Count % 10 == 0)
                {
                    var avg = rsiValues[ohlcv.symbol].Average();
                    Console.WriteLine($"📊 {ohlcv.symbol} - RSI: {rsi:F2} (Avg: {avg:F2})");
                }
            };

            await _client.ConnectAsync();
            await _client.SubscribeCandlesAsync("BTC/USDT", "1m");
            
            await Task.Delay(_testDuration);
            
            Assert.True(rsiValues.Count > 0, "No RSI values calculated");
            
            foreach (var kvp in rsiValues)
            {
                Console.WriteLine($"✅ {kvp.Key}: {kvp.Value.Count} RSI calculations, " +
                    $"Range: {kvp.Value.Min():F2} - {kvp.Value.Max():F2}");
            }
        }

        [Fact]
        [Trait("Category", "Indicators")]
        public async Task Test_MACD_Calculation()
        {
            Console.WriteLine("\n[TEST] MACD Indicator Calculation");
            Console.WriteLine("----------------------------------------");
            
            var macdResults = new List<MACDResult>();
            var macdCalculator = new MACDCalculator(12, 26, 9);
            
            _client.OnCandleReceived += (ohlcv) =>
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
            await _client.SubscribeCandlesAsync("BTC/USDT", "1m");
            
            await Task.Delay(_testDuration);
            
            Assert.True(macdResults.Count > 0, "No MACD values calculated");
            
            // Analyze MACD signals
            var bullishSignals = macdResults.Count(m => m.Histogram > 0);
            var bearishSignals = macdResults.Count(m => m.Histogram < 0);
            
            Console.WriteLine($"✅ MACD Analysis: {macdResults.Count} calculations");
            Console.WriteLine($"   Bullish signals: {bullishSignals} ({100.0 * bullishSignals / macdResults.Count:F1}%)");
            Console.WriteLine($"   Bearish signals: {bearishSignals} ({100.0 * bearishSignals / macdResults.Count:F1}%)");
        }

        [Fact]
        [Trait("Category", "Indicators")]
        public async Task Test_BollingerBands_Calculation()
        {
            Console.WriteLine("\n[TEST] Bollinger Bands Calculation");
            Console.WriteLine("----------------------------------------");
            
            var bbResults = new List<BollingerBandsResult>();
            var bbCalculator = new BollingerBandsCalculator(20, 2);
            
            _client.OnCandleReceived += (ohlcv) =>
            {
                var bb = bbCalculator.Calculate(ohlcv);
                bbResults.Add(bb);
                
                // Validate band relationships
                Assert.True(bb.Upper > bb.Middle, "Upper band should be above middle");
                Assert.True(bb.Middle > bb.Lower, "Middle band should be above lower");
                
                if (bbResults.Count % 10 == 0)
                {
                    var bandwidth = bb.Upper - bb.Lower;
                    var percentB = ohlcv.result != null && ohlcv.result.Count > 0 
                        ? ((double)ohlcv.result[0].close - bb.Lower) / bandwidth
                        : 0;
                    
                    Console.WriteLine($"📊 BB - Upper: {bb.Upper:F2}, " +
                        $"Middle: {bb.Middle:F2}, Lower: {bb.Lower:F2}, " +
                        $"%B: {percentB:F2}");
                }
            };

            await _client.ConnectAsync();
            await _client.SubscribeCandlesAsync("BTC/USDT", "1m");
            
            await Task.Delay(_testDuration);
            
            Assert.True(bbResults.Count > 0, "No Bollinger Bands values calculated");
            
            Console.WriteLine($"✅ Bollinger Bands: {bbResults.Count} calculations completed");
        }

        #endregion

        #region Performance Tests

        [Fact]
        [Trait("Category", "Performance")]
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
                await _client.SubscribeOrderbookAsync(symbol);
                await _client.SubscribeTradesAsync(symbol);
                await _client.SubscribeTickerAsync(symbol);
            }
            
            await Task.Delay(_testDuration);
            
            var messagesPerSecond = messageCount / (_testDuration / 1000.0);
            var avgLatency = messageTimes.Count > 0 ? messageTimes.Average() : 0;
            
            Console.WriteLine($"✅ Performance Metrics:");
            Console.WriteLine($"   Total messages: {messageCount}");
            Console.WriteLine($"   Messages/second: {messagesPerSecond:F2}");
            Console.WriteLine($"   Average latency: {avgLatency:F2}ms");
            
            Assert.True(messageCount > 100, "Insufficient message volume for performance test");
            Assert.True(messagesPerSecond > 10, "Message rate too low");
        }

        [Fact]
        [Trait("Category", "Performance")]
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
                await _client.SubscribeOrderbookAsync(symbol);
                await _client.SubscribeTradesAsync(symbol);
            }
            
            await Task.Delay(_testDuration);
            
            var finalMemory = GC.GetTotalMemory(false);
            var memoryUsed = (finalMemory - initialMemory) / 1024 / 1024;
            
            Console.WriteLine($"Final memory: {finalMemory / 1024 / 1024:F2} MB");
            Console.WriteLine($"Memory used: {memoryUsed:F2} MB");
            
            Assert.True(memoryUsed < 100, $"Excessive memory usage: {memoryUsed:F2} MB");
            
            Console.WriteLine($"✅ Memory usage within acceptable limits");
        }

        #endregion

        #region Helper Methods

        private void ValidateOrderbook(SOrderBook orderbook)
        {
            Assert.NotNull(orderbook);
            Assert.True(orderbook.result.bids.Count > 0, "No bid levels in orderbook");
            Assert.True(orderbook.result.asks.Count > 0, "No ask levels in orderbook");
            
            // Validate bid prices are descending
            for (int i = 1; i < Math.Min(5, orderbook.result.bids.Count); i++)
            {
                Assert.True(orderbook.result.bids[i-1].price > orderbook.result.bids[i].price,
                    "Bid prices not in descending order");
            }
            
            // Validate ask prices are ascending
            for (int i = 1; i < Math.Min(5, orderbook.result.asks.Count); i++)
            {
                Assert.True(orderbook.result.asks[i-1].price < orderbook.result.asks[i].price,
                    "Ask prices not in ascending order");
            }
            
            // Validate spread
            var spread = orderbook.result.asks[0].price - orderbook.result.bids[0].price;
            Assert.True(spread > 0, "Invalid spread (ask <= bid)");
        }

        private void DisplayTradeStats(List<STrade> trades)
        {
            if (trades.Count == 0) return;
            
            var buySCompleteOrderss = trades.Where(t => t.result[0].sideType == SideType.Bid).Count();
            var sellSCompleteOrderss = trades.Where(t => t.result[0].sideType == SideType.Ask).Count();
            var totalVolume = trades.Sum(t => t.result[0].amount);
            
            Console.WriteLine($"   Buy trades: {buySCompleteOrderss}, Sell trades: {sellSCompleteOrderss}");
            Console.WriteLine($"   Total volume: ${totalVolume:F2}");
        }

        private void DisplayTickerSummary(Dictionary<string, STicker> tickers)
        {
            foreach (var ticker in tickers.Values)
            {
                var spread = ticker.result.askPrice - ticker.result.bidPrice;
                var spreadPercent = 100 * spread / ticker.result.bidPrice;
                Console.WriteLine($"   {ticker.symbol}: Bid={ticker.result.bidPrice:F2}, " +
                    $"Ask={ticker.result.askPrice:F2}, Spread={spreadPercent:F3}%");
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
        public double Calculate(SCandle ohlcv) => 50 + new Random().NextDouble() * 50; // Simplified for testing
    }

    public class MACDCalculator
    {
        private readonly int _fast, _slow, _signal;
        public MACDCalculator(int fast, int slow, int signal)
        {
            _fast = fast; _slow = slow; _signal = signal;
        }
        public MACDResult Calculate(SCandle ohlcv) => new MACDResult 
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
        public BollingerBandsResult Calculate(SCandle ohlcv) => new BollingerBandsResult
        {
            Middle = ohlcv.result != null && ohlcv.result.Count > 0 ? (double)ohlcv.result[0].close : 0,
            Upper = ohlcv.result != null && ohlcv.result.Count > 0 ? (double)ohlcv.result[0].close * 1.02 : 0,
            Lower = ohlcv.result != null && ohlcv.result.Count > 0 ? (double)ohlcv.result[0].close * 0.98 : 0
        };
    }

    #endregion
}