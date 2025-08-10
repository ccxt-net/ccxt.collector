using System;
using System.Threading;
using System.Threading.Tasks;

namespace CCXT.Collector.Samples.Exchanges
{
    /// <summary>
    /// Helper class for exchange samples with simple countdown timer
    /// </summary>
    public static class SampleHelper
    {
        /// <summary>
        /// Wait for specified duration with simple countdown display
        /// </summary>
        /// <param name="milliseconds">Duration to wait in milliseconds</param>
        /// <returns>Always returns true (completed normally)</returns>
        public static async Task<bool> WaitForDurationOrEsc(int milliseconds)
        {
            var seconds = milliseconds / 1000;
            var startTime = DateTime.Now;
            
            // Simple countdown without progress bar or keyboard detection
            for (int i = seconds; i > 0; i--)
            {
                // Display remaining time on new line to avoid display issues
                Console.WriteLine($"  Collecting data... {i} seconds remaining");
                await Task.Delay(1000);
            }
            
            return true;
        }
        
        /// <summary>
        /// Properly disconnect WebSocket client with cleanup
        /// </summary>
        /// <param name="client">WebSocket client to disconnect</param>
        /// <param name="clientName">Name of the exchange for logging</param>
        public static async Task SafeDisconnectAsync(dynamic client, string clientName)
        {
            try
            {
                Console.WriteLine($"\nDisconnecting from {clientName}...");
                
                // Give some time for any pending messages to be processed
                await Task.Delay(500);
                
                // Disconnect the WebSocket
                await client.DisconnectAsync();
                
                // Additional delay to ensure clean shutdown
                await Task.Delay(500);
                
                Console.WriteLine($"{clientName} disconnected successfully.");
                
                // Clear any input that may have been buffered during WebSocket operation
                ClearInputBuffer();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error disconnecting from {clientName}: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Alternative method with silent wait
        /// </summary>
        /// <param name="milliseconds">Duration to wait in milliseconds</param>
        /// <returns>Always returns true</returns>
        public static async Task<bool> SilentWait(int milliseconds)
        {
            await Task.Delay(milliseconds);
            return true;
        }
        
        /// <summary>
        /// Clear the console input buffer to prevent old input from being processed
        /// </summary>
        public static void ClearInputBuffer()
        {
            while (Console.KeyAvailable)
            {
                Console.ReadKey(true);
            }
        }
    }
}