using CCXT.Collector.BitMEX.Private;
using CCXT.Collector.Library;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OdinSdk.BaseLib.Configuration;
using OdinSdk.BaseLib.Extension;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CCXT.Collector.BitMEX
{
    public class Pushing
    {
        private const string __auth_point = "/realtime";
        private const string __end_point = __auth_point + "md";

        private string webSocketUrl
        {
            get
            {
                if (__bmconfig.UseLiveServer == true)
                    return $"wss://www.bitmex.com{__end_point}";
                else
                    return $"wss://testnet.bitmex.com{__end_point}";
            }
        }

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

        private readonly BMConfig __bmconfig;

        public Pushing(IConfiguration configuration)
        {
            __bmconfig = new BMConfig(configuration);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="message"></param>
        public static void SendCommandQ(QMessage message)
        {
            CommandQ.Enqueue(message);
        }

        private async Task sendMessage(CancellationToken cancelToken, ClientWebSocket cws, string message)
        {
            var _cmd_bytes = Encoding.UTF8.GetBytes(message);
            await cws.SendAsync(
                        new ArraySegment<byte>(_cmd_bytes),
                        WebSocketMessageType.Text,
                        endOfMessage: true,
                        cancellationToken: cancelToken
                    );
        }

        private async Task sendMuxMessage(CancellationToken cancelToken, ClientWebSocket cws, string id, string topic, int type, string payload = "")
        {
            var _mux_msg = "["
                         + $"{type},'{id}','{topic}'"
                         + $"{(String.IsNullOrEmpty(payload) == false ? ", " + payload : "")}"
                         + "]";

            await sendMessage(cancelToken, cws, _mux_msg.Replace('\'', '\"'));
        }

        private async Task publicOpen(string id, string symbol, string topic = "public")
        {
            await Task.Delay(0);

            SendCommandQ(new QMessage
            {
                command = "OPEN",
                type = 1,
                id = id,
                topic = topic,
                payload = ""
            });

            var _args = new List<string>();
            {
                _args.Add($"'trade:{symbol}'");
                _args.Add($"'orderBookL2_25:{symbol}'");
                //_args.Add($"'instrument:{symbol}'");
            }

            var _json_msg = "{"
                         + " 'op': 'subscribe', "
                         //+ " 'args': ['chat','liquidation','connected'," + String.Join(",", _args) + "] "
                         + " 'args': [" + String.Join(",", _args) + "] "
                         + "}";

            SendCommandQ(new QMessage
            {
                command = "SUBC",
                type = 0,
                id = id,
                topic = topic,
                payload = _json_msg
            });
        }

        private async Task privateOpen(string id, string connect_key, string secret_key, string topic)
        {
            SendCommandQ(new QMessage
            {
                command = "OPEN",
                type = 1,
                id = id,
                topic = topic,
                payload = ""
            });

            var _private_api = new PrivateApi(connect_key, secret_key);
            var _private_cli = (BitmexClient)_private_api.privateClient;

            var _expires = (_private_cli.GenerateOnlyNonce(10) + 3600).ToString();
            var _signature = await _private_cli.CreateSignature(RestSharp.Method.GET, __auth_point, _expires);

            var _json_sign = "{ "
                           + $"'op': 'authKeyExpires', "
                           + $"'args': ['{connect_key}', {_expires}, '{_signature}']"
                           + "}";

            SendCommandQ(new QMessage
            {
                command = "SIGN",
                type = 0,
                id = id,
                topic = topic,
                payload = _json_sign
            });

            var _json_subc = "{"
                         + " 'op': 'subscribe', "
                         + " 'args': ['order'] "
                         //+ " 'args': ['position','execution','order'] "
                         //+ " 'args': ['margin'] "
                         + "}";

            SendCommandQ(new QMessage
            {
                command = "SUBC",
                type = 0,
                id = id,
                topic = topic,
                payload = _json_subc
            });
        }

        private volatile static string __stream_id = "";

        private async Task Open(string symbol)
        {
            __stream_id = CExtension.GenerateRandomString(13);

            await publicOpen(__stream_id, symbol);

            if (__bmconfig.UseMyOrderStream == true)
                await privateOpen(__stream_id, __bmconfig.ConnectKey, __bmconfig.SecretKey, __bmconfig.LoginName);
        }

        private volatile int __last_receive_time = 0;

        public async Task Start(CancellationToken cancelToken, string symbol)
        {
            BMLogger.SNG.WriteO(this, $"pushing service start: symbol => {symbol}...");

            using (var _cws = new ClientWebSocket())
            {
                await _cws.ConnectAsync(new Uri(webSocketUrl), cancelToken);

                var _sending = Task.Run(async () =>
                {
                    await Open(symbol);

                    while (true)
                    {
                        try
                        {
                            await Task.Delay(0);

                            var _waiting_time = CUnixTime.Now - __last_receive_time;
                            if (_waiting_time > 60 && __last_receive_time > 0)
                            {
                                await Open(symbol);
                            }
                            else if (_waiting_time > 5)
                            {
                                await sendMessage(cancelToken, _cws, "ping");
                                __last_receive_time = (int)CUnixTime.Now;
                            }

                            var _message = (QMessage)null;

                            if (CommandQ.TryDequeue(out _message) == false)
                            {
                                var _cancelled = cancelToken.WaitHandle.WaitOne(10);
                                if (_cancelled == true)
                                    break;
                            }
                            else
                            {
                                await sendMuxMessage(
                                        cancelToken, _cws,
                                        _message.id, _message.topic, _message.type, _message.payload
                                    );
                            }
                        }
                        catch (Exception ex)
                        {
                            BMLogger.SNG.WriteX(this, ex.ToString());
                        }
                        //finally
                        {
                            if (_cws.State != WebSocketState.Open && _cws.State != WebSocketState.Connecting)
                            {
                                //tokenSource.Cancel();
                                break;
                            }

                            if (cancelToken.IsCancellationRequested == true)
                                break;
                        }
                    }
                },
                cancelToken
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
                            var _result = await _cws.ReceiveAsync(new ArraySegment<byte>(_buffer, _offset, _free), cancelToken);

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

                            __last_receive_time = (int)CUnixTime.Now;

                            if (_result.MessageType == WebSocketMessageType.Text)
                            {
                                var _json = Encoding.UTF8.GetString(_buffer, 0, _offset);

                                while (true)
                                {
                                    if (_json[0] != '[')
                                    {
                                        if (_json != "pong")
                                            BMLogger.SNG.WriteO(this, _json);
                                        break;
                                    }

                                    var _packet = JsonConvert.DeserializeObject<JArray>(_json);
                                    if (_packet.Count < 4)
                                    {
                                        BMLogger.SNG.WriteO(this, _json);
                                        break;
                                    }

                                    var _selector = _packet[3].ToObject<WsData>();
                                    if (_selector == null || String.IsNullOrEmpty(_selector.table) || String.IsNullOrEmpty(_selector.action))
                                    {
                                        BMLogger.SNG.WriteO(this, _json);
                                        break;
                                    }

                                    if (_selector.table == "orderBookL2_25")
                                        _selector.table = "orderbook";

                                    Processing.SendReceiveQ(new QMessage
                                    {
                                        command = "WS",
                                        exchange = BMLogger.SNG.exchange_name,
                                        symbol = symbol,
                                        stream = _selector.table,
                                        action = _selector.action,
                                        payload = _selector.data.ToString(Formatting.None)
                                    });

                                    break;
                                }
                            }
                            else if (_result.MessageType == WebSocketMessageType.Binary)
                            {
                            }
                            else if (_result.MessageType == WebSocketMessageType.Close)
                            {
                                await _cws.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "", cancelToken);

                                BMLogger.SNG.WriteO(this, $"receive close message from server: symbol => {symbol}...");
                                //cancelToken.Cancel();
                                break;
                            }
                        }
                        catch (TaskCanceledException)
                        {
                        }
                        catch (Exception ex)
                        {
                            BMLogger.SNG.WriteX(this, ex.ToString());
                        }
                        //finally
                        {
                            if (_cws.State != WebSocketState.Open && _cws.State != WebSocketState.Connecting)
                            {
                                BMLogger.SNG.WriteO(this, $"disconnect from server: symbol => {symbol}...");
                                //cancelToken.Cancel();
                                break;
                            }

                            if (cancelToken.IsCancellationRequested == true)
                                break;

                            _offset = 0;
                            _free = _buffer.Length;
                        }
                    }
                },
                cancelToken
                );

                await Task.WhenAll(_sending, _receiving);

                BMLogger.SNG.WriteO(this, $"pushing service stopped: symbol => {symbol}...");
            }
        }
    }
}