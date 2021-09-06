using CCXT.NET.Shared.Configuration;
using CCXT.Collector.Deribit.Model;
using CCXT.Collector.Library;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CCXT.Collector.Deribit
{
    public class Pushing
    {
        private const string __auth_point = "/ws/api/v2";

        private long __last_receive_time = 0;
        private int __request_id = 0;

        private string webSocketUrl
        {
            get
            {
                if (__drconfig.UseLiveServer == true)
                    return $"wss://www.deribit.com{__auth_point}";
                else
                    return $"wss://test.deribit.com{__auth_point}";
            }
        }

        private static ConcurrentQueue<JsonRpcRequest> __command_queue = null;

        /// <summary>
        ///
        /// </summary>
        private static ConcurrentQueue<JsonRpcRequest> CommandQ
        {
            get
            {
                if (__command_queue == null)
                    __command_queue = new ConcurrentQueue<JsonRpcRequest>();

                return __command_queue;
            }
        }

        private readonly DRConfig __drconfig;

        public Pushing(IConfiguration configuration)
        {
            __drconfig = new DRConfig(configuration);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="message"></param>
        public static void SendCommandQ(JsonRpcRequest message)
        {
            CommandQ.Enqueue(message);
        }

        private JsonRpcRequest getMessage(string method, object @params = null)
        {
            if (@params == null)
                @params = new
                {
                };

            return new JsonRpcRequest
            {
                jsonrpc = "2.0",
                id = __request_id++,
                method = method,
                @params = @params
            };
        }

        private async ValueTask sendMessage(CancellationToken cancelToken, ClientWebSocket cws, JsonRpcRequest request)
        {
            var _json_string = JsonConvert.SerializeObject(request);
#if DEBUG
            DRLogger.SNG.WriteC(this, _json_string);
#endif

            var _message = Encoding.UTF8.GetBytes(_json_string);
            await cws.SendAsync(
                        new ArraySegment<byte>(_message),
                        WebSocketMessageType.Text,
                        endOfMessage: true,
                        cancellationToken: cancelToken
                    );
        }

        private async ValueTask Subscribe(string symbol)
        {
            await Task.Delay(0);

            SendCommandQ(
                getMessage("public/subscribe", new
                {
                    channels = new List<string>
                        {
                            $"book.{symbol}.100ms",
                            $"trades.{symbol}.100ms"
                        }
                })
            );
        }

        public async ValueTask Start(CancellationTokenSource cancelTokenSource, string symbol)
        {
            DRLogger.SNG.WriteO(this, $"pushing service start: symbol => {symbol}...");

            using (var _cws = new ClientWebSocket())
            {
                await _cws.ConnectAsync(new Uri(webSocketUrl), cancelTokenSource.Token);

                var _sending = Task.Run(async () =>
                {
                    while (true)
                    {
                        try
                        {
                            await Task.Delay(0);

                            var _waiting_time = CUnixTime.Now - __last_receive_time;
                            if (_waiting_time > 30)
                            {
                                if (_waiting_time > 60)
                                {
                                    await Subscribe(symbol);
                                }
                                else
                                {
                                    await sendMessage(cancelTokenSource.Token, _cws, getMessage("public/test"));
                                }

                                __last_receive_time = CUnixTime.Now;
                            }

                            var _request = (JsonRpcRequest)null;

                            if (CommandQ.TryDequeue(out _request) == false)
                            {
                                var _cancelled = cancelTokenSource.Token.WaitHandle.WaitOne(10);
                                if (_cancelled == true)
                                    break;
                            }
                            else
                            {
                                await sendMessage(cancelTokenSource.Token, _cws, _request);
                            }
                        }
                        catch (Exception ex)
                        {
                            DRLogger.SNG.WriteX(this, ex.ToString());
                        }
                        //finally
                        {
                            if (cancelTokenSource.Token.IsCancellationRequested == true)
                                break;
                        }
                    }
                },
                cancelTokenSource.Token
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
                            var _result = await _cws.ReceiveAsync(new ArraySegment<byte>(_buffer, _offset, _free), cancelTokenSource.Token);

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

                            __last_receive_time = CUnixTime.Now;

                            if (_result.MessageType == WebSocketMessageType.Text)
                            {
                                var _json_string = Encoding.UTF8.GetString(_buffer, 0, _offset);

                                while (true)
                                {
                                    var _response = JsonConvert.DeserializeObject<JsonRpcResponse<JToken>>(_json_string);
                                    if (_response.method != "subscription")
                                    {
                                        DRLogger.SNG.WriteO(this, _json_string);
                                        break;
                                    }

                                    var _stream = "";
                                    if (_response.@params.channel.Contains("book."))
                                        _stream = "orderbook";
                                    else if (_response.@params.channel.Contains("trades."))
                                        _stream = "trade";

                                    Processing.SendReceiveQ(new QMessage
                                    {
                                        command = "WS",
                                        exchange = DRLogger.SNG.exchange_name,
                                        symbol = symbol,
                                        stream = _stream,
                                        action = "pushing",
                                        payload = _response.@params.data.ToString(Formatting.None)
                                    });

                                    break;
                                }
                            }
                            else if (_result.MessageType == WebSocketMessageType.Binary)
                            {
                            }
                            else if (_result.MessageType == WebSocketMessageType.Close)
                            {
                                await _cws.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "", cancelTokenSource.Token);

                                DRLogger.SNG.WriteO(this, $"receive close message from server: symbol => {symbol}...");
                                //cancelToken.Cancel();
                                break;
                            }
                        }
                        catch (TaskCanceledException)
                        {
                        }
                        catch (Exception ex)
                        {
                            DRLogger.SNG.WriteX(this, ex.ToString());
                        }
                        //finally
                        {
                            if (_cws.State != WebSocketState.Open && _cws.State != WebSocketState.Connecting)
                            {
                                DRLogger.SNG.WriteO(this, $"disconnect from server: symbol => {symbol}...");

                                cancelTokenSource.Cancel();
                                break;
                            }

                            if (cancelTokenSource.Token.IsCancellationRequested == true)
                                break;

                            _offset = 0;
                            _free = _buffer.Length;
                        }
                    }
                },
                cancelTokenSource.Token
                );

                await Task.WhenAll(_sending, _receiving);

                DRLogger.SNG.WriteO(this, $"pushing service stopped: symbol => {symbol}...");
            }
        }
    }
}