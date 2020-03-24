using CCXT.Collector.Library;
using CCXT.Collector.Service;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CCXT.Sample
{
    /// <summary>
    ///
    /// </summary>
    public class Program
    {
        private static CancellationTokenSource? __main_token_source;

        public static CancellationTokenSource MainTokenSource
        {
            get
            {
                if (__main_token_source == null)
                    __main_token_source = new CancellationTokenSource();

                return __main_token_source;
            }
        }

        private static SynchronizedCollection<Task>? __main_tasks;

        /// <summary>
        ///
        /// </summary>
        public static SynchronizedCollection<Task> MainTasks
        {
            get
            {
                if (__main_tasks == null)
                    __main_tasks = new SynchronizedCollection<Task>();

                return __main_tasks;
            }
            set
            {
                __main_tasks = value;
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="args">Add additional attributes for each exchange</param>
        public static void Main(string[] args)
        {
            var provider = CodePagesEncodingProvider.Instance;
            Encoding.RegisterProvider(provider);

            //XConfig.SNG.SetConfigRoot();

            try
            {
#if DEBUG
                FactoryX.RootQName = "odin";
#endif

                Console.Out.WriteLine($"{FactoryX.RootQName} collector {XConfig.SNG.CollectorVersion} start...");

                if (XConfig.SNG.IsWindows == false)
                {
                    MainTasks.Add((new TickerQ()).Start(MainTokenSource));
                    MainTasks.Add((new OrderbookQ()).Start(MainTokenSource));

                    MainTasks.Add((new LoggerQ()).Start(MainTokenSource));
                    MainTasks.Add((new SnapshotQ()).Start(MainTokenSource));

                    Task.WaitAll(MainTasks.ToArray(), MainTokenSource.Token);

                    Console.Out.WriteLine($"{FactoryX.RootQName} collector {XConfig.SNG.CollectorVersion} stop...");
                }
                else
                {
                    MainTasks.Add((new TickerQ()).Start(MainTokenSource));
                    MainTasks.Add((new OrderbookQ()).Start(MainTokenSource));

                    MainTasks.Add((new LoggerQ()).Start(MainTokenSource));
                    MainTasks.Add((new SnapshotQ()).Start(MainTokenSource));
                }
            }
            catch (TaskCanceledException)
            {
            }
            catch (Exception ex)
            {
                Console.Out.WriteLine($"thread exit: {ex.Message}");
            }

            if (XConfig.SNG.IsWindows == true)
            {
                while (Console.ReadKey().Key != ConsoleKey.Escape)
                    Console.Out.WriteLine("Enter 'ESC' to stop the services and end the process...");

                MainTokenSource.Cancel();
                Console.Out.WriteLine("[program] all services stopping.");

                // Keep the console alive for a second to allow the user to see the message.
                Thread.Sleep(1000);

                Console.Out.WriteLine("Hit return to exit...");
                Console.ReadLine();
            }
        }
    }
}