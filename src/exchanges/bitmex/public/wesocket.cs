using CCXT.Collector.Library;
using CCXT.Collector.Library.Types;
using OdinSdk.BaseLib.Configuration;
using System;
using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CCXT.Collector.BitMEX.Public
{
    public partial class WebSocket
    {
        private static ConcurrentQueue<QMessage> __command_queue = null;

        /// <summary>
        ///
        /// </summary>
        private static ConcurrentQueue<QMessage> CommandQ
        {
            get
            {
                if (__command_queue == null)
                    __command_queue = new ConcurrentQueue<QMessage>();

                return __command_queue;
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="message"></param>
        public static void SendCommandQ(QMessage message)
        {
            CommandQ.Enqueue(message);
        }

        private async Task SendAsync(CancellationTokenSource tokenSource, ClientWebSocket cws, string message)
        {
            var _cmd_bytes = Encoding.UTF8.GetBytes(message);
            await cws.SendAsync(
                        new ArraySegment<byte>(_cmd_bytes),
                        WebSocketMessageType.Text,
                        endOfMessage: true,
                        cancellationToken: tokenSource.Token
                    );
        }

        private async Task Open(CancellationTokenSource tokenSource, ClientWebSocket cws, string symbol)
        {
            if (cws.State != WebSocketState.Open)
            {
                //var _wss_url = $"wss://stream.binance.com:9443/stream?streams={symbol.ToLower()}@depth/{symbol.ToLower()}@aggTrade";
                var _wss_url = $"wss://stream.binance.com:9443/stream?streams={symbol.ToLower()}@aggTrade";

                await cws.ConnectAsync(new Uri(_wss_url), tokenSource.Token);
            }
        }

        private long __last_receive_time = 0;

        public async Task Start(CancellationTokenSource tokenSource, string symbol, int limit = 32, int sleep_seconds = 60)
        {
            BMLogger.WriteO($"websocket service start: symbol => {symbol}...");

            using (var _cws = new ClientWebSocket())
            {
                var _sending = Task.Run(async () =>
                {
                    while (true)
                    {
                        try
                        {
                            await Task.Delay(0);

                            var _waiting_time = CUnixTime.NowMilli - __last_receive_time;
                            if (_waiting_time > KConfig.BinanceWebSocketRetry * 1000)
                            {
                                __last_receive_time = CUnixTime.NowMilli;
                                await Open(tokenSource, _cws, symbol);

                                BMLogger.WriteO($"websocket open: symbol => {symbol}...");
                            }

                            var _message = (QMessage)null;

                            if (CommandQ.TryDequeue(out _message) == false)
                            {
                                var _cancelled = tokenSource.Token.WaitHandle.WaitOne(0);
                                if (_cancelled == true)
                                    break;

                                await Task.Delay(10);
                            }
                            else
                            {
                                await SendAsync(tokenSource, _cws, _message.json);
                            }
                        }
                        catch (TaskCanceledException)
                        {
                        }
                        catch (Exception ex)
                        {
                            BMLogger.WriteX(ex.ToString());
                        }
                        //finally
                        {
                            if (_cws.State != WebSocketState.Open)
                            {
                                BMLogger.WriteO($"disconnect from server(cmd): symbol => {symbol}...");
                                tokenSource.Cancel();
                                break;
                            }

                            if (tokenSource.IsCancellationRequested == true)
                                break;
                        }
                    }
                },
                tokenSource.Token
                );

                var _receiving = Task.Run(async () =>
                {
                    var _buffer_size = 1024 * 16;
                    var _buffer = new byte[_buffer_size];

                    var _offset = 0;
                    var _free = _buffer.Length;

                    while (true)
                    {
                        try
                        {
                            if (_cws.State == WebSocketState.None || _cws.State == WebSocketState.Connecting)
                            {
                                await Task.Delay(1000);
                                continue;
                            }

                            var _result = await _cws.ReceiveAsync(new ArraySegment<byte>(_buffer, _offset, _free), tokenSource.Token);

                            _offset += _result.Count;
                            _free -= _result.Count;

                            if (_result.EndOfMessage == false)
                            {
                                if (_free == 0)
                                {
                                    Array.Resize(ref _buffer, _buffer.Length + _buffer_size);
                                    _free = _buffer.Length - _offset;
                                }

                                continue;
                            }

                            __last_receive_time = CUnixTime.NowMilli;

                            if (_result.MessageType == WebSocketMessageType.Text)
                            {
                                var _data = Encoding.UTF8.GetString(_buffer, 0, _offset);
                                Processing.SendReceiveQ(new QMessage { command = "WS", json = _data });
                            }
                            else if (_result.MessageType == WebSocketMessageType.Binary)
                            {
                            }
                            else if (_result.MessageType == WebSocketMessageType.Close)
                            {
                                await _cws.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "", tokenSource.Token);

                                BMLogger.WriteO($"receive close message from server: symbol => {symbol}...");
                                tokenSource.Cancel();
                                break;
                            }
                        }
                        catch (TaskCanceledException)
                        {
                        }
                        catch (Exception ex)
                        {
                            BMLogger.WriteX(ex.ToString());
                        }
                        //finally
                        {
                            if (_cws.State != WebSocketState.Open)
                            {
                                BMLogger.WriteO($"disconnect from server: symbol => {symbol}...");
                                tokenSource.Cancel();
                                break;
                            }

                            if (tokenSource.IsCancellationRequested == true)
                                break;

                            _offset = 0;
                            _free = _buffer.Length;
                        }
                    }
                },
                tokenSource.Token
                );

                await Task.WhenAll(_sending, _receiving);

                BMLogger.WriteO($"websocket service stopped: symbol => {symbol}...");
            }
        }
    }
}