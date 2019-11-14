using CCXT.Collector.BitMEX.Private;
using CCXT.Collector.Library;
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
                if (KConfig.BitMexUseLiveServer == true)
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

        /// <summary>
        ///
        /// </summary>
        /// <param name="message"></param>
        public static void SendCommandQ(QMessage message)
        {
            CommandQ.Enqueue(message);
        }

        private async Task sendMessage(CancellationTokenSource tokenSource, ClientWebSocket cws, string message)
        {
            var _cmd_bytes = Encoding.UTF8.GetBytes(message);
            await cws.SendAsync(
                        new ArraySegment<byte>(_cmd_bytes),
                        WebSocketMessageType.Text,
                        endOfMessage: true,
                        cancellationToken: tokenSource.Token
                    );
        }

        private async Task sendMuxMessage(CancellationTokenSource tokenSource, ClientWebSocket cws, string id, string topic, int type, string payload = "")
        {
            var _mux_msg = "["
                         + $"{type},'{id}','{topic}'"
                         + $"{(String.IsNullOrEmpty(payload) == false ? ", " + payload : "")}"
                         + "]";

            await sendMessage(tokenSource, cws, _mux_msg.Replace('\'', '\"'));
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
                         + " 'args': ['position','execution','order'] "
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
            if (String.IsNullOrEmpty(__stream_id) == true)
                __stream_id = CExtension.GenerateRandomString(13);

            await publicOpen(__stream_id, symbol);

            //await privateOpen(__stream_id, KConfig.BitMexConnectKey, KConfig.BitMexSecretKey, KConfig.BitMexUserName);
        }

        private volatile int __last_receive_time = 0;

        public async Task Start(CancellationTokenSource tokenSource, string symbol)
        {
            BMLogger.WriteO($"pushing service start: symbol => {symbol}...");

            using (var _cws = new ClientWebSocket())
            {
                await _cws.ConnectAsync(new Uri(webSocketUrl), tokenSource.Token);

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
                                await sendMessage(tokenSource, _cws, "ping");
                                __last_receive_time = (int)CUnixTime.Now;
                            }

                            var _message = (QMessage)null;

                            if (CommandQ.TryDequeue(out _message) == false)
                            {
                                var _cancelled = tokenSource.Token.WaitHandle.WaitOne(10);
                                if (_cancelled == true)
                                    break;
                            }
                            else
                            {
                                await sendMuxMessage(
                                        tokenSource, _cws,
                                        _message.id, _message.topic, _message.type, _message.payload
                                    );
                            }
                        }
                        catch (Exception ex)
                        {
                            BMLogger.WriteX(ex.ToString());
                        }
                        //finally
                        {
                            if (_cws.State != WebSocketState.Open && _cws.State != WebSocketState.Connecting)
                            {
                                //tokenSource.Cancel();
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

                            __last_receive_time = (int)CUnixTime.Now;

                            if (_result.MessageType == WebSocketMessageType.Text)
                            {
                                var _json = Encoding.UTF8.GetString(_buffer, 0, _offset);

                                while (true)
                                {
                                    if (_json[0] != '[')
                                    {
                                        if (_json != "pong")
                                            BMLogger.WriteO(_json);
                                        break;
                                    }

                                    var _packet = JsonConvert.DeserializeObject<JArray>(_json);
                                    if (_packet.Count < 4)
                                    {
                                        BMLogger.WriteO(_json);
                                        break;
                                    }

                                    var _selector = _packet[3].ToObject<WsData>();
                                    if (String.IsNullOrEmpty(_selector.table) || String.IsNullOrEmpty(_selector.action))
                                    {
                                        BMLogger.WriteO(_json);
                                        break;
                                    }

                                    if (_selector.table == "orderBookL2_25")
                                        _selector.table = "orderbook";

                                    Processing.SendReceiveQ(new QMessage
                                    {
                                        command = "WS",
                                        exchange = BMLogger.exchange_name,
                                        symbol = symbol,
                                        stream = _selector.table,
                                        action = _selector.action,
                                        payload = _selector.data.ToString(Formatting.None)
                                    });

                                    BMLogger.WriteO(_json);

                                    break;
                                }
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
                            if (_cws.State != WebSocketState.Open && _cws.State != WebSocketState.Connecting)
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

                BMLogger.WriteO($"pushing service stopped: symbol => {symbol}...");
            }
        }
    }
}