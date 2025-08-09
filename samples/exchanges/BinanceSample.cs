using CCXT.Collector.Binance;
using CCXT.Collector.Library;
using CCXT.Collector.Service;
using CCXT.Collector.Indicator;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CCXT.Collector.Samples.Exchanges
{
    /// <summary>
    /// Binance Exchange Sample Implementation
    /// Demonstrates WebSocket connection, real-time data reception, and technical analysis
    /// </summary>
    public class BinanceSample
    {
        private readonly BinanceWebSocketClient _client;
        private readonly List<Ohlc> _ohlcBuffer;
        private readonly Dictionary<string, IIndicatorCalculator> _indicators;
        private CancellationTokenSource _cancellationTokenSource;

        public BinanceSample()
        {
            _client = new BinanceWebSocketClient();
            _ohlcBuffer = new List<Ohlc>();
            _indicators = new Dictionary<string, IIndicatorCalculator>();
            _cancellationTokenSource = new CancellationTokenSource();

            InitializeIndicators();
        }

        private void InitializeIndicators()
        {
            _indicators["RSI"] = new RSI(14);
            _indicators["MACD"] = new MACD(12, 26, 9);
            _indicators["BB"] = new BollingerBand(20, 2);
            _indicators["SMA20"] = new SMA(20);
            _indicators["SMA50"] = new SMA(50);
            _indicators["EMA20"] = new EMA(20);
            _indicators["ATR"] = new ATR(14);
            _indicators["ADX"] = new ADX(14);
        }

        /// <summary>
        /// Run complete Binance sample with all features
        /// </summary>
        public async Task RunAsync()
        {
            Console.WriteLine("===========================================");
            Console.WriteLine("     Binance Exchange Sample");
            Console.WriteLine("===========================================\n");

            Console.WriteLine("Select operation mode:");
            Console.WriteLine("1. Real-time Orderbook Monitoring");
            Console.WriteLine("2. Trade Stream Analysis");
            Console.WriteLine("3. Technical Indicator Dashboard");
            Console.WriteLine("4. Multi-Symbol Ticker Monitor");
            Console.WriteLine("5. Advanced Trading Signals");
            Console.WriteLine("6. Full Demo (All Features)");
            Console.WriteLine("0. Exit");

            Console.Write("\nYour choice: ");
            var choice = Console.ReadLine();

            try
            {
                switch (choice)
                {
                    case "1":
                        await RunOrderbookMonitoring();
                        break;
                    case "2":
                        await RunTradeAnalysis();
                        break;
                    case "3":
                        await RunTechnicalIndicators();
                        break;
                    case "4":
                        await RunMultiSymbolTicker();
                        break;
                    case "5":
                        await RunAdvancedSignals();
                        break;
                    case "6":
                        await RunFullDemo();
                        break;
                    case "0":
                        return;
                    default:
                        Console.WriteLine("Invalid choice");
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
            finally
            {
                await Cleanup();
            }
        }

        /// <summary>
        /// Real-time orderbook depth monitoring
        /// </summary>
        private async Task RunOrderbookMonitoring()
        {
            Console.WriteLine("\n📊 Starting Binance Orderbook Monitor...\n");

            var orderbookStats = new OrderbookStatistics();

            _client.OnOrderbookReceived += (orderbook) =>
            {
                orderbookStats.Update(orderbook);

                Console.Clear();
                Console.WriteLine($"BINANCE ORDERBOOK - {orderbook.symbol}");
                Console.WriteLine($"Time: {DateTime.Now:HH:mm:ss.fff} | Updates: {orderbookStats.UpdateCount}");
                Console.WriteLine(new string('=', 70));

                // Display top 10 asks (sell orders) in reverse
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("\nASKS (Sell Orders):");
                Console.WriteLine($"{"Price",12} {"Amount",15} {"Total",15} {"Depth"}");
                Console.WriteLine(new string('-', 70));

                var askDepth = 0m;
                for (int i = Math.Min(9, orderbook.asks.Count - 1); i >= 0; i--)
                {
                    var ask = orderbook.asks[i];
                    askDepth += ask.quantity;
                    var barLength = (int)(ask.quantity * 2);
                    Console.WriteLine($"{ask.price,12:F2} {ask.quantity,15:F8} {askDepth,15:F8} {new string('█', Math.Min(barLength, 30))}");
                }

                // Display spread
                Console.ResetColor();
                var spread = orderbook.asks[0].price - orderbook.bids[0].price;
                var spreadPercent = (spread / orderbook.bids[0].price) * 100;
                Console.WriteLine($"\n{"SPREAD:",12} {spread:F2} ({spreadPercent:F4}%)");

                // Display top 10 bids (buy orders)
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("\nBIDS (Buy Orders):");
                Console.WriteLine($"{"Price",12} {"Amount",15} {"Total",15} {"Depth"}");
                Console.WriteLine(new string('-', 70));

                var bidDepth = 0m;
                for (int i = 0; i < Math.Min(10, orderbook.bids.Count); i++)
                {
                    var bid = orderbook.bids[i];
                    bidDepth += bid.quantity;
                    var barLength = (int)(bid.quantity * 2);
                    Console.WriteLine($"{bid.price,12:F2} {bid.quantity,15:F8} {bidDepth,15:F8} {new string('█', Math.Min(barLength, 30))}");
                }

                Console.ResetColor();

                // Display statistics
                Console.WriteLine($"\n📈 Statistics:");
                Console.WriteLine($"  Mid Price: {orderbookStats.MidPrice:F2}");
                Console.WriteLine($"  Bid/Ask Ratio: {orderbookStats.BidAskRatio:F2}");
                Console.WriteLine($"  Total Bid Volume: {orderbookStats.TotalBidVolume:F2}");
                Console.WriteLine($"  Total Ask Volume: {orderbookStats.TotalAskVolume:F2}");
                Console.WriteLine($"  Avg Spread: {orderbookStats.AverageSpread:F4}");

                Console.WriteLine("\nPress 'Q' to quit...");
            };

            await _client.ConnectAsync();
            await _client.SubscribeOrderbook("BTC/USDT");

            await WaitForExit();
        }

        /// <summary>
        /// Real-time trade stream analysis
        /// </summary>
        private async Task RunTradeAnalysis()
        {
            Console.WriteLine("\n💹 Starting Binance Trade Analysis...\n");

            var tradeStats = new TradeStatistics();
            var recentTrades = new Queue<SCompleteOrderItem>(100);

            _client.OnTradeReceived += (trade) =>
            {
                tradeStats.AddTrade(trade);
                recentTrades.Enqueue(trade);
                if (recentTrades.Count > 100) recentTrades.Dequeue();

                Console.Clear();
                Console.WriteLine($"BINANCE TRADE STREAM - {trade.symbol}");
                Console.WriteLine($"Time: {DateTime.Now:HH:mm:ss.fff}");
                Console.WriteLine(new string('=', 70));

                // Display recent trades
                Console.WriteLine("\nRecent Trades:");
                Console.WriteLine($"{"Time",10} {"Price",12} {"Amount",15} {"Side",6} {"Value (USDT)",15}");
                Console.WriteLine(new string('-', 70));

                foreach (var t in recentTrades.TakeLast(20).Reverse())
                {
                    var color = t.sideType == "B" ? ConsoleColor.Green : ConsoleColor.Red;
                    Console.ForegroundColor = color;
                    var value = t.price * t.quantity;
                    Console.WriteLine($"{t.timestamp:HH:mm:ss} {t.price,12:F2} {t.quantity,15:F8} {(t.sideType == "B" ? "BUY" : "SELL"),6} {value,15:F2}");
                }

                Console.ResetColor();

                // Display statistics
                Console.WriteLine($"\n📊 Trade Statistics (Last {tradeStats.TradeCount} trades):");
                Console.WriteLine($"  Total Volume: {tradeStats.TotalVolume:F8} BTC");
                Console.WriteLine($"  Buy Volume: {tradeStats.BuyVolume:F8} BTC ({tradeStats.BuyVolumePercent:F1}%)");
                Console.WriteLine($"  Sell Volume: {tradeStats.SellVolume:F8} BTC ({tradeStats.SellVolumePercent:F1}%)");
                Console.WriteLine($"  VWAP: ${tradeStats.VWAP:F2}");
                Console.WriteLine($"  Avg Trade Size: {tradeStats.AverageTradeSize:F8} BTC");
                Console.WriteLine($"  Large Trades (>1 BTC): {tradeStats.LargeTradeCount}");

                // Market sentiment
                var sentiment = tradeStats.BuyVolumePercent > 55 ? "BULLISH 🟢" :
                               tradeStats.BuyVolumePercent < 45 ? "BEARISH 🔴" : "NEUTRAL ⚪";
                Console.WriteLine($"\n🎯 Market Sentiment: {sentiment}");

                Console.WriteLine("\nPress 'Q' to quit...");
            };

            await _client.ConnectAsync();
            await _client.SubscribeTrades("BTC/USDT");

            await WaitForExit();
        }

        /// <summary>
        /// Technical indicator dashboard
        /// </summary>
        private async Task RunTechnicalIndicators()
        {
            Console.WriteLine("\n📈 Starting Binance Technical Analysis Dashboard...\n");

            _client.OnOhlcvReceived += (ohlcv) =>
            {
                _ohlcBuffer.Add(ohlcv);
                if (_ohlcBuffer.Count > 100) _ohlcBuffer.RemoveAt(0);

                if (_ohlcBuffer.Count < 50) 
                {
                    Console.WriteLine($"Collecting data... ({_ohlcBuffer.Count}/50)");
                    return;
                }

                Console.Clear();
                Console.WriteLine($"BINANCE TECHNICAL ANALYSIS - {ohlcv.symbol}");
                Console.WriteLine($"Time: {DateTime.Now:HH:mm:ss} | Price: ${ohlcv.closePrice:F2}");
                Console.WriteLine(new string('=', 70));

                // Calculate and display indicators
                var rsi = ((RSI)_indicators["RSI"]).Calculate(_ohlcBuffer);
                var macd = ((MACD)_indicators["MACD"]).Calculate(_ohlcBuffer);
                var bb = ((BollingerBand)_indicators["BB"]).Calculate(_ohlcBuffer);
                var sma20 = ((SMA)_indicators["SMA20"]).Calculate(_ohlcBuffer);
                var sma50 = ((SMA)_indicators["SMA50"]).Calculate(_ohlcBuffer);
                var ema20 = ((EMA)_indicators["EMA20"]).Calculate(_ohlcBuffer);
                var atr = ((ATR)_indicators["ATR"]).Calculate(_ohlcBuffer);
                var adx = ((ADX)_indicators["ADX"]).Calculate(_ohlcBuffer);

                Console.WriteLine("\n📊 Technical Indicators:");
                Console.WriteLine(new string('-', 70));

                // Trend Indicators
                Console.WriteLine("TREND INDICATORS:");
                Console.WriteLine($"  SMA(20):  ${sma20:F2} {GetTrendArrow(ohlcv.closePrice, sma20)}");
                Console.WriteLine($"  SMA(50):  ${sma50:F2} {GetTrendArrow(ohlcv.closePrice, sma50)}");
                Console.WriteLine($"  EMA(20):  ${ema20:F2} {GetTrendArrow(ohlcv.closePrice, ema20)}");

                // Momentum Indicators
                Console.WriteLine("\nMOMENTUM INDICATORS:");
                Console.WriteLine($"  RSI(14):  {rsi.Value:F2} {GetRSIStatus(rsi.Value)}");
                Console.WriteLine($"  MACD:     {macd.MACD:F2}");
                Console.WriteLine($"  Signal:   {macd.Signal:F2}");
                Console.WriteLine($"  Histogram: {macd.Histogram:F2} {GetMACDSignal(macd.Histogram)}");

                // Volatility Indicators
                Console.WriteLine("\nVOLATILITY INDICATORS:");
                Console.WriteLine($"  BB Upper: ${bb.UpperBand:F2}");
                Console.WriteLine($"  BB Middle: ${bb.MiddleBand:F2}");
                Console.WriteLine($"  BB Lower: ${bb.LowerBand:F2}");
                Console.WriteLine($"  ATR(14):  {atr.ATR:F2}");
                Console.WriteLine($"  ADX(14):  {adx.ADX:F2} {GetADXStrength(adx.ADX)}");

                // Trading Signals
                Console.WriteLine("\n🎯 TRADING SIGNALS:");
                var signals = GenerateTradingSignals(ohlcv.closePrice, rsi.Value, macd, bb, sma20, sma50, adx.ADX);
                foreach (var signal in signals)
                {
                    Console.WriteLine($"  • {signal}");
                }

                Console.WriteLine("\nPress 'Q' to quit...");
            };

            await _client.ConnectAsync();
            await _client.SubscribeOhlcv("BTC/USDT", "1m");

            await WaitForExit();
        }

        /// <summary>
        /// Multi-symbol ticker monitoring
        /// </summary>
        private async Task RunMultiSymbolTicker()
        {
            Console.WriteLine("\n📊 Starting Binance Multi-Symbol Monitor...\n");

            var symbols = new[] { "BTC/USDT", "ETH/USDT", "BNB/USDT", "ADA/USDT", "SOL/USDT", "DOT/USDT", "AVAX/USDT", "MATIC/USDT" };
            var tickers = new Dictionary<string, TickerData>();

            _client.OnTickerReceived += (ticker) =>
            {
                if (!tickers.ContainsKey(ticker.symbol))
                    tickers[ticker.symbol] = new TickerData();

                tickers[ticker.symbol].Update(ticker);

                Console.Clear();
                Console.WriteLine("BINANCE MULTI-SYMBOL TICKER MONITOR");
                Console.WriteLine($"Last Update: {DateTime.Now:HH:mm:ss}");
                Console.WriteLine(new string('=', 100));
                Console.WriteLine($"{"Symbol",-10} {"Last Price",12} {"24h Change",12} {"24h High",12} {"24h Low",12} {"Volume",15} {"Trend",8}");
                Console.WriteLine(new string('-', 100));

                foreach (var kvp in tickers.OrderBy(x => x.Key))
                {
                    var data = kvp.Value;
                    var changeColor = data.Change24h >= 0 ? ConsoleColor.Green : ConsoleColor.Red;
                    
                    Console.Write($"{kvp.Key,-10} ");
                    Console.Write($"{data.LastPrice,12:F2} ");
                    
                    Console.ForegroundColor = changeColor;
                    Console.Write($"{data.Change24h,11:F2}% ");
                    Console.ResetColor();
                    
                    Console.Write($"{data.High24h,12:F2} ");
                    Console.Write($"{data.Low24h,12:F2} ");
                    Console.Write($"{data.Volume24h,15:F0} ");
                    
                    // Trend indicator
                    var trend = data.GetTrend();
                    var trendColor = trend == "↑↑↑" ? ConsoleColor.Green :
                                    trend == "↓↓↓" ? ConsoleColor.Red : ConsoleColor.Yellow;
                    Console.ForegroundColor = trendColor;
                    Console.WriteLine($"{trend,8}");
                    Console.ResetColor();
                }

                // Market overview
                var gainers = tickers.Values.Count(t => t.Change24h > 0);
                var losers = tickers.Values.Count(t => t.Change24h < 0);
                var avgChange = tickers.Values.Average(t => t.Change24h);

                Console.WriteLine($"\n📊 Market Overview:");
                Console.WriteLine($"  Gainers: {gainers} | Losers: {losers} | Average Change: {avgChange:F2}%");

                // Top performer and worst performer
                if (tickers.Any())
                {
                    var topGainer = tickers.OrderByDescending(t => t.Value.Change24h).First();
                    var topLoser = tickers.OrderBy(t => t.Value.Change24h).First();
                    
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"  🚀 Top Gainer: {topGainer.Key} ({topGainer.Value.Change24h:F2}%)");
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"  📉 Top Loser: {topLoser.Key} ({topLoser.Value.Change24h:F2}%)");
                    Console.ResetColor();
                }

                Console.WriteLine("\nPress 'Q' to quit...");
            };

            await _client.ConnectAsync();
            foreach (var symbol in symbols)
            {
                await _client.SubscribeTicker(symbol);
                await Task.Delay(100); // Small delay between subscriptions
            }

            await WaitForExit();
        }

        /// <summary>
        /// Advanced trading signal generation
        /// </summary>
        private async Task RunAdvancedSignals()
        {
            Console.WriteLine("\n🎯 Starting Binance Advanced Signal Generator...\n");

            var signalEngine = new SignalEngine();

            _client.OnOhlcvReceived += (ohlcv) =>
            {
                _ohlcBuffer.Add(ohlcv);
                if (_ohlcBuffer.Count > 200) _ohlcBuffer.RemoveAt(0);

                if (_ohlcBuffer.Count < 100)
                {
                    Console.WriteLine($"Collecting data for analysis... ({_ohlcBuffer.Count}/100)");
                    return;
                }

                var signal = signalEngine.GenerateSignal(_ohlcBuffer, _indicators);

                Console.Clear();
                Console.WriteLine($"BINANCE ADVANCED TRADING SIGNALS - {ohlcv.symbol}");
                Console.WriteLine($"Time: {DateTime.Now:HH:mm:ss} | Price: ${ohlcv.closePrice:F2}");
                Console.WriteLine(new string('=', 70));

                // Display signal strength meter
                Console.WriteLine("\n📊 SIGNAL STRENGTH METER:");
                DrawSignalMeter(signal.Strength);

                // Display signal details
                Console.WriteLine($"\n🎯 TRADING SIGNAL: ");
                var signalColor = signal.Type == SignalType.StrongBuy ? ConsoleColor.Green :
                                 signal.Type == SignalType.Buy ? ConsoleColor.DarkGreen :
                                 signal.Type == SignalType.StrongSell ? ConsoleColor.Red :
                                 signal.Type == SignalType.Sell ? ConsoleColor.DarkRed :
                                 ConsoleColor.Yellow;

                Console.ForegroundColor = signalColor;
                Console.WriteLine($"  {signal.Type.ToString().ToUpper()}");
                Console.ResetColor();

                Console.WriteLine($"  Confidence: {signal.Confidence:F1}%");
                Console.WriteLine($"  Risk Level: {signal.RiskLevel}");

                // Display supporting indicators
                Console.WriteLine("\n📈 SUPPORTING INDICATORS:");
                foreach (var indicator in signal.SupportingIndicators)
                {
                    Console.WriteLine($"  • {indicator}");
                }

                // Display recommended actions
                if (signal.Type == SignalType.StrongBuy || signal.Type == SignalType.Buy)
                {
                    Console.WriteLine("\n💡 RECOMMENDED ACTIONS:");
                    Console.WriteLine($"  • Entry Price: ${signal.EntryPrice:F2}");
                    Console.WriteLine($"  • Stop Loss: ${signal.StopLoss:F2} ({signal.StopLossPercent:F2}%)");
                    Console.WriteLine($"  • Take Profit 1: ${signal.TakeProfit1:F2} ({signal.TakeProfit1Percent:F2}%)");
                    Console.WriteLine($"  • Take Profit 2: ${signal.TakeProfit2:F2} ({signal.TakeProfit2Percent:F2}%)");
                    Console.WriteLine($"  • Position Size: {signal.RecommendedPositionSize}% of portfolio");
                }

                // Risk warnings
                if (signal.Warnings.Any())
                {
                    Console.WriteLine("\n⚠️ WARNINGS:");
                    foreach (var warning in signal.Warnings)
                    {
                        Console.WriteLine($"  • {warning}");
                    }
                }

                Console.WriteLine("\nPress 'Q' to quit...");
            };

            await _client.ConnectAsync();
            await _client.SubscribeOhlcv("BTC/USDT", "5m");

            await WaitForExit();
        }

        /// <summary>
        /// Full demo with all features
        /// </summary>
        private async Task RunFullDemo()
        {
            Console.WriteLine("\n🚀 Starting Binance Full Feature Demo...\n");
            Console.WriteLine("This demo will showcase all features in sequence.");
            Console.WriteLine("Each feature will run for 30 seconds.\n");

            // Feature 1: Orderbook
            Console.WriteLine("📊 Feature 1/5: Orderbook Monitoring");
            await RunTimedFeature(RunOrderbookMonitoring, 30);

            // Feature 2: Trades
            Console.WriteLine("💹 Feature 2/5: Trade Analysis");
            await RunTimedFeature(RunTradeAnalysis, 30);

            // Feature 3: Technical Indicators
            Console.WriteLine("📈 Feature 3/5: Technical Indicators");
            await RunTimedFeature(RunTechnicalIndicators, 30);

            // Feature 4: Multi-Symbol
            Console.WriteLine("📊 Feature 4/5: Multi-Symbol Monitoring");
            await RunTimedFeature(RunMultiSymbolTicker, 30);

            // Feature 5: Signals
            Console.WriteLine("🎯 Feature 5/5: Advanced Signals");
            await RunTimedFeature(RunAdvancedSignals, 30);

            Console.WriteLine("\n✅ Demo completed! All features demonstrated successfully.");
        }

        // Helper methods
        private async Task RunTimedFeature(Func<Task> feature, int seconds)
        {
            var cts = new CancellationTokenSource(TimeSpan.FromSeconds(seconds));
            _cancellationTokenSource = cts;

            var task = feature();
            await Task.Delay(seconds * 1000);
            cts.Cancel();

            await _client.DisconnectAsync();
            _ohlcBuffer.Clear();
            
            Console.Clear();
        }

        private async Task WaitForExit()
        {
            await Task.Run(() =>
            {
                while (!_cancellationTokenSource.Token.IsCancellationRequested)
                {
                    if (Console.KeyAvailable)
                    {
                        var key = Console.ReadKey(true);
                        if (key.KeyChar == 'q' || key.KeyChar == 'Q')
                        {
                            _cancellationTokenSource.Cancel();
                            break;
                        }
                    }
                    Thread.Sleep(100);
                }
            });
        }

        private async Task Cleanup()
        {
            await _client.DisconnectAsync();
            _cancellationTokenSource?.Dispose();
        }

        private void DrawSignalMeter(double strength)
        {
            Console.Write("  [");
            var fillLength = (int)(strength / 5);
            Console.ForegroundColor = strength > 60 ? ConsoleColor.Green :
                                     strength > 40 ? ConsoleColor.Yellow :
                                     ConsoleColor.Red;
            Console.Write(new string('█', fillLength));
            Console.Write(new string('░', 20 - fillLength));
            Console.ResetColor();
            Console.WriteLine($"] {strength:F0}%");
        }

        private string GetTrendArrow(decimal price, decimal ma)
        {
            return price > ma ? "↑" : price < ma ? "↓" : "→";
        }

        private string GetRSIStatus(double rsi)
        {
            if (rsi > 70) return "🔴 Overbought";
            if (rsi < 30) return "🟢 Oversold";
            return "⚪ Neutral";
        }

        private string GetMACDSignal(double histogram)
        {
            if (histogram > 0) return "🟢 Bullish";
            return "🔴 Bearish";
        }

        private string GetADXStrength(double adx)
        {
            if (adx > 50) return "Very Strong";
            if (adx > 25) return "Strong";
            return "Weak";
        }

        private List<string> GenerateTradingSignals(decimal price, double rsi, MACDSerie macd, 
            BollingerBandSerie bb, decimal sma20, decimal sma50, double adx)
        {
            var signals = new List<string>();

            // Trend signals
            if (sma20 > sma50)
                signals.Add("🟢 Golden Cross - Bullish Trend");
            else if (sma20 < sma50)
                signals.Add("🔴 Death Cross - Bearish Trend");

            // RSI signals
            if (rsi < 30)
                signals.Add("🟢 RSI Oversold - Potential Buy");
            else if (rsi > 70)
                signals.Add("🔴 RSI Overbought - Potential Sell");

            // Bollinger Band signals
            if (price <= (decimal)bb.LowerBand)
                signals.Add("🟢 Price at Lower BB - Potential Bounce");
            else if (price >= (decimal)bb.UpperBand)
                signals.Add("🔴 Price at Upper BB - Potential Resistance");

            // MACD signals
            if (macd.Histogram > 0 && macd.MACD > macd.Signal)
                signals.Add("🟢 MACD Bullish Crossover");
            else if (macd.Histogram < 0 && macd.MACD < macd.Signal)
                signals.Add("🔴 MACD Bearish Crossover");

            // ADX signals
            if (adx > 25)
                signals.Add($"📊 Strong Trend (ADX: {adx:F1})");

            if (!signals.Any())
                signals.Add("⚪ No Clear Signals - Wait for Confirmation");

            return signals;
        }
    }

    // Supporting classes
    public class OrderbookStatistics
    {
        public int UpdateCount { get; private set; }
        public decimal MidPrice { get; private set; }
        public decimal BidAskRatio { get; private set; }
        public decimal TotalBidVolume { get; private set; }
        public decimal TotalAskVolume { get; private set; }
        public decimal AverageSpread { get; private set; }
        private List<decimal> _spreads = new List<decimal>();

        public void Update(SOrderBookItem orderbook)
        {
            UpdateCount++;
            
            if (orderbook.bids.Any() && orderbook.asks.Any())
            {
                MidPrice = (orderbook.bids[0].price + orderbook.asks[0].price) / 2;
                TotalBidVolume = orderbook.bids.Sum(b => b.quantity);
                TotalAskVolume = orderbook.asks.Sum(a => a.quantity);
                BidAskRatio = TotalBidVolume / Math.Max(TotalAskVolume, 0.001m);
                
                var spread = orderbook.asks[0].price - orderbook.bids[0].price;
                _spreads.Add(spread);
                if (_spreads.Count > 100) _spreads.RemoveAt(0);
                AverageSpread = _spreads.Average();
            }
        }
    }

    public class TradeStatistics
    {
        public int TradeCount { get; private set; }
        public decimal TotalVolume { get; private set; }
        public decimal BuyVolume { get; private set; }
        public decimal SellVolume { get; private set; }
        public decimal VWAP { get; private set; }
        public decimal AverageTradeSize { get; private set; }
        public int LargeTradeCount { get; private set; }
        public decimal BuyVolumePercent => TotalVolume > 0 ? (BuyVolume / TotalVolume) * 100 : 0;
        public decimal SellVolumePercent => TotalVolume > 0 ? (SellVolume / TotalVolume) * 100 : 0;

        private decimal _totalValue;

        public void AddTrade(SCompleteOrderItem trade)
        {
            TradeCount++;
            TotalVolume += trade.quantity;
            _totalValue += trade.price * trade.quantity;

            if (trade.sideType == "B")
                BuyVolume += trade.quantity;
            else
                SellVolume += trade.quantity;

            if (trade.quantity > 1)
                LargeTradeCount++;

            VWAP = TotalVolume > 0 ? _totalValue / TotalVolume : 0;
            AverageTradeSize = TotalVolume / Math.Max(TradeCount, 1);
        }
    }

    public class TickerData
    {
        public decimal LastPrice { get; private set; }
        public decimal Change24h { get; private set; }
        public decimal High24h { get; private set; }
        public decimal Low24h { get; private set; }
        public decimal Volume24h { get; private set; }
        private Queue<decimal> _priceHistory = new Queue<decimal>(10);

        public void Update(STickerItem ticker)
        {
            LastPrice = ticker.lastPrice;
            Change24h = ticker.changePercent;
            High24h = ticker.highPrice;
            Low24h = ticker.lowPrice;
            Volume24h = ticker.volume;

            _priceHistory.Enqueue(LastPrice);
            if (_priceHistory.Count > 10) _priceHistory.Dequeue();
        }

        public string GetTrend()
        {
            if (_priceHistory.Count < 3) return "---";
            
            var prices = _priceHistory.ToArray();
            var recentTrend = prices[^1] > prices[^2];
            var mediumTrend = prices[^1] > prices[^3];
            var longTrend = _priceHistory.Count >= 5 && prices[^1] > prices[^5];

            if (recentTrend && mediumTrend && longTrend) return "↑↑↑";
            if (!recentTrend && !mediumTrend && !longTrend) return "↓↓↓";
            if (recentTrend && mediumTrend) return "↑↑";
            if (!recentTrend && !mediumTrend) return "↓↓";
            if (recentTrend) return "↑";
            if (!recentTrend) return "↓";
            return "→";
        }
    }

    public class SignalEngine
    {
        public TradingSignal GenerateSignal(List<Ohlc> data, Dictionary<string, IIndicatorCalculator> indicators)
        {
            var signal = new TradingSignal();
            var currentPrice = data.Last().Close;

            // Calculate all indicators
            var rsi = ((RSI)indicators["RSI"]).Calculate(data);
            var macd = ((MACD)indicators["MACD"]).Calculate(data);
            var bb = ((BollingerBand)indicators["BB"]).Calculate(data);
            var sma20 = ((SMA)indicators["SMA20"]).Calculate(data);
            var sma50 = ((SMA)indicators["SMA50"]).Calculate(data);
            var atr = ((ATR)indicators["ATR"]).Calculate(data);
            var adx = ((ADX)indicators["ADX"]).Calculate(data);

            // Calculate signal strength
            var bullishPoints = 0;
            var bearishPoints = 0;

            // RSI Analysis
            if (rsi.Value < 30)
            {
                bullishPoints += 20;
                signal.SupportingIndicators.Add("RSI Oversold (<30)");
            }
            else if (rsi.Value > 70)
            {
                bearishPoints += 20;
                signal.SupportingIndicators.Add("RSI Overbought (>70)");
            }

            // MACD Analysis
            if (macd.MACD > macd.Signal && macd.Histogram > 0)
            {
                bullishPoints += 15;
                signal.SupportingIndicators.Add("MACD Bullish Crossover");
            }
            else if (macd.MACD < macd.Signal && macd.Histogram < 0)
            {
                bearishPoints += 15;
                signal.SupportingIndicators.Add("MACD Bearish Crossover");
            }

            // Moving Average Analysis
            if (currentPrice > sma20 && sma20 > sma50)
            {
                bullishPoints += 25;
                signal.SupportingIndicators.Add("Price above MA20 > MA50 (Uptrend)");
            }
            else if (currentPrice < sma20 && sma20 < sma50)
            {
                bearishPoints += 25;
                signal.SupportingIndicators.Add("Price below MA20 < MA50 (Downtrend)");
            }

            // Bollinger Band Analysis
            if (currentPrice <= (decimal)bb.LowerBand)
            {
                bullishPoints += 15;
                signal.SupportingIndicators.Add("Price at Lower Bollinger Band");
            }
            else if (currentPrice >= (decimal)bb.UpperBand)
            {
                bearishPoints += 15;
                signal.SupportingIndicators.Add("Price at Upper Bollinger Band");
            }

            // ADX Trend Strength
            if (adx.ADX > 25)
            {
                var trendMultiplier = 1 + (adx.ADX - 25) / 100;
                bullishPoints = (int)(bullishPoints * trendMultiplier);
                bearishPoints = (int)(bearishPoints * trendMultiplier);
                signal.SupportingIndicators.Add($"Strong Trend (ADX: {adx.ADX:F1})");
            }
            else
            {
                signal.Warnings.Add("Weak trend - consider waiting for stronger signal");
            }

            // Determine signal type and strength
            signal.Strength = Math.Max(bullishPoints, bearishPoints);
            signal.Confidence = Math.Min(signal.Strength, 100);

            if (bullishPoints > bearishPoints)
            {
                if (bullishPoints > 60)
                    signal.Type = SignalType.StrongBuy;
                else if (bullishPoints > 40)
                    signal.Type = SignalType.Buy;
                else
                    signal.Type = SignalType.Neutral;

                // Calculate entry and exit points
                signal.EntryPrice = currentPrice;
                signal.StopLoss = currentPrice - (currentPrice * (decimal)(atr.ATR * 2 / (double)currentPrice));
                signal.StopLossPercent = ((currentPrice - signal.StopLoss) / currentPrice) * 100;
                signal.TakeProfit1 = currentPrice + (currentPrice * (decimal)(atr.ATR * 3 / (double)currentPrice));
                signal.TakeProfit1Percent = ((signal.TakeProfit1 - currentPrice) / currentPrice) * 100;
                signal.TakeProfit2 = currentPrice + (currentPrice * (decimal)(atr.ATR * 5 / (double)currentPrice));
                signal.TakeProfit2Percent = ((signal.TakeProfit2 - currentPrice) / currentPrice) * 100;
            }
            else if (bearishPoints > bullishPoints)
            {
                if (bearishPoints > 60)
                    signal.Type = SignalType.StrongSell;
                else if (bearishPoints > 40)
                    signal.Type = SignalType.Sell;
                else
                    signal.Type = SignalType.Neutral;
            }
            else
            {
                signal.Type = SignalType.Neutral;
                signal.Warnings.Add("Mixed signals - consider waiting for clearer direction");
            }

            // Risk assessment
            var volatility = atr.ATR / (double)currentPrice * 100;
            if (volatility > 5)
            {
                signal.RiskLevel = "High";
                signal.RecommendedPositionSize = 1;
                signal.Warnings.Add($"High volatility ({volatility:F2}%) - reduce position size");
            }
            else if (volatility > 3)
            {
                signal.RiskLevel = "Medium";
                signal.RecommendedPositionSize = 2;
            }
            else
            {
                signal.RiskLevel = "Low";
                signal.RecommendedPositionSize = 3;
            }

            return signal;
        }
    }

    public class TradingSignal
    {
        public SignalType Type { get; set; }
        public double Strength { get; set; }
        public double Confidence { get; set; }
        public string RiskLevel { get; set; }
        public decimal EntryPrice { get; set; }
        public decimal StopLoss { get; set; }
        public decimal StopLossPercent { get; set; }
        public decimal TakeProfit1 { get; set; }
        public decimal TakeProfit1Percent { get; set; }
        public decimal TakeProfit2 { get; set; }
        public decimal TakeProfit2Percent { get; set; }
        public int RecommendedPositionSize { get; set; }
        public List<string> SupportingIndicators { get; set; } = new List<string>();
        public List<string> Warnings { get; set; } = new List<string>();
    }

    public enum SignalType
    {
        StrongBuy,
        Buy,
        Neutral,
        Sell,
        StrongSell
    }

    // Placeholder interface - should be replaced with actual implementation
    public interface IIndicatorCalculator { }
}