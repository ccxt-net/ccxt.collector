using System;
using System.Threading.Tasks;
using CCXT.Collector.Samples.Exchanges;

namespace CCXT.Collector.Samples
{
    /// <summary>
    /// Sample runner for all 15 completed exchanges
    /// </summary>
    public class AllExchangesSample
    {
        public static async Task RunMenu()
        {
            while (true)
            {
                Console.Clear();
                Console.WriteLine("==============================================");
                Console.WriteLine("   CCXT.Collector - All Exchanges Sample");
                Console.WriteLine("==============================================");
                Console.WriteLine("\n📊 Select Exchange to Test:\n");
                
                // Global Exchanges
                Console.WriteLine("🌍 Global Exchanges:");
                Console.WriteLine("  1. Binance      - World's largest exchange");
                Console.WriteLine("  2. OKX          - Leading derivatives platform");
                Console.WriteLine("  3. Gate.io      - Diverse altcoin selection");
                Console.WriteLine("  4. KuCoin       - The People's Exchange");
                Console.WriteLine("  5. Huobi        - Major Asian exchange");
                Console.WriteLine("  6. Bybit        - Derivatives specialist");
                Console.WriteLine("  7. Bitget       - Copy trading leader");
                
                // US Exchanges
                Console.WriteLine("\n🇺🇸 US Exchanges:");
                Console.WriteLine("  8. Coinbase     - US regulated exchange");
                Console.WriteLine("  9. Crypto.com   - All-in-one platform");
                Console.WriteLine(" 10. Bittrex      - US exchange (SignalR)");
                
                // Korean Exchanges
                Console.WriteLine("\n🇰🇷 Korean Exchanges:");
                Console.WriteLine(" 11. Upbit        - Korea's largest");
                Console.WriteLine(" 12. Bithumb      - High volume KRW");
                Console.WriteLine(" 13. Coinone      - Korean pioneer");
                Console.WriteLine(" 14. Korbit       - Korea's first");
                
                // Special Options
                Console.WriteLine("\n⚡ Special Tests:");
                Console.WriteLine(" 15. Multi-Exchange - Test 3 exchanges simultaneously");
                Console.WriteLine(" 16. All Korean     - Test all KRW markets");
                Console.WriteLine(" 17. Top 5 Global   - Test top 5 by volume");
                
                Console.WriteLine("\n  0. Back to Main Menu");
                Console.WriteLine("\n==============================================");
                Console.Write("\nEnter your choice (0-17): ");
                
                // Clear any buffered input before reading
                while (Console.KeyAvailable)
                {
                    Console.ReadKey(true);
                }
                
                var choice = Console.ReadLine();
                
                try
                {
                    switch (choice)
                    {
                        case "1":
                            await BinanceSample.RunSample();
                            break;
                        case "2":
                            await OkxExample.RunSample();
                            break;
                        case "3":
                            await GateioExample.RunSample();
                            break;
                        case "4":
                            await KucoinExample.RunSample();
                            break;
                        case "5":
                            await HuobiExample.RunSample();
                            break;
                        case "6":
                            await BybitExample.RunSample();
                            break;
                        case "7":
                            await BitgetExample.RunSample();
                            break;
                        case "8":
                            await CoinbaseExample.RunSample();
                            break;
                        case "9":
                            await CryptocomExample.RunSample();
                            break;
                        case "10":
                            await BittrexExample.RunSample();
                            break;
                        case "11":
                            await UpbitSample.RunSample();
                            break;
                        case "12":
                            await BithumbSample.RunSample();
                            break;
                        case "13":
                            await CoinoneExample.RunSample();
                            break;
                        case "14":
                            await KorbitExample.RunSample();
                            break;
                        case "15":
                            await RunMultiExchangeTest();
                            break;
                        case "16":
                            await RunAllKoreanExchanges();
                            break;
                        case "17":
                            await RunTop5Global();
                            break;
                        case "0":
                            return;
                        default:
                            Console.WriteLine("\n❌ Invalid choice. Please try again.");
                            break;
                    }
                    
                    if (choice != "0")
                    {
                        Console.WriteLine("\n✅ Test completed. Press any key to continue...");
                        
                        // Clear any buffered input before waiting for key
                        while (Console.KeyAvailable)
                        {
                            Console.ReadKey(true);
                        }
                        
                        Console.ReadKey();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"\n❌ Error: {ex.Message}");
                    Console.WriteLine("Press any key to continue...");
                    
                    // Clear any buffered input before waiting for key
                    while (Console.KeyAvailable)
                    {
                        Console.ReadKey(true);
                    }
                    
                    Console.ReadKey();
                }
            }
        }
        
        private static async Task RunMultiExchangeTest()
        {
            Console.WriteLine("\n=== Multi-Exchange Test ===");
            Console.WriteLine("Testing Binance, OKX, and Coinbase simultaneously...\n");
            
            var tasks = new Task[]
            {
                Task.Run(async () => 
                {
                    Console.WriteLine("[1/3] Starting Binance...");
                    await BinanceSample.RunSample();
                }),
                Task.Run(async () => 
                {
                    Console.WriteLine("[2/3] Starting OKX...");
                    await OkxExample.RunSample();
                }),
                Task.Run(async () => 
                {
                    Console.WriteLine("[3/3] Starting Coinbase...");
                    await CoinbaseExample.RunSample();
                })
            };
            
            await Task.WhenAll(tasks);
            Console.WriteLine("\n✅ Multi-exchange test completed!");
        }
        
        private static async Task RunAllKoreanExchanges()
        {
            Console.WriteLine("\n=== All Korean Exchanges Test ===");
            Console.WriteLine("Testing all KRW markets...\n");
            
            Console.WriteLine("[1/4] Testing Upbit...");
            await UpbitSample.RunSample();
            
            Console.WriteLine("\n[2/4] Testing Bithumb...");
            await BithumbSample.RunSample();
            
            Console.WriteLine("\n[3/4] Testing Coinone...");
            await CoinoneExample.RunSample();
            
            Console.WriteLine("\n[4/4] Testing Korbit...");
            await KorbitExample.RunSample();
            
            Console.WriteLine("\n✅ All Korean exchanges tested!");
        }
        
        private static async Task RunTop5Global()
        {
            Console.WriteLine("\n=== Top 5 Global Exchanges Test ===");
            Console.WriteLine("Testing top exchanges by volume...\n");
            
            Console.WriteLine("[1/5] Testing Binance...");
            await BinanceSample.RunSample();
            
            Console.WriteLine("\n[2/5] Testing OKX...");
            await OkxExample.RunSample();
            
            Console.WriteLine("\n[3/5] Testing Bybit...");
            await BybitExample.RunSample();
            
            Console.WriteLine("\n[4/5] Testing Gate.io...");
            await GateioExample.RunSample();
            
            Console.WriteLine("\n[5/5] Testing KuCoin...");
            await KucoinExample.RunSample();
            
            Console.WriteLine("\n✅ Top 5 global exchanges tested!");
        }
    }
}