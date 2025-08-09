using CCXT.Collector.Upbit;
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
    /// Upbit Exchange Sample Implementation (Korean Exchange)
    /// Demonstrates KRW market data collection and analysis
    /// </summary>
    public class UpbitSample
    {
        private readonly UpbitWebSocketClient _client;
        private readonly Dictionary<string, STicker> _marketData;
        private CancellationTokenSource _cancellationTokenSource;

        public UpbitSample()
        {
            _client = new UpbitWebSocketClient();
            _marketData = new Dictionary<string, STicker>();
            _cancellationTokenSource = new CancellationTokenSource();
        }

        /// <summary>
        /// Run Upbit sample with Korean market focus
        /// </summary>
        public async Task RunAsync()
        {
            Console.WriteLine("===========================================");
            Console.WriteLine("     Upbit Exchange Sample (업비트)");
            Console.WriteLine("===========================================\n");

            Console.WriteLine("Select operation mode:");
            Console.WriteLine("1. KRW Market Overview (원화 마켓)");
            Console.WriteLine("2. Real-time Orderbook (실시간 호가창)");
            Console.WriteLine("3. Trade Analysis (체결 분석)");
            Console.WriteLine("4. Multi-Symbol Monitor (멀티 종목 모니터)");
            Console.WriteLine("0. Exit");

            Console.Write("\nYour choice: ");
            var choice = Console.ReadLine();

            try
            {
                switch (choice)
                {
                    case "1":
                        await RunKRWMarketOverview();
                        break;
                    case "2":
                        await RunOrderbookMonitor();
                        break;
                    case "3":
                        await RunTradeAnalysis();
                        break;
                    case "4":
                        await RunMultiSymbolMonitor();
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
        /// KRW Market Overview
        /// </summary>
        private async Task RunKRWMarketOverview()
        {
            Console.WriteLine("\n📊 Starting Upbit KRW Market Overview...\n");

            var krwSymbols = new[] { "BTC/KRW", "ETH/KRW", "XRP/KRW", "ADA/KRW", "DOGE/KRW" };

            _client.OnTickerReceived += (ticker) =>
            {
                if (ticker.symbol.EndsWith("/KRW"))
                {
                    _marketData[ticker.symbol] = ticker;

                    Console.Clear();
                    Console.WriteLine("UPBIT KRW MARKET OVERVIEW (원화 마켓)");
                    Console.WriteLine($"Update Time: {DateTime.Now:HH:mm:ss}");
                    Console.WriteLine(new string('=', 90));
                    Console.WriteLine($"{"종목",-10} {"현재가 (KRW)",15} {"변동률",10} {"거래량",15} {"거래대금 (억)",15}");
                    Console.WriteLine(new string('-', 90));

                    foreach (var kvp in _marketData.OrderBy(x => x.Key))
                    {
                        var t = kvp.Value.result;
                        var changeColor = t.percentage >= 0 ? ConsoleColor.Green : ConsoleColor.Red;
                        var tradingValue = (t.volume * t.closePrice) / 100000000; // 억 단위

                        Console.Write($"{kvp.Key,-10} ");
                        Console.Write($"₩{t.closePrice,14:N0} ");
                        
                        Console.ForegroundColor = changeColor;
                        Console.Write($"{t.percentage,9:F2}% ");
                        Console.ResetColor();
                        
                        Console.Write($"{t.volume,15:F4} ");
                        Console.WriteLine($"₩{tradingValue,14:F2}");
                    }

                    // Market summary
                    if (_marketData.Count > 0)
                    {
                        var avgChange = _marketData.Values.Average(t => t.result.percentage);
                        var totalVolume = _marketData.Values.Sum(t => t.result.volume * t.result.closePrice) / 100000000;
                        
                        Console.WriteLine($"\n📈 시장 요약:");
                        Console.WriteLine($"  평균 변동률: {avgChange:F2}%");
                        Console.WriteLine($"  총 거래대금: ₩{totalVolume:F2}억");
                    }
                }

                Console.WriteLine("\nPress 'Q' to quit...");
            };

            _client.OnError += (error) => Console.WriteLine($"Error: {error}");
            _client.OnConnected += () => Console.WriteLine("Connected to Upbit WebSocket");
            _client.OnDisconnected += () => Console.WriteLine("Disconnected from Upbit");

            await _client.ConnectAsync();
            
            foreach (var symbol in krwSymbols)
            {
                await _client.SubscribeTickerAsync(symbol);
                await Task.Delay(100);
            }

            await WaitForExit();
        }

        /// <summary>
        /// Real-time orderbook monitoring
        /// </summary>
        private async Task RunOrderbookMonitor()
        {
            Console.WriteLine("\n📊 Starting Upbit Orderbook Monitor (호가창)...\n");

            _client.OnOrderbookReceived += (orderbook) =>
            {
                Console.Clear();
                Console.WriteLine($"UPBIT ORDERBOOK - {orderbook.symbol}");
                Console.WriteLine($"Time: {DateTime.Now:HH:mm:ss.fff}");
                Console.WriteLine(new string('=', 70));

                if (orderbook.result?.asks?.Count > 0 && orderbook.result?.bids?.Count > 0)
                {
                    // Display asks (매도 호가)
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("\n매도 호가 (ASKS):");
                    Console.WriteLine($"{"가격 (KRW)",15} {"수량",15} {"총액 (KRW)",15}");
                    Console.WriteLine(new string('-', 50));

                    for (int i = Math.Min(4, orderbook.result.asks.Count - 1); i >= 0; i--)
                    {
                        var ask = orderbook.result.asks[i];
                        var total = ask.price * ask.quantity;
                        Console.WriteLine($"₩{ask.price,14:N0} {ask.quantity,15:F8} ₩{total,14:N0}");
                    }

                    // Display spread
                    Console.ResetColor();
                    var spread = orderbook.result.asks[0].price - orderbook.result.bids[0].price;
                    Console.WriteLine($"\n스프레드: ₩{spread:N0}");

                    // Display bids (매수 호가)
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("\n매수 호가 (BIDS):");
                    Console.WriteLine($"{"가격 (KRW)",15} {"수량",15} {"총액 (KRW)",15}");
                    Console.WriteLine(new string('-', 50));

                    for (int i = 0; i < Math.Min(5, orderbook.result.bids.Count); i++)
                    {
                        var bid = orderbook.result.bids[i];
                        var total = bid.price * bid.quantity;
                        Console.WriteLine($"₩{bid.price,14:N0} {bid.quantity,15:F8} ₩{total,14:N0}");
                    }

                    Console.ResetColor();
                }

                Console.WriteLine("\nPress 'Q' to quit...");
            };

            _client.OnError += (error) => Console.WriteLine($"Error: {error}");
            _client.OnConnected += () => Console.WriteLine("Connected to Upbit WebSocket");
            _client.OnDisconnected += () => Console.WriteLine("Disconnected from Upbit");

            await _client.ConnectAsync();
            await _client.SubscribeOrderbookAsync("BTC/KRW");

            await WaitForExit();
        }

        /// <summary>
        /// Trade analysis for KRW markets
        /// </summary>
        private async Task RunTradeAnalysis()
        {
            Console.WriteLine("\n💹 Starting Upbit Trade Analysis (체결 분석)...\n");

            var buyVolume = 0m;
            var sellVolume = 0m;
            var tradeCount = 0;

            _client.OnTradeReceived += (trade) =>
            {
                if (trade.result != null && trade.result.Count > 0 && trade.symbol.EndsWith("/KRW"))
                {
                    var tradeItem = trade.result[0];
                    tradeCount++;
                    var isBuy = tradeItem.sideType == SideType.Bid;
                    
                    if (isBuy)
                        buyVolume += tradeItem.amount;
                    else
                        sellVolume += tradeItem.amount;

                    Console.Clear();
                    Console.WriteLine($"UPBIT TRADE ANALYSIS - {trade.symbol}");
                    Console.WriteLine($"Time: {DateTime.Now:HH:mm:ss.fff}");
                    Console.WriteLine(new string('=', 70));

                    Console.WriteLine("\n최근 체결:");
                    Console.ForegroundColor = isBuy ? ConsoleColor.Green : ConsoleColor.Red;
                    Console.WriteLine($"  구분: {(isBuy ? "매수 ↑" : "매도 ↓")}");
                    Console.ResetColor();
                    Console.WriteLine($"  체결가: ₩{tradeItem.price:N0}");
                    Console.WriteLine($"  수량: {tradeItem.quantity:F8}");
                    Console.WriteLine($"  대금: ₩{tradeItem.amount:N0}");

                    Console.WriteLine($"\n📊 체결 통계:");
                    Console.WriteLine($"  총 체결 수: {tradeCount}");
                    Console.WriteLine($"  매수 금액: ₩{buyVolume:N0}");
                    Console.WriteLine($"  매도 금액: ₩{sellVolume:N0}");
                    
                    var totalVolume = buyVolume + sellVolume;
                    if (totalVolume > 0)
                    {
                        var buyRatio = (buyVolume / totalVolume) * 100;
                        Console.WriteLine($"  매수 비율: {buyRatio:F1}%");
                        
                        var sentiment = buyRatio > 55 ? "강세 🟢" :
                                       buyRatio < 45 ? "약세 🔴" : "중립 ⚪";
                        Console.WriteLine($"\n시장 심리: {sentiment}");
                    }
                }

                Console.WriteLine("\nPress 'Q' to quit...");
            };

            _client.OnError += (error) => Console.WriteLine($"Error: {error}");
            _client.OnConnected += () => Console.WriteLine("Connected to Upbit WebSocket");
            _client.OnDisconnected += () => Console.WriteLine("Disconnected from Upbit");

            await _client.ConnectAsync();
            await _client.SubscribeTradesAsync("BTC/KRW");

            await WaitForExit();
        }

        /// <summary>
        /// Multi-symbol monitoring
        /// </summary>
        private async Task RunMultiSymbolMonitor()
        {
            Console.WriteLine("\n📊 Starting Upbit Multi-Symbol Monitor...\n");

            var symbols = new[] { "BTC/KRW", "ETH/KRW", "XRP/KRW", "ADA/KRW", "DOGE/KRW", "SOL/KRW", "MATIC/KRW" };
            var previousPrices = new Dictionary<string, decimal>();

            _client.OnTickerReceived += (ticker) =>
            {
                _marketData[ticker.symbol] = ticker;
                
                // Track price changes
                decimal previousPrice = 0;
                if (previousPrices.ContainsKey(ticker.symbol))
                    previousPrice = previousPrices[ticker.symbol];
                previousPrices[ticker.symbol] = ticker.result.closePrice;

                Console.Clear();
                Console.WriteLine("UPBIT MULTI-SYMBOL MONITOR");
                Console.WriteLine($"Last Update: {DateTime.Now:HH:mm:ss}");
                Console.WriteLine(new string('=', 100));
                Console.WriteLine($"{"Symbol",-10} {"Price (KRW)",15} {"Change",10} {"Trend",8} {"Volume",15} {"Value (억)",12}");
                Console.WriteLine(new string('-', 100));

                foreach (var kvp in _marketData.OrderByDescending(x => Math.Abs(x.Value.result.percentage)))
                {
                    var t = kvp.Value.result;
                    var changeColor = t.percentage >= 0 ? ConsoleColor.Green : ConsoleColor.Red;
                    var tradingValue = (t.volume * t.closePrice) / 100000000;
                    
                    // Price trend indicator
                    string trend = "→";
                    if (previousPrices.ContainsKey(kvp.Key))
                    {
                        var prev = previousPrices[kvp.Key];
                        trend = t.closePrice > prev ? "↑" : t.closePrice < prev ? "↓" : "→";
                    }
                    
                    Console.Write($"{kvp.Key,-10} ");
                    Console.Write($"₩{t.closePrice,14:N0} ");
                    
                    Console.ForegroundColor = changeColor;
                    Console.Write($"{t.percentage,9:F2}% ");
                    Console.Write($"{trend,8} ");
                    Console.ResetColor();
                    
                    Console.Write($"{t.volume,15:F4} ");
                    Console.WriteLine($"₩{tradingValue,11:F2}");
                }

                // Top movers
                if (_marketData.Count > 0)
                {
                    var topGainer = _marketData.OrderByDescending(x => x.Value.result.percentage).First();
                    var topLoser = _marketData.OrderBy(x => x.Value.result.percentage).First();
                    
                    Console.WriteLine($"\n📊 주요 지표:");
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"  최고 상승: {topGainer.Key} ({topGainer.Value.result.percentage:F2}%)");
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"  최고 하락: {topLoser.Key} ({topLoser.Value.result.percentage:F2}%)");
                    Console.ResetColor();
                }

                Console.WriteLine("\nPress 'Q' to quit...");
            };

            _client.OnError += (error) => Console.WriteLine($"Error: {error}");
            _client.OnConnected += () => Console.WriteLine("Connected to Upbit WebSocket");
            _client.OnDisconnected += () => Console.WriteLine("Disconnected from Upbit");

            await _client.ConnectAsync();
            
            foreach (var symbol in symbols)
            {
                await _client.SubscribeTickerAsync(symbol);
                await Task.Delay(100);
            }

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