using System;
using System.Net.Http;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace CCXT.Collector.Samples
{
    public class SignalRConnectivityTest
    {
        public static async Task TestBittrexSignalR()
        {
            Console.WriteLine("=====================================");
            Console.WriteLine("  🔌 Bittrex SignalR Connection Test");
            Console.WriteLine("=====================================");
            
            try
            {
                // Step 1: Negotiate with SignalR endpoint
                Console.WriteLine("1. Negotiating with SignalR endpoint...");
                var negotiateUrl = "https://socket-v3.bittrex.com/signalr/negotiate?clientProtocol=1.5&connectionData=%5B%7B%22name%22%3A%22c3%22%7D%5D";
                
                using var httpClient = new HttpClient();
                var negotiateResponse = await httpClient.GetStringAsync(negotiateUrl);
                Console.WriteLine($"   Negotiate Response: {negotiateResponse.Substring(0, Math.Min(100, negotiateResponse.Length))}...");
                
                // Parse the negotiate response
                using var doc = JsonDocument.Parse(negotiateResponse);
                var root = doc.RootElement;
                var connectionToken = root.GetProperty("ConnectionToken").GetString();
                var connectionId = root.GetProperty("ConnectionId").GetString();
                
                Console.WriteLine($"   Connection ID: {connectionId}");
                Console.WriteLine($"   Connection Token: {connectionToken?.Substring(0, Math.Min(20, connectionToken.Length))}...");
                
                // Step 2: Connect via WebSocket with the negotiated parameters
                Console.WriteLine("\n2. Connecting via WebSocket...");
                var wsUrl = $"wss://socket-v3.bittrex.com/signalr/connect?transport=webSockets&clientProtocol=1.5&connectionToken={Uri.EscapeDataString(connectionToken)}&connectionData=%5B%7B%22name%22%3A%22c3%22%7D%5D&tid=10";
                
                using var webSocket = new ClientWebSocket();
                webSocket.Options.SetRequestHeader("User-Agent", "Mozilla/5.0");
                webSocket.Options.SetRequestHeader("Origin", "https://global.bittrex.com");
                
                await webSocket.ConnectAsync(new Uri(wsUrl), CancellationToken.None);
                
                if (webSocket.State == WebSocketState.Open)
                {
                    Console.WriteLine("   ✅ WebSocket connected successfully!");
                    
                    // Step 3: Send SignalR handshake
                    Console.WriteLine("\n3. Sending SignalR handshake...");
                    var handshake = JsonSerializer.Serialize(new { protocol = "json", version = 1 });
                    var handshakeBytes = Encoding.UTF8.GetBytes(handshake + "\x1e");
                    await webSocket.SendAsync(new ArraySegment<byte>(handshakeBytes), WebSocketMessageType.Text, true, CancellationToken.None);
                    
                    // Step 4: Receive initial messages
                    Console.WriteLine("\n4. Receiving initial messages...");
                    var buffer = new ArraySegment<byte>(new byte[4096]);
                    var receiveTask = webSocket.ReceiveAsync(buffer, CancellationToken.None);
                    
                    if (await Task.WhenAny(receiveTask, Task.Delay(3000)) == receiveTask)
                    {
                        var result = await receiveTask;
                        if (result.MessageType == WebSocketMessageType.Text)
                        {
                            var message = Encoding.UTF8.GetString(buffer.Array, 0, result.Count);
                            Console.WriteLine($"   Received: {message.Substring(0, Math.Min(100, message.Length))}...");
                            
                            // Try subscribing to a channel
                            Console.WriteLine("\n5. Subscribing to ticker channel...");
                            var subscribeMsg = JsonSerializer.Serialize(new
                            {
                                H = "c3",
                                M = "Subscribe",
                                A = new[] { new[] { "ticker_BTC-USDT" } },
                                I = 1
                            });
                            var subscribeBytes = Encoding.UTF8.GetBytes(subscribeMsg + "\x1e");
                            await webSocket.SendAsync(new ArraySegment<byte>(subscribeBytes), WebSocketMessageType.Text, true, CancellationToken.None);
                            
                            // Wait for subscription response
                            await Task.Delay(1000);
                            
                            // Try to receive more messages
                            for (int i = 0; i < 3; i++)
                            {
                                var buffer2 = new ArraySegment<byte>(new byte[4096]);
                                var receiveTask2 = webSocket.ReceiveAsync(buffer2, CancellationToken.None);
                                
                                if (await Task.WhenAny(receiveTask2, Task.Delay(2000)) == receiveTask2)
                                {
                                    var result2 = await receiveTask2;
                                    if (result2.MessageType == WebSocketMessageType.Text)
                                    {
                                        var message2 = Encoding.UTF8.GetString(buffer2.Array, 0, result2.Count);
                                        Console.WriteLine($"   Message {i+1}: {message2.Substring(0, Math.Min(100, message2.Length))}...");
                                    }
                                }
                            }
                            
                            Console.WriteLine("\n✅ Bittrex SignalR connection test successful!");
                        }
                    }
                    else
                    {
                        Console.WriteLine("   ⚠️ No initial message received (timeout)");
                    }
                    
                    // Close connection
                    await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Test complete", CancellationToken.None);
                }
                else
                {
                    Console.WriteLine($"   ❌ Connection failed. State: {webSocket.State}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n❌ Error: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"   Inner: {ex.InnerException.Message}");
                }
            }
            
            Console.WriteLine("\nPress any key to exit...");
            Console.ReadKey();
        }
        
        public static async Task RunTest()
        {
            await TestBittrexSignalR();
        }
    }
}