using CCXT.Collector.Upbit;
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
    /// Upbit Exchange Sample Implementation (Korean Exchange)
    /// Demonstrates KRW market data collection and analysis
    /// </summary>
    public class UpbitSample
    {
        private readonly UpbitWebSocketClient _client;
        private readonly Dictionary<string, MarketData> _marketData;
        private readonly List<Ohlc> _ohlcBuffer;
        private CancellationTokenSource _cancellationTokenSource;

        public UpbitSample()
        {
            _client = new UpbitWebSocketClient();
            _marketData = new Dictionary<string, MarketData>();
            _ohlcBuffer = new List<Ohlc>();
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
            Console.WriteLine("3. Volume Analysis (거래량 분석)");
            Console.WriteLine("4. Premium Monitor (김치 프리미엄)");
            Console.WriteLine("5. Top Movers (급등/급락 종목)");
            Console.WriteLine("6. Full KRW Market Dashboard");
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
                        await RunVolumeAnalysis();
                        break;
                    case "4":
                        await RunPremiumMonitor();
                        break;
                    case "5":
                        await RunTopMovers();
                        break;
                    case "6":
                        await RunFullDashboard();
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

            var krwSymbols = new[]
            {
                "KRW-BTC", "KRW-ETH", "KRW-XRP", "KRW-ADA", "KRW-SOL",
                "KRW-DOGE", "KRW-AVAX", "KRW-DOT", "KRW-MATIC", "KRW-LINK",
                "KRW-ATOM", "KRW-UNI", "KRW-ETC", "KRW-BCH", "KRW-TRX"
            };

            _client.OnTickerReceived += (ticker) =>
            {
                if (!_marketData.ContainsKey(ticker.symbol))
                    _marketData[ticker.symbol] = new MarketData();

                _marketData[ticker.symbol].UpdateFromTicker(ticker);

                Console.Clear();
                Console.WriteLine("UPBIT KRW MARKET OVERVIEW (업비트 원화 마켓)");
                Console.WriteLine($"Time: {DateTime.Now:HH:mm:ss} KST");
                Console.WriteLine(new string('=', 120));
                Console.WriteLine($"{"Symbol",-12} {"현재가 (KRW)",15} {"변동률",10} {"거래대금 (억)",12} {"거래량",15} {"고가",15} {"저가",15}");
                Console.WriteLine(new string('-', 120));

                var sortedMarkets = _marketData.OrderByDescending(x => x.Value.Volume24hKRW);

                foreach (var market in sortedMarkets)
                {
                    var data = market.Value;
                    var changeColor = data.Change24h >= 0 ? ConsoleColor.Green : ConsoleColor.Red;

                    Console.Write($"{market.Key,-12} ");
                    Console.Write($"₩{data.LastPrice,14:N0} ");

                    Console.ForegroundColor = changeColor;
                    Console.Write($"{(data.Change24h >= 0 ? "▲" : "▼")}{Math.Abs(data.Change24h),8:F2}% ");
                    Console.ResetColor();

                    var volumeInBillion = data.Volume24hKRW / 100_000_000; // 억 단위
                    Console.Write($"{volumeInBillion,12:N0}억 ");
                    Console.Write($"{data.Volume24h,15:N2} ");
                    Console.Write($"₩{data.High24h,14:N0} ");
                    Console.WriteLine($"₩{data.Low24h,14:N0}");
                }

                // Market Statistics
                var totalVolume = _marketData.Values.Sum(x => x.Volume24hKRW) / 100_000_000;
                var avgChange = _marketData.Values.Average(x => x.Change24h);
                var gainers = _marketData.Values.Count(x => x.Change24h > 0);
                var losers = _marketData.Values.Count(x => x.Change24h < 0);

                Console.WriteLine($"\n📊 시장 통계:");
                Console.WriteLine($"  총 거래대금: {totalVolume:N0}억원");
                Console.WriteLine($"  평균 변동률: {avgChange:F2}%");
                Console.WriteLine($"  상승: {gainers}종목 | 하락: {losers}종목");

                // Top gainers and losers
                var topGainer = _marketData.OrderByDescending(x => x.Value.Change24h).FirstOrDefault();
                var topLoser = _marketData.OrderBy(x => x.Value.Change24h).FirstOrDefault();

                if (topGainer.Key != null)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"  🚀 최고 상승: {topGainer.Key} (+{topGainer.Value.Change24h:F2}%)");
                }
                if (topLoser.Key != null)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"  📉 최고 하락: {topLoser.Key} ({topLoser.Value.Change24h:F2}%)");
                }
                Console.ResetColor();

                Console.WriteLine("\nPress 'Q' to quit...");
            };

            await _client.ConnectAsync();
            foreach (var symbol in krwSymbols)
            {
                await _client.SubscribeTicker(symbol);
                await Task.Delay(50);
            }

            await WaitForExit();
        }

        /// <summary>
        /// Real-time orderbook monitoring with Korean market characteristics
        /// </summary>
        private async Task RunOrderbookMonitor()
        {
            Console.WriteLine("\n📊 Starting Upbit Orderbook Monitor (호가창)...\n");

            var orderbookAnalyzer = new OrderbookAnalyzer();

            _client.OnOrderbookReceived += (orderbook) =>
            {
                orderbookAnalyzer.Analyze(orderbook);

                Console.Clear();
                Console.WriteLine($"UPBIT ORDERBOOK - {orderbook.symbol}");
                Console.WriteLine($"Time: {DateTime.Now:HH:mm:ss} KST");
                Console.WriteLine(new string('=', 80));

                // Display asks (매도 호가)
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("\n매도 호가 (ASKS):");
                Console.WriteLine($"{"가격 (KRW)",15} {"수량",12} {"누적",12} {"벽 강도",10}");
                Console.WriteLine(new string('-', 80));

                for (int i = Math.Min(9, orderbook.asks.Count - 1); i >= 0; i--)
                {
                    var ask = orderbook.asks[i];
                    var accumulated = orderbook.asks.Take(i + 1).Sum(a => a.quantity);
                    var wallStrength = orderbookAnalyzer.GetWallStrength(ask.quantity, true);
                    
                    Console.WriteLine($"₩{ask.price,14:N0} {ask.quantity,12:F4} {accumulated,12:F4} {wallStrength}");
                }

                // Display spread
                Console.ResetColor();
                var spread = orderbook.asks[0].price - orderbook.bids[0].price;
                var spreadPercent = (spread / orderbook.bids[0].price) * 100;
                var midPrice = (orderbook.asks[0].price + orderbook.bids[0].price) / 2;

                Console.WriteLine($"\n{"스프레드:",15} ₩{spread:N0} ({spreadPercent:F4}%)");
                Console.WriteLine($"{"중간가:",15} ₩{midPrice:N0}");

                // Display bids (매수 호가)
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("\n매수 호가 (BIDS):");
                Console.WriteLine($"{"가격 (KRW)",15} {"수량",12} {"누적",12} {"벽 강도",10}");
                Console.WriteLine(new string('-', 80));

                for (int i = 0; i < Math.Min(10, orderbook.bids.Count); i++)
                {
                    var bid = orderbook.bids[i];
                    var accumulated = orderbook.bids.Take(i + 1).Sum(b => b.quantity);
                    var wallStrength = orderbookAnalyzer.GetWallStrength(bid.quantity, false);
                    
                    Console.WriteLine($"₩{bid.price,14:N0} {bid.quantity,12:F4} {accumulated,12:F4} {wallStrength}");
                }

                Console.ResetColor();

                // Display orderbook analysis
                Console.WriteLine($"\n📊 호가창 분석:");
                Console.WriteLine($"  매수 총량: {orderbookAnalyzer.TotalBidVolume:F4}");
                Console.WriteLine($"  매도 총량: {orderbookAnalyzer.TotalAskVolume:F4}");
                Console.WriteLine($"  매수/매도 비율: {orderbookAnalyzer.BidAskRatio:F2}");
                Console.WriteLine($"  호가 불균형: {orderbookAnalyzer.GetImbalance()}");
                Console.WriteLine($"  매수 벽: {orderbookAnalyzer.BidWalls.Count}개 | 매도 벽: {orderbookAnalyzer.AskWalls.Count}개");

                // Market sentiment
                var sentiment = orderbookAnalyzer.GetMarketSentiment();
                Console.WriteLine($"\n🎯 시장 심리: {sentiment}");

                Console.WriteLine("\nPress 'Q' to quit...");
            };

            await _client.ConnectAsync();
            await _client.SubscribeOrderbook("KRW-BTC");

            await WaitForExit();
        }

        /// <summary>
        /// Volume analysis for Korean market
        /// </summary>
        private async Task RunVolumeAnalysis()
        {
            Console.WriteLine("\n📊 Starting Upbit Volume Analysis (거래량 분석)...\n");

            var volumeTracker = new VolumeTracker();
            var symbols = new[] { "KRW-BTC", "KRW-ETH", "KRW-XRP", "KRW-ADA", "KRW-SOL" };

            _client.OnTradeReceived += (trade) =>
            {
                volumeTracker.AddTrade(trade);

                Console.Clear();
                Console.WriteLine("UPBIT VOLUME ANALYSIS (거래량 분석)");
                Console.WriteLine($"Time: {DateTime.Now:HH:mm:ss} KST");
                Console.WriteLine(new string('=', 100));

                // Display volume by time period
                Console.WriteLine("\n⏰ 시간대별 거래량:");
                Console.WriteLine($"{"구분",-10} {"거래량",15} {"거래대금 (원)",20} {"평균 거래가",15} {"거래 횟수",10}");
                Console.WriteLine(new string('-', 100));

                var periods = new[] { "1분", "5분", "15분", "1시간" };
                var periodData = volumeTracker.GetPeriodData();

                foreach (var period in periodData)
                {
                    Console.WriteLine($"{period.Period,-10} {period.Volume,15:F4} ₩{period.Value,19:N0} ₩{period.AvgPrice,14:N0} {period.TradeCount,10}");
                }

                // Display buy/sell pressure
                Console.WriteLine("\n💹 매수/매도 압력:");
                var pressure = volumeTracker.GetBuySellPressure();
                DrawPressureBar(pressure);

                // Display large trades
                var largeTrades = volumeTracker.GetLargeTrades(5);
                if (largeTrades.Any())
                {
                    Console.WriteLine("\n🐋 대량 거래 (최근 5건):");
                    Console.WriteLine($"{"시간",10} {"종목",12} {"방향",6} {"수량",15} {"가격",15} {"거래대금",20}");
                    Console.WriteLine(new string('-', 100));

                    foreach (var lt in largeTrades)
                    {
                        var color = lt.sideType ==  SideType.Bid  ? ConsoleColor.Green : ConsoleColor.Red;
                        Console.ForegroundColor = color;
                        var value = lt.price * lt.quantity;
                        Console.WriteLine($"{lt.timestamp:HH:mm:ss} {trade.symbol, 12} {(lt.sideType ==  SideType.Bid ? "매수" : "매도"),6} " +
                                        $"{lt.quantity,15:F4} ₩{lt.price,14:N0} ₩{value,19:N0}");
                        Console.ResetColor();
                    }
                }

                // Display volume patterns
                Console.WriteLine("\n📈 거래 패턴 분석:");
                var patterns = volumeTracker.DetectPatterns();
                foreach (var pattern in patterns)
                {
                    Console.WriteLine($"  • {pattern}");
                }

                Console.WriteLine("\nPress 'Q' to quit...");
            };

            await _client.ConnectAsync();
            foreach (var symbol in symbols)
            {
                await _client.SubscribeTrades(symbol);
                await Task.Delay(50);
            }

            await WaitForExit();
        }

        /// <summary>
        /// Korea Premium Monitor (Kimchi Premium)
        /// </summary>
        private async Task RunPremiumMonitor()
        {
            Console.WriteLine("\n💰 Starting Upbit Premium Monitor (김치 프리미엄)...\n");

            var premiumCalculator = new PremiumCalculator();

            _client.OnTickerReceived += (ticker) =>
            {
                premiumCalculator.UpdateKoreanPrice(ticker.symbol, ticker.lastPrice);

                Console.Clear();
                Console.WriteLine("UPBIT PREMIUM MONITOR (김치 프리미엄)");
                Console.WriteLine($"Time: {DateTime.Now:HH:mm:ss} KST");
                Console.WriteLine(new string('=', 100));

                var premiums = premiumCalculator.GetPremiums();
                
                Console.WriteLine($"{"종목",-10} {"업비트 (KRW)",15} {"글로벌 (USD)",15} {"환율",10} {"프리미엄",12} {"상태",10}");
                Console.WriteLine(new string('-', 100));

                foreach (var premium in premiums.OrderByDescending(p => p.Value.PremiumPercent))
                {
                    var data = premium.Value;
                    var premiumColor = data.PremiumPercent > 3 ? ConsoleColor.Red :
                                      data.PremiumPercent > 1 ? ConsoleColor.Yellow :
                                      data.PremiumPercent < -1 ? ConsoleColor.Green :
                                      ConsoleColor.White;

                    Console.Write($"{premium.Key,-10} ");
                    Console.Write($"₩{data.KoreanPrice,14:N0} ");
                    Console.Write($"${data.GlobalPrice,14:N2} ");
                    Console.Write($"{data.ExchangeRate,10:N0} ");

                    Console.ForegroundColor = premiumColor;
                    Console.Write($"{data.PremiumPercent,11:F2}% ");
                    
                    var status = data.PremiumPercent > 3 ? "과열 🔥" :
                                data.PremiumPercent > 1 ? "상승 📈" :
                                data.PremiumPercent < -1 ? "역프 📉" :
                                "정상 ✓";
                    Console.WriteLine($"{status,10}");
                    Console.ResetColor();
                }

                // Display statistics
                var avgPremium = premiums.Values.Average(p => p.PremiumPercent);
                var maxPremium = premiums.Values.Max(p => p.PremiumPercent);
                var minPremium = premiums.Values.Min(p => p.PremiumPercent);

                Console.WriteLine($"\n📊 프리미엄 통계:");
                Console.WriteLine($"  평균 프리미엄: {avgPremium:F2}%");
                Console.WriteLine($"  최대 프리미엄: {maxPremium:F2}%");
                Console.WriteLine($"  최소 프리미엄: {minPremium:F2}%");
                Console.WriteLine($"  USD/KRW 환율: {premiumCalculator.ExchangeRate:N0}원");

                // Premium alert
                if (avgPremium > 3)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("\n⚠️ 경고: 평균 프리미엄이 3%를 초과했습니다!");
                }
                else if (avgPremium < -1)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("\n💡 알림: 역프리미엄 상태입니다!");
                }
                Console.ResetColor();

                Console.WriteLine("\nPress 'Q' to quit...");
            };

            await _client.ConnectAsync();
            var symbols = new[] { "KRW-BTC", "KRW-ETH", "KRW-XRP", "KRW-ADA", "KRW-SOL" };
            foreach (var symbol in symbols)
            {
                await _client.SubscribeTicker(symbol);
                await Task.Delay(50);
            }

            await WaitForExit();
        }

        /// <summary>
        /// Top movers monitoring
        /// </summary>
        private async Task RunTopMovers()
        {
            Console.WriteLine("\n🚀 Starting Upbit Top Movers (급등/급락)...\n");

            var allSymbols = GetAllKRWSymbols();

            _client.OnTickerReceived += (ticker) =>
            {
                if (!_marketData.ContainsKey(ticker.symbol))
                    _marketData[ticker.symbol] = new MarketData();

                _marketData[ticker.symbol].UpdateFromTicker(ticker);

                if (_marketData.Count < 20) return; // Wait for enough data

                Console.Clear();
                Console.WriteLine("UPBIT TOP MOVERS (급등/급락 종목)");
                Console.WriteLine($"Time: {DateTime.Now:HH:mm:ss} KST");
                Console.WriteLine(new string('=', 100));

                // Top Gainers
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("\n🚀 급등 TOP 10:");
                Console.WriteLine($"{"순위",5} {"종목",-12} {"현재가",15} {"변동률",10} {"거래대금",15} {"신호",20}");
                Console.WriteLine(new string('-', 100));

                var topGainers = _marketData.OrderByDescending(x => x.Value.Change24h).Take(10);
                int rank = 1;
                foreach (var gainer in topGainers)
                {
                    var signals = GetTradingSignals(gainer.Value);
                    Console.WriteLine($"{rank,5} {gainer.Key,-12} ₩{gainer.Value.LastPrice,14:N0} " +
                                    $"▲{gainer.Value.Change24h,9:F2}% {gainer.Value.Volume24hKRW / 100_000_000,14:N0}억 {signals}");
                    rank++;
                }
                Console.ResetColor();

                // Top Losers
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("\n📉 급락 TOP 10:");
                Console.WriteLine($"{"순위",5} {"종목",-12} {"현재가",15} {"변동률",10} {"거래대금",15} {"신호",20}");
                Console.WriteLine(new string('-', 100));

                var topLosers = _marketData.OrderBy(x => x.Value.Change24h).Take(10);
                rank = 1;
                foreach (var loser in topLosers)
                {
                    var signals = GetTradingSignals(loser.Value);
                    Console.WriteLine($"{rank,5} {loser.Key,-12} ₩{loser.Value.LastPrice,14:N0} " +
                                    $"▼{Math.Abs(loser.Value.Change24h),9:F2}% {loser.Value.Volume24hKRW / 100_000_000,14:N0}억 {signals}");
                    rank++;
                }
                Console.ResetColor();

                // Volume Leaders
                Console.WriteLine("\n💰 거래대금 TOP 5:");
                Console.WriteLine($"{"순위",5} {"종목",-12} {"거래대금",15} {"변동률",10} {"거래량 증가율",15}");
                Console.WriteLine(new string('-', 70));

                var volumeLeaders = _marketData.OrderByDescending(x => x.Value.Volume24hKRW).Take(5);
                rank = 1;
                foreach (var leader in volumeLeaders)
                {
                    var changeColor = leader.Value.Change24h >= 0 ? ConsoleColor.Green : ConsoleColor.Red;
                    Console.ForegroundColor = changeColor;
                    var volumeChange = leader.Value.GetVolumeChangeRate();
                    Console.WriteLine($"{rank,5} {leader.Key,-12} {leader.Value.Volume24hKRW / 100_000_000,14:N0}억 " +
                                    $"{leader.Value.Change24h,9:F2}% {volumeChange,14:F1}%");
                    rank++;
                    Console.ResetColor();
                }

                Console.WriteLine("\nPress 'Q' to quit...");
            };

            await _client.ConnectAsync();
            foreach (var symbol in allSymbols.Take(30)) // Subscribe to top 30 symbols
            {
                await _client.SubscribeTicker(symbol);
                await Task.Delay(30);
            }

            await WaitForExit();
        }

        /// <summary>
        /// Full KRW market dashboard
        /// </summary>
        private async Task RunFullDashboard()
        {
            Console.WriteLine("\n🎯 Starting Upbit Full Dashboard...\n");
            
            var dashboardData = new DashboardData();
            var updateCounter = 0;

            _client.OnTickerReceived += (ticker) =>
            {
                dashboardData.UpdateTicker(ticker);
                updateCounter++;

                if (updateCounter % 10 != 0) return; // Update display every 10 updates

                Console.Clear();
                Console.WriteLine("UPBIT FULL MARKET DASHBOARD");
                Console.WriteLine($"Time: {DateTime.Now:HH:mm:ss} KST | Updates: {updateCounter}");
                Console.WriteLine(new string('=', 120));

                // Market Overview Section
                Console.WriteLine("\n📊 시장 개요:");
                Console.WriteLine($"  총 거래대금: {dashboardData.TotalVolume / 100_000_000:N0}억원");
                Console.WriteLine($"  활성 종목: {dashboardData.ActiveSymbols}개");
                Console.WriteLine($"  상승: {dashboardData.Gainers}개 ({dashboardData.GainerPercent:F1}%)");
                Console.WriteLine($"  하락: {dashboardData.Losers}개 ({dashboardData.LoserPercent:F1}%)");

                // Top Movers Section
                Console.WriteLine("\n🚀 주요 변동:");
                var topMovers = dashboardData.GetTopMovers(3);
                foreach (var mover in topMovers)
                {
                    var color = mover.Change >= 0 ? ConsoleColor.Green : ConsoleColor.Red;
                    Console.ForegroundColor = color;
                    Console.WriteLine($"  {mover.Symbol}: {(mover.Change >= 0 ? "▲" : "▼")}{Math.Abs(mover.Change):F2}% (₩{mover.Price:N0})");
                    Console.ResetColor();
                }

                // Volume Leaders Section
                Console.WriteLine("\n💰 거래대금 상위:");
                var volumeLeaders = dashboardData.GetVolumeLeaders(3);
                foreach (var leader in volumeLeaders)
                {
                    Console.WriteLine($"  {leader.Symbol}: {leader.Volume / 100_000_000:N0}억원");
                }

                // Market Sentiment
                Console.WriteLine("\n🎯 시장 심리:");
                var sentiment = dashboardData.GetMarketSentiment();
                DrawSentimentMeter(sentiment);

                // Alerts Section
                var alerts = dashboardData.GetAlerts();
                if (alerts.Any())
                {
                    Console.WriteLine("\n⚠️ 알림:");
                    foreach (var alert in alerts)
                    {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine($"  • {alert}");
                        Console.ResetColor();
                    }
                }

                Console.WriteLine("\nPress 'Q' to quit...");
            };

            // Subscribe to orderbook for depth analysis
            _client.OnOrderbookReceived += (orderbook) =>
            {
                dashboardData.UpdateOrderbook(orderbook);
            };

            // Subscribe to trades for flow analysis
            _client.OnTradeReceived += (trade) =>
            {
                dashboardData.UpdateTrade(trade);
            };

            await _client.ConnectAsync();
            
            // Subscribe to major symbols
            var symbols = new[] { "KRW-BTC", "KRW-ETH", "KRW-XRP", "KRW-ADA", "KRW-SOL", "KRW-DOGE" };
            foreach (var symbol in symbols)
            {
                await _client.SubscribeTicker(symbol);
                await _client.SubscribeOrderbook(symbol);
                await _client.SubscribeTrades(symbol);
                await Task.Delay(50);
            }

            await WaitForExit();
        }

        // Helper methods
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

        private void DrawPressureBar(double pressure)
        {
            Console.Write("  매도 ");
            Console.ForegroundColor = ConsoleColor.Red;
            var sellBars = (int)((100 - pressure) / 5);
            Console.Write(new string('█', sellBars));
            Console.ResetColor();
            Console.Write(new string('░', 20 - sellBars));
            Console.Write(" | ");
            Console.Write(new string('░', 20 - (int)(pressure / 5)));
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write(new string('█', (int)(pressure / 5)));
            Console.ResetColor();
            Console.WriteLine($" 매수 ({pressure:F1}%)");
        }

        private void DrawSentimentMeter(MarketSentiment sentiment)
        {
            var meter = "  [";
            var position = (int)((sentiment.Score + 100) / 10); // -100 to 100 -> 0 to 20

            for (int i = 0; i < 20; i++)
            {
                if (i == position)
                    meter += "◆";
                else if (i < 7)
                    meter += "─";
                else if (i < 13)
                    meter += "═";
                else
                    meter += "─";
            }
            meter += "]";

            var color = sentiment.Score > 30 ? ConsoleColor.Green :
                       sentiment.Score < -30 ? ConsoleColor.Red :
                       ConsoleColor.Yellow;

            Console.ForegroundColor = color;
            Console.WriteLine($"{meter} {sentiment.Label} ({sentiment.Score:F0})");
            Console.ResetColor();
        }

        private string[] GetAllKRWSymbols()
        {
            // Return major KRW trading pairs
            return new[]
            {
                "KRW-BTC", "KRW-ETH", "KRW-XRP", "KRW-ADA", "KRW-SOL",
                "KRW-DOGE", "KRW-AVAX", "KRW-DOT", "KRW-MATIC", "KRW-LINK",
                "KRW-ATOM", "KRW-UNI", "KRW-ETC", "KRW-BCH", "KRW-TRX",
                "KRW-NEAR", "KRW-ICP", "KRW-APT", "KRW-IMX", "KRW-OP"
            };
        }

        private string GetTradingSignals(MarketData data)
        {
            var signals = new List<string>();

            if (data.Change24h > 10) signals.Add("🔥");
            if (data.VolumeIncreasing) signals.Add("📈");
            if (data.IsBreakout) signals.Add("💥");
            if (data.Change24h < -10) signals.Add("⚠️");

            return string.Join(" ", signals);
        }
    }

    // Supporting classes
    public class MarketData
    {
        public decimal LastPrice { get; private set; }
        public decimal Change24h { get; private set; }
        public decimal High24h { get; private set; }
        public decimal Low24h { get; private set; }
        public decimal Volume24h { get; private set; }
        public decimal Volume24hKRW { get; private set; }
        public bool VolumeIncreasing { get; private set; }
        public bool IsBreakout { get; private set; }
        private Queue<decimal> _volumeHistory = new Queue<decimal>(10);

        public void UpdateFromTicker(STickerItem ticker)
        {
            LastPrice = ticker.lastPrice;
            Change24h = ticker.changePercent;
            High24h = ticker.highPrice;
            Low24h = ticker.lowPrice;
            Volume24h = ticker.volume;
            Volume24hKRW = ticker.volume * ticker.lastPrice;

            _volumeHistory.Enqueue(Volume24h);
            if (_volumeHistory.Count > 10) _volumeHistory.Dequeue();

            VolumeIncreasing = _volumeHistory.Count > 5 && 
                              _volumeHistory.TakeLast(3).Average() > _volumeHistory.Take(3).Average() * 1.5m;

            IsBreakout = Math.Abs(Change24h) > 15 && VolumeIncreasing;
        }

        public decimal GetVolumeChangeRate()
        {
            if (_volumeHistory.Count < 2) return 0;
            var recent = _volumeHistory.Last();
            var previous = _volumeHistory.First();
            return previous > 0 ? ((recent - previous) / previous) * 100 : 0;
        }
    }

    public class OrderbookAnalyzer
    {
        public decimal TotalBidVolume { get; private set; }
        public decimal TotalAskVolume { get; private set; }
        public decimal BidAskRatio { get; private set; }
        public List<decimal> BidWalls { get; private set; } = new List<decimal>();
        public List<decimal> AskWalls { get; private set; } = new List<decimal>();

        public void Analyze(SOrderBookItem orderbook)
        {
            TotalBidVolume = orderbook.bids.Sum(b => b.quantity);
            TotalAskVolume = orderbook.asks.Sum(a => a.quantity);
            BidAskRatio = TotalAskVolume > 0 ? TotalBidVolume / TotalAskVolume : 1;

            // Detect walls (orders > 3x average)
            var avgBidSize = TotalBidVolume / Math.Max(orderbook.bids.Count, 1);
            var avgAskSize = TotalAskVolume / Math.Max(orderbook.asks.Count, 1);

            BidWalls = orderbook.bids.Where(b => b.quantity > avgBidSize * 3).Select(b => b.price).ToList();
            AskWalls = orderbook.asks.Where(a => a.quantity > avgAskSize * 3).Select(a => a.price).ToList();
        }

        public string GetWallStrength(decimal quantity, bool isAsk)
        {
            var avgSize = isAsk ? TotalAskVolume / 10 : TotalBidVolume / 10;
            var ratio = quantity / avgSize;

            if (ratio > 5) return "██████ 초대형";
            if (ratio > 3) return "████ 대형";
            if (ratio > 2) return "██ 중형";
            return "";
        }

        public string GetImbalance()
        {
            if (BidAskRatio > 1.5m) return "강한 매수세 🟢🟢";
            if (BidAskRatio > 1.2m) return "매수 우세 🟢";
            if (BidAskRatio < 0.67m) return "강한 매도세 🔴🔴";
            if (BidAskRatio < 0.83m) return "매도 우세 🔴";
            return "균형 ⚪";
        }

        public string GetMarketSentiment()
        {
            if (BidWalls.Count > AskWalls.Count * 2) return "매수 지지 강함 💪";
            if (AskWalls.Count > BidWalls.Count * 2) return "매도 압력 강함 📉";
            if (BidAskRatio > 1.5m) return "적극 매수 🚀";
            if (BidAskRatio < 0.67m) return "적극 매도 ⚠️";
            return "관망세 👀";
        }
    }

    public class VolumeTracker
    {
        private List<SCompleteOrderItem> _trades = new List<SCompleteOrderItem>();
        private Dictionary<string, List<SCompleteOrderItem>> _tradesBySymbol = new Dictionary<string, List<SCompleteOrderItem>>();

        public void AddTrade(SCompleteOrders trade)
        {
            _trades.AddRange(trade.result);

            if (_trades.Count > 10000) _trades.RemoveAt(0);

            if (!_tradesBySymbol.ContainsKey(trade.symbol))
                _tradesBySymbol[trade.symbol] = new List<SCompleteOrderItem>();

            _tradesBySymbol[trade.symbol].AddRange(trade.result);

            if (_tradesBySymbol[trade.symbol].Count > 1000)
                _tradesBySymbol[trade.symbol].RemoveAt(0);
        }

        public List<PeriodVolumeData> GetPeriodData()
        {
            var now = DateTime.Now;
            var periods = new List<PeriodVolumeData>();

            // 1 minute
            var oneMin = _trades.Where(t => (now - t.timestamp).TotalMinutes <= 1).ToList();
            periods.Add(CreatePeriodData("1분", oneMin));

            // 5 minutes
            var fiveMin = _trades.Where(t => (now - t.timestamp).TotalMinutes <= 5).ToList();
            periods.Add(CreatePeriodData("5분", fiveMin));

            // 15 minutes
            var fifteenMin = _trades.Where(t => (now - t.timestamp).TotalMinutes <= 15).ToList();
            periods.Add(CreatePeriodData("15분", fifteenMin));

            // 1 hour
            var oneHour = _trades.Where(t => (now - t.timestamp).TotalHours <= 1).ToList();
            periods.Add(CreatePeriodData("1시간", oneHour));

            return periods;
        }

        private PeriodVolumeData CreatePeriodData(string period, List<SCompleteOrderItem> trades)
        {
            return new PeriodVolumeData
            {
                Period = period,
                Volume = trades.Sum(t => t.quantity),
                Value = trades.Sum(t => t.price * t.quantity),
                AvgPrice = trades.Any() ? trades.Average(t => t.price) : 0,
                TradeCount = trades.Count
            };
        }

        public double GetBuySellPressure()
        {
            if (!_trades.Any()) return 50;

            var buyVolume = _trades.Where(t => t.sideType == "B").Sum(t => t.quantity);
            var sellVolume = _trades.Where(t => t.sideType == "S").Sum(t => t.quantity);
            var total = buyVolume + sellVolume;

            return total > 0 ? (double)(buyVolume / total * 100) : 50;
        }

        public List<SCompleteOrderItem> GetLargeTrades(int count)
        {
            var threshold = _trades.Any() ? _trades.Average(t => t.quantity) * 10 : 0;
            return _trades.Where(t => t.quantity > threshold)
                         .OrderByDescending(t => t.timestamp)
                         .Take(count)
                         .ToList();
        }

        public List<string> DetectPatterns()
        {
            var patterns = new List<string>();

            // Volume spike detection
            if (_trades.Count > 100)
            {
                var recentVolume = _trades.TakeLast(20).Sum(t => t.quantity);
                var avgVolume = _trades.Take(80).Sum(t => t.quantity) / 4;

                if (recentVolume > avgVolume * 3)
                    patterns.Add("📊 거래량 급증 감지 (평균 대비 3배↑)");
            }

            // Buy/Sell dominance
            var pressure = GetBuySellPressure();
            if (pressure > 70)
                patterns.Add("🟢 강한 매수세 지속 중");
            else if (pressure < 30)
                patterns.Add("🔴 강한 매도세 지속 중");

            // Large trade concentration
            var largeTrades = GetLargeTrades(10);
            if (largeTrades.Count >= 5)
                patterns.Add($"🐋 대량 거래 집중 ({largeTrades.Count}건)");

            if (!patterns.Any())
                patterns.Add("정상적인 거래 패턴");

            return patterns;
        }
    }

    public class PeriodVolumeData
    {
        public string Period { get; set; }
        public decimal Volume { get; set; }
        public decimal Value { get; set; }
        public decimal AvgPrice { get; set; }
        public int TradeCount { get; set; }
    }

    public class PremiumCalculator
    {
        private Dictionary<string, PremiumData> _premiums = new Dictionary<string, PremiumData>();
        public decimal ExchangeRate { get; set; } = 1350; // Default USD/KRW rate

        public void UpdateKoreanPrice(string symbol, decimal price)
        {
            var baseSymbol = symbol.Replace("KRW-", "");
            if (!_premiums.ContainsKey(baseSymbol))
                _premiums[baseSymbol] = new PremiumData();

            _premiums[baseSymbol].KoreanPrice = price;
            _premiums[baseSymbol].ExchangeRate = ExchangeRate;
            CalculatePremium(baseSymbol);
        }

        public void UpdateGlobalPrice(string symbol, decimal price)
        {
            if (!_premiums.ContainsKey(symbol))
                _premiums[symbol] = new PremiumData();

            _premiums[symbol].GlobalPrice = price;
            CalculatePremium(symbol);
        }

        private void CalculatePremium(string symbol)
        {
            if (!_premiums.ContainsKey(symbol)) return;

            var data = _premiums[symbol];
            if (data.KoreanPrice > 0 && data.GlobalPrice > 0)
            {
                var globalPriceInKRW = data.GlobalPrice * ExchangeRate;
                data.PremiumPercent = ((data.KoreanPrice - globalPriceInKRW) / globalPriceInKRW) * 100;
            }
        }

        public Dictionary<string, PremiumData> GetPremiums()
        {
            // Simulate global prices for demo
            SimulateGlobalPrices();
            return _premiums;
        }

        private void SimulateGlobalPrices()
        {
            // Simulated global prices for demo purposes
            var globalPrices = new Dictionary<string, decimal>
            {
                { "BTC", 70000 },
                { "ETH", 3500 },
                { "XRP", 0.65m },
                { "ADA", 0.45m },
                { "SOL", 150 }
            };

            foreach (var price in globalPrices)
            {
                UpdateGlobalPrice(price.Key, price.Value);
            }
        }
    }

    public class PremiumData
    {
        public decimal KoreanPrice { get; set; }
        public decimal GlobalPrice { get; set; }
        public decimal PremiumPercent { get; set; }
        public decimal ExchangeRate { get; set; }
    }

    public class DashboardData
    {
        private Dictionary<string, MarketData> _marketData = new Dictionary<string, MarketData>();
        private List<SCompleteOrderItem> _recentTrades = new List<SCompleteOrderItem>();
        private SOrderBookItem _lastOrderbook;

        public decimal TotalVolume => _marketData.Values.Sum(m => m.Volume24hKRW);
        public int ActiveSymbols => _marketData.Count;
        public int Gainers => _marketData.Values.Count(m => m.Change24h > 0);
        public int Losers => _marketData.Values.Count(m => m.Change24h < 0);
        public decimal GainerPercent => ActiveSymbols > 0 ? (Gainers * 100m / ActiveSymbols) : 0;
        public decimal LoserPercent => ActiveSymbols > 0 ? (Losers * 100m / ActiveSymbols) : 0;

        public void UpdateTicker(STickerItem ticker)
        {
            if (!_marketData.ContainsKey(ticker.symbol))
                _marketData[ticker.symbol] = new MarketData();

            _marketData[ticker.symbol].UpdateFromTicker(ticker);
        }

        public void UpdateOrderbook(SOrderBookItem orderbook)
        {
            _lastOrderbook = orderbook;
        }

        public void UpdateTrade(SCompleteOrderItem trade)
        {
            _recentTrades.Add(trade);
            if (_recentTrades.Count > 1000) _recentTrades.RemoveAt(0);
        }

        public List<(string Symbol, decimal Price, decimal Change)> GetTopMovers(int count)
        {
            return _marketData.OrderByDescending(m => Math.Abs(m.Value.Change24h))
                             .Take(count)
                             .Select(m => (m.Key, m.Value.LastPrice, m.Value.Change24h))
                             .ToList();
        }

        public List<(string Symbol, decimal Volume)> GetVolumeLeaders(int count)
        {
            return _marketData.OrderByDescending(m => m.Value.Volume24hKRW)
                             .Take(count)
                             .Select(m => (m.Key, m.Value.Volume24hKRW))
                             .ToList();
        }

        public MarketSentiment GetMarketSentiment()
        {
            var avgChange = _marketData.Values.Any() ? _marketData.Values.Average(m => m.Change24h) : 0;
            var volumeTrend = CalculateVolumeTrend();

            var score = (double)avgChange * 10 + volumeTrend;
            var label = score > 30 ? "매우 강세 🚀" :
                       score > 10 ? "강세 📈" :
                       score < -30 ? "매우 약세 📉" :
                       score < -10 ? "약세 🔻" :
                       "중립 ➡️";

            return new MarketSentiment { Score = score, Label = label };
        }

        private double CalculateVolumeTrend()
        {
            if (_recentTrades.Count < 100) return 0;

            var recentBuyVolume = _recentTrades.TakeLast(50).Count(t => t.sideType == "B");
            var previousBuyVolume = _recentTrades.Take(50).Count(t => t.sideType == "B");

            return (recentBuyVolume - previousBuyVolume) * 2;
        }

        public List<string> GetAlerts()
        {
            var alerts = new List<string>();

            // Volume spike alert
            var highVolumeSymbols = _marketData.Where(m => m.Value.VolumeIncreasing).Select(m => m.Key);
            if (highVolumeSymbols.Any())
                alerts.Add($"거래량 급증: {string.Join(", ", highVolumeSymbols.Take(3))}");

            // Extreme moves alert
            var extremeMovers = _marketData.Where(m => Math.Abs(m.Value.Change24h) > 20);
            if (extremeMovers.Any())
                alerts.Add($"급등/급락 주의: {string.Join(", ", extremeMovers.Select(m => m.Key).Take(3))}");

            // Market imbalance alert
            if (GainerPercent > 80)
                alerts.Add("시장 과열 경고 - 상승 종목 80% 초과");
            else if (LoserPercent > 80)
                alerts.Add("시장 침체 경고 - 하락 종목 80% 초과");

            return alerts;
        }
    }

    public class MarketSentiment
    {
        public double Score { get; set; }
        public string Label { get; set; }
    }
}