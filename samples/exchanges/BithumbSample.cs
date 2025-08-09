using CCXT.Collector.Bithumb;
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
    /// Bithumb Exchange Sample Implementation (Korean Exchange)
    /// Focuses on payment coins and KRW market analysis
    /// </summary>
    public class BithumbSample
    {
        private readonly BithumbWebSocketClient _client;
        private readonly Dictionary<string, STicker> _tickerData;
        private readonly Dictionary<string, decimal> _volumeTracker;
        private CancellationTokenSource _cancellationTokenSource;

        // Payment coins commonly traded on Bithumb
        private readonly string[] _paymentCoins = { "XRP/KRW", "ADA/KRW", "DOGE/KRW", "TRX/KRW", "MATIC/KRW" };

        public BithumbSample()
        {
            _client = new BithumbWebSocketClient();
            _tickerData = new Dictionary<string, STicker>();
            _volumeTracker = new Dictionary<string, decimal>();
            _cancellationTokenSource = new CancellationTokenSource();
        }

        /// <summary>
        /// Run Bithumb sample focused on payment coins
        /// </summary>
        public async Task RunAsync()
        {
            Console.WriteLine("===========================================");
            Console.WriteLine("     Bithumb Exchange Sample (빗썸)");
            Console.WriteLine("===========================================\n");

            Console.WriteLine("Select operation mode:");
            Console.WriteLine("1. Payment Coin Monitor (결제 코인 모니터)");
            Console.WriteLine("2. Real-time Orderbook (실시간 호가)");
            Console.WriteLine("3. Trade Flow Analysis (거래 흐름 분석)");
            Console.WriteLine("4. Volume Analysis (거래량 분석)");
            Console.WriteLine("0. Exit");

            Console.Write("\nYour choice: ");
            var choice = Console.ReadLine();

            try
            {
                switch (choice)
                {
                    case "1":
                        await RunPaymentCoinMonitor();
                        break;
                    case "2":
                        await RunOrderbookMonitor();
                        break;
                    case "3":
                        await RunTradeFlowAnalysis();
                        break;
                    case "4":
                        await RunVolumeAnalysis();
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
        /// Monitor payment coins specifically
        /// </summary>
        private async Task RunPaymentCoinMonitor()
        {
            Console.WriteLine("\n🪙 Starting Bithumb Payment Coin Monitor...\n");

            _client.OnTickerReceived += (ticker) =>
            {
                if (Array.Exists(_paymentCoins, coin => coin == ticker.symbol))
                {
                    _tickerData[ticker.symbol] = ticker;

                    Console.Clear();
                    Console.WriteLine("BITHUMB PAYMENT COIN MONITOR (결제 코인)");
                    Console.WriteLine($"Time: {DateTime.Now:HH:mm:ss}");
                    Console.WriteLine(new string('=', 100));
                    Console.WriteLine($"{"코인",-10} {"현재가 (KRW)",15} {"변동률",10} {"거래량",15} {"거래대금 (억)",15} {"상태",10}");
                    Console.WriteLine(new string('-', 100));

                    foreach (var kvp in _tickerData.OrderByDescending(x => Math.Abs(x.Value.result.percentage)))
                    {
                        var t = kvp.Value.result;
                        var changeColor = t.percentage >= 0 ? ConsoleColor.Green : ConsoleColor.Red;
                        var tradingValue = (t.volume * t.closePrice) / 100000000;
                        
                        // Determine market state
                        string state = "중립";
                        if (t.percentage > 5) state = "급등 🚀";
                        else if (t.percentage > 2) state = "상승 ↑";
                        else if (t.percentage < -5) state = "급락 📉";
                        else if (t.percentage < -2) state = "하락 ↓";
                        
                        Console.Write($"{kvp.Key,-10} ");
                        Console.Write($"₩{t.closePrice,14:N2} ");
                        
                        Console.ForegroundColor = changeColor;
                        Console.Write($"{t.percentage,9:F2}% ");
                        Console.ResetColor();
                        
                        Console.Write($"{t.volume,15:F2} ");
                        Console.Write($"₩{tradingValue,14:F2} ");
                        Console.WriteLine($"{state,10}");
                    }

                    // Payment coin analysis
                    if (_tickerData.Count > 0)
                    {
                        var avgChange = _tickerData.Values.Average(t => t.result.percentage);
                        var totalVolume = _tickerData.Values.Sum(t => t.result.volume * t.result.closePrice) / 100000000;
                        var bestPerformer = _tickerData.OrderByDescending(x => x.Value.result.percentage).First();
                        
                        Console.WriteLine($"\n📊 결제 코인 분석:");
                        Console.WriteLine($"  평균 변동률: {avgChange:F2}%");
                        Console.WriteLine($"  총 거래대금: ₩{totalVolume:F2}억");
                        Console.WriteLine($"  최고 상승: {bestPerformer.Key} ({bestPerformer.Value.result.percentage:F2}%)");
                        
                        var sentiment = avgChange > 2 ? "매수 우세 🟢" :
                                       avgChange < -2 ? "매도 우세 🔴" : "관망세 ⚪";
                        Console.WriteLine($"  시장 심리: {sentiment}");
                    }
                }

                Console.WriteLine("\nPress 'Q' to quit...");
            };

            _client.OnError += (error) => Console.WriteLine($"Error: {error}");
            _client.OnConnected += () => Console.WriteLine("Connected to Bithumb WebSocket");
            _client.OnDisconnected += () => Console.WriteLine("Disconnected from Bithumb");

            await _client.ConnectAsync();
            
            foreach (var coin in _paymentCoins)
            {
                await _client.SubscribeTickerAsync(coin);
                await Task.Delay(100);
            }

            await WaitForExit();
        }

        /// <summary>
        /// Real-time orderbook monitoring with depth analysis
        /// </summary>
        private async Task RunOrderbookMonitor()
        {
            Console.WriteLine("\n📊 Starting Bithumb Orderbook Monitor...\n");

            decimal totalBidVolume = 0;
            decimal totalAskVolume = 0;

            _client.OnOrderbookReceived += (orderbook) =>
            {
                Console.Clear();
                Console.WriteLine($"BITHUMB ORDERBOOK - {orderbook.symbol}");
                Console.WriteLine($"Time: {DateTime.Now:HH:mm:ss.fff}");
                Console.WriteLine(new string('=', 80));

                if (orderbook.result?.asks?.Count > 0 && orderbook.result?.bids?.Count > 0)
                {
                    // Calculate volumes
                    totalBidVolume = orderbook.result.bids.Sum(b => b.quantity);
                    totalAskVolume = orderbook.result.asks.Sum(a => a.quantity);
                    var volumeRatio = totalBidVolume / Math.Max(totalAskVolume, 0.001m);

                    // Display market pressure
                    Console.WriteLine($"\n📊 시장 압력 분석:");
                    Console.WriteLine($"  매수 물량: {totalBidVolume:F8}");
                    Console.WriteLine($"  매도 물량: {totalAskVolume:F8}");
                    Console.WriteLine($"  매수/매도 비율: {volumeRatio:F2}");
                    
                    var pressure = volumeRatio > 1.2m ? "강한 매수압 🟢" :
                                  volumeRatio < 0.8m ? "강한 매도압 🔴" : "균형 ⚪";
                    Console.WriteLine($"  시장 압력: {pressure}");

                    // Display asks
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("\n매도 호가 (ASKS):");
                    Console.WriteLine($"{"가격 (KRW)",15} {"수량",15} {"누적",15}");
                    Console.WriteLine(new string('-', 50));

                    decimal askAccum = 0;
                    for (int i = Math.Min(4, orderbook.result.asks.Count - 1); i >= 0; i--)
                    {
                        var ask = orderbook.result.asks[i];
                        askAccum += ask.quantity;
                        Console.WriteLine($"₩{ask.price,14:N0} {ask.quantity,15:F8} {askAccum,15:F8}");
                    }

                    // Display spread
                    Console.ResetColor();
                    var spread = orderbook.result.asks[0].price - orderbook.result.bids[0].price;
                    var spreadPercent = (spread / orderbook.result.bids[0].price) * 100;
                    Console.WriteLine($"\n스프레드: ₩{spread:N0} ({spreadPercent:F4}%)");

                    // Display bids
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("\n매수 호가 (BIDS):");
                    Console.WriteLine($"{"가격 (KRW)",15} {"수량",15} {"누적",15}");
                    Console.WriteLine(new string('-', 50));

                    decimal bidAccum = 0;
                    for (int i = 0; i < Math.Min(5, orderbook.result.bids.Count); i++)
                    {
                        var bid = orderbook.result.bids[i];
                        bidAccum += bid.quantity;
                        Console.WriteLine($"₩{bid.price,14:N0} {bid.quantity,15:F8} {bidAccum,15:F8}");
                    }

                    Console.ResetColor();
                }

                Console.WriteLine("\nPress 'Q' to quit...");
            };

            _client.OnError += (error) => Console.WriteLine($"Error: {error}");
            _client.OnConnected += () => Console.WriteLine("Connected to Bithumb WebSocket");
            _client.OnDisconnected += () => Console.WriteLine("Disconnected from Bithumb");

            await _client.ConnectAsync();
            await _client.SubscribeOrderbookAsync("BTC/KRW");

            await WaitForExit();
        }

        /// <summary>
        /// Analyze trade flow and market sentiment
        /// </summary>
        private async Task RunTradeFlowAnalysis()
        {
            Console.WriteLine("\n💹 Starting Bithumb Trade Flow Analysis...\n");

            var buyCount = 0;
            var sellCount = 0;
            decimal buyVolume = 0;
            decimal sellVolume = 0;
            var largeTrades = new List<(DateTime time, decimal price, decimal amount, bool isBuy)>();

            _client.OnTradeReceived += (trade) =>
            {
                if (trade.result != null && trade.result.Count > 0)
                {
                    var tradeItem = trade.result[0];
                    var isBuy = tradeItem.sideType == SideType.Bid;
                    
                    if (isBuy)
                    {
                        buyCount++;
                        buyVolume += tradeItem.amount;
                    }
                    else
                    {
                        sellCount++;
                        sellVolume += tradeItem.amount;
                    }

                    // Track large trades (> 10 million KRW)
                    if (tradeItem.amount > 10000000)
                    {
                        largeTrades.Add((DateTime.Now, tradeItem.price, tradeItem.amount, isBuy));
                        if (largeTrades.Count > 10) largeTrades.RemoveAt(0);
                    }

                    Console.Clear();
                    Console.WriteLine($"BITHUMB TRADE FLOW ANALYSIS - {trade.symbol}");
                    Console.WriteLine($"Time: {DateTime.Now:HH:mm:ss}");
                    Console.WriteLine(new string('=', 80));

                    // Current trade
                    Console.WriteLine("\n현재 체결:");
                    Console.ForegroundColor = isBuy ? ConsoleColor.Green : ConsoleColor.Red;
                    Console.WriteLine($"  {(isBuy ? "매수" : "매도")} ₩{tradeItem.price:N0} × {tradeItem.quantity:F8} = ₩{tradeItem.amount:N0}");
                    Console.ResetColor();

                    // Flow statistics
                    Console.WriteLine($"\n📊 거래 흐름 통계:");
                    Console.WriteLine($"  매수: {buyCount}건 (₩{buyVolume:N0})");
                    Console.WriteLine($"  매도: {sellCount}건 (₩{sellVolume:N0})");
                    
                    var netFlow = buyVolume - sellVolume;
                    var flowDirection = netFlow > 0 ? "매수 우세 🟢" : netFlow < 0 ? "매도 우세 🔴" : "균형 ⚪";
                    Console.WriteLine($"  순 거래대금: ₩{netFlow:N0}");
                    Console.WriteLine($"  방향성: {flowDirection}");

                    // Large trades
                    if (largeTrades.Count > 0)
                    {
                        Console.WriteLine($"\n🐋 대량 거래 (1천만원 이상):");
                        foreach (var lt in largeTrades.TakeLast(5))
                        {
                            Console.ForegroundColor = lt.isBuy ? ConsoleColor.Green : ConsoleColor.Red;
                            Console.WriteLine($"  [{lt.time:HH:mm:ss}] {(lt.isBuy ? "매수" : "매도")} ₩{lt.amount:N0}");
                        }
                        Console.ResetColor();
                    }

                    // Market sentiment
                    var buyRatio = (buyCount + sellCount) > 0 ? (buyCount * 100.0 / (buyCount + sellCount)) : 50;
                    Console.WriteLine($"\n🎯 시장 심리:");
                    Console.WriteLine($"  매수 비율: {buyRatio:F1}%");
                    
                    string sentiment;
                    if (buyRatio > 60) sentiment = "매우 강세 🟢🟢";
                    else if (buyRatio > 55) sentiment = "강세 🟢";
                    else if (buyRatio < 40) sentiment = "매우 약세 🔴🔴";
                    else if (buyRatio < 45) sentiment = "약세 🔴";
                    else sentiment = "중립 ⚪";
                    
                    Console.WriteLine($"  심리: {sentiment}");
                }

                Console.WriteLine("\nPress 'Q' to quit...");
            };

            _client.OnError += (error) => Console.WriteLine($"Error: {error}");
            _client.OnConnected += () => Console.WriteLine("Connected to Bithumb WebSocket");
            _client.OnDisconnected += () => Console.WriteLine("Disconnected from Bithumb");

            await _client.ConnectAsync();
            await _client.SubscribeTradesAsync("BTC/KRW");

            await WaitForExit();
        }

        /// <summary>
        /// Volume analysis across multiple symbols
        /// </summary>
        private async Task RunVolumeAnalysis()
        {
            Console.WriteLine("\n📊 Starting Bithumb Volume Analysis...\n");

            var symbols = new[] { "BTC/KRW", "ETH/KRW", "XRP/KRW", "ADA/KRW", "DOGE/KRW" };

            _client.OnTickerReceived += (ticker) =>
            {
                _tickerData[ticker.symbol] = ticker;
                
                // Track volume changes
                if (!_volumeTracker.ContainsKey(ticker.symbol))
                    _volumeTracker[ticker.symbol] = ticker.result.volume;
                
                var volumeChange = ticker.result.volume - _volumeTracker[ticker.symbol];
                _volumeTracker[ticker.symbol] = ticker.result.volume;

                Console.Clear();
                Console.WriteLine("BITHUMB VOLUME ANALYSIS");
                Console.WriteLine($"Time: {DateTime.Now:HH:mm:ss}");
                Console.WriteLine(new string('=', 110));
                Console.WriteLine($"{"Symbol",-10} {"Price (KRW)",15} {"Volume",15} {"Vol Change",15} {"Value (억)",12} {"Activity",10}");
                Console.WriteLine(new string('-', 110));

                foreach (var kvp in _tickerData.OrderByDescending(x => x.Value.result.volume))
                {
                    var t = kvp.Value.result;
                    var tradingValue = (t.volume * t.closePrice) / 100000000;
                    var volChange = _volumeTracker.ContainsKey(kvp.Key) ? 
                        t.volume - _volumeTracker[kvp.Key] : 0;
                    
                    // Activity level based on volume
                    string activity;
                    if (tradingValue > 100) activity = "매우활발 🔥";
                    else if (tradingValue > 50) activity = "활발 🟢";
                    else if (tradingValue > 10) activity = "보통 ⚪";
                    else activity = "저조 🔴";
                    
                    Console.Write($"{kvp.Key,-10} ");
                    Console.Write($"₩{t.closePrice,14:N0} ");
                    Console.Write($"{t.volume,15:F4} ");
                    
                    var changeColor = volChange >= 0 ? ConsoleColor.Green : ConsoleColor.Red;
                    Console.ForegroundColor = changeColor;
                    Console.Write($"{volChange,15:F4} ");
                    Console.ResetColor();
                    
                    Console.Write($"₩{tradingValue,11:F2} ");
                    Console.WriteLine($"{activity,10}");
                }

                // Volume analysis summary
                if (_tickerData.Count > 0)
                {
                    var totalVolume = _tickerData.Values.Sum(t => t.result.volume * t.result.closePrice) / 100000000;
                    var topVolume = _tickerData.OrderByDescending(x => x.Value.result.volume * x.Value.result.closePrice).First();
                    
                    Console.WriteLine($"\n📊 거래량 요약:");
                    Console.WriteLine($"  총 거래대금: ₩{totalVolume:F2}억");
                    Console.WriteLine($"  최대 거래: {topVolume.Key}");
                    
                    var marketActivity = totalVolume > 500 ? "매우 활발" :
                                        totalVolume > 200 ? "활발" :
                                        totalVolume > 100 ? "보통" : "저조";
                    Console.WriteLine($"  시장 활동: {marketActivity}");
                }

                Console.WriteLine("\nPress 'Q' to quit...");
            };

            _client.OnError += (error) => Console.WriteLine($"Error: {error}");
            _client.OnConnected += () => Console.WriteLine("Connected to Bithumb WebSocket");
            _client.OnDisconnected += () => Console.WriteLine("Disconnected from Bithumb");

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