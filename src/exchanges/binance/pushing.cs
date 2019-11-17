﻿using CCXT.Collector.Library;
using Newtonsoft.Json;
using OdinSdk.BaseLib.Configuration;
using System;
using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CCXT.Collector.Binance
{
    /*
     * 1. wss://stream.binance.com:9443/ws/bnbbtc@depth 스트림을 엽니다.
     * 2. 스트림으로 부터 이벤트를 받아 버퍼링 합니다.
     * 3. 스냅 샷은 https://www.binance.com/api/v1/depth?symbol=BNBBTC&limit=1000 에서 가져옵니다.
     * 4. 스냅 샷에서 u <= lastUpdateId 인 이벤트는 삭제 합니다.
     * 5. 첫 처리시 U <= lastUpdateId + 1 AND u >= lastUpdateId + 1 이어야 합니다.
     * 6. 스트림을 수신하는 동안, 새로 도착하는 이벤트의 U는 이전에 수신한 이벤트의 u + 1과 동일해야합니다.
     * 7. 각 이벤트의 데이터 수량은 가격에 대한 절대량입니다.
     * 8. 수량이 0 인 경우 가격을 제거합니다.
     * 9. Local orderbook에 없는 가격을 제거하라는 이벤트를 수신 할수 있습니다.
     */

    /// <summary>
    ///
    /// </summary>
    public partial class Pushing
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

        public async Task Start(CancellationTokenSource tokenSource, string symbol)
        {
            BNLogger.WriteO($"pushing service start: symbol => {symbol}...");

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

                                BNLogger.WriteO($"pushing open: symbol => {symbol}...");
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
                            BNLogger.WriteX(ex.ToString());
                        }
                        //finally
                        {
                            if (_cws.State != WebSocketState.Open)
                            {
                                BNLogger.WriteO($"disconnect from server(cmd): symbol => {symbol}...");
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
                                var _json = Encoding.UTF8.GetString(_buffer, 0, _offset);
                                var _selector = JsonConvert.DeserializeObject<QSelector>(_json);
                                
                                var _stream = _selector.stream.Split('@')[1];
                                if (_stream == "aggTrade")
                                    _stream = "trade";
                                else if (_stream == "depth")
                                    _stream = "orderbook";

                                Processing.SendReceiveQ(new QMessage 
                                { 
                                    command = "WS", 
                                    stream = _stream,
                                    payload = _json 
                                });
                            }
                            else if (_result.MessageType == WebSocketMessageType.Binary)
                            {
                            }
                            else if (_result.MessageType == WebSocketMessageType.Close)
                            {
                                await _cws.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "", tokenSource.Token);

                                BNLogger.WriteO($"receive close message from server: symbol => {symbol}...");
                                tokenSource.Cancel();
                                break;
                            }
                        }
                        catch (TaskCanceledException)
                        {
                        }
                        catch (Exception ex)
                        {
                            BNLogger.WriteX(ex.ToString());
                        }
                        //finally
                        {
                            if (_cws.State != WebSocketState.Open)
                            {
                                BNLogger.WriteO($"disconnect from server: symbol => {symbol}...");
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

                BNLogger.WriteO($"pushing service stopped: symbol => {symbol}...");
            }
        }
    }
}