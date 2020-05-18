using CCXT.Collector.Library;
using CCXT.Collector.Service;
using Newtonsoft.Json;
using System.Collections.Concurrent;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace CCXT.Collector.Deribit
{
    public partial class Processing
    {
        private readonly CCOrderBook __orderbook = new CCOrderBook();
        private readonly CCTrading __trading = new CCTrading();
        private readonly CCTicker __ticker = new CCTicker();

        private static ConcurrentDictionary<string, SOrderBooks> __qOrderBooks = new ConcurrentDictionary<string, SOrderBooks>();
        private static ConcurrentDictionary<string, Settings> __qSettings = new ConcurrentDictionary<string, Settings>();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="cob"></param>
        /// <returns></returns>
        private async ValueTask<bool> mergeOrderbooks(SOrderBooks cob)
        {
            var _result = false;

            SOrderBooks _qob;
            {
                if (__qOrderBooks.TryGetValue(cob.symbol, out _qob) == true)
                {
                    _qob.stream = cob.stream;

                    var _settings = GetSettings(cob.symbol);
                    _result = await updateOrderbooks(_qob, cob, _settings);
                }
                else 
                {
                    if (cob.action == "snapshot" || cob.action == "polling")
                        _result = await insertOrderbooks(cob);
                }
            }

            return _result;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="qob">orderbooks on dictionary</param>
        /// <param name="cob">current new orderbooks</param>
        /// <param name="settings"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private async ValueTask<bool> updateOrderbooks(SOrderBooks qob, SOrderBooks cob, Settings settings)
        {
            var _nob = new SOrderBooks
            {
                exchange = cob.exchange,
                stream = "diffbooks",
                symbol = cob.symbol,
                action = cob.action,
                sequentialId = cob.sequentialId,

                result = new SOrderBook
                {
                    timestamp = cob.result.timestamp,

                    askSumQty = 0,
                    bidSumQty = 0
                }
            };

            lock (__qOrderBooks)
            {
                foreach (var _ca in cob.result.asks)
                {
                    var _qa = qob.result.asks.Where(o => o.price == _ca.price).SingleOrDefault();
                    if (_qa == null)
                    {
                        var _ia = new SOrderBookItem
                        {
                            action = "insert",
                            id = _ca.id,
                            price = _ca.price,
                            quantity = _ca.quantity,
                            amount = _ca.price * _ca.quantity,
                            count = 1
                        };

                        _nob.result.asks.Add(_ia);
                        qob.result.asks.Add(_ia);

                        _nob.result.askSumQty += _ca.quantity;
                    }
                    else if (_ca.quantity == 0)
                    {
                        _nob.result.asks.Add(new SOrderBookItem
                        {
                            action = "delete",
                            id = _qa.id,
                            price = _qa.price,
                            quantity = _qa.quantity,
                            amount = _qa.price * _qa.quantity,
                            count = 1
                        });

                        _nob.result.askSumQty -= _qa.quantity;

                        _qa.quantity = 0;
                        _qa.amount = 0;
                    }
                    else if (_qa.quantity != _ca.quantity)
                    {
                        _nob.result.asks.Add(new SOrderBookItem
                        {
                            action = "update",
                            id = _qa.id,
                            price = _qa.price,
                            quantity = _ca.quantity,
                            amount = _qa.price * _ca.quantity,
                            count = 1
                        });

                        _qa.quantity = _ca.quantity;
                        _qa.amount = _qa.price * _qa.quantity;

                        _nob.result.askSumQty += _ca.quantity;
                    }
                }

                foreach (var _cb in cob.result.bids)
                {
                    var _qb = qob.result.bids.Where(o => o.price == _cb.price).SingleOrDefault();
                    if (_qb == null)
                    {
                        var _ib = new SOrderBookItem
                        {
                            action = "insert",
                            id = _cb.id,
                            price = _cb.price,
                            quantity = _cb.quantity,
                            amount = _cb.price * _cb.quantity,
                            count = 1
                        };

                        _nob.result.bids.Add(_ib);
                        qob.result.bids.Add(_ib);

                        _nob.result.bidSumQty += _cb.quantity;
                    }
                    else if (_cb.quantity == 0)
                    {
                        _nob.result.bids.Add(new SOrderBookItem
                        {
                            action = "delete",
                            id = _qb.id,
                            price = _qb.price,
                            quantity = _qb.quantity,
                            amount = _qb.price * _qb.quantity,
                            count = 1
                        });

                        _nob.result.bidSumQty -= _qb.quantity;

                        _qb.quantity = 0;
                        _qb.amount = 0;
                    }
                    else if (_qb.quantity != _cb.quantity)
                    {
                        _nob.result.bids.Add(new SOrderBookItem
                        {
                            action = "update",
                            id = _qb.id,
                            price = _qb.price,
                            quantity = _cb.quantity,
                            amount = _qb.price * _cb.quantity,
                            count = 1
                        });

                        _qb.quantity = _cb.quantity;
                        _qb.amount = _qb.quantity * _qb.price;

                        _nob.result.bidSumQty += _cb.quantity;
                    }
                }

                qob.result.asks.RemoveAll(o => o.quantity == 0);
                qob.result.bids.RemoveAll(o => o.quantity == 0);
            }

            if (++settings.orderbook_count == __drconfig.SnapshotSkipCounter)
            {
                qob.sequentialId = cob.sequentialId;
                await snapshotOrderbook(_nob.symbol);
            }
            else 
            {
                _nob.result.asks.RemoveAll(o => o.quantity == 0);
                _nob.result.bids.RemoveAll(o => o.quantity == 0);

                if (_nob.result.asks.Count > 0 || _nob.result.bids.Count > 0)
                    await publishOrderbook(_nob);
            }

            return true;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="cob"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private async ValueTask<bool> insertOrderbooks(SOrderBooks cob)
        {
            var _result = false;

            cob.result.asks.ForEach(a => a.action = "insert");
            cob.result.bids.ForEach(a => a.action = "insert");

            if (__qOrderBooks.TryAdd(cob.symbol, cob) == true)
            {
                var _settings = GetSettings(cob.symbol);
                {
                    _settings.last_orderbook_time = cob.result.timestamp;
                    _settings.orderbook_count = 0;
                }

                await snapshotOrderbook(cob.symbol);
                _result = true;
            }

            return _result;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="cco"></param>
        /// <returns></returns>
        private async ValueTask<bool> mergeTrades(SCompleteOrders cco)
        {
            var _result = false;

            if (__drconfig.UsePublishTrade == true)
                await publishTrading(cco);

            SOrderBooks _qob;
            if (__qOrderBooks.TryGetValue(cco.symbol, out _qob) == true)
            {
                _qob.stream = cco.stream;

                if (cco.result != null)
                {
                    var _settings = GetSettings(cco.symbol);
                    _result = await updateTrades(_qob, cco, _settings);
                }
            }

            return _result;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="qob"></param>
        /// <param name="cco"></param>
        /// <param name="settings"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private async ValueTask<bool> updateTrades(SOrderBooks qob, SCompleteOrders cco, Settings settings)
        {
            var _nob = new SOrderBooks
            {
                exchange = cco.exchange,
                stream = "difftrade",
                symbol = cco.symbol,
                action = cco.action,
                sequentialId = cco.sequentialId,

                result = new SOrderBook
                {
                    timestamp = cco.result.Max(o => o.timestamp),

                    askSumQty = 0,
                    bidSumQty = 0
                }
            };

            lock (__qOrderBooks)
            {
                var _trades = cco.result.GroupBy(o => o.price,
                                            (price, items) => new
                                            {
                                                price,
                                                count = items.Count(),
                                                quantity = items.Sum(o => o.quantity),
                                                timestamp = items.Max(o => o.timestamp),
                                                items.First().sideType
                                            });

                foreach (var _t in _trades.OrderBy(t => t.timestamp))
                {
                    if (settings.last_trade_id >= _t.timestamp)
                        continue;

                    settings.last_trade_id = _t.timestamp;

                    var _ask = qob.result.asks.Where(o => o.price == _t.price).SingleOrDefault();
                    if (_ask != null)
                    {
                        if (_ask.quantity <= _t.quantity)
                        {
                            _nob.result.asks.Add(new SOrderBookItem
                            {
                                action = "delete",
                                id = _ask.id,
                                price = _ask.price,
                                quantity = _ask.quantity,
                                amount = _ask.price * _ask.quantity,
                                count = 1
                            });

                            _nob.result.askSumQty -= _ask.quantity;

                            _ask.quantity = 0;
                            _ask.amount = 0;
                        }
                        else
                        {
                            _ask.quantity -= _t.quantity;
                            _ask.amount = _ask.price * _ask.quantity;

                            _nob.result.asks.Add(new SOrderBookItem
                            {
                                action = "update",
                                id = _ask.id,
                                price = _ask.price,
                                quantity = _ask.quantity,
                                amount = _ask.price * _ask.quantity,
                                count = 1
                            });

                            _nob.result.askSumQty += _ask.quantity;
                        }
                    }

                    var _bid = qob.result.bids.Where(o => o.price == _t.price).SingleOrDefault();
                    if (_bid != null)
                    {
                        if (_bid.quantity <= _t.quantity)
                        {
                            _nob.result.bids.Add(new SOrderBookItem
                            {
                                action = "delete",
                                id = _bid.id,
                                price = _bid.price,
                                quantity = _bid.quantity,
                                amount = _bid.price * _bid.quantity,
                                count = 1
                            });

                            _nob.result.bidSumQty -= _bid.quantity;

                            _bid.quantity = 0;
                            _bid.amount = 0;
                        }
                        else
                        {
                            _bid.quantity -= _t.quantity;
                            _bid.amount = _bid.price * _bid.quantity;

                            _nob.result.bids.Add(new SOrderBookItem
                            {
                                action = "update",
                                id = _bid.id,
                                price = _bid.price,
                                quantity = _bid.quantity,
                                amount = _bid.price * _bid.quantity,
                                count = 1
                            });

                            _nob.result.bidSumQty += _bid.quantity;
                        }
                    }
                }

                qob.sequentialId = _nob.sequentialId;

                qob.result.asks.RemoveAll(o => o.quantity == 0);
                qob.result.bids.RemoveAll(o => o.quantity == 0);
            }

            _nob.result.asks.RemoveAll(o => o.quantity == 0);
            _nob.result.bids.RemoveAll(o => o.quantity == 0);

            if (_nob.result.asks.Count + _nob.result.bids.Count > 0)
            {
                await publishOrderbook(_nob);

                settings.orderbook_count = 0;
                settings.trades_flag = true;
            }

            return true;
        }

        private async Task snapshotOrderbook(string symbol)
        {
            SOrderBooks _qob;

            lock (__qOrderBooks)
            {
                if (__qOrderBooks.TryGetValue(symbol, out _qob) == true)
                    _qob.stream = "snapshot";
            }

            if (_qob != null)
                await publishOrderbook(_qob);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private async Task publishOrderbook(SOrderBooks sob)
        {
            await Task.Delay(0);

            var _json_data = JsonConvert.SerializeObject(sob);
            __orderbook.Write(this, DRConfig.DealerName, _json_data);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private async Task publishTrading(SCompleteOrders sco)
        {
            await Task.Delay(0);

            var _json_data = JsonConvert.SerializeObject(sco);
            __trading.Write(this, DRConfig.DealerName, _json_data);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private Settings GetSettings(string symbol)
        {
            Settings _result;

            if (__qSettings.TryGetValue(symbol, out _result) == false)
            {
                _result = new Settings();
                __qSettings.TryAdd(symbol, _result);
            }

            return _result;
        }
    }
}