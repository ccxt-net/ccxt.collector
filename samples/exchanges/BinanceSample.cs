using CCXT.Collector.Binance;
using CCXT.Collector.Core.Abstractions;
using CCXT.Collector.Service;
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
        private readonly List<SCandleItem> _ohlcBuffer;
        private CancellationTokenSource _cancellationTokenSource;

        public BinanceSample()
        {
            _client = new BinanceWebSocketClient();
            _ohlcBuffer = new List<SCandleItem>();
            _cancellationTokenSource = new CancellationTokenSource();
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
            Console.WriteLine("3. Ticker Dashboard");
            Console.WriteLine("4. Candlestick Data");
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
                        await RunTickerDashboard();
                        break;
                    case "4":
                        await RunCandlestickData();
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

            _client.OnOrderbookReceived += (orderbook) =>
            {
                Console.Clear();
                Console.WriteLine($"BINANCE ORDERBOOK - {orderbook.symbol}");
                Console.WriteLine($"Time: {DateTime.Now:HH:mm:ss.fff}");
                Console.WriteLine(new string('=', 70));

                if (orderbook.result?.asks?.Count > 0 && orderbook.result?.bids?.Count > 0)
                {
                    // Display top 5 asks (sell orders) in reverse
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("\nASKS (Sell Orders):");
                    Console.WriteLine($"{"Price",12} {"Amount",15} {"Total",15}");
                    Console.WriteLine(new string('-', 50));

                    for (int i = Math.Min(4, orderbook.result.asks.Count - 1); i >= 0; i--)
                    {
                        var ask = orderbook.result.asks[i];
                        Console.WriteLine($"{ask.price,12:F2} {ask.quantity,15:F8} {ask.amount,15:F2}");
                    }

                    // Display spread
                    Console.ResetColor();
                    var spread = orderbook.result.asks[0].price - orderbook.result.bids[0].price;
                    var spreadPercent = (spread / orderbook.result.bids[0].price) * 100;
                    Console.WriteLine($"\n{"SPREAD:",12} {spread:F2} ({spreadPercent:F4}%)");

                    // Display top 5 bids (buy orders)
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("\nBIDS (Buy Orders):");
                    Console.WriteLine($"{"Price",12} {"Amount",15} {"Total",15}");
                    Console.WriteLine(new string('-', 50));

                    for (int i = 0; i < Math.Min(5, orderbook.result.bids.Count); i++)
                    {
                        var bid = orderbook.result.bids[i];
                        Console.WriteLine($"{bid.price,12:F2} {bid.quantity,15:F8} {bid.amount,15:F2}");
                    }

                    Console.ResetColor();
                }

                Console.WriteLine("\nPress 'Q' to quit...");
            };

            _client.OnError += (error) => Console.WriteLine($"Error: {error}");
            _client.OnConnected += () => Console.WriteLine("Connected to Binance WebSocket");
            _client.OnDisconnected += () => Console.WriteLine("Disconnected from Binance");

            await _client.ConnectAsync();
            await _client.SubscribeOrderbookAsync("BTC/USDT");

            await WaitForExit();
        }

        /// <summary>
        /// Real-time trade stream analysis
        /// </summary>
        private async Task RunTradeAnalysis()
        {
            Console.WriteLine("\n💹 Starting Binance Trade Analysis...\n");

            var tradeCount = 0;
            var buyVolume = 0m;
            var sellVolume = 0m;

            _client.OnTradeReceived += (trade) =>
            {
                if (trade.result != null && trade.result.Count > 0)
                {
                    var tradeItem = trade.result[0];
                    tradeCount++;
                    var isBuy = tradeItem.sideType == SideType.Bid;
                    
                    if (isBuy)
                        buyVolume += tradeItem.quantity;
                    else
                        sellVolume += tradeItem.quantity;

                    Console.Clear();
                    Console.WriteLine($"BINANCE TRADE STREAM - {trade.symbol}");
                    Console.WriteLine($"Time: {DateTime.Now:HH:mm:ss.fff}");
                    Console.WriteLine(new string('=', 70));

                    // Display trade info
                    Console.WriteLine("\nLatest Trade:");
                    Console.ForegroundColor = isBuy ? ConsoleColor.Green : ConsoleColor.Red;
                    Console.WriteLine($"  Side: {(isBuy ? "BUY ↑" : "SELL ↓")}");
                    Console.ResetColor();
                    Console.WriteLine($"  Price: ${tradeItem.price:F2}");
                    Console.WriteLine($"  Amount: {tradeItem.quantity:F8} BTC");
                    Console.WriteLine($"  Value: ${tradeItem.amount:F2}");

                    // Display statistics
                    Console.WriteLine($"\n📊 Trade Statistics:");
                    Console.WriteLine($"  Total Trades: {tradeCount}");
                    Console.WriteLine($"  Buy Volume: {buyVolume:F8} BTC");
                    Console.WriteLine($"  Sell Volume: {sellVolume:F8} BTC");
                    
                    var totalVolume = buyVolume + sellVolume;
                    if (totalVolume > 0)
                    {
                        var buyPercent = (buyVolume / totalVolume) * 100;
                        var sentiment = buyPercent > 55 ? "BULLISH 🟢" :
                                       buyPercent < 45 ? "BEARISH 🔴" : "NEUTRAL ⚪";
                        Console.WriteLine($"  Buy %: {buyPercent:F1}%");
                        Console.WriteLine($"\n🎯 Market Sentiment: {sentiment}");
                    }
                }

                Console.WriteLine("\nPress 'Q' to quit...");
            };

            _client.OnError += (error) => Console.WriteLine($"Error: {error}");
            _client.OnConnected += () => Console.WriteLine("Connected to Binance WebSocket");
            _client.OnDisconnected += () => Console.WriteLine("Disconnected from Binance");

            await _client.ConnectAsync();
            await _client.SubscribeTradesAsync("BTC/USDT");

            await WaitForExit();
        }

        /// <summary>
        /// Real-time ticker dashboard
        /// </summary>
        private async Task RunTickerDashboard()
        {
            Console.WriteLine("\n📈 Starting Binance Ticker Dashboard...\n");

            var symbols = new[] { "BTC/USDT", "ETH/USDT", "BNB/USDT", "ADA/USDT", "SOL/USDT" };
            var tickers = new Dictionary<string, STicker>();

            _client.OnTickerReceived += (ticker) =>
            {
                tickers[ticker.symbol] = ticker;

                Console.Clear();
                Console.WriteLine("BINANCE TICKER DASHBOARD");
                Console.WriteLine($"Last Update: {DateTime.Now:HH:mm:ss}");
                Console.WriteLine(new string('=', 100));
                Console.WriteLine($"{"Symbol",-10} {"Last Price",12} {"24h Change",12} {"24h High",12} {"24h Low",12} {"Volume",15}");
                Console.WriteLine(new string('-', 100));

                foreach (var kvp in tickers.OrderBy(x => x.Key))
                {
                    var t = kvp.Value.result;
                    var changeColor = t.percentage >= 0 ? ConsoleColor.Green : ConsoleColor.Red;
                    
                    Console.Write($"{kvp.Key,-10} ");
                    Console.Write($"{t.closePrice,12:F2} ");
                    
                    Console.ForegroundColor = changeColor;
                    Console.Write($"{t.percentage,11:F2}% ");
                    Console.ResetColor();
                    
                    Console.Write($"{t.highPrice,12:F2} ");
                    Console.Write($"{t.lowPrice,12:F2} ");
                    Console.WriteLine($"{t.volume,15:F2}");
                }

                Console.WriteLine("\nPress 'Q' to quit...");
            };

            _client.OnError += (error) => Console.WriteLine($"Error: {error}");
            _client.OnConnected += () => Console.WriteLine("Connected to Binance WebSocket");
            _client.OnDisconnected += () => Console.WriteLine("Disconnected from Binance");

            await _client.ConnectAsync();
            
            foreach (var symbol in symbols)
            {
                await _client.SubscribeTickerAsync(symbol);
                await Task.Delay(100); // Small delay between subscriptions
            }

            await WaitForExit();
        }

        /// <summary>
        /// Candlestick data monitoring
        /// </summary>
        private async Task RunCandlestickData()
        {
            Console.WriteLine("\n🕯️ Starting Binance Candlestick Monitor...\n");

            _client.OnCandleReceived += (candle) =>
            {
                if (candle.result != null && candle.result.Count > 0)
                {
                    // Process each candle in the batch
                    foreach (var candleItem in candle.result)
                    {
                        _ohlcBuffer.Add(candleItem);
                        if (_ohlcBuffer.Count > 50) _ohlcBuffer.RemoveAt(0);
                    }

                    Console.Clear();
                    Console.WriteLine($"BINANCE CANDLESTICK DATA - {candle.symbol}");
                    Console.WriteLine($"Interval: {candle.interval} | Time: {DateTime.Now:HH:mm:ss}");
                    Console.WriteLine(new string('=', 70));

                    var c = candle.result[0]; // Display first candle
                    Console.WriteLine($"\nCurrent Candle:");
                    Console.WriteLine($"  Open:  ${c.open:F2}");
                    Console.WriteLine($"  High:  ${c.high:F2}");
                    Console.WriteLine($"  Low:   ${c.low:F2}");
                    Console.WriteLine($"  Close: ${c.close:F2}");
                    Console.WriteLine($"  Volume: {c.volume:F8}");
                    Console.WriteLine($"  Closed: {(c.isClosed ? "Yes" : "No")}");

                    // Simple moving average from buffer
                    if (_ohlcBuffer.Count >= 20)
                    {
                        var sma20 = _ohlcBuffer.Skip(_ohlcBuffer.Count - 20).Average(x => x.close);
                        Console.WriteLine($"\n📊 Simple Indicators:");
                        Console.WriteLine($"  SMA(20): ${sma20:F2}");
                        Console.WriteLine($"  Trend: {(c.close > sma20 ? "Above SMA ↑" : "Below SMA ↓")}");
                    }

                    Console.WriteLine("\nPress 'Q' to quit...");
                }
            };

            _client.OnError += (error) => Console.WriteLine($"Error: {error}");
            _client.OnConnected += () => Console.WriteLine("Connected to Binance WebSocket");
            _client.OnDisconnected += () => Console.WriteLine("Disconnected from Binance");

            await _client.ConnectAsync();
            await _client.SubscribeCandlesAsync("BTC/USDT", "1m");

            await WaitForExit();
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
    }
}