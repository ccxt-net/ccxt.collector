using CCXT.Collector.BitMEX.Public;
using CCXT.Collector.Library;
using CCXT.Collector.Library.Types;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CCXT.Collector.BitMEX
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
            BMLogger.WriteO($"processing service start...");

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

                        var _json_data = JsonConvert.DeserializeObject<QSelector>(_message.payload);
                        if (_message.command == "WS")
                        {
                            var _stream = _json_data.stream.Split('@');
                            if (_stream.Length > 1)
                            {
                                //if (_stream[1] == "aggTrade")
                                //{
                                //    var _trade = JsonConvert.DeserializeObject<BWTrade>(_message.json);
                                //    await mergeTradeItem(_trade.data);
                                //}
                                //else if (_stream[1] == "depth")
                                //{
                                //    var _orderbook = JsonConvert.DeserializeObject<BWOrderBook>(_message.json);
                                //    await compareOrderbook(_orderbook);
                                //}
                            }
                        }
                        else if (_message.command == "AP")
                        {
                            if (_json_data.stream == "trade")
                            {
                                var _a_trade_data = JsonConvert.DeserializeObject<List<BCompleteOrderItem>>(_message.payload);

                                var _trades = new SCompleteOrders
                                {
                                    exchange = _message.exchange,
                                    stream = _message.stream,
                                    symbol = _message.symbol,
                                    sequentialId = _a_trade_data.Max(t => t.timestamp),
                                    result = _a_trade_data.Select(t =>
                                    {
                                        return new SCompleteOrderItem
                                        {
                                            timestamp = t.timestamp,
                                            sideType = t.sideType,
                                            price = t.price,
                                            quantity = t.quantity
                                        };
                                    })
                                    .ToList<ISCompleteOrderItem>()
                                };

                                _trades.SetSuccess();

                                await mergeTradeItems(_trades);
                            }
                            else if (_json_data.stream == "orderbook")
                            {
                                var _a_book_data = JsonConvert.DeserializeObject<BOrderBook>(_message.payload);

                                var _orderbook = new SOrderBook
                                {
                                    timestamp = _a_book_data.data.timestamp,
                                    askSumQty = _a_book_data.data.askSumQty,
                                    bidSumQty = _a_book_data.data.bidSumQty,
                                    asks = _a_book_data.data.asks,
                                    bids = _a_book_data.data.bids
                                };

                                await mergeOrderbook(_orderbook, _message.exchange, _message.symbol);
                            }
                        }
                        else if (_message.command == "SS")
                        {
                            await snapshotOrderbook(_message.exchange);
                        }
#if DEBUG
                        else
                            BMLogger.WriteO(_message.payload);
#endif
                        if (tokenSource.IsCancellationRequested == true)
                            break;
                    }
                    catch (TaskCanceledException)
                    {
                    }
                    catch (Exception ex)
                    {
                        BMLogger.WriteX(ex.ToString());
                    }
                }
            },
           tokenSource.Token
           );

            await Task.WhenAll(_processing);

            BMLogger.WriteO($"processing service stopped...");
        }
    }
}