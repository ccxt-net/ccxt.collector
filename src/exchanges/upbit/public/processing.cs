using CCXT.Collector.Library.Types;
using CCXT.Collector.Upbit.Types;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace CCXT.Collector.Upbit.Orderbook
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
            UPLogger.WriteO($"processing service start...");

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
                            if (_json_data.type == "trade")
                            {
                                var _trade = JsonConvert.DeserializeObject<UWTradeItem>(_message.json);
                                await mergeTradeItem(_trade);
                            }
                            else if (_json_data.type == "orderbook")
                            {
                                var _orderbook = JsonConvert.DeserializeObject<UWOrderBook>(_message.json);
                                await mergeOrderbook(_orderbook);
                            }
                        }
                        else if (_message.command == "AP")
                        {
                            if (_json_data.type == "trades")
                            {
                                var _trades = JsonConvert.DeserializeObject<UATrade>(_message.json);
                                await mergeTradeItems(_trades);
                            }
                            else if (_json_data.type == "orderbooks")
                            {
                                var _orderbook = JsonConvert.DeserializeObject<UAOrderBook>(_message.json);
                                await mergeOrderbook(_orderbook);
                            }
                            else if (_json_data.stream == "bookticker")
                            {
                                var _bookticker = JsonConvert.DeserializeObject<SBookTicker>(_message.json);
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
                            UPLogger.WriteO(_message.json);
#endif
                        if (tokenSource.IsCancellationRequested == true)
                            break;
                    }
                    catch (TaskCanceledException)
                    {
                    }
                    catch (Exception ex)
                    {
                        UPLogger.WriteX(ex.ToString());
                    }
                }
            },
            tokenSource.Token
            );

            await Task.WhenAll(_processing);

            UPLogger.WriteO($"processing service stopped...");
        }
    }
}