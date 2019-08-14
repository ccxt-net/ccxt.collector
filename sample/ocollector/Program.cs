using CCXT.Collector.Library;
using CCXT.Collector.Library.Service;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CCXT.OCollector
{
    /// <summary>
    ///
    /// </summary>
    public class Program
    {
        private static CancellationTokenSource __main_token_source;

        public static CancellationTokenSource MainTokenSource
        {
            get
            {
                if (__main_token_source == null)
                    __main_token_source = new CancellationTokenSource();

                return __main_token_source;
            }
        }

        private static SynchronizedCollection<Task> __main_tasks;

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

            KConfig.SetConfigRoot();

            try
            {
#if DEBUG
                FactoryQ.RootQName = "odin";
#endif

                Console.Out.WriteLine($"{FactoryQ.RootQName} collector {KConfig.CollectorVersion} start...");

                if (KConfig.CConfig.IsWindows == false)
                {
                    MainTasks.Add((new BooktickerQ()).Start(MainTokenSource));
                    MainTasks.Add((new OrderbookQ()).Start(MainTokenSource));

                    MainTasks.Add((new LoggerQ()).Start(MainTokenSource));
                    MainTasks.Add((new SnapshotQ()).Start(MainTokenSource));

                    Task.WaitAll(MainTasks.ToArray(), MainTokenSource.Token);

                    Console.Out.WriteLine($"{FactoryQ.RootQName} collector {KConfig.CollectorVersion} stop...");
                }
                else
                {
                    MainTasks.Add((new BooktickerQ()).Start(MainTokenSource));
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

            if (KConfig.CConfig.IsWindows == true)
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