using CCXT.Collector.Deribit.Model;
using CCXT.Collector.Deribit.Public;
using CCXT.Collector.Library;
using CCXT.Collector.Service;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CCXT.Collector.Deribit
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
        
        private readonly DRConfig __drconfig;

        public Processing(IConfiguration configuration)
        {
            __drconfig = new DRConfig(configuration);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="message"></param>
        public static void SendReceiveQ(QMessage message)
        {
            ReceiveQ.Enqueue(message);
        }

        public async Task Start(CancellationTokenSource cancelTokenSource)
        {
            DRLogger.SNG.WriteO(this, $"processing service start...");

            var _processing = Task.Run(async () =>
            {
                var _last_polling_trade = 0L;
                var _orderbook_size = 25;

                while (true)
                {
                    try
                    {
                        await Task.Delay(0);

                        var _message = (QMessage)null;
                        if (ReceiveQ.TryDequeue(out _message) == false)
                        {
                            var _cancelled = cancelTokenSource.Token.WaitHandle.WaitOne(0);
                            if (_cancelled == true)
                                break;

                            await Task.Delay(10);
                            continue;
                        }

                        if (_message.command == "WS")
                        {
                            if (_message.stream == "trade")
                            {
                                var _w_trades = JsonConvert.DeserializeObject<List<DCompleteOrderItem>>(_message.payload ?? "");

                                var _s_trades = new SCompleteOrders
                                {
                                    exchange = _message.exchange,   // deribit
                                    stream = _message.stream,       // trade
                                    symbol = _message.symbol,       // BTC-PERPETUAL
                                    action = _message.action,       // pushing

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
                                    .ToList()
                                };

                                if (_s_trades.result.Count() > 0)
                                {
                                    _last_polling_trade = _s_trades.sequentialId;
                                    await mergeTrades(_s_trades);
                                }
                            }
                            else if (_message.stream == "orderbook")
                            {
                                var _w_orderbooks = JsonConvert.DeserializeObject<DWsOrderBook>(_message.payload ?? "");
                                if (_w_orderbooks.asks.Count() > 0 || _w_orderbooks.bids.Count() > 0)
                                {
                                    var _timestamp = _w_orderbooks.timestamp;

                                    var _s_orderbooks = new SOrderBooks
                                    {
                                        exchange = _message.exchange,       // deribit
                                        symbol = _message.symbol,           // BTC-PERPETUAL
                                        stream = _message.stream,           // orderbook
                                        action = _w_orderbooks.type,        // snapshot, change

                                        sequentialId = _timestamp,

                                        result = new SOrderBook
                                        {
                                            timestamp = _timestamp,

                                            askSumQty = 0,
                                            bidSumQty = 0,

                                            asks = new List<SOrderBookItem>(),
                                            bids = new List<SOrderBookItem>()
                                        }
                                    };

                                    foreach (var _a in _w_orderbooks.asks.OrderBy(a => a[1]).Take(_orderbook_size))
                                        _s_orderbooks.result.asks.Add(new SOrderBookItem
                                        {
                                            action = _a[0].ToString(),
                                            quantity = Convert.ToDecimal(_a[2]),
                                            price = Convert.ToDecimal(_a[1]),
                                            amount = 0,
                                            id = 0,
                                            count = 1
                                        });

                                    foreach (var _b in _w_orderbooks.bids.OrderByDescending(b => b[1]).Take(_orderbook_size))
                                        _s_orderbooks.result.bids.Add(new SOrderBookItem
                                        {
                                            action = _b[0].ToString(),
                                            quantity = Convert.ToDecimal(_b[2]),
                                            price = Convert.ToDecimal(_b[1]),
                                            amount = 0,
                                            id = 0,
                                            count = 1
                                        });

                                    await mergeOrderbooks(_s_orderbooks);
                                }
                            }
                        }
                        else if (_message.command == "AP")
                        {
                            if (_message.stream == "trade")
                            {
                                var _a_trades = JsonConvert.DeserializeObject<DRResults<DCompleteOrders>>(_message.payload ?? "");
                                if (_a_trades.result.trades.Count > 0)
                                {
                                    var _s_trades = new SCompleteOrders
                                    {
                                        exchange = _message.exchange,   // deribit
                                        symbol = _message.symbol,       // BTC-PERPETUAL
                                        stream = _message.stream,       // trade
                                        action = _message.action,       // polling

                                        sequentialId = _a_trades.result.trades.Max(t => t.timestamp),

                                        result = _a_trades.result.trades.Where(t => t.timestamp > _last_polling_trade).Select(t =>
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

                                    if (_s_trades.result.Count() > 0)
                                    {
                                        _last_polling_trade = _s_trades.sequentialId;
                                        await mergeTrades(_s_trades);
                                    }
                                }
                            }
                            else if (_message.stream == "orderbook")
                            {
                                var _a_orderbooks = JsonConvert.DeserializeObject<DRResults<DOrderBook>>(_message.payload ?? "");
                                if (_a_orderbooks.result.asks.Count > 0 || _a_orderbooks.result.bids.Count > 0)
                                {
                                    var _timestamp = _a_orderbooks.result.timestamp;

                                    var _s_orderbooks = new SOrderBooks
                                    {
                                        exchange = _message.exchange,   // deribit
                                        symbol = _message.symbol,       // BTC-PERPETUAL
                                        stream = _message.stream,       // orderbook
                                        action = _message.action,       // polling

                                        sequentialId = _timestamp,

                                        result = new SOrderBook
                                        {
                                            timestamp = _timestamp,

                                            askSumQty = 0,
                                            bidSumQty = 0,

                                            asks = _a_orderbooks.result.asks.Select(o =>
                                            {
                                                return new SOrderBookItem
                                                {
                                                    quantity = o.quantity,
                                                    price = o.price,
                                                    amount = 0,
                                                    id = 0,
                                                    count = 1
                                                };
                                            })
                                            .ToList(),

                                            bids = _a_orderbooks.result.bids.Select(o =>
                                            {
                                                return new SOrderBookItem
                                                {
                                                    quantity = o.quantity,
                                                    price = o.price,
                                                    amount = 0,
                                                    id = 0,
                                                    count = 1
                                                };
                                            })
                                            .ToList()
                                        }
                                    };

                                    await mergeOrderbooks(_s_orderbooks);
                                }
                            }
                        }
                        else if (_message.command == "SS")
                        {
                            await snapshotOrderbook(_message.exchange);
                        }
#if DEBUG
                        else
                            DRLogger.SNG.WriteO(this, _message.payload);
#endif
                        if (cancelTokenSource.Token.IsCancellationRequested == true)
                            break;
                    }
                    catch (TaskCanceledException)
                    {
                    }
                    catch (Exception ex)
                    {
                        DRLogger.SNG.WriteX(this, ex.ToString());
                    }
                }
            },
            cancelTokenSource.Token
            );

            await Task.WhenAll(_processing);

            DRLogger.SNG.WriteO(this, $"processing service stop...");
        }
    }
}