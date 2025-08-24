using CCXT.Collector.Samples.Base;

namespace CCXT.Collector.Samples.Utilities
{
    /// <summary>
    /// Runner for individual exchange sample demonstrations
    /// </summary>
    public static class SampleDemoRunner
    {
        /// <summary>
        /// Run exchange sample demonstration with menu selection
        /// </summary>
        public static async Task RunExchangeSampleDemo()
        {
            var samples = ExchangeRegistry.GetExchangeSamples();
            
            Console.WriteLine("Available exchange samples:");
            for (int i = 0; i < samples.Count; i++)
            {
                Console.WriteLine($"{i + 1,2}. {samples[i].ExchangeName}");
            }
            Console.WriteLine($"{samples.Count + 1,2}. Run all samples sequentially");
            Console.WriteLine(" 0. Back to main menu");

            Console.Write("\nSelect an option: ");
            if (int.TryParse(Console.ReadLine(), out int selection))
            {
                if (selection == 0)
                {
                    Console.Clear();
                    return;
                }
                else if (selection > 0 && selection <= samples.Count)
                {
                    // Run selected sample
                    await RunSingleSample(samples[selection - 1]);
                }
                else if (selection == samples.Count + 1)
                {
                    // Run all samples
                    await RunAllSamples(samples);
                }
                else
                {
                    Console.WriteLine("Invalid selection.");
                }
            }
            else
            {
                Console.WriteLine("Invalid input.");
            }

            Console.WriteLine("\nPress any key to continue...");
            Console.ReadKey();
            Console.Clear();
        }

        /// <summary>
        /// Run a single exchange sample
        /// </summary>
        private static async Task RunSingleSample(IExchangeSample sample)
        {
            try
            {
                Console.Clear();
                Console.WriteLine($"Running {sample.ExchangeName} sample...\n");
                await sample.SampleRun();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\nError running {sample.ExchangeName} sample: {ex.Message}");
            }
        }

        /// <summary>
        /// Run all exchange samples sequentially
        /// </summary>
        private static async Task RunAllSamples(List<IExchangeSample> samples)
        {
            Console.Clear();
            Console.WriteLine("Running all exchange samples sequentially...\n");
            Console.WriteLine("Press any key to skip to the next exchange.\n");

            foreach (var sample in samples)
            {
                try
                {
                    Console.WriteLine($"\n{'='.ToString().PadRight(60, '=')}\n");
                    Console.WriteLine($"Starting {sample.ExchangeName} sample...\n");
                    
                    // Create a task for the sample
                    var sampleTask = sample.SampleRun();
                    
                    // Wait for either the sample to complete or user input
                    var completedTask = await Task.WhenAny(
                        sampleTask,
                        Task.Run(() => Console.ReadKey())
                    );

                    if (completedTask != sampleTask)
                    {
                        Console.WriteLine($"\n\nSkipping {sample.ExchangeName}...");
                        // Give it a moment to clean up
                        await Task.Delay(500);
                    }
                    else
                    {
                        await sampleTask; // Ensure any exceptions are observed
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"\nError running {sample.ExchangeName} sample: {ex.Message}");
                    Console.WriteLine("Press any key to continue to the next exchange...");
                    Console.ReadKey();
                }
            }

            Console.WriteLine($"\n{'='.ToString().PadRight(60, '=')}\n");
            Console.WriteLine("All samples completed.");
        }
    }
}