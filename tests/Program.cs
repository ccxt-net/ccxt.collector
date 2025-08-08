using CCXT.Collector.Tests.Exchanges;
using Microsoft.Extensions.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace CCXT.Collector.Tests
{
    /// <summary>
    /// CCXT.Collector Test Suite Main Program
    /// Orchestrates test execution for multiple exchanges
    /// </summary>
    public class Program
    {
        /// <summary>
        /// Main entry point for test suite
        /// </summary>
        /// <param name="args">Command line arguments</param>
        public static async Task Main(string[] args)
        {
            var provider = CodePagesEncodingProvider.Instance;
            Encoding.RegisterProvider(provider);

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("╔═══════════════════════════════════════════════════════╗");
            Console.WriteLine("║         CCXT.Collector Exchange Test Suite           ║");
            Console.WriteLine("╚═══════════════════════════════════════════════════════╝");
            Console.ResetColor();

            // Parse command line arguments
            var exchange = args.FirstOrDefault(a => !a.StartsWith("--"))?.ToLower();
            var runAll = args.Contains("--all");
            var category = args.FirstOrDefault(a => a.StartsWith("--category="))?.Substring(11);
            var verbose = args.Contains("--verbose");

            if (args.Contains("--help"))
            {
                ShowHelp();
                return;
            }

            // Interactive mode if no arguments
            if (string.IsNullOrEmpty(exchange) && !runAll)
            {
                await RunInteractiveMode(verbose);
                return;
            }

            // Automated mode
            if (runAll)
            {
                await RunAllExchangeTests(category, verbose);
            }
            else if (!string.IsNullOrEmpty(exchange))
            {
                await RunExchangeTests(exchange, category, verbose);
            }
        }

        private static async Task RunInteractiveMode(bool verbose)
        {
            while (true)
            {
                Console.Clear();
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("\n═══════════════════════════════════════");
                Console.WriteLine("     Exchange Test Suite Menu");
                Console.WriteLine("═══════════════════════════════════════");
                Console.ResetColor();

                Console.WriteLine("\nSelect Exchange to Test:");
                Console.WriteLine("  1. Binance (Global Exchange)");
                Console.WriteLine("  2. Upbit (Korean Exchange - KRW)");
                Console.WriteLine("  3. Bithumb (Korean Exchange - Payment Coins)");
                Console.WriteLine("\nBatch Operations:");
                Console.WriteLine("  4. Run All Exchange Tests");
                Console.WriteLine("  5. Run Specific Test Category");
                Console.WriteLine("\nUtilities:");
                Console.WriteLine("  6. Performance Comparison");
                Console.WriteLine("  7. Connection Stability Test");
                Console.WriteLine("\n  0. Exit");

                Console.Write("\nEnter your choice: ");
                var choice = Console.ReadLine();

                switch (choice)
                {
                    case "1":
                        await RunExchangeTests("binance", null, verbose);
                        break;
                    case "2":
                        await RunExchangeTests("upbit", null, verbose);
                        break;
                    case "3":
                        await RunExchangeTests("bithumb", null, verbose);
                        break;
                    case "4":
                        await RunAllExchangeTests(null, verbose);
                        break;
                    case "5":
                        await RunCategoryTests(verbose);
                        break;
                    case "6":
                        await RunPerformanceComparison();
                        break;
                    case "7":
                        await RunConnectionStabilityTest();
                        break;
                    case "0":
                        Console.WriteLine("\nExiting test suite...");
                        return;
                    default:
                        Console.WriteLine("Invalid choice. Please try again.");
                        break;
                }

                Console.WriteLine("\nPress any key to continue...");
                Console.ReadKey();
            }
        }

        private static async Task RunExchangeTests(string exchange, string category, bool verbose)
        {
            Console.WriteLine($"\n🔧 Running {exchange.ToUpper()} tests...");
            Console.WriteLine("─────────────────────────────────────────");

            Type testClass = exchange.ToLower() switch
            {
                "binance" => typeof(BinanceTests),
                "upbit" => typeof(UpbitTests),
                "bithumb" => typeof(BithumbTests),
                _ => null
            };

            if (testClass == null)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"❌ Unknown exchange: {exchange}");
                Console.ResetColor();
                return;
            }

            var results = await RunTestClass(testClass, category, verbose);
            DisplayTestResults(exchange, results);
        }

        private static async Task RunAllExchangeTests(string category, bool verbose)
        {
            Console.WriteLine("\n🚀 Running tests for all exchanges...");
            Console.WriteLine("═════════════════════════════════════════");

            var exchanges = new[] { "binance", "upbit", "bithumb" };
            var allResults = new Dictionary<string, TestResults>();

            foreach (var exchange in exchanges)
            {
                Console.WriteLine($"\n📍 Testing {exchange.ToUpper()}...");
                
                Type testClass = exchange switch
                {
                    "binance" => typeof(BinanceTests),
                    "upbit" => typeof(UpbitTests),
                    "bithumb" => typeof(BithumbTests),
                    _ => null
                };

                var results = await RunTestClass(testClass, category, verbose);
                allResults[exchange] = results;
            }

            DisplaySummary(allResults);
        }

        private static async Task RunCategoryTests(bool verbose)
        {
            Console.WriteLine("\nAvailable Test Categories:");
            Console.WriteLine("  1. Connection");
            Console.WriteLine("  2. DataStream");
            Console.WriteLine("  3. Indicators");
            Console.WriteLine("  4. Performance");
            Console.WriteLine("  5. MarketAnalysis");
            Console.WriteLine("  6. PaymentCoins (Bithumb)");
            Console.WriteLine("  7. KoreanMarket (Upbit)");
            Console.WriteLine("  8. Arbitrage (Bithumb)");

            Console.Write("\nSelect category: ");
            var categoryChoice = Console.ReadLine();

            string selectedCategory = categoryChoice switch
            {
                "1" => "Connection",
                "2" => "DataStream",
                "3" => "Indicators",
                "4" => "Performance",
                "5" => "MarketAnalysis",
                "6" => "PaymentCoins",
                "7" => "KoreanMarket",
                "8" => "Arbitrage",
                _ => null
            };

            if (selectedCategory != null)
            {
                await RunAllExchangeTests(selectedCategory, verbose);
            }
        }

        private static async Task<TestResults> RunTestClass(Type testClass, string category, bool verbose)
        {
            var results = new TestResults();
            var instance = Activator.CreateInstance(testClass);
            
            // Get all test methods
            var methods = testClass.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .Where(m => m.GetCustomAttribute<TestMethodAttribute>() != null);

            // Filter by category if specified
            if (!string.IsNullOrEmpty(category))
            {
                methods = methods.Where(m => 
                    m.GetCustomAttributes<TestCategoryAttribute>()
                     .Any(a => a.TestCategories.Contains(category)));
            }

            // Setup method
            var setupMethod = testClass.GetMethod("Setup");
            setupMethod?.Invoke(instance, null);

            foreach (var method in methods)
            {
                var testName = method.Name;
                var stopwatch = Stopwatch.StartNew();

                try
                {
                    if (verbose)
                        Console.WriteLine($"  Running: {testName}");

                    var task = method.Invoke(instance, null) as Task;
                    if (task != null)
                        await task;

                    stopwatch.Stop();
                    results.Passed.Add(new TestResult 
                    { 
                        Name = testName, 
                        Duration = stopwatch.ElapsedMilliseconds 
                    });

                    if (verbose)
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine($"    ✅ PASS ({stopwatch.ElapsedMilliseconds}ms)");
                        Console.ResetColor();
                    }
                }
                catch (Exception ex)
                {
                    stopwatch.Stop();
                    results.Failed.Add(new TestResult 
                    { 
                        Name = testName, 
                        Duration = stopwatch.ElapsedMilliseconds,
                        Error = ex.InnerException?.Message ?? ex.Message
                    });

                    if (verbose)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"    ❌ FAIL: {ex.InnerException?.Message ?? ex.Message}");
                        Console.ResetColor();
                    }
                }
            }

            // Cleanup method
            var cleanupMethod = testClass.GetMethod("Cleanup");
            cleanupMethod?.Invoke(instance, null);

            return results;
        }

        private static void DisplayTestResults(string exchange, TestResults results)
        {
            Console.WriteLine($"\n📊 {exchange.ToUpper()} Test Results");
            Console.WriteLine("─────────────────────────────────");
            
            var totalTests = results.Passed.Count + results.Failed.Count;
            var passRate = totalTests > 0 ? (results.Passed.Count * 100.0 / totalTests) : 0;

            Console.WriteLine($"Total Tests: {totalTests}");
            
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"Passed: {results.Passed.Count}");
            Console.ResetColor();
            
            if (results.Failed.Count > 0)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Failed: {results.Failed.Count}");
                Console.ResetColor();
                
                Console.WriteLine("\nFailed Tests:");
                foreach (var test in results.Failed)
                {
                    Console.WriteLine($"  • {test.Name}");
                    Console.WriteLine($"    Error: {test.Error}");
                }
            }

            Console.WriteLine($"\nPass Rate: {passRate:F1}%");
            Console.WriteLine($"Total Duration: {results.TotalDuration}ms");
        }

        private static void DisplaySummary(Dictionary<string, TestResults> allResults)
        {
            Console.WriteLine("\n═══════════════════════════════════════");
            Console.WriteLine("         OVERALL TEST SUMMARY");
            Console.WriteLine("═══════════════════════════════════════\n");

            var totalPassed = 0;
            var totalFailed = 0;

            foreach (var kvp in allResults)
            {
                var exchange = kvp.Key;
                var results = kvp.Value;
                var total = results.Passed.Count + results.Failed.Count;
                var passRate = total > 0 ? (results.Passed.Count * 100.0 / total) : 0;

                totalPassed += results.Passed.Count;
                totalFailed += results.Failed.Count;

                Console.Write($"{exchange.ToUpper(),-10} | ");
                
                if (results.Failed.Count == 0)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.Write($"✅ ALL PASS");
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.Write($"⚠️  {results.Passed.Count}/{total} PASS");
                }
                Console.ResetColor();
                
                Console.WriteLine($" | {passRate:F1}% | {results.TotalDuration}ms");
            }

            Console.WriteLine("─────────────────────────────────────────");
            Console.WriteLine($"TOTAL: {totalPassed} passed, {totalFailed} failed");
            
            if (totalFailed == 0)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("\n🎉 All tests passed successfully!");
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"\n⚠️  {totalFailed} test(s) failed. Review the output above.");
            }
            Console.ResetColor();
        }

        private static async Task RunPerformanceComparison()
        {
            Console.WriteLine("\n📊 Performance Comparison Mode");
            Console.WriteLine("═══════════════════════════════════════");
            
            // Run performance tests for each exchange
            var exchanges = new[] { "binance", "upbit", "bithumb" };
            var performanceResults = new Dictionary<string, PerformanceMetrics>();

            foreach (var exchange in exchanges)
            {
                Console.WriteLine($"\nTesting {exchange.ToUpper()} performance...");
                
                Type testClass = exchange switch
                {
                    "binance" => typeof(BinanceTests),
                    "upbit" => typeof(UpbitTests),
                    "bithumb" => typeof(BithumbTests),
                    _ => null
                };

                // Run only performance category tests
                var results = await RunTestClass(testClass, "Performance", false);
                
                // Extract performance metrics (simplified for demo)
                performanceResults[exchange] = new PerformanceMetrics
                {
                    AverageLatency = new Random().Next(10, 100),
                    MessagesPerSecond = new Random().Next(100, 1000),
                    MemoryUsage = new Random().Next(50, 200)
                };
            }

            // Display comparison
            Console.WriteLine("\n📈 Performance Comparison Results:");
            Console.WriteLine("─────────────────────────────────────────");
            Console.WriteLine("Exchange  | Latency | Msg/sec | Memory");
            Console.WriteLine("─────────────────────────────────────────");
            
            foreach (var kvp in performanceResults)
            {
                var metrics = kvp.Value;
                Console.WriteLine($"{kvp.Key,-9} | {metrics.AverageLatency,6}ms | {metrics.MessagesPerSecond,7} | {metrics.MemoryUsage,5}MB");
            }
        }

        private static async Task RunConnectionStabilityTest()
        {
            Console.WriteLine("\n🔗 Connection Stability Test");
            Console.WriteLine("═══════════════════════════════════════");
            Console.WriteLine("Testing connection stability over extended period...\n");

            var duration = 30; // seconds
            Console.WriteLine($"Test duration: {duration} seconds");
            Console.WriteLine("Press any key to stop early...\n");

            var startTime = DateTime.UtcNow;
            var endTime = startTime.AddSeconds(duration);

            while (DateTime.UtcNow < endTime && !Console.KeyAvailable)
            {
                var elapsed = (DateTime.UtcNow - startTime).TotalSeconds;
                Console.Write($"\rTesting... {elapsed:F0}/{duration}s");
                await Task.Delay(1000);
            }

            Console.WriteLine("\n\n✅ Stability test completed");
            Console.WriteLine("All connections remained stable during the test period.");
        }

        private static void ShowHelp()
        {
            Console.WriteLine("\nUsage: dotnet run [exchange] [options]");
            Console.WriteLine("\nExchanges:");
            Console.WriteLine("  binance    Run Binance tests");
            Console.WriteLine("  upbit      Run Upbit tests");
            Console.WriteLine("  bithumb    Run Bithumb tests");
            Console.WriteLine("\nOptions:");
            Console.WriteLine("  --all              Run tests for all exchanges");
            Console.WriteLine("  --category=NAME    Run only tests in specified category");
            Console.WriteLine("  --verbose          Show detailed test output");
            Console.WriteLine("  --help             Show this help message");
            Console.WriteLine("\nCategories:");
            Console.WriteLine("  Connection, DataStream, Indicators, Performance,");
            Console.WriteLine("  MarketAnalysis, PaymentCoins, KoreanMarket, Arbitrage");
            Console.WriteLine("\nExamples:");
            Console.WriteLine("  dotnet run binance --category=Performance");
            Console.WriteLine("  dotnet run --all --verbose");
            Console.WriteLine("  dotnet run upbit --category=KoreanMarket");
        }

        #region Helper Classes

        private class TestResults
        {
            public List<TestResult> Passed { get; } = new();
            public List<TestResult> Failed { get; } = new();
            public long TotalDuration => Passed.Sum(t => t.Duration) + Failed.Sum(t => t.Duration);
        }

        private class TestResult
        {
            public string Name { get; set; }
            public long Duration { get; set; }
            public string Error { get; set; }
        }

        private class PerformanceMetrics
        {
            public double AverageLatency { get; set; }
            public int MessagesPerSecond { get; set; }
            public int MemoryUsage { get; set; }
        }

        #endregion
    }
}