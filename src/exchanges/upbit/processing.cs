using CCXT.Collector.Library;
using CCXT.Collector.Library.Types;
using CCXT.Collector.Upbit.Public;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CCXT.Collector.Upbit
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

                        if (_message.command == "WS")
                        {
                            if (_message.stream == "trade")
                            {
                                var _trade = JsonConvert.DeserializeObject<UWCompleteOrder>(_message.json);
                                await mergeTradeItem(_trade);
                            }
                            else if (_message.stream == "orderbook")
                            {
                                var _orderbook = JsonConvert.DeserializeObject<UWOrderBook>(_message.json);
                                await mergeOrderbook(_orderbook);
                            }
                        }
                        else if (_message.command == "AP")
                        {
                            if (_message.stream == "trade")
                            {
                                var _t_json_data = JsonConvert.DeserializeObject<List<UACompleteOrder>>(_message.json);
                                
                                await mergeTradeItems(new SCompleteOrders
                                {
                                    exchange = _message.exchange,
                                    stream = _message.stream,
                                    symbol = _message.symbol,
                                    data = _t_json_data.ToList<SCompleteOrder>()
                                });
                            }
                            else if (_message.stream == "orderbook")
                            {
                                var _o_json_data = JsonConvert.DeserializeObject<List<UAOrderBook>>(_message.json);
                                _o_json_data[0].type = _message.stream;
                                await mergeOrderbook(_o_json_data[0]);
                            }
                            else if (_message.stream == "ticker")
                            {
                                var _b_json_data = JsonConvert.DeserializeObject<List<UAOrderBook>>(_message.json);

                                await publishTicker(new STickers
                                {
                                    exchange = _message.exchange,
                                    stream = _message.stream,
                                    sequential_id = _message.sequential_id,
                                    data = _b_json_data.Select(o =>
                                    {
                                        var _ask = o.asks.OrderBy(a => a.price).First();
                                        var _bid = o.bids.OrderBy(a => a.price).Last();

                                        return new STickerItem
                                        {
                                            symbol = o.symbol,
                                            askPrice = _ask.price,
                                            askQuantity = _ask.quantity,
                                            bidPrice = _bid.price,
                                            bidQuantity = _bid.quantity
                                        };
                                    })
                                    .ToList()
                                });
                            }
                        }
                        else if (_message.command == "SS")
                        {
                            await snapshotOrderbook(_message.exchange, _message.symbol);
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