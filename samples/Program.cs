using CCXT.Collector.Binance;
using CCXT.Collector.Upbit;
using CCXT.Collector.Core.Abstractions;
using CCXT.Collector.Library;
using CCXT.Collector.Service;
using CCXT.Collector.Indicator;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CCXT.Collector.Samples
{
    // Define temporary classes for the sample (these should be moved to proper locations)
    
    public class Ohlc
    {
        public long openTime { get; set; }
        public long closeTime { get; set; }
        public decimal Open { get; set; }
        public decimal High { get; set; }
        public decimal Low { get; set; }
        public decimal Close { get; set; }
        public decimal Volume { get; set; }
    }
    
    class RSI
    {
        private int _period;
        public RSI(int period) { _period = period; }
        public double Calculate(List<Ohlc> data) => 50.0; // Placeholder
    }
    
    class MACD
    {
        private int _fast, _slow, _signal;
        public MACD(int fast, int slow, int signal) 
        { 
            _fast = fast; 
            _slow = slow; 
            _signal = signal; 
        }
        public (double MACD, double Signal) Calculate(List<Ohlc> data) => (0, 0); // Placeholder
    }
    
    class BollingerBand
    {
        private int _period;
        private double _stdDev;
        public BollingerBand(int period, double stdDev) 
        { 
            _period = period; 
            _stdDev = stdDev; 
        }
        public (double Upper, double Middle, double Lower) Calculate(List<Ohlc> data) => (0, 0, 0); // Placeholder
    }
    
    class SMA
    {
        private int _period;
        public SMA(int period) { _period = period; }
        public decimal Calculate(List<Ohlc> data) => 0; // Placeholder
    }
    
    class EMA
    {
        private int _period;
        public EMA(int period) { _period = period; }
        public decimal Calculate(List<Ohlc> data) => 0; // Placeholder
    }
    
    class ADX
    {
        private int _period;
        public ADX(int period) { _period = period; }
        public (double ADX, double PlusDI, double MinusDI) Calculate(List<Ohlc> data) => (25, 0, 0); // Placeholder
    }
    
    class ATR
    {
        private int _period;
        public ATR(int period) { _period = period; }
        public (double ATR, double TrueRange) Calculate(List<Ohlc> data) => (0, 0); // Placeholder
    }
    
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("CCXT.Collector Sample Application");
            Console.WriteLine("==================================\n");

            // Load configuration
            var configuration = new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .Build();

            // Display menu
            Console.WriteLine("Select a sample to run:");
            Console.WriteLine("1. Basic WebSocket Connection (Binance)");
            Console.WriteLine("2. Multi-Exchange Data Collection");
            Console.WriteLine("3. Technical Indicator Analysis");
            Console.WriteLine("4. Orderbook Depth Monitoring");
            Console.WriteLine("5. Trade History Collection");
            Console.WriteLine("6. Real-time Ticker Updates");
            Console.WriteLine("7. Advanced Indicator Combination");
            Console.WriteLine("8. ✨ All 15 Exchanges Test Menu");
            Console.WriteLine("0. Exit");

            Console.Write("\nEnter your choice: ");
            var choice = Console.ReadLine();

            switch (choice)
            {
                case "1":
                    await RunBasicWebSocketSample();
                    break;
                case "2":
                    await RunMultiExchangeSample();
                    break;
                case "3":
                    await RunTechnicalIndicatorSample();
                    break;
                case "4":
                    await RunOrderbookMonitoringSample();
                    break;
                case "5":
                    await RunTradeHistorySample();
                    break;
                case "6":
                    await RunTickerUpdatesSample();
                    break;
                case "7":
                    await RunAdvancedIndicatorSample();
                    break;
                case "8":
                    await AllExchangesSample.RunMenu();
                    break;
                case "0":
                    return;
                default:
                    Console.WriteLine("Invalid choice. Please try again.");
                    break;
            }

            Console.WriteLine("\nPress any key to exit...");
            Console.ReadKey();
        }

        /// <summary>
        /// Sample 1: Basic WebSocket Connection
        /// </summary>
        static async Task RunBasicWebSocketSample()
        {
            Console.WriteLine("\n=== Basic WebSocket Connection Sample ===\n");

            var client = new BinanceWebSocketClient();
            var cts = new CancellationTokenSource();

            // Register callbacks
            client.OnOrderbookReceived += (orderbook) =>
            {
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] Orderbook Update");
                Console.WriteLine($"  Symbol: {orderbook.symbol}");
                if (orderbook.result.bids.Count > 0 && orderbook.result.asks.Count > 0)
                {
                    Console.WriteLine($"  Best Bid: {orderbook.result.bids[0].price:F2} @ {orderbook.result.bids[0].quantity:F8}");
                    Console.WriteLine($"  Best Ask: {orderbook.result.asks[0].price:F2} @ {orderbook.result.asks[0].quantity:F8}");
                    Console.WriteLine($"  Spread: {(orderbook.result.asks[0].price - orderbook.result.bids[0].price):F2}\n");
                }
            };

            client.OnConnected += () =>
            {
                Console.WriteLine($"Connection State: Connected");
            };
            
            client.OnDisconnected += () =>
            {
                Console.WriteLine($"Connection State: Disconnected");
            };
            
            client.OnError += (error) =>
            {
                Console.WriteLine($"Error: {error}");
            };

            // Connect and subscribe
            await client.ConnectAsync();
            await client.SubscribeOrderbookAsync("BTC/USDT");

            Console.WriteLine("Connected to Binance WebSocket. Press 'Q' to quit.\n");

            // Wait for user to quit
            await WaitForExit(cts);

            await client.DisconnectAsync();
        }

        /// <summary>
        /// Sample 2: Multi-Exchange Data Collection
        /// </summary>
        static async Task RunMultiExchangeSample()
        {
            Console.WriteLine("\n=== Multi-Exchange Data Collection Sample ===\n");

            var binanceClient = new BinanceWebSocketClient();
            var upbitClient = new UpbitWebSocketClient();
            var cts = new CancellationTokenSource();

            var priceComparison = new Dictionary<string, Dictionary<string, decimal>>();

            // Binance ticker callback
            binanceClient.OnTickerReceived += (ticker) =>
            {
                if (!priceComparison.ContainsKey(ticker.symbol))
                    priceComparison[ticker.symbol] = new Dictionary<string, decimal>();
                
                priceComparison[ticker.symbol]["Binance"] = ticker.result.closePrice;
                DisplayPriceComparison(priceComparison);
            };

            // Upbit ticker callback
            upbitClient.OnTickerReceived += (ticker) =>
            {
                if (!priceComparison.ContainsKey(ticker.symbol))
                    priceComparison[ticker.symbol] = new Dictionary<string, decimal>();
                
                priceComparison[ticker.symbol]["Upbit"] = ticker.result.closePrice;
                DisplayPriceComparison(priceComparison);
            };

            // Connect to exchanges
            await Task.WhenAll(
                binanceClient.ConnectAsync(),
                upbitClient.ConnectAsync()
            );

            // Subscribe to same markets
            await Task.WhenAll(
                binanceClient.SubscribeTickerAsync("BTC/USDT"),
                upbitClient.SubscribeTickerAsync("BTC/USDT")
            );

            Console.WriteLine("Connected to multiple exchanges. Press 'Q' to quit.\n");

            await WaitForExit(cts);

            await Task.WhenAll(
                binanceClient.DisconnectAsync(),
                upbitClient.DisconnectAsync()
            );
        }

        /// <summary>
        /// Sample 3: Technical Indicator Analysis
        /// </summary>
        static async Task RunTechnicalIndicatorSample()
        {
            Console.WriteLine("\n=== Technical Indicator Analysis Sample ===\n");

            var client = new BinanceWebSocketClient();
            var cts = new CancellationTokenSource();

            // Initialize indicators
            var rsi = new RSI(14);
            var macd = new MACD(12, 26, 9);
            var bb = new BollingerBand(20, 2);
            var sma = new SMA(50);
            var ema = new EMA(20);

            var ohlcvBuffer = new List<Ohlc>();

            client.OnCandleReceived += (candle) =>
            {
                if (candle.result == null || candle.result.Count == 0) return;
                
                // Process each candle in the batch
                foreach (var candleItem in candle.result)
                {
                    var ohlcv = new Ohlc
                    {
                        openTime = candleItem.openTime,
                        closeTime = candleItem.closeTime,
                        Open = candleItem.open,
                        High = candleItem.high,
                        Low = candleItem.low,
                        Close = candleItem.close,
                        Volume = candleItem.volume
                    };
                    ohlcvBuffer.Add(ohlcv);
                }

                if (ohlcvBuffer.Count < 50) return; // Wait for enough data

                // Calculate indicators
                var rsiValue = rsi.Calculate(ohlcvBuffer);
                var macdResult = macd.Calculate(ohlcvBuffer);
                var bbResult = bb.Calculate(ohlcvBuffer);
                var smaValue = sma.Calculate(ohlcvBuffer);
                var emaValue = ema.Calculate(ohlcvBuffer);

                Console.WriteLine($"\n[{DateTime.Now:HH:mm:ss}] Technical Analysis for {candle.symbol}");
                if (candle.result != null && candle.result.Count > 0)
                {
                    Console.WriteLine($"  Price: {candle.result[0].close:F2}");
                }
                Console.WriteLine($"  RSI(14): {rsiValue:F2} {GetRSISignal(rsiValue)}");
                Console.WriteLine($"  MACD: {macdResult.MACD:F2}, Signal: {macdResult.Signal:F2}");
                Console.WriteLine($"  Bollinger Bands: Upper={bbResult.Upper:F2}, Middle={bbResult.Middle:F2}, Lower={bbResult.Lower:F2}");
                Console.WriteLine($"  SMA(50): {smaValue:F2}");
                Console.WriteLine($"  EMA(20): {emaValue:F2}");
                if (candle.result != null && candle.result.Count > 0)
                {
                    Console.WriteLine($"  Trend: {GetTrendSignal(candle.result[0].close, smaValue, emaValue)}");
                }
            };

            await client.ConnectAsync();
            await client.SubscribeCandlesAsync("BTC/USDT", "1m");

            Console.WriteLine("Calculating technical indicators. Press 'Q' to quit.\n");

            await WaitForExit(cts);

            await client.DisconnectAsync();
        }

        /// <summary>
        /// Sample 4: Orderbook Depth Monitoring
        /// </summary>
        static async Task RunOrderbookMonitoringSample()
        {
            Console.WriteLine("\n=== Orderbook Depth Monitoring Sample ===\n");

            var client = new BinanceWebSocketClient();
            var cts = new CancellationTokenSource();

            client.OnOrderbookReceived += (orderbook) =>
            {
                Console.Clear();
                Console.WriteLine($"Orderbook Depth Monitor - {orderbook.symbol}");
                Console.WriteLine($"Last Update: {DateTime.Now:HH:mm:ss.fff}");
                Console.WriteLine(new string('=', 60));

                // Display asks (reversed for visual representation)
                Console.WriteLine("\nASKS (Sell Orders):");
                for (int i = Math.Min(9, orderbook.result.asks.Count - 1); i >= 0; i--)
                {
                    var ask = orderbook.result.asks[i];
                    var barLength = (int)(ask.quantity * 10);
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"  {ask.price,10:F2} | {ask.quantity,12:F8} | {new string('█', Math.Min(barLength, 40))}");
                }

                Console.ResetColor();
                Console.WriteLine($"\n  SPREAD: {(orderbook.result.asks[0].price - orderbook.result.bids[0].price):F2}");

                // Display bids
                Console.WriteLine("\nBIDS (Buy Orders):");
                for (int i = 0; i < Math.Min(10, orderbook.result.bids.Count); i++)
                {
                    var bid = orderbook.result.bids[i];
                    var barLength = (int)(bid.quantity * 10);
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"  {bid.price,10:F2} | {bid.quantity,12:F8} | {new string('█', Math.Min(barLength, 40))}");
                }

                Console.ResetColor();
                Console.WriteLine("\nPress 'Q' to quit.");
            };

            await client.ConnectAsync();
            await client.SubscribeOrderbookAsync("BTC/USDT");

            await WaitForExit(cts);

            await client.DisconnectAsync();
        }

        /// <summary>
        /// Sample 5: Trade History Collection
        /// </summary>
        static async Task RunTradeHistorySample()
        {
            Console.WriteLine("\n=== Trade History Collection Sample ===\n");

            var client = new BinanceWebSocketClient();
            var cts = new CancellationTokenSource();
            var trades = new List<STradeItem>();
            var volumeStats = new VolumeStatistics();

            client.OnTradeReceived += (tradeData) =>
            {
                if (tradeData.result.Count > 0)
                {
                    var trade = tradeData.result[0];
                    trades.Add(trade);
                    volumeStats.Update(trade);

                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] Trade Executed");
                    Console.WriteLine($"  Symbol: {tradeData.symbol}");
                    Console.WriteLine($"  Price: {trade.price:F2}");
                    Console.WriteLine($"  Amount: {trade.quantity:F8}");
                    Console.WriteLine($"  Side: {(trade.sideType == SideType.Bid ? "BUY ↑" : "SELL ↓")}");
                Console.WriteLine($"  Total Trades: {trades.Count}");
                Console.WriteLine($"  Buy Volume: {volumeStats.BuyVolume:F8}");
                Console.WriteLine($"  Sell Volume: {volumeStats.SellVolume:F8}");
                Console.WriteLine($"  Net Volume: {volumeStats.NetVolume:F8}");
                    Console.WriteLine($"  VWAP: {volumeStats.VWAP:F2}\n");
                }
            };

            await client.ConnectAsync();
            await client.SubscribeTradesAsync("BTC/USDT");

            Console.WriteLine("Collecting trade history. Press 'Q' to quit.\n");

            await WaitForExit(cts);

            // Display summary
            Console.WriteLine("\n=== Trade Summary ===");
            Console.WriteLine($"Total Trades Collected: {trades.Count}");
            Console.WriteLine($"Time Period: {(trades.Count > 0 ? $"{DateTimeOffset.FromUnixTimeMilliseconds(trades[0].timestamp).ToString("HH:mm:ss")} - {DateTimeOffset.FromUnixTimeMilliseconds(trades[^1].timestamp).ToString("HH:mm:ss")}" : "N/A")}");
            Console.WriteLine($"Final Buy Volume: {volumeStats.BuyVolume:F8}");
            Console.WriteLine($"Final Sell Volume: {volumeStats.SellVolume:F8}");
            Console.WriteLine($"Final VWAP: {volumeStats.VWAP:F2}");

            await client.DisconnectAsync();
        }

        /// <summary>
        /// Sample 6: Real-time Ticker Updates
        /// </summary>
        static async Task RunTickerUpdatesSample()
        {
            Console.WriteLine("\n=== Real-time Ticker Updates Sample ===\n");

            var client = new BinanceWebSocketClient();
            var cts = new CancellationTokenSource();
            var symbols = new[] { "BTC/USDT", "ETH/USDT", "BNB/USDT", "ADA/USDT", "SOL/USDT" };
            var tickers = new Dictionary<string, TickerInfo>();

            client.OnTickerReceived += (ticker) =>
            {
                if (!tickers.ContainsKey(ticker.symbol))
                    tickers[ticker.symbol] = new TickerInfo();

                var info = tickers[ticker.symbol];
                info.Update(ticker);

                Console.Clear();
                Console.WriteLine("Real-time Ticker Monitor");
                Console.WriteLine($"Last Update: {DateTime.Now:HH:mm:ss}");
                Console.WriteLine(new string('=', 80));
                Console.WriteLine($"{"Symbol",-10} {"Last Price",12} {"24h Change",12} {"24h Volume",15} {"Bid",12} {"Ask",12}");
                Console.WriteLine(new string('-', 80));

                foreach (var kvp in tickers)
                {
                    var t = kvp.Value;
                    var changeColor = t.Change24h >= 0 ? ConsoleColor.Green : ConsoleColor.Red;
                    
                    Console.Write($"{kvp.Key,-10} ");
                    Console.Write($"{t.LastPrice,12:F2} ");
                    
                    Console.ForegroundColor = changeColor;
                    Console.Write($"{t.Change24h,11:F2}% ");
                    Console.ResetColor();
                    
                    Console.WriteLine($"{t.Volume24h,15:F2} {t.Bid,12:F2} {t.Ask,12:F2}");
                }

                Console.WriteLine("\nPress 'Q' to quit.");
            };

            await client.ConnectAsync();
            foreach (var symbol in symbols)
            {
                await client.SubscribeTickerAsync(symbol);
            }

            await WaitForExit(cts);

            await client.DisconnectAsync();
        }

        /// <summary>
        /// Sample 7: Advanced Indicator Combination
        /// </summary>
        static async Task RunAdvancedIndicatorSample()
        {
            Console.WriteLine("\n=== Advanced Indicator Combination Sample ===\n");

            var client = new BinanceWebSocketClient();
            var cts = new CancellationTokenSource();

            // Initialize multiple indicators
            var indicators = new IndicatorSet();
            var signalGenerator = new SignalGenerator();

            client.OnCandleReceived += (candle) =>
            {
                indicators.Update(candle);

                if (!indicators.IsReady) return;

                var signal = signalGenerator.GenerateSignal(indicators);

                Console.WriteLine($"\n[{DateTime.Now:HH:mm:ss}] Market Analysis for {candle.symbol}");
                if (candle.result != null && candle.result.Count > 0)
                {
                    Console.WriteLine($"  Current Price: {candle.result[0].close:F2}");
                }
                Console.WriteLine("\nIndicator Values:");
                Console.WriteLine($"  RSI(14): {indicators.RSI:F2}");
                Console.WriteLine($"  MACD: {indicators.MACD:F2}");
                Console.WriteLine($"  Stochastic: {indicators.Stochastic:F2}");
                Console.WriteLine($"  ADX: {indicators.ADX:F2}");
                Console.WriteLine($"  ATR: {indicators.ATR:F2}");
                Console.WriteLine($"  Volume Ratio: {indicators.VolumeRatio:F2}");
                
                Console.WriteLine($"\nMarket Conditions:");
                Console.WriteLine($"  Trend: {indicators.Trend}");
                Console.WriteLine($"  Volatility: {indicators.Volatility}");
                Console.WriteLine($"  Momentum: {indicators.Momentum}");
                
                Console.WriteLine($"\nSignal:");
                Console.ForegroundColor = signal.Type == SignalType.Buy ? ConsoleColor.Green : 
                                         signal.Type == SignalType.Sell ? ConsoleColor.Red : 
                                         ConsoleColor.Yellow;
                Console.WriteLine($"  {signal.Type} - Strength: {signal.Strength}/5");
                Console.WriteLine($"  Reason: {signal.Reason}");
                Console.ResetColor();
            };

            await client.ConnectAsync();
            await client.SubscribeCandlesAsync("BTC/USDT", "5m");

            Console.WriteLine("Running advanced indicator analysis. Press 'Q' to quit.\n");

            await WaitForExit(cts);

            await client.DisconnectAsync();
        }

        // Helper methods
        static async Task WaitForExit(CancellationTokenSource cts)
        {
            await Task.Run(() =>
            {
                while (true)
                {
                    var key = Console.ReadKey(true);
                    if (key.KeyChar == 'q' || key.KeyChar == 'Q')
                    {
                        cts.Cancel();
                        break;
                    }
                }
            });
        }

        static void DisplayPriceComparison(Dictionary<string, Dictionary<string, decimal>> prices)
        {
            Console.Clear();
            Console.WriteLine("Multi-Exchange Price Comparison");
            Console.WriteLine($"Last Update: {DateTime.Now:HH:mm:ss}");
            Console.WriteLine(new string('=', 60));
            Console.WriteLine($"{"Symbol",-15} {"Binance",15} {"Upbit",15} {"Difference",15}");
            Console.WriteLine(new string('-', 60));

            foreach (var symbol in prices.Keys)
            {
                var exchangePrices = prices[symbol];
                decimal binancePrice = exchangePrices.ContainsKey("Binance") ? exchangePrices["Binance"] : 0;
                decimal upbitPrice = exchangePrices.ContainsKey("Upbit") ? exchangePrices["Upbit"] : 0;
                decimal diff = binancePrice - upbitPrice;
                decimal diffPercent = binancePrice > 0 ? (diff / binancePrice * 100) : 0;

                Console.WriteLine($"{symbol,-15} {binancePrice,15:F2} {upbitPrice,15:F2} {diffPercent,14:F2}%");
            }

            Console.WriteLine("\nPress 'Q' to quit.");
        }

        static string GetRSISignal(double rsi)
        {
            if (rsi > 70) return "(Overbought ⚠️)";
            if (rsi < 30) return "(Oversold ⚠️)";
            return "(Neutral)";
        }

        static string GetTrendSignal(decimal price, decimal sma, decimal ema)
        {
            if (price > sma && price > ema) return "Bullish ↑";
            if (price < sma && price < ema) return "Bearish ↓";
            return "Neutral →";
        }
    }

    // Helper classes
    class VolumeStatistics
    {
        public decimal BuyVolume { get; private set; }
        public decimal SellVolume { get; private set; }
        public decimal NetVolume => BuyVolume - SellVolume;
        public decimal TotalVolume => BuyVolume + SellVolume;
        public decimal TotalValue { get; private set; }
        public decimal VWAP => TotalVolume > 0 ? TotalValue / TotalVolume : 0;

        public void Update(STradeItem trade)
        {
            if (trade.sideType == SideType.Bid)
                BuyVolume += trade.quantity;
            else
                SellVolume += trade.quantity;

            TotalValue += trade.price * trade.quantity;
        }
    }

    class TickerInfo
    {
        public decimal LastPrice { get; private set; }
        public decimal Bid { get; private set; }
        public decimal Ask { get; private set; }
        public decimal Volume24h { get; private set; }
        public decimal Change24h { get; private set; }

        public void Update(STicker ticker)
        {
            LastPrice = ticker.result.closePrice;
            Bid = ticker.result.bidPrice;
            Ask = ticker.result.askPrice;
            Volume24h = ticker.result.volume;
            Change24h = ticker.result.percentage;
        }
    }

    class IndicatorSet
    {
        private RSI rsi = new RSI(14);
        private MACD macd = new MACD(12, 26, 9);
        private ADX adx = new ADX(14);
        private ATR atr = new ATR(14);
        private List<Ohlc> buffer = new List<Ohlc>();

        public double RSI { get; private set; }
        public double MACD { get; private set; }
        public double Stochastic { get; private set; }
        public double ADX { get; private set; }
        public double ATR { get; private set; }
        public double VolumeRatio { get; private set; }
        public string Trend { get; private set; } = string.Empty;
        public string Volatility { get; private set; } = string.Empty;
        public string Momentum { get; private set; } = string.Empty;
        public bool IsReady => buffer.Count >= 50;

        public void Update(SCandle candle)
        {
            if (candle.result == null || candle.result.Count == 0) return;
            
            var candleItem = candle.result[0]; // Get first candle item
            var ohlc = new Ohlc
            {
                openTime = candleItem.openTime,
                closeTime = candleItem.closeTime,
                Open = candleItem.open,
                High = candleItem.high,
                Low = candleItem.low,
                Close = candleItem.close,
                Volume = candleItem.volume
            };
            buffer.Add(ohlc);
            if (buffer.Count > 100) buffer.RemoveAt(0);

            if (!IsReady) return;

            RSI = rsi.Calculate(buffer);
            var macdResult = macd.Calculate(buffer);
            MACD = macdResult.MACD;
            ADX = adx.Calculate(buffer).ADX;
            ATR = atr.Calculate(buffer).ATR;

            // Calculate additional metrics
            VolumeRatio = CalculateVolumeRatio();
            Trend = DetermineTrend();
            Volatility = DetermineVolatility();
            Momentum = DetermineMomentum();
        }

        private double CalculateVolumeRatio()
        {
            if (buffer.Count < 20) return 1;
            var recent = buffer.Skip(buffer.Count - 5).Average(x => (double)x.Volume);
            var average = buffer.Skip(buffer.Count - 20).Average(x => (double)x.Volume);
            return average > 0 ? recent / average : 1;
        }

        private string DetermineTrend()
        {
            if (ADX > 25 && MACD > 0) return "Strong Uptrend";
            if (ADX > 25 && MACD < 0) return "Strong Downtrend";
            if (ADX < 25) return "Ranging";
            return "Weak Trend";
        }

        private string DetermineVolatility()
        {
            var avgATR = buffer.Skip(buffer.Count - 20).Average(x => (double)x.Close) * 0.02;
            if (ATR > avgATR * 1.5) return "High";
            if (ATR < avgATR * 0.5) return "Low";
            return "Normal";
        }

        private string DetermineMomentum()
        {
            if (RSI > 60 && MACD > 0) return "Strong Bullish";
            if (RSI < 40 && MACD < 0) return "Strong Bearish";
            return "Neutral";
        }
    }

    class SignalGenerator
    {
        public Signal GenerateSignal(IndicatorSet indicators)
        {
            int bullishPoints = 0;
            int bearishPoints = 0;
            var reasons = new List<string>();

            // RSI signals
            if (indicators.RSI < 30)
            {
                bullishPoints += 2;
                reasons.Add("RSI oversold");
            }
            else if (indicators.RSI > 70)
            {
                bearishPoints += 2;
                reasons.Add("RSI overbought");
            }

            // MACD signals
            if (indicators.MACD > 0)
            {
                bullishPoints++;
                reasons.Add("MACD positive");
            }
            else
            {
                bearishPoints++;
                reasons.Add("MACD negative");
            }

            // Volume signals
            if (indicators.VolumeRatio > 1.5)
            {
                if (indicators.MACD > 0) bullishPoints++;
                else bearishPoints++;
                reasons.Add("High volume");
            }

            // Trend signals
            if (indicators.Trend.Contains("Uptrend"))
            {
                bullishPoints += 2;
                reasons.Add("Strong uptrend");
            }
            else if (indicators.Trend.Contains("Downtrend"))
            {
                bearishPoints += 2;
                reasons.Add("Strong downtrend");
            }

            // Generate signal
            SignalType type;
            int strength;

            if (bullishPoints > bearishPoints + 2)
            {
                type = SignalType.Buy;
                strength = Math.Min(5, bullishPoints);
            }
            else if (bearishPoints > bullishPoints + 2)
            {
                type = SignalType.Sell;
                strength = Math.Min(5, bearishPoints);
            }
            else
            {
                type = SignalType.Hold;
                strength = 1;
            }

            return new Signal
            {
                Type = type,
                Strength = strength,
                Reason = string.Join(", ", reasons)
            };
        }
    }

    class Signal
    {
        public SignalType Type { get; set; }
        public int Strength { get; set; }
        public string Reason { get; set; } = string.Empty;
    }

    enum SignalType
    {
        Buy,
        Sell,
        Hold
    }
}