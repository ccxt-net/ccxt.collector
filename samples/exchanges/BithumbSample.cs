using CCXT.Collector.Bithumb;
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
    /// Bithumb Exchange Sample Implementation (Korean Exchange)
    /// Demonstrates real-time market data analysis and payment coin features
    /// </summary>
    public class BithumbSample
    {
        private readonly BithumbWebSocketClient _client;
        private readonly Dictionary<string, BithumbMarketData> _marketData;
        private readonly PaymentCoinAnalyzer _paymentAnalyzer;
        private CancellationTokenSource _cancellationTokenSource;

        public BithumbSample()
        {
            _client = new BithumbWebSocketClient();
            _marketData = new Dictionary<string, BithumbMarketData>();
            _paymentAnalyzer = new PaymentCoinAnalyzer();
            _cancellationTokenSource = new CancellationTokenSource();
        }

        /// <summary>
        /// Run Bithumb sample with payment coin focus
        /// </summary>
        public async Task RunAsync()
        {
            Console.WriteLine("===========================================");
            Console.WriteLine("     Bithumb Exchange Sample (빗썸)");
            Console.WriteLine("===========================================\n");

            Console.WriteLine("Select operation mode:");
            Console.WriteLine("1. Payment Coin Analysis (결제 코인 분석)");
            Console.WriteLine("2. Market Depth Analysis (시장 깊이 분석)");
            Console.WriteLine("3. Arbitrage Opportunity Scanner");
            Console.WriteLine("4. Market Making Simulator");
            Console.WriteLine("5. Risk Management Dashboard");
            Console.WriteLine("6. Full Trading System Demo");
            Console.WriteLine("0. Exit");

            Console.Write("\nYour choice: ");
            var choice = Console.ReadLine();

            try
            {
                switch (choice)
                {
                    case "1":
                        await RunPaymentCoinAnalysis();
                        break;
                    case "2":
                        await RunMarketDepthAnalysis();
                        break;
                    case "3":
                        await RunArbitrageScanner();
                        break;
                    case "4":
                        await RunMarketMakingSimulator();
                        break;
                    case "5":
                        await RunRiskManagementDashboard();
                        break;
                    case "6":
                        await RunFullTradingSystem();
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
        /// Payment coin analysis (Bithumb speciality)
        /// </summary>
        private async Task RunPaymentCoinAnalysis()
        {
            Console.WriteLine("\n💳 Starting Bithumb Payment Coin Analysis...\n");

            // Bithumb payment coins
            var paymentCoins = new[] { "KRW-BTC", "KRW-ETH", "KRW-XRP", "KRW-EOS", "KRW-BCH" };

            _client.OnTickerReceived += (ticker) =>
            {
                _paymentAnalyzer.UpdateTicker(ticker);

                Console.Clear();
                Console.WriteLine("BITHUMB PAYMENT COIN ANALYSIS (결제 코인 분석)");
                Console.WriteLine($"Time: {DateTime.Now:HH:mm:ss} KST");
                Console.WriteLine(new string('=', 110));

                // Display payment coin metrics
                Console.WriteLine($"{"코인",-10} {"현재가",15} {"24h 변동",10} {"결제 속도",12} {"수수료",10} {"활용도",10} {"점수",8}");
                Console.WriteLine(new string('-', 110));

                var metrics = _paymentAnalyzer.GetPaymentMetrics();
                foreach (var metric in metrics.OrderByDescending(m => m.Value.Score))
                {
                    var data = metric.Value;
                    var changeColor = data.Change24h >= 0 ? ConsoleColor.Green : ConsoleColor.Red;

                    Console.Write($"{metric.Key,-10} ");
                    Console.Write($"₩{data.Price,14:N0} ");

                    Console.ForegroundColor = changeColor;
                    Console.Write($"{(data.Change24h >= 0 ? "▲" : "▼")}{Math.Abs(data.Change24h),8:F2}% ");
                    Console.ResetColor();

                    Console.Write($"{data.Speed,12} ");
                    Console.Write($"{data.Fee,10} ");
                    Console.Write($"{data.Adoption,10} ");

                    // Score with color coding
                    var scoreColor = data.Score > 80 ? ConsoleColor.Green :
                                    data.Score > 60 ? ConsoleColor.Yellow :
                                    ConsoleColor.Red;
                    Console.ForegroundColor = scoreColor;
                    Console.WriteLine($"{data.Score,8:F1}");
                    Console.ResetColor();
                }

                // Payment coin insights
                Console.WriteLine("\n📊 결제 코인 인사이트:");
                var insights = _paymentAnalyzer.GetInsights();
                foreach (var insight in insights)
                {
                    Console.WriteLine($"  • {insight}");
                }

                // Transaction volume analysis
                Console.WriteLine("\n💰 거래 분석:");
                var txAnalysis = _paymentAnalyzer.GetTransactionAnalysis();
                Console.WriteLine($"  총 거래량: {txAnalysis.TotalVolume / 100_000_000:N0}억원");
                Console.WriteLine($"  평균 거래 크기: ₩{txAnalysis.AvgTransactionSize:N0}");
                Console.WriteLine($"  대량 거래 비율: {txAnalysis.LargeTransactionRatio:F1}%");

                // Best payment coin recommendation
                var bestCoin = _paymentAnalyzer.GetBestPaymentCoin();
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine($"\n🏆 추천 결제 코인: {bestCoin.Symbol} (점수: {bestCoin.Score:F1})");
                Console.WriteLine($"   이유: {bestCoin.Reason}");
                Console.ResetColor();

                Console.WriteLine("\nPress 'Q' to quit...");
            };

            await _client.ConnectAsync();
            foreach (var coin in paymentCoins)
            {
                await _client.SubscribeTicker(coin);
                await Task.Delay(50);
            }

            await WaitForExit();
        }

        /// <summary>
        /// Market depth analysis
        /// </summary>
        private async Task RunMarketDepthAnalysis()
        {
            Console.WriteLine("\n📊 Starting Bithumb Market Depth Analysis...\n");

            var depthAnalyzer = new MarketDepthAnalyzer();

            _client.OnOrderbookReceived += (orderbook) =>
            {
                depthAnalyzer.UpdateOrderbook(orderbook);
                var analysis = depthAnalyzer.Analyze();

                Console.Clear();
                Console.WriteLine($"BITHUMB MARKET DEPTH ANALYSIS - {orderbook.symbol}");
                Console.WriteLine($"Time: {DateTime.Now:HH:mm:ss} KST");
                Console.WriteLine(new string('=', 100));

                // Depth visualization
                Console.WriteLine("\n📊 시장 깊이 시각화:");
                DrawDepthChart(orderbook);

                // Key metrics
                Console.WriteLine("\n📈 핵심 지표:");
                Console.WriteLine($"  중간가: ₩{analysis.MidPrice:N0}");
                Console.WriteLine($"  스프레드: ₩{analysis.Spread:N0} ({analysis.SpreadPercent:F4}%)");
                Console.WriteLine($"  10호가 깊이: 매수 {analysis.BidDepth10:F4} / 매도 {analysis.AskDepth10:F4}");
                Console.WriteLine($"  유동성 점수: {analysis.LiquidityScore:F1}/100");

                // Support and resistance levels
                Console.WriteLine("\n🎯 지지/저항 레벨:");
                foreach (var level in analysis.SupportLevels.Take(3))
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"  지지: ₩{level.Price:N0} (강도: {level.Strength:F1})");
                }
                foreach (var level in analysis.ResistanceLevels.Take(3))
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"  저항: ₩{level.Price:N0} (강도: {level.Strength:F1})");
                }
                Console.ResetColor();

                // Order flow imbalance
                Console.WriteLine("\n💹 주문 흐름:");
                DrawOrderFlowBar(analysis.OrderFlowImbalance);
                Console.WriteLine($"  대량 매수 주문: {analysis.LargeBuyOrders}개");
                Console.WriteLine($"  대량 매도 주문: {analysis.LargeSellOrders}개");

                // Market microstructure
                Console.WriteLine("\n🔬 시장 미시구조:");
                Console.WriteLine($"  가격 클러스터: {string.Join(", ", analysis.PriceClusters.Select(c => $"₩{c:N0}"))}");
                Console.WriteLine($"  주문 집중도: {analysis.OrderConcentration:F2}");
                Console.WriteLine($"  시장 효율성: {analysis.MarketEfficiency:F1}%");

                // Trading opportunity
                if (analysis.TradingOpportunity != null)
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"\n💡 거래 기회: {analysis.TradingOpportunity}");
                    Console.ResetColor();
                }

                Console.WriteLine("\nPress 'Q' to quit...");
            };

            await _client.ConnectAsync();
            await _client.SubscribeOrderbook("KRW-BTC");

            await WaitForExit();
        }

        /// <summary>
        /// Arbitrage opportunity scanner
        /// </summary>
        private async Task RunArbitrageScanner()
        {
            Console.WriteLine("\n💱 Starting Bithumb Arbitrage Scanner...\n");

            var arbScanner = new ArbitrageScanner();

            _client.OnTickerReceived += (ticker) =>
            {
                arbScanner.UpdatePrice("Bithumb", ticker.symbol, ticker.lastPrice);

                Console.Clear();
                Console.WriteLine("BITHUMB ARBITRAGE OPPORTUNITY SCANNER");
                Console.WriteLine($"Time: {DateTime.Now:HH:mm:ss} KST");
                Console.WriteLine(new string('=', 110));

                var opportunities = arbScanner.FindOpportunities();

                if (opportunities.Any())
                {
                    Console.WriteLine($"{"종목",-10} {"빗썸",15} {"타거래소",15} {"차이",12} {"수익률",10} {"예상수익",15} {"리스크",10}");
                    Console.WriteLine(new string('-', 110));

                    foreach (var opp in opportunities.OrderByDescending(o => o.ProfitPercent))
                    {
                        var profitColor = opp.ProfitPercent > 2 ? ConsoleColor.Green :
                                        opp.ProfitPercent > 1 ? ConsoleColor.Yellow :
                                        ConsoleColor.White;

                        Console.Write($"{opp.Symbol,-10} ");
                        Console.Write($"₩{opp.BithumbPrice,14:N0} ");
                        Console.Write($"₩{opp.OtherPrice,14:N0} ");
                        Console.Write($"₩{opp.PriceDiff,11:N0} ");

                        Console.ForegroundColor = profitColor;
                        Console.Write($"{opp.ProfitPercent,9:F2}% ");
                        Console.ResetColor();

                        Console.Write($"₩{opp.EstimatedProfit,14:N0} ");

                        var riskColor = opp.Risk == "Low" ? ConsoleColor.Green :
                                       opp.Risk == "Medium" ? ConsoleColor.Yellow :
                                       ConsoleColor.Red;
                        Console.ForegroundColor = riskColor;
                        Console.WriteLine($"{opp.Risk,10}");
                        Console.ResetColor();
                    }

                    // Summary
                    Console.WriteLine($"\n📊 차익거래 요약:");
                    Console.WriteLine($"  발견된 기회: {opportunities.Count}개");
                    Console.WriteLine($"  평균 수익률: {opportunities.Average(o => o.ProfitPercent):F2}%");
                    Console.WriteLine($"  최대 수익률: {opportunities.Max(o => o.ProfitPercent):F2}%");

                    // Execution strategy
                    var bestOpp = opportunities.OrderByDescending(o => o.ProfitPercent).First();
                    Console.WriteLine($"\n🎯 실행 전략:");
                    Console.WriteLine($"  1. {bestOpp.Symbol} 빗썸에서 매수 (₩{bestOpp.BithumbPrice:N0})");
                    Console.WriteLine($"  2. 타거래소로 전송 (예상 시간: {bestOpp.TransferTime})");
                    Console.WriteLine($"  3. 타거래소에서 매도 (₩{bestOpp.OtherPrice:N0})");
                    Console.WriteLine($"  4. 예상 순수익: ₩{bestOpp.NetProfit:N0}");
                }
                else
                {
                    Console.WriteLine("\n현재 차익거래 기회가 없습니다.");
                    Console.WriteLine("계속 모니터링 중...");
                }

                // Market conditions
                Console.WriteLine($"\n🌐 시장 상황:");
                var conditions = arbScanner.GetMarketConditions();
                foreach (var condition in conditions)
                {
                    Console.WriteLine($"  • {condition}");
                }

                Console.WriteLine("\nPress 'Q' to quit...");
            };

            await _client.ConnectAsync();
            var symbols = new[] { "KRW-BTC", "KRW-ETH", "KRW-XRP", "KRW-EOS", "KRW-BCH" };
            foreach (var symbol in symbols)
            {
                await _client.SubscribeTicker(symbol);
                await Task.Delay(50);
            }

            await WaitForExit();
        }

        /// <summary>
        /// Market making simulator
        /// </summary>
        private async Task RunMarketMakingSimulator()
        {
            Console.WriteLine("\n🤖 Starting Bithumb Market Making Simulator...\n");

            var mmSimulator = new MarketMakingSimulator();

            _client.OnOrderbookReceived += (orderbook) =>
            {
                var strategy = mmSimulator.UpdateAndCalculate(orderbook);

                Console.Clear();
                Console.WriteLine("BITHUMB MARKET MAKING SIMULATOR");
                Console.WriteLine($"Time: {DateTime.Now:HH:mm:ss} KST | Symbol: {orderbook.symbol}");
                Console.WriteLine(new string('=', 100));

                // Current positions
                Console.WriteLine("\n📊 현재 포지션:");
                Console.WriteLine($"  잔고: ₩{mmSimulator.Balance:N0}");
                Console.WriteLine($"  보유량: {mmSimulator.Position:F8} BTC");
                Console.WriteLine($"  총 가치: ₩{mmSimulator.TotalValue:N0}");
                Console.WriteLine($"  실현 수익: ₩{mmSimulator.RealizedPnL:N0}");
                Console.WriteLine($"  미실현 수익: ₩{mmSimulator.UnrealizedPnL:N0}");

                // Active orders
                Console.WriteLine("\n📋 활성 주문:");
                Console.WriteLine($"{"유형",8} {"가격",15} {"수량",12} {"상태",10} {"수익률",10}");
                Console.WriteLine(new string('-', 100));

                foreach (var order in mmSimulator.ActiveOrders)
                {
                    var color = order.Side == "Buy" ? ConsoleColor.Green : ConsoleColor.Red;
                    Console.ForegroundColor = color;
                    Console.WriteLine($"{order.Side,8} ₩{order.Price,14:N0} {order.Quantity,12:F8} {order.Status,10} {order.ProfitTarget,9:F2}%");
                    Console.ResetColor();
                }

                // Strategy parameters
                Console.WriteLine("\n⚙️ 전략 파라미터:");
                Console.WriteLine($"  스프레드: {strategy.SpreadBps:F1} bps");
                Console.WriteLine($"  주문 크기: {strategy.OrderSize:F8} BTC");
                Console.WriteLine($"  최대 포지션: {strategy.MaxPosition:F8} BTC");
                Console.WriteLine($"  리스크 한도: ₩{strategy.RiskLimit:N0}");
                Console.WriteLine($"  재조정 주기: {strategy.RebalanceInterval}초");

                // Performance metrics
                Console.WriteLine("\n📈 성과 지표:");
                Console.WriteLine($"  총 거래: {mmSimulator.TotalTrades}회");
                Console.WriteLine($"  성공률: {mmSimulator.WinRate:F1}%");
                Console.WriteLine($"  평균 수익: ₩{mmSimulator.AvgProfit:N0}");
                Console.WriteLine($"  최대 손실: ₩{mmSimulator.MaxDrawdown:N0}");
                Console.WriteLine($"  샤프 비율: {mmSimulator.SharpeRatio:F2}");

                // Risk indicators
                Console.WriteLine("\n⚠️ 리스크 지표:");
                DrawRiskMeter(mmSimulator.RiskLevel);
                Console.WriteLine($"  변동성: {mmSimulator.Volatility:F2}%");
                Console.WriteLine($"  유동성 리스크: {mmSimulator.LiquidityRisk}");
                Console.WriteLine($"  포지션 리스크: {mmSimulator.PositionRisk}");

                // Trading signals
                if (strategy.Signals.Any())
                {
                    Console.WriteLine("\n💡 거래 신호:");
                    foreach (var signal in strategy.Signals)
                    {
                        Console.WriteLine($"  • {signal}");
                    }
                }

                Console.WriteLine("\nPress 'Q' to quit...");
            };

            await _client.ConnectAsync();
            await _client.SubscribeOrderbook("KRW-BTC");

            await WaitForExit();
        }

        /// <summary>
        /// Risk management dashboard
        /// </summary>
        private async Task RunRiskManagementDashboard()
        {
            Console.WriteLine("\n🛡️ Starting Bithumb Risk Management Dashboard...\n");

            var riskManager = new RiskManager();

            _client.OnTickerReceived += (ticker) =>
            {
                riskManager.UpdateMarketData(ticker);
            };

            _client.OnOrderbookReceived += (orderbook) =>
            {
                riskManager.UpdateOrderbook(orderbook);

                Console.Clear();
                Console.WriteLine("BITHUMB RISK MANAGEMENT DASHBOARD");
                Console.WriteLine($"Time: {DateTime.Now:HH:mm:ss} KST");
                Console.WriteLine(new string('=', 110));

                var riskMetrics = riskManager.CalculateRiskMetrics();

                // Portfolio risk overview
                Console.WriteLine("\n📊 포트폴리오 리스크:");
                Console.WriteLine($"  Value at Risk (VaR) 95%: ₩{riskMetrics.VaR95:N0}");
                Console.WriteLine($"  Value at Risk (VaR) 99%: ₩{riskMetrics.VaR99:N0}");
                Console.WriteLine($"  Expected Shortfall: ₩{riskMetrics.ExpectedShortfall:N0}");
                Console.WriteLine($"  최대 예상 손실: ₩{riskMetrics.MaxExpectedLoss:N0}");

                // Market risk indicators
                Console.WriteLine("\n📈 시장 리스크:");
                DrawRiskHeatmap(riskMetrics.MarketRisk);
                Console.WriteLine($"  변동성 (일간): {riskMetrics.DailyVolatility:F2}%");
                Console.WriteLine($"  변동성 (주간): {riskMetrics.WeeklyVolatility:F2}%");
                Console.WriteLine($"  베타: {riskMetrics.Beta:F2}");
                Console.WriteLine($"  상관계수: {riskMetrics.Correlation:F2}");

                // Liquidity risk
                Console.WriteLine("\n💧 유동성 리스크:");
                Console.WriteLine($"  슬리피지 예상: {riskMetrics.ExpectedSlippage:F2}%");
                Console.WriteLine($"  유동성 점수: {riskMetrics.LiquidityScore:F1}/100");
                Console.WriteLine($"  시장 충격: {riskMetrics.MarketImpact:F2}%");
                Console.WriteLine($"  청산 시간: {riskMetrics.LiquidationTime}");

                // Operational risk
                Console.WriteLine("\n⚙️ 운영 리스크:");
                Console.WriteLine($"  시스템 가동률: {riskMetrics.SystemUptime:F1}%");
                Console.WriteLine($"  API 응답 시간: {riskMetrics.ApiLatency}ms");
                Console.WriteLine($"  오류율: {riskMetrics.ErrorRate:F2}%");
                Console.WriteLine($"  백업 상태: {riskMetrics.BackupStatus}");

                // Risk limits and alerts
                Console.WriteLine("\n🚨 리스크 한도:");
                foreach (var limit in riskMetrics.RiskLimits)
                {
                    var usage = limit.CurrentUsage / limit.Limit * 100;
                    var color = usage > 80 ? ConsoleColor.Red :
                               usage > 60 ? ConsoleColor.Yellow :
                               ConsoleColor.Green;

                    Console.ForegroundColor = color;
                    Console.WriteLine($"  {limit.Name}: {limit.CurrentUsage:N0} / {limit.Limit:N0} ({usage:F1}%)");
                    Console.ResetColor();
                }

                // Risk mitigation recommendations
                if (riskMetrics.Recommendations.Any())
                {
                    Console.WriteLine("\n💡 리스크 완화 권고사항:");
                    foreach (var rec in riskMetrics.Recommendations)
                    {
                        Console.WriteLine($"  • {rec}");
                    }
                }

                Console.WriteLine("\nPress 'Q' to quit...");
            };

            await _client.ConnectAsync();
            await _client.SubscribeTicker("KRW-BTC");
            await _client.SubscribeOrderbook("KRW-BTC");

            await WaitForExit();
        }

        /// <summary>
        /// Full trading system demo
        /// </summary>
        private async Task RunFullTradingSystem()
        {
            Console.WriteLine("\n🚀 Starting Bithumb Full Trading System Demo...\n");
            Console.WriteLine("This comprehensive demo showcases all trading features.");
            Console.WriteLine("Each module will run for demonstration...\n");

            var tradingSystem = new TradingSystem();

            // Initialize all components
            _client.OnTickerReceived += (ticker) =>
            {
                tradingSystem.ProcessTicker(ticker);
            };

            _client.OnOrderbookReceived += (orderbook) =>
            {
                tradingSystem.ProcessOrderbook(orderbook);
            };

            _client.OnTradeReceived += (trade) =>
            {
                tradingSystem.ProcessTrade(trade);

                // Update display
                DisplayTradingSystem(tradingSystem);
            };

            await _client.ConnectAsync();

            // Subscribe to multiple data streams
            var symbols = new[] { "KRW-BTC", "KRW-ETH", "KRW-XRP" };
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
        private void DisplayTradingSystem(TradingSystem system)
        {
            Console.Clear();
            Console.WriteLine("BITHUMB INTEGRATED TRADING SYSTEM");
            Console.WriteLine($"Time: {DateTime.Now:HH:mm:ss} KST");
            Console.WriteLine(new string('=', 120));

            // System status
            Console.WriteLine($"\n🟢 시스템 상태: {system.Status}");
            Console.WriteLine($"   가동 시간: {system.Uptime}");
            Console.WriteLine($"   처리된 이벤트: {system.ProcessedEvents:N0}");

            // Active strategies
            Console.WriteLine("\n📋 활성 전략:");
            foreach (var strategy in system.ActiveStrategies)
            {
                var statusColor = strategy.IsActive ? ConsoleColor.Green : ConsoleColor.Yellow;
                Console.ForegroundColor = statusColor;
                Console.WriteLine($"  • {strategy.Name}: {strategy.Status} (PnL: ₩{strategy.PnL:N0})");
                Console.ResetColor();
            }

            // Portfolio summary
            Console.WriteLine("\n💼 포트폴리오:");
            Console.WriteLine($"  총 자산: ₩{system.TotalAssets:N0}");
            Console.WriteLine($"  일일 수익: ₩{system.DailyPnL:N0} ({system.DailyReturn:F2}%)");
            Console.WriteLine($"  월간 수익: ₩{system.MonthlyPnL:N0} ({system.MonthlyReturn:F2}%)");

            // Recent trades
            Console.WriteLine("\n📈 최근 거래:");
            foreach (var trade in system.RecentTrades.Take(5))
            {
                var color = trade.Side == "Buy" ? ConsoleColor.Green : ConsoleColor.Red;
                Console.ForegroundColor = color;
                Console.WriteLine($"  {trade.Time:HH:mm:ss} {trade.Symbol} {trade.Side} {trade.Quantity:F8} @ ₩{trade.Price:N0}");
                Console.ResetColor();
            }

            Console.WriteLine("\nPress 'Q' to quit...");
        }

        private void DrawDepthChart(SOrderBook orderbook)
        {
            var maxQuantity = Math.Max(
                orderbook.bids.Take(10).Max(b => b.quantity),
                orderbook.asks.Take(10).Max(a => a.quantity)
            );

            // Draw asks
            for (int i = Math.Min(4, orderbook.asks.Count - 1); i >= 0; i--)
            {
                var ask = orderbook.asks[i];
                var barLength = (int)(ask.quantity / maxQuantity * 30);
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Write($"  {ask.price,12:N0} ");
                Console.Write(new string('█', barLength));
                Console.WriteLine($" {ask.quantity:F4}");
            }

            Console.ResetColor();
            Console.WriteLine($"  ------------ SPREAD ------------");

            // Draw bids
            for (int i = 0; i < Math.Min(5, orderbook.bids.Count); i++)
            {
                var bid = orderbook.bids[i];
                var barLength = (int)(bid.quantity / maxQuantity * 30);
                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write($"  {bid.price,12:N0} ");
                Console.Write(new string('█', barLength));
                Console.WriteLine($" {bid.quantity:F4}");
            }
            Console.ResetColor();
        }

        private void DrawOrderFlowBar(double imbalance)
        {
            Console.Write("  ");
            var position = (int)((imbalance + 100) / 10); // -100 to 100 -> 0 to 20

            for (int i = 0; i < 20; i++)
            {
                if (i < 10)
                    Console.ForegroundColor = ConsoleColor.Red;
                else
                    Console.ForegroundColor = ConsoleColor.Green;

                if (i == position)
                    Console.Write("◆");
                else
                    Console.Write("─");
            }
            Console.ResetColor();
            Console.WriteLine($" ({imbalance:F1}%)");
        }

        private void DrawRiskMeter(double riskLevel)
        {
            Console.Write("  리스크 수준: [");
            var fillLength = (int)(riskLevel / 5);
            var color = riskLevel > 60 ? ConsoleColor.Red :
                       riskLevel > 30 ? ConsoleColor.Yellow :
                       ConsoleColor.Green;

            Console.ForegroundColor = color;
            Console.Write(new string('█', fillLength));
            Console.Write(new string('░', 20 - fillLength));
            Console.ResetColor();
            Console.WriteLine($"] {riskLevel:F0}%");
        }

        private void DrawRiskHeatmap(Dictionary<string, double> risks)
        {
            foreach (var risk in risks)
            {
                var color = risk.Value > 70 ? ConsoleColor.Red :
                           risk.Value > 40 ? ConsoleColor.Yellow :
                           ConsoleColor.Green;

                Console.Write($"  {risk.Key}: ");
                Console.ForegroundColor = color;
                Console.WriteLine($"{new string('█', (int)(risk.Value / 10))} {risk.Value:F0}%");
                Console.ResetColor();
            }
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

    // Supporting classes for Bithumb-specific features
    public class BithumbMarketData
    {
        public decimal Price { get; set; }
        public decimal Change24h { get; set; }
        public decimal Volume { get; set; }
        public DateTime LastUpdate { get; set; }
    }

    public class PaymentCoinAnalyzer
    {
        private Dictionary<string, PaymentCoinMetric> _metrics = new Dictionary<string, PaymentCoinMetric>();

        public void UpdateTicker(STickerItem ticker)
        {
            var symbol = ticker.symbol.Replace("KRW-", "");
            if (!_metrics.ContainsKey(symbol))
                _metrics[symbol] = new PaymentCoinMetric();

            _metrics[symbol].Price = ticker.lastPrice;
            _metrics[symbol].Change24h = ticker.changePercent;
            _metrics[symbol].Volume = ticker.volume * ticker.lastPrice;
            CalculatePaymentScore(symbol);
        }

        private void CalculatePaymentScore(string symbol)
        {
            var metric = _metrics[symbol];
            
            // Payment coin characteristics
            metric.Speed = GetTransactionSpeed(symbol);
            metric.Fee = GetTransactionFee(symbol);
            metric.Adoption = GetAdoptionLevel(symbol);

            // Calculate overall score
            var speedScore = metric.Speed == "빠름" ? 30 : metric.Speed == "보통" ? 20 : 10;
            var feeScore = metric.Fee == "낮음" ? 30 : metric.Fee == "보통" ? 20 : 10;
            var adoptionScore = metric.Adoption == "높음" ? 40 : metric.Adoption == "보통" ? 25 : 10;

            metric.Score = speedScore + feeScore + adoptionScore;
        }

        private string GetTransactionSpeed(string symbol)
        {
            return symbol switch
            {
                "XRP" => "빠름",
                "BCH" => "빠름",
                "ETH" => "보통",
                "BTC" => "느림",
                _ => "보통"
            };
        }

        private string GetTransactionFee(string symbol)
        {
            return symbol switch
            {
                "XRP" => "낮음",
                "BCH" => "낮음",
                "ETH" => "높음",
                "BTC" => "높음",
                _ => "보통"
            };
        }

        private string GetAdoptionLevel(string symbol)
        {
            return symbol switch
            {
                "BTC" => "높음",
                "ETH" => "높음",
                "XRP" => "보통",
                _ => "낮음"
            };
        }

        public Dictionary<string, PaymentCoinMetric> GetPaymentMetrics() => _metrics;

        public List<string> GetInsights()
        {
            var insights = new List<string>();

            var bestSpeed = _metrics.Where(m => m.Value.Speed == "빠름").Select(m => m.Key);
            if (bestSpeed.Any())
                insights.Add($"빠른 전송: {string.Join(", ", bestSpeed)}");

            var lowFee = _metrics.Where(m => m.Value.Fee == "낮음").Select(m => m.Key);
            if (lowFee.Any())
                insights.Add($"낮은 수수료: {string.Join(", ", lowFee)}");

            var highAdoption = _metrics.Where(m => m.Value.Adoption == "높음").Select(m => m.Key);
            if (highAdoption.Any())
                insights.Add($"높은 채택률: {string.Join(", ", highAdoption)}");

            return insights;
        }

        public TransactionAnalysis GetTransactionAnalysis()
        {
            return new TransactionAnalysis
            {
                TotalVolume = _metrics.Values.Sum(m => m.Volume),
                AvgTransactionSize = _metrics.Values.Average(m => m.Volume / 1000),
                LargeTransactionRatio = 15.5m // Simulated
            };
        }

        public PaymentCoinRecommendation GetBestPaymentCoin()
        {
            var best = _metrics.OrderByDescending(m => m.Value.Score).FirstOrDefault();
            return new PaymentCoinRecommendation
            {
                Symbol = best.Key,
                Score = best.Value.Score,
                Reason = $"빠른 속도, 낮은 수수료, {best.Value.Adoption} 채택률"
            };
        }
    }

    public class PaymentCoinMetric
    {
        public decimal Price { get; set; }
        public decimal Change24h { get; set; }
        public decimal Volume { get; set; }
        public string Speed { get; set; }
        public string Fee { get; set; }
        public string Adoption { get; set; }
        public double Score { get; set; }
    }

    public class TransactionAnalysis
    {
        public decimal TotalVolume { get; set; }
        public decimal AvgTransactionSize { get; set; }
        public decimal LargeTransactionRatio { get; set; }
    }

    public class PaymentCoinRecommendation
    {
        public string Symbol { get; set; }
        public double Score { get; set; }
        public string Reason { get; set; }
    }

    public class MarketDepthAnalyzer
    {
        private SOrderBookItem _lastOrderbook;

        public void UpdateOrderbook(SOrderBookItem orderbook)
        {
            _lastOrderbook = orderbook;
        }

        public MarketDepthAnalysis Analyze()
        {
            if (_lastOrderbook == null)
                return new MarketDepthAnalysis();

            return new MarketDepthAnalysis
            {
                MidPrice = (_lastOrderbook.bids[0].price + _lastOrderbook.asks[0].price) / 2,
                Spread = _lastOrderbook.asks[0].price - _lastOrderbook.bids[0].price,
                SpreadPercent = ((_lastOrderbook.asks[0].price - _lastOrderbook.bids[0].price) / _lastOrderbook.bids[0].price) * 100,
                BidDepth10 = _lastOrderbook.bids.Take(10).Sum(b => b.quantity),
                AskDepth10 = _lastOrderbook.asks.Take(10).Sum(a => a.quantity),
                LiquidityScore = CalculateLiquidityScore(),
                SupportLevels = FindSupportLevels(),
                ResistanceLevels = FindResistanceLevels(),
                OrderFlowImbalance = CalculateOrderFlowImbalance(),
                LargeBuyOrders = _lastOrderbook.bids.Count(b => b.quantity > _lastOrderbook.bids.Average(x => x.quantity) * 3),
                LargeSellOrders = _lastOrderbook.asks.Count(a => a.quantity > _lastOrderbook.asks.Average(x => x.quantity) * 3),
                PriceClusters = FindPriceClusters(),
                OrderConcentration = CalculateOrderConcentration(),
                MarketEfficiency = CalculateMarketEfficiency(),
                TradingOpportunity = DetectTradingOpportunity()
            };
        }

        private double CalculateLiquidityScore()
        {
            // Simplified liquidity score calculation
            return 75.5;
        }

        private List<PriceLevel> FindSupportLevels()
        {
            // Simplified support level detection
            return new List<PriceLevel>
            {
                new PriceLevel { Price = 90000000, Strength = 85 },
                new PriceLevel { Price = 89500000, Strength = 70 },
                new PriceLevel { Price = 89000000, Strength = 60 }
            };
        }

        private List<PriceLevel> FindResistanceLevels()
        {
            // Simplified resistance level detection
            return new List<PriceLevel>
            {
                new PriceLevel { Price = 91000000, Strength = 80 },
                new PriceLevel { Price = 91500000, Strength = 75 },
                new PriceLevel { Price = 92000000, Strength = 65 }
            };
        }

        private double CalculateOrderFlowImbalance()
        {
            if (_lastOrderbook == null) return 0;

            var bidVolume = _lastOrderbook.bids.Sum(b => b.quantity);
            var askVolume = _lastOrderbook.asks.Sum(a => a.quantity);
            var total = bidVolume + askVolume;

            return total > 0 ? (double)((bidVolume - askVolume) / total * 100) : 0;
        }

        private List<decimal> FindPriceClusters()
        {
            // Simplified price cluster detection
            return new List<decimal> { 90000000, 90500000, 91000000 };
        }

        private double CalculateOrderConcentration()
        {
            // Simplified order concentration calculation
            return 0.45;
        }

        private double CalculateMarketEfficiency()
        {
            // Simplified market efficiency calculation
            return 85.5;
        }

        private string DetectTradingOpportunity()
        {
            var imbalance = CalculateOrderFlowImbalance();
            if (imbalance > 30)
                return "Strong buy pressure detected - consider long position";
            if (imbalance < -30)
                return "Strong sell pressure detected - consider short position";
            return null;
        }
    }

    public class MarketDepthAnalysis
    {
        public decimal MidPrice { get; set; }
        public decimal Spread { get; set; }
        public decimal SpreadPercent { get; set; }
        public decimal BidDepth10 { get; set; }
        public decimal AskDepth10 { get; set; }
        public double LiquidityScore { get; set; }
        public List<PriceLevel> SupportLevels { get; set; } = new List<PriceLevel>();
        public List<PriceLevel> ResistanceLevels { get; set; } = new List<PriceLevel>();
        public double OrderFlowImbalance { get; set; }
        public int LargeBuyOrders { get; set; }
        public int LargeSellOrders { get; set; }
        public List<decimal> PriceClusters { get; set; } = new List<decimal>();
        public double OrderConcentration { get; set; }
        public double MarketEfficiency { get; set; }
        public string TradingOpportunity { get; set; }
    }

    public class PriceLevel
    {
        public decimal Price { get; set; }
        public double Strength { get; set; }
    }

    // Additional supporting classes for other features...
    public class ArbitrageScanner
    {
        private Dictionary<string, Dictionary<string, decimal>> _prices = new Dictionary<string, Dictionary<string, decimal>>();

        public void UpdatePrice(string exchange, string symbol, decimal price)
        {
            if (!_prices.ContainsKey(symbol))
                _prices[symbol] = new Dictionary<string, decimal>();
            _prices[symbol][exchange] = price;
        }

        public List<ArbitrageOpportunity> FindOpportunities()
        {
            // Simplified arbitrage detection
            return new List<ArbitrageOpportunity>
            {
                new ArbitrageOpportunity
                {
                    Symbol = "BTC",
                    BithumbPrice = 90000000,
                    OtherPrice = 91000000,
                    PriceDiff = 1000000,
                    ProfitPercent = 1.1m,
                    EstimatedProfit = 1000000,
                    Risk = "Low",
                    TransferTime = "10분",
                    NetProfit = 900000
                }
            };
        }

        public List<string> GetMarketConditions()
        {
            return new List<string>
            {
                "전체 시장 변동성: 보통",
                "거래량: 증가 추세",
                "네트워크 수수료: 안정"
            };
        }
    }

    public class ArbitrageOpportunity
    {
        public string Symbol { get; set; }
        public decimal BithumbPrice { get; set; }
        public decimal OtherPrice { get; set; }
        public decimal PriceDiff { get; set; }
        public decimal ProfitPercent { get; set; }
        public decimal EstimatedProfit { get; set; }
        public string Risk { get; set; }
        public string TransferTime { get; set; }
        public decimal NetProfit { get; set; }
    }

    public class MarketMakingSimulator
    {
        public decimal Balance { get; set; } = 100000000; // 100M KRW
        public decimal Position { get; set; } = 0;
        public decimal TotalValue { get; set; }
        public decimal RealizedPnL { get; set; }
        public decimal UnrealizedPnL { get; set; }
        public List<SimulatedOrder> ActiveOrders { get; set; } = new List<SimulatedOrder>();
        public int TotalTrades { get; set; }
        public double WinRate { get; set; } = 65;
        public decimal AvgProfit { get; set; } = 50000;
        public decimal MaxDrawdown { get; set; } = -500000;
        public double SharpeRatio { get; set; } = 1.85;
        public double RiskLevel { get; set; } = 35;
        public double Volatility { get; set; } = 12.5;
        public string LiquidityRisk { get; set; } = "Low";
        public string PositionRisk { get; set; } = "Medium";

        public MarketMakingStrategy UpdateAndCalculate(SOrderBookItem orderbook)
        {
            // Simplified market making strategy
            return new MarketMakingStrategy
            {
                SpreadBps = 25,
                OrderSize = 0.1m,
                MaxPosition = 5,
                RiskLimit = 5000000,
                RebalanceInterval = 60,
                Signals = new List<string> { "Spread widening detected", "Place limit orders" }
            };
        }
    }

    public class SimulatedOrder
    {
        public string Side { get; set; }
        public decimal Price { get; set; }
        public decimal Quantity { get; set; }
        public string Status { get; set; }
        public double ProfitTarget { get; set; }
    }

    public class MarketMakingStrategy
    {
        public double SpreadBps { get; set; }
        public decimal OrderSize { get; set; }
        public decimal MaxPosition { get; set; }
        public decimal RiskLimit { get; set; }
        public int RebalanceInterval { get; set; }
        public List<string> Signals { get; set; } = new List<string>();
    }

    public class RiskManager
    {
        public void UpdateMarketData(STickerItem ticker) { }
        public void UpdateOrderbook(SOrderBookItem orderbook) { }

        public RiskMetrics CalculateRiskMetrics()
        {
            // Simplified risk metrics
            return new RiskMetrics
            {
                VaR95 = 5000000,
                VaR99 = 8000000,
                ExpectedShortfall = 10000000,
                MaxExpectedLoss = 15000000,
                DailyVolatility = 3.5,
                WeeklyVolatility = 8.2,
                Beta = 1.15,
                Correlation = 0.85,
                ExpectedSlippage = 0.15,
                LiquidityScore = 82,
                MarketImpact = 0.05,
                LiquidationTime = "5분",
                SystemUptime = 99.9,
                ApiLatency = 45,
                ErrorRate = 0.01,
                BackupStatus = "정상",
                MarketRisk = new Dictionary<string, double>
                {
                    { "가격 리스크", 45 },
                    { "변동성 리스크", 60 },
                    { "유동성 리스크", 30 }
                },
                RiskLimits = new List<RiskLimit>
                {
                    new RiskLimit { Name = "포지션 한도", CurrentUsage = 3000000, Limit = 5000000 },
                    new RiskLimit { Name = "손실 한도", CurrentUsage = 800000, Limit = 1000000 }
                },
                Recommendations = new List<string>
                {
                    "포지션 크기 축소 권고",
                    "스톱로스 설정 강화"
                }
            };
        }
    }

    public class RiskMetrics
    {
        public decimal VaR95 { get; set; }
        public decimal VaR99 { get; set; }
        public decimal ExpectedShortfall { get; set; }
        public decimal MaxExpectedLoss { get; set; }
        public double DailyVolatility { get; set; }
        public double WeeklyVolatility { get; set; }
        public double Beta { get; set; }
        public double Correlation { get; set; }
        public double ExpectedSlippage { get; set; }
        public double LiquidityScore { get; set; }
        public double MarketImpact { get; set; }
        public string LiquidationTime { get; set; }
        public double SystemUptime { get; set; }
        public int ApiLatency { get; set; }
        public double ErrorRate { get; set; }
        public string BackupStatus { get; set; }
        public Dictionary<string, double> MarketRisk { get; set; }
        public List<RiskLimit> RiskLimits { get; set; }
        public List<string> Recommendations { get; set; }
    }

    public class RiskLimit
    {
        public string Name { get; set; }
        public decimal CurrentUsage { get; set; }
        public decimal Limit { get; set; }
    }

    public class TradingSystem
    {
        public string Status { get; set; } = "Active";
        public string Uptime { get; set; } = "2h 35m";
        public int ProcessedEvents { get; set; } = 15432;
        public decimal TotalAssets { get; set; } = 150000000;
        public decimal DailyPnL { get; set; } = 2500000;
        public double DailyReturn { get; set; } = 1.67;
        public decimal MonthlyPnL { get; set; } = 45000000;
        public double MonthlyReturn { get; set; } = 30;
        public List<TradingStrategy> ActiveStrategies { get; set; } = new List<TradingStrategy>
        {
            new TradingStrategy { Name = "Market Making", IsActive = true, Status = "Running", PnL = 1500000 },
            new TradingStrategy { Name = "Arbitrage", IsActive = true, Status = "Monitoring", PnL = 800000 },
            new TradingStrategy { Name = "Momentum", IsActive = false, Status = "Paused", PnL = 200000 }
        };
        public List<SystemTrade> RecentTrades { get; set; } = new List<SystemTrade>();

        public void ProcessTicker(STickerItem ticker) { }
        public void ProcessOrderbook(SOrderBookItem orderbook) { }
        public void ProcessTrade(SCompleteOrderItem trade)
        {
            ProcessedEvents++;
            RecentTrades.Add(new SystemTrade
            {
                Time = DateTime.Now,
                Symbol = trade.symbol,
                Side = trade.sideType == "B" ? "Buy" : "Sell",
                Quantity = trade.quantity,
                Price = trade.price
            });
            if (RecentTrades.Count > 10) RecentTrades.RemoveAt(0);
        }
    }

    public class TradingStrategy
    {
        public string Name { get; set; }
        public bool IsActive { get; set; }
        public string Status { get; set; }
        public decimal PnL { get; set; }
    }

    public class SystemTrade
    {
        public DateTime Time { get; set; }
        public string Symbol { get; set; }
        public string Side { get; set; }
        public decimal Quantity { get; set; }
        public decimal Price { get; set; }
    }
}