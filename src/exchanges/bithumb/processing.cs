using CCXT.Collector.Library;
using CCXT.Collector.Service;
using CCXT.Collector.Upbit.Public;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CCXT.Collector.Bithumb
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

        private readonly BTConfig __btconfig;

        public Processing(IConfiguration configuration)
        {
            __btconfig = new BTConfig(configuration);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="message"></param>
        public static void SendReceiveQ(QMessage message)
        {
            ReceiveQ.Enqueue(message);
        }

        public async ValueTask Start(CancellationToken cancelToken)
        {
            BTLogger.SNG.WriteO(this, $"processing service start...");

            var _processing = Task.Run(async () =>
            {
                var _last_polling_trade = 0L;
                
                while (true)
                {
                    try
                    {
                        await Task.Delay(0);

                        var _message = (QMessage)null;
                        if (ReceiveQ.TryDequeue(out _message) == false)
                        {
                            var _cancelled = cancelToken.WaitHandle.WaitOne(0);
                            if (_cancelled == true)
                                break;

                            await Task.Delay(10);
                            continue;
                        }

                        if (_message.command == "WS")
                        {
                            if (_message.stream == "trade")
                            {
                                var _w_trade = JsonConvert.DeserializeObject<UWCompleteOrderItem>(_message.payload ?? "");

                                var _s_trade = new SCompleteOrders
                                {
                                    exchange = _message.exchange,
                                    symbol = _message.symbol,
                                    stream = _message.stream,
                                    action = _message.action,
                                    sequentialId = _w_trade.timestamp,

                                    result = new List<SCompleteOrderItem>
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

                                if (_s_trade.result.Count() > 0)
                                {
                                    _last_polling_trade = _s_trade.sequentialId;
                                    await mergeCompleteOrder(_s_trade);
                                }
                            }
                            else if (_message.stream == "orderbook")
                            {
                                var _w_orderbook = JsonConvert.DeserializeObject<UWOrderBook>(_message.payload ?? "");

                                var _s_orderbooks = new SOrderBooks
                                {
                                    exchange = _message.exchange,
                                    symbol = _message.symbol,
                                    stream = _message.stream,
                                    action = _message.action,
                                    sequentialId = _w_orderbook.timestamp,

                                    result = new SOrderBook
                                    {
                                        timestamp = _w_orderbook.timestamp,
                                        
                                        askSumQty = _w_orderbook.askSumQty,
                                        bidSumQty = _w_orderbook.bidSumQty,
                                     
                                        asks = _w_orderbook.asks,
                                        bids = _w_orderbook.bids
                                    }
                                };

                                await mergeOrderbook(_s_orderbooks);
                            }
                        }
                        else if (_message.command == "AP")
                        {
                            if (_message.stream == "trade")
                            {
                                var _a_trades = JsonConvert.DeserializeObject<List<UACompleteOrderItem>>(_message.payload ?? "");

                                var _s_trade = new SCompleteOrders
                                {
                                    exchange = _message.exchange,
                                    symbol = _message.symbol,
                                    stream = _message.stream,
                                    action = _message.action,
                                    sequentialId = _a_trades.Max(t => t.timestamp),

                                    result = _a_trades.Where(t => t.timestamp > _last_polling_trade).Select(t => 
                                    {
                                        return new SCompleteOrderItem
                                        {
                                            timestamp = t.timestamp,
                                            sideType = t.sideType,
                                            price = t.price,
                                            quantity = t.quantity
                                        };
                                    })
                                    .ToList()
                                };

                                if (_s_trade.result.Count() > 0)
                                {
                                    _last_polling_trade = _s_trade.sequentialId;
                                    await mergeCompleteOrder(_s_trade);
                                }
                            }
                            else if (_message.stream == "orderbook")
                            {
                                var _a_orderbooks = JsonConvert.DeserializeObject<List<UAOrderBook>>(_message.payload ?? "");

                                var _timestamp = _a_orderbooks.Max(o => o.timestamp);
                                var _asks = _a_orderbooks[0].asks;
                                var _bids = _a_orderbooks[0].bids;

                                var _s_orderbooks = new SOrderBooks
                                {
                                    exchange = _message.exchange,
                                    symbol = _message.symbol,
                                    stream = _message.stream,
                                    action = _message.action,
                                    sequentialId = _a_orderbooks.Max(t => t.timestamp),

                                    result = new SOrderBook
                                    {
                                        timestamp = _timestamp,
                                        
                                        askSumQty = _asks.Sum(o => o.quantity),
                                        bidSumQty = _bids.Sum(o => o.quantity),
                                     
                                        asks = _asks,
                                        bids = _bids
                                    }
                                };

                                await mergeOrderbook(_s_orderbooks);
                            }
                            else if (_message.stream == "ticker")
                            {
                                var _a_ticker_data = JsonConvert.DeserializeObject<List<UAOrderBook>>(_message.payload ?? "");

                                await publishTicker(new STickers
                                {
                                    exchange = _message.exchange,
                                    symbol = _message.symbol,
                                    stream = _message.stream,
                                    action = _message.action,
                                    sequentialId = _message.sequentialId,
                                    
                                    timestamp = _a_ticker_data.Max(o => o.timestamp),
                                    totalAskSize = _a_ticker_data.Sum(o => o.askSumQty),
                                    totalBidSize = _a_ticker_data.Sum(o => o.bidSumQty),

                                    result = _a_ticker_data.Select(o =>
                                    {
                                        var _ask = o.asks.OrderBy(a => a.price).First();
                                        var _bid = o.bids.OrderBy(a => a.price).Last();

                                        return new STickerItem
                                        {
                                            askPrice = _ask.price,
                                            askSize = _ask.quantity,
                                            bidPrice = _bid.price,
                                            bidSize = _bid.quantity
                                        };
                                    })
                                    .ToList()
                                });
                            }
                        }
                        else if (_message.command == "SS")
                        {
                            await snapshotOrderbook(_message.symbol);
                        }
#if DEBUG
                        else
                            BTLogger.SNG.WriteO(this, _message.payload);
#endif
                        if (cancelToken.IsCancellationRequested == true)
                            break;
                    }
                    catch (TaskCanceledException)
                    {
                    }
                    catch (Exception ex)
                    {
                        BTLogger.SNG.WriteX(this, ex.ToString());
                    }
                }
            },
            cancelToken
            );

            await Task.WhenAll(_processing);

            BTLogger.SNG.WriteO(this, $"processing service stop...");
        }
    }
}