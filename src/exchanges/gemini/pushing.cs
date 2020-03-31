using CCXT.Collector.Library;
using Newtonsoft.Json;
using OdinSdk.BaseLib.Configuration;
using System;
using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CCXT.Collector.Gemini
{
    public class Pushing
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
            var _cmd_bytes = Encoding.UTF8.GetBytes(message.Replace('\'', '\"'));
            await cws.SendAsync(
                        new ArraySegment<byte>(_cmd_bytes),
                        WebSocketMessageType.Text,
                        endOfMessage: true,
                        cancellationToken: tokenSource.Token
                    );
        }

        private async Task Open(CancellationTokenSource tokenSource, ClientWebSocket cws, string symbol, bool reopen = true)
        {
            if (cws.State == WebSocketState.Open)
                await cws.CloseAsync(WebSocketCloseStatus.NormalClosure, "reopen", tokenSource.Token);

            await cws.ConnectAsync(new Uri("wss://api.upbit.com/websocket/v1"), tokenSource.Token);

            await SendAsync(tokenSource, cws,
                //"[{'ticket':'ccxt-collector'},{'type':'orderbook','codes':['" + symbol + "']},{'format':'SIMPLE'},{'type':'trade','codes':['" + symbol + "']},{'format':'SIMPLE'}]"
                "[{'ticket':'ccxt-collector'},{'type':'orderbook','codes':['" + symbol + "']},{'format':'DEFAULT'},{'type':'trade','codes':['" + symbol + "']},{'format':'DEFAULT'}]"
            );
        }

        private long __last_receive_time = 0;

        public async Task Start(CancellationTokenSource tokenSource, string symbol)
        {
            GMLogger.SNG.WriteO(this, $"pushing service start: symbol => {symbol}...");

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
                            if (_waiting_time > GMConfig.SNG.WebSocketRetry * 1000)
                            {
                                __last_receive_time = CUnixTime.NowMilli;
                                await Open(tokenSource, _cws, symbol);

                                GMLogger.SNG.WriteO(this, $"pushing open: symbol => {symbol}...");
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
                                await SendAsync(tokenSource, _cws, _message.payload);
                            }
                        }
                        catch (TaskCanceledException)
                        {
                        }
                        catch (Exception ex)
                        {
                            GMLogger.SNG.WriteX(this, ex.ToString());
                        }
                        //finally
                        {
                            if (_cws.State != WebSocketState.Open)
                            {
                                GMLogger.SNG.WriteO(this, $"disconnect from server(cmd): symbol => {symbol}...");
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
                            }
                            else if (_result.MessageType == WebSocketMessageType.Binary)
                            {
                                var _json = Encoding.UTF8.GetString(_buffer, 0, _offset);
                                var _selector = JsonConvert.DeserializeObject<QSelector>(_json);

                                Processing.SendReceiveQ(new QMessage 
                                { 
                                    command = "WS",
                                    exchange = GMLogger.SNG.exchange_name,
                                    symbol = symbol,
                                    stream = _selector.type,
                                    action = _selector.stream_type,
                                    payload = _json 
                                });
                            }
                            else if (_result.MessageType == WebSocketMessageType.Close)
                            {
                                await _cws.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "", tokenSource.Token);

                                GMLogger.SNG.WriteO(this, $"receive close message from server: symbol => {symbol}...");
                                tokenSource.Cancel();
                                break;
                            }
                        }
                        catch (TaskCanceledException)
                        {
                        }
                        catch (Exception ex)
                        {
                            GMLogger.SNG.WriteX(this, ex.ToString());
                        }
                        //finally
                        {
                            if (_cws.State != WebSocketState.Open)
                            {
                                GMLogger.SNG.WriteO(this, $"disconnect from server: symbol => {symbol}...");
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

                GMLogger.SNG.WriteO(this, $"pushing service stopped: symbol => {symbol}...");
            }
        }
    }
}