using CCXT.Collector.Binance.Public;
using CCXT.Collector.Binance.Types;
using CCXT.Collector.Library.Types;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace CCXT.Collector.Binance
{
    public partial class Processing
    {
        private static ConcurrentQueue<QMessage> __recv_queue = null;

        /// <summary>
        ///
        /// </summary>
        private static ConcurrentQueue<QMessage> ReceiveQ
        {
            get
            {
                if (__recv_queue == null)
                    __recv_queue = new ConcurrentQueue<QMessage>();

                return __recv_queue;
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="message"></param>
        public static void SendReceiveQ(QMessage message)
        {
            ReceiveQ.Enqueue(message);
        }

        public async Task Start(CancellationTokenSource tokenSource)
        {
            BNLogger.WriteO($"processing service start...");

            var _processing = Task.Run(async () =>
            {
                while (true)
                {
                    try
                    {
                        await Task.Delay(0);

                        var _message = (QMessage)null;
                        if (ReceiveQ.TryDequeue(out _message) == false)
                        {
                            var _cancelled = tokenSource.Token.WaitHandle.WaitOne(0);
                            if (_cancelled == true)
                                break;

                            await Task.Delay(10);
                            continue;
                        }

                        var _json_data = JsonConvert.DeserializeObject<QSelector>(_message.json);
                        if (_message.command == "WS")
                        {
                            var _stream = _json_data.stream.Split('@');
                            if (_stream.Length > 1)
                            {
                                if (_stream[1] == "aggTrade")
                                {
                                    var _trade = JsonConvert.DeserializeObject<BWTrade>(_message.json);
                                    await mergeTradeItem(_trade.data);
                                }
                                //else if (_stream[1] == "depth")
                                //{
                                //    var _orderbook = JsonConvert.DeserializeObject<BWOrderBook>(_message.json);
                                //    await compareOrderbook(_orderbook);
                                //}
                            }
                        }
                        else if (_message.command == "AP")
                        {
                            if (_json_data.stream == "trades")
                            {
                                var _trades = JsonConvert.DeserializeObject<BATrade>(_message.json);
                                await mergeTradeItems(_trades);
                            }
                            else if (_json_data.stream == "arderbook")
                            {
                                var _orderbook = JsonConvert.DeserializeObject<BAOrderBook>(_message.json);
                                await mergeOrderbook(_orderbook);
                            }
                            else if (_json_data.stream == "bookticker")
                            {
                                var _bookticker = JsonConvert.DeserializeObject<SBookTickers>(_message.json);
                                await publishBookticker(_bookticker);
                            }
                        }
                        else if (_message.command == "SS")
                        {
                            _json_data.type = _message.command;
                            await snapshotOrderbook(_json_data.exchange, _json_data.symbol);
                        }
#if DEBUG
                        else
                            BNLogger.WriteO(_message.json);
#endif
                        if (tokenSource.IsCancellationRequested == true)
                            break;
                    }
                    catch (TaskCanceledException)
                    {
                    }
                    catch (Exception ex)
                    {
                        BNLogger.WriteX(ex.ToString());
                    }
                }
            },
            tokenSource.Token
            );

            await Task.WhenAll(_processing);

            BNLogger.WriteO($"processing service stopped...");
        }
    }
}