﻿using CCXT.Collector.BitMEX.Public;
using CCXT.Collector.Library;
using CCXT.Collector.Library.Types;
using Newtonsoft.Json;
using OdinSdk.BaseLib.Coin.Types;
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

                        if (_message.command == "WS")
                        {
                            if (_message.stream == "trade")
                            {
                                var _w_trades = JsonConvert.DeserializeObject<List<BCompleteOrderItem>>(_message.payload);

                                var _s_trade = new SCompleteOrder
                                {
                                    exchange = _message.exchange,
                                    stream = _message.stream,
                                    symbol = _message.symbol,
                                    sequentialId = _w_trades.Max(t => t.timestamp),
                                    result = _w_trades.Select(t =>
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

                                await mergeTradeItems(_s_trade);

                                if (KConfig.UsePublishTrade == true)
                                    await publishTrading(_s_trade);
                            }
                            else if (_message.stream == "orderbook")
                            {
                                var _w_orderbooks = JsonConvert.DeserializeObject<List<BOrderBookItem>>(_message.payload);

                                var _timestamp = _w_orderbooks.Max(o => o.id);
                                var _asks = _w_orderbooks.Where(o => o.sideType == SideType.Ask);
                                var _bids = _w_orderbooks.Where(o => o.sideType == SideType.Bid);

                                var _s_orderbook = new SOrderBook
                                {
                                    timestamp = _timestamp,
                                    askSumQty = _asks.Sum(o => o.quantity),
                                    bidSumQty = _bids.Sum(o => o.quantity),
                                    asks = _asks.Select(o =>
                                    {
                                        return new SOrderBookItem
                                        {
                                            quantity = o.quantity,
                                            price = o.price,
                                            amount = o.quantity * o.price,
                                            count = 1
                                        };
                                    }).ToList(),
                                    bids = _bids.Select(o =>
                                    {
                                        return new SOrderBookItem
                                        {
                                            quantity = o.quantity,
                                            price = o.price,
                                            amount = o.quantity * o.price,
                                            count = 1
                                        };
                                    }).ToList()
                                };

                                await mergeOrderbook(_s_orderbook, _message.exchange, _message.symbol);
                            }
                        }
                        else if (_message.command == "AP")
                        {
                            if (_message.stream == "trade")
                            {
                                var _a_trades = JsonConvert.DeserializeObject<List<BCompleteOrderItem>>(_message.payload);

                                var _s_trade = new SCompleteOrder
                                {
                                    exchange = _message.exchange,
                                    stream = _message.stream,
                                    symbol = _message.symbol,
                                    sequentialId = _a_trades.Max(t => t.timestamp),
                                    result = _a_trades.Select(t =>
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

                                await mergeTradeItems(_s_trade);
                            }
                            else if (_message.stream == "orderbook")
                            {
                                var _a_orderbooks = JsonConvert.DeserializeObject<List<BOrderBookItem>>(_message.payload);

                                var _timestamp = _a_orderbooks.Max(o => o.id);
                                var _asks = _a_orderbooks.Where(o => o.sideType == SideType.Ask);
                                var _bids = _a_orderbooks.Where(o => o.sideType == SideType.Bid);

                                var _s_orderbook = new SOrderBook
                                {
                                    timestamp = _timestamp,
                                    askSumQty = _asks.Sum(o => o.quantity),
                                    bidSumQty = _bids.Sum(o => o.quantity),
                                    asks = _asks.Select(o =>
                                    {
                                        return new SOrderBookItem
                                        {
                                            quantity = o.quantity,
                                            price = o.price,
                                            amount = o.quantity * o.price,
                                            count = 1
                                        };
                                    }).ToList(),
                                    bids = _bids.Select(o =>
                                    {
                                        return new SOrderBookItem
                                        {
                                            quantity = o.quantity,
                                            price = o.price,
                                            amount = o.quantity * o.price,
                                            count = 1
                                        };
                                    }).ToList()
                                };

                                await mergeOrderbook(_s_orderbook, _message.exchange, _message.symbol);
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