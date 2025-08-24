using System;
using System.Threading.Tasks;
using CCXT.Collector.Samples.Utilities;

namespace CCXT.Collector.Samples
{
    /// <summary>
    /// Main program for CCXT.Collector samples and tests
    /// </summary>
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("╔══════════════════════════════════════════════════════════╗");
            Console.WriteLine("║         CCXT.Collector WebSocket Test Suite v2.1         ║");
            Console.WriteLine("║              Real-time Crypto Market Data                ║");
            Console.WriteLine("╚══════════════════════════════════════════════════════════╝");
            Console.WriteLine();

            while (true)
            {
                DisplayMainMenu();
                
                var choice = Console.ReadLine();
                Console.Clear();

                try
                {
                    switch (choice)
                    {
                        case "1":
                            await ExchangeTestRunner.TestAllExchanges();
                            break;
                        case "2":
                            await ExchangeTestRunner.TestSingleExchange();
                            break;
                        case "3":
                            await ExchangeTestRunner.QuickConnectivityCheck();
                            break;
                        case "4":
                            await MultiMarketTestRunner.TestMultiMarketDataReception();
                            break;
                        case "5":
                            await SampleDemoRunner.RunExchangeSampleDemo();
                            break;
                        case "0":
                            Console.WriteLine("Exiting... Thank you for using CCXT.Collector!");
                            return;
                        default:
                            Console.WriteLine("Invalid option. Please try again.");
                            Console.WriteLine("\nPress any key to continue...");
                            Console.ReadKey();
                            Console.Clear();
                            break;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"\nAn error occurred: {ex.Message}");
                    Console.WriteLine("\nPress any key to continue...");
                    Console.ReadKey();
                    Console.Clear();
                }
            }
        }

        /// <summary>
        /// Display the main menu options
        /// </summary>
        private static void DisplayMainMenu()
        {
            Console.WriteLine("╔══════════════════════════════════════════════════════════╗");
            Console.WriteLine("║                      Main Menu                           ║");
            Console.WriteLine("╠══════════════════════════════════════════════════════════╣");
            Console.WriteLine("║  1. Test All Exchanges (Full Test)                       ║");
            Console.WriteLine("║  2. Test Single Exchange                                 ║");
            Console.WriteLine("║  3. Quick Connectivity Check                             ║");
            Console.WriteLine("║  4. Test Multi-Market Data Reception                     ║");
            Console.WriteLine("║  5. Run Exchange Sample Demo                             ║");
            Console.WriteLine("║  0. Exit                                                 ║");
            Console.WriteLine("╚══════════════════════════════════════════════════════════╝");
            Console.Write("\nSelect an option: ");
        }
    }
}