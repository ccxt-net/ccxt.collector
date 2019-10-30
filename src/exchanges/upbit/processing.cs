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
                                var _wt = JsonConvert.DeserializeObject<UWCompleteOrderItem>(_message.json);

                                var _trades = new SCompleteOrders
                                {
                                    exchange = _message.exchange,
                                    stream = $"{_message.command.ToLower()}.{_message.stream.ToLower()}",
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

                                if (KConfig.UpbitUsePublishTrade == true)
                                    await publishTrading(_trades);
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
                                var _a_trade_data = JsonConvert.DeserializeObject<List<UACompleteOrderItem>>(_message.json);

                                var _trades = new SCompleteOrders
                                {
                                    exchange = _message.exchange,
                                    stream = $"{_message.command.ToLower()}.{_message.stream.ToLower()}",
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
                                var _a_book_data = JsonConvert.DeserializeObject<List<UAOrderBook>>(_message.json);
                                _a_book_data[0].type = _message.stream;
                                await mergeOrderbook(_a_book_data[0]);
                            }
                            else if (_message.stream == "ticker")
                            {
                                var _a_ticker_data = JsonConvert.DeserializeObject<List<UAOrderBook>>(_message.json);

                                await publishTicker(new STickers
                                {
                                    exchange = _message.exchange,
                                    stream = $"{_message.command.ToLower()}.{_message.stream.ToLower()}",
                                    symbol = _message.symbol,
                                    sequentialId = _message.sequentialId,
                                    result = _a_ticker_data.Select(o =>
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
                                    .ToList<ISTickerItem>()
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