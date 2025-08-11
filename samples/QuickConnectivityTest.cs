using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CCXT.Collector.Samples
{
    public class QuickConnectivityTest
    {
        // Define the 15 main exchanges with their WebSocket URLs
        private static readonly Dictionary<string, string> ExchangeUrls = new Dictionary<string, string>
        {
            { "Binance", "wss://stream.binance.com:9443/ws" },
            { "Bitget", "wss://ws.bitget.com/spot/v1/stream" },
            { "Bithumb", "wss://pubwss.bithumb.com/pub/ws" },
            { "Bittrex", "wss://socket-v3.bittrex.com/signalr" },
            { "Bybit", "wss://stream.bybit.com/v5/public/spot" },
            { "Coinbase", "wss://ws-feed.exchange.coinbase.com" },
            { "Coinone", "wss://stream.coinone.co.kr" },
            { "Crypto.com", "wss://stream.crypto.com/v2/market" },
            { "Gate.io", "wss://api.gateio.ws/ws/v4/" },
            { "Gopax", "wss://wsapi.gopax.co.kr" },
            { "Huobi", "wss://api.huobi.pro/ws" },
            { "Korbit", "wss://ws.korbit.co.kr/v1/user/push" },
            { "Kucoin", "wss://ws-api-spot.kucoin.com" },
            { "OKX", "wss://ws.okx.com:8443/ws/v5/public" },
            { "Upbit", "wss://api.upbit.com/websocket/v1" }
        };

        public static async Task RunTest()
        {
            Console.WriteLine("=====================================");
            Console.WriteLine("  🔌 Quick WebSocket Connectivity Test");
            Console.WriteLine("=====================================");
            Console.WriteLine("Testing raw WebSocket connections to 15 exchanges...");
            Console.WriteLine();

            var results = new List<(string Name, bool Success, string Status, TimeSpan ResponseTime)>();

            foreach (var exchange in ExchangeUrls)
            {
                Console.Write($"Testing {exchange.Key,-15} ");
                
                var startTime = DateTime.UtcNow;
                var (success, status) = await TestWebSocketConnection(exchange.Key, exchange.Value);
                var responseTime = DateTime.UtcNow - startTime;
                
                results.Add((exchange.Key, success, status, responseTime));
                
                var statusIcon = success ? "✅" : "❌";
                Console.WriteLine($"{statusIcon} {status} ({responseTime.TotalMilliseconds:F0}ms)");
            }

            // Print summary
            Console.WriteLine("\n=====================================");
            Console.WriteLine("           SUMMARY");
            Console.WriteLine("=====================================");
            
            var successCount = results.Count(r => r.Success);
            var failCount = results.Count - successCount;
            
            Console.WriteLine($"Total: {results.Count} | Success: {successCount} | Failed: {failCount}");
            Console.WriteLine($"Success Rate: {(successCount * 100.0 / results.Count):F1}%");
            
            if (successCount > 0)
            {
                var avgResponseTime = results.Where(r => r.Success).Average(r => r.ResponseTime.TotalMilliseconds);
                Console.WriteLine($"Average Response Time: {avgResponseTime:F0}ms");
            }
            
            if (failCount > 0)
            {
                Console.WriteLine("\nFailed Exchanges:");
                foreach (var failed in results.Where(r => !r.Success))
                {
                    Console.WriteLine($"  • {failed.Name}: {failed.Status}");
                }
            }

            Console.WriteLine("\n✅ Test completed!");
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }

        private static async Task<(bool Success, string Status)> TestWebSocketConnection(string name, string url)
        {
            ClientWebSocket webSocket = null;
            var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            
            try
            {
                webSocket = new ClientWebSocket();
                
                // Some exchanges require specific headers
                if (name == "Upbit")
                {
                    webSocket.Options.SetRequestHeader("User-Agent", "Mozilla/5.0");
                }
                
                // Connect with timeout
                await webSocket.ConnectAsync(new Uri(url), cts.Token);
                
                if (webSocket.State == WebSocketState.Open)
                {
                    // Try to receive initial message (some exchanges send welcome messages)
                    var buffer = new ArraySegment<byte>(new byte[4096]);
                    var receiveTask = webSocket.ReceiveAsync(buffer, CancellationToken.None);
                    
                    if (await Task.WhenAny(receiveTask, Task.Delay(1000)) == receiveTask)
                    {
                        var result = await receiveTask;
                        if (result.MessageType == WebSocketMessageType.Text)
                        {
                            var message = Encoding.UTF8.GetString(buffer.Array, 0, result.Count);
                            return (true, "Connected + Message received");
                        }
                    }
                    
                    return (true, "Connected");
                }
                
                return (false, $"Connection state: {webSocket.State}");
            }
            catch (WebSocketException wsEx)
            {
                return (false, $"WebSocket error: {wsEx.WebSocketErrorCode}");
            }
            catch (TaskCanceledException)
            {
                return (false, "Connection timeout");
            }
            catch (Exception ex)
            {
                return (false, $"Error: {ex.Message}");
            }
            finally
            {
                try
                {
                    if (webSocket?.State == WebSocketState.Open)
                    {
                        await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Test complete", CancellationToken.None);
                    }
                    webSocket?.Dispose();
                }
                catch { }
                cts?.Dispose();
            }
        }
    }
}