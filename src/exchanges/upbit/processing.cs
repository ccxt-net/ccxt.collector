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
                                var _wt = JsonConvert.DeserializeObject<UWCompleteOrderItem>(_message.payload);

                                var _trades = new SCompleteOrders
                                {
                                    exchange = _message.exchange,
                                    stream = _message.stream,
                                    symbol = _wt.symbol,
                                    sequentialId = _wt.sequential_id,
                                    result = new List<ISCompleteOrderItem>
                                    {
                                        new SCompleteOrderItem
                                        {
                                            timestamp = _wt.timestamp,
                                            sideType = _wt.sideType,
                                            price = _wt.price,
                                            quantity = _wt.quantity
                                        }
                                    }
                                };

                                _trades.SetSuccess();

                                await mergeTradeItems(_trades);

                                if (KConfig.UsePublishTrade == true)
                                    await publishTrading(_trades);
                            }
                            else if (_message.stream == "orderbook")
                            {
                                var _w_book_data = JsonConvert.DeserializeObject<UWOrderBook>(_message.payload);

                                var _orderbook = new SOrderBook
                                {
                                    timestamp = _w_book_data.timestamp,
                                    askSumQty = _w_book_data.askSumQty,
                                    bidSumQty = _w_book_data.bidSumQty,
                                    asks = _w_book_data.asks,
                                    bids = _w_book_data.bids
                                };

                                await mergeOrderbook(_orderbook, _message.exchange, _message.symbol);
                            }
                        }
                        else if (_message.command == "AP")
                        {
                            if (_message.stream == "trade")
                            {
                                var _a_trade_data = JsonConvert.DeserializeObject<List<UACompleteOrderItem>>(_message.payload);

                                var _trades = new SCompleteOrders
                                {
                                    exchange = _message.exchange,
                                    stream = _message.stream,
                                    symbol = _message.symbol,
                                    sequentialId = _a_trade_data.Max(t => t.sequential_id),
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
                            else if (_message.stream == "orderbook")
                            {
                                var _a_book_data = JsonConvert.DeserializeObject<List<UAOrderBook>>(_message.payload);

                                var _orderbook = new SOrderBook
                                {
                                    timestamp = _a_book_data[0].timestamp,
                                    askSumQty = _a_book_data[0].askSumQty,
                                    bidSumQty = _a_book_data[0].bidSumQty,
                                    asks = _a_book_data[0].asks,
                                    bids = _a_book_data[0].bids
                                };

                                await mergeOrderbook(_orderbook, _message.exchange, _message.symbol);
                            }
                            else if (_message.stream == "ticker")
                            {
                                var _a_ticker_data = JsonConvert.DeserializeObject<List<UAOrderBook>>(_message.payload);

                                await publishTicker(new STickers
                                {
                                    exchange = _message.exchange,
                                    stream = _message.stream,
                                    symbol = _message.symbol,
                                    sequentialId = _message.sequentialId,
                                    result = _a_ticker_data.Select(o =>
                                    {
                                        var _ask = o.asks.OrderBy(a => a.price).First();
                                        var _bid = o.bids.OrderBy(a => a.price).Last();

                                        return new STickerItem
                                        {
                                            symbol = _message.symbol,
                                            askPrice = _ask.price,
                                            askQuantity = _ask.quantity,
                                            bidPrice = _bid.price,
                                            bidQuantity = _bid.quantity
                                        };
                                    })
                                    .ToList<ISTickerItem>()
                                });
                            }
                        }
                        else if (_message.command == "SS")
                        {
                            await snapshotOrderbook(_message.symbol);
                        }
#if DEBUG
                        else
                            UPLogger.WriteO(_message.payload);
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