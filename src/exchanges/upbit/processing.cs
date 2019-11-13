﻿using CCXT.Collector.Library;
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
                                var _w_trade = JsonConvert.DeserializeObject<UWCompleteOrderItem>(_message.payload);

                                var _s_trade = new SCompleteOrder
                                {
                                    exchange = _message.exchange,
                                    stream = _message.stream,
                                    symbol = _w_trade.symbol,
                                    sequentialId = _w_trade.sequential_id,
                                    result = new List<ISCompleteOrderItem>
                                    {
                                        new SCompleteOrderItem
                                        {
                                            timestamp = _w_trade.timestamp,
                                            sideType = _w_trade.sideType,
                                            price = _w_trade.price,
                                            quantity = _w_trade.quantity
                                        }
                                    }
                                };

                                await mergeTradeItems(_s_trade);

                                if (KConfig.UsePublishTrade == true)
                                    await publishTrading(_s_trade);
                            }
                            else if (_message.stream == "orderbook")
                            {
                                var _w_orderbook = JsonConvert.DeserializeObject<UWOrderBook>(_message.payload);

                                var _s_orderbook = new SOrderBook
                                {
                                    timestamp = _w_orderbook.timestamp,
                                    askSumQty = _w_orderbook.askSumQty,
                                    bidSumQty = _w_orderbook.bidSumQty,
                                    asks = _w_orderbook.asks,
                                    bids = _w_orderbook.bids
                                };

                                await mergeOrderbook(_s_orderbook, _message.exchange, _message.symbol);
                            }
                        }
                        else if (_message.command == "AP")
                        {
                            if (_message.stream == "trade")
                            {
                                var _a_trades = JsonConvert.DeserializeObject<List<UACompleteOrderItem>>(_message.payload);

                                var _s_trade = new SCompleteOrder
                                {
                                    exchange = _message.exchange,
                                    stream = _message.stream,
                                    symbol = _message.symbol,
                                    sequentialId = _a_trades.Max(t => t.sequential_id),
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
                                var _a_orderbooks = JsonConvert.DeserializeObject<List<UAOrderBook>>(_message.payload);

                                var _timestamp = _a_orderbooks.Max(o => o.timestamp);
                                var _asks = _a_orderbooks[0].asks;
                                var _bids = _a_orderbooks[0].bids;

                                var _s_orderbook = new SOrderBook
                                {
                                    timestamp = _timestamp,
                                    askSumQty = _asks.Sum(o => o.quantity),
                                    bidSumQty = _bids.Sum(o => o.quantity),
                                    asks = _asks,
                                    bids = _bids
                                };

                                await mergeOrderbook(_s_orderbook, _message.exchange, _message.symbol);
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