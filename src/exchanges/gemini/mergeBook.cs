using CCXT.Collector.Library;
using CCXT.Collector.Service;
using Newtonsoft.Json;
using System.Collections.Concurrent;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace CCXT.Collector.Gemini
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
        private async ValueTask<bool> mergeOrderbook(SOrderBooks cob)
        {
            var _result = false;

            if (cob.symbol != null)
            {
                SOrderBooks _qob;

                if (__qOrderBooks.TryGetValue(cob.symbol, out _qob) == true)
                {
                    _qob.stream = cob.stream;

                    var _settings = GetSettings(cob.symbol);
                    _result = await modifyOrderbook(_qob, cob, _settings);
                }
                else
                {
                    _result = await insertOrderbook(cob);
                }
            }

            return _result;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="cob"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private async ValueTask<bool> insertOrderbook(SOrderBooks cob)
        {
            var _result = false;

            if (cob.symbol != null)
            {
                var _sqo = await createOrderbook(cob);
                if (__qOrderBooks.TryAdd(cob.symbol, _sqo) == true)
                {
                    var _settings = GetSettings(cob.symbol);
                    {
                        _settings.last_orderbook_time = cob.result.timestamp;
                        _settings.orderbook_count = 0;
                    }

                    await snapshotOrderbook(cob.symbol);
                    _result = true;
                }
            }

            return _result;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="qob"></param>
        /// <param name="cob"></param>
        /// <param name="settings"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private async ValueTask<bool> modifyOrderbook(SOrderBooks qob, SOrderBooks cob, Settings settings)
        {
            var _result = false;

            if (settings.last_orderbook_time < cob.result.timestamp)
            {
                settings.last_orderbook_time = cob.result.timestamp;

                var _current_ask_size = cob.result.askSumQty;
                var _current_bid_size = cob.result.bidSumQty;

                if (settings.last_order_ask_size != _current_ask_size || settings.last_order_bid_size != _current_bid_size)
                {
                    if (settings.before_trade_ask_size != _current_ask_size || settings.before_trade_bid_size != _current_bid_size)
                    {
                        if (settings.trades_flag == true)
                        {
                            var _t_ask = qob.result.asks.OrderByDescending(o => o.price).LastOrDefault();
                            var _t_bid = qob.result.bids.OrderByDescending(o => o.price).FirstOrDefault();

                            if (_t_ask != null && _t_bid != null)
                            {
                                cob.result.asks.RemoveAll(o => o.price < _t_ask.price);
                                cob.result.bids.RemoveAll(o => o.price > _t_bid.price);

                                var _c_ask = cob.result.asks.Where(o => o.price == _t_ask.price).SingleOrDefault();
                                var _c_bid = cob.result.bids.Where(o => o.price == _t_bid.price).SingleOrDefault();

                                if (_c_ask != null && _c_bid != null)
                                {
                                    if (_t_ask.quantity != _c_ask.quantity || _t_bid.quantity != _c_bid.quantity)
                                    {
                                        _t_ask.quantity = _c_ask.quantity;
                                        _t_bid.quantity = _c_bid.quantity;
                                    }
                                }
                            }

                            settings.trades_flag = false;
                        }

                        _result = await updateOrderbook(qob, cob, settings);

                        settings.last_order_ask_size = qob.result.asks.Sum(o => o.quantity);
                        settings.last_order_bid_size = qob.result.bids.Sum(o => o.quantity);
                    }
                }
            }

            return _result;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="qob"></param>
        /// <param name="cob"></param>
        /// <param name="settings"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private async ValueTask<bool> updateOrderbook(SOrderBooks qob, SOrderBooks cob, Settings settings)
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
                foreach (var _ci in cob.result.asks)
                {
                    var _qi = qob.result.asks.Where(o => o.price == _ci.price).SingleOrDefault();
                    if (_qi == null)
                    {
                        var _ici = new SOrderBookItem
                        {
                            action = "insert",
                            id = _ci.id,
                            price = _ci.price,
                            quantity = _ci.quantity,
                            amount = _ci.price * _ci.quantity,
                            count = 1
                        };

                        _nob.result.asks.Add(_ici);
                        qob.result.asks.Add(_ici);

                        _nob.result.askSumQty += _ci.quantity;
                    }
                    else if (_qi.quantity != _ci.quantity)
                    {
                        _nob.result.asks.Add(new SOrderBookItem
                        {
                            action = "update",
                            id = _qi.id,
                            price = _qi.price,
                            quantity = _ci.quantity,
                            amount = _qi.price * _ci.quantity,
                            count = 1
                        });

                        _qi.quantity = _ci.quantity;
                        _qi.amount = _qi.price * _qi.quantity;

                        _nob.result.askSumQty += _ci.quantity;
                    }
                }

                foreach (var _ci in cob.result.bids)
                {
                    var _qi = qob.result.bids.Where(o => o.price == _ci.price).SingleOrDefault();
                    if (_qi == null)
                    {
                        var _ici = new SOrderBookItem
                        {
                            action = "insert",
                            id = _ci.id,
                            price = _ci.price,
                            quantity = _ci.quantity,
                            amount = _ci.price * _ci.quantity,
                            count = 1
                        };

                        _nob.result.bids.Add(_ici);
                        qob.result.bids.Add(_ici);

                        _nob.result.bidSumQty += _ci.quantity;
                    }
                    else if (_qi.quantity != _ci.quantity)
                    {
                        _nob.result.bids.Add(new SOrderBookItem
                        {
                            action = "update",
                            id = _qi.id,
                            price = _qi.price,
                            quantity = _ci.quantity,
                            amount = _qi.price * _ci.quantity,
                            count = 1
                        });

                        _qi.quantity = _ci.quantity;
                        _qi.amount = _qi.quantity * _qi.price;

                        _nob.result.bidSumQty += _ci.quantity;
                    }
                }

                foreach (var _qi in qob.result.asks)
                {
                    var _ci = cob.result.asks.Where(o => o.price == _qi.price).SingleOrDefault();
                    if (_ci == null)
                    {
                        _nob.result.asks.Add(new SOrderBookItem
                        {
                            action = "delete",
                            id = _qi.id,
                            price = _qi.price,
                            quantity = _qi.quantity,
                            amount = _qi.price * _qi.quantity,
                            count = 1
                        });

                        _nob.result.askSumQty -= _qi.quantity;

                        _qi.quantity = 0;
                        _qi.amount = 0;
                    }
                }

                foreach (var _qi in qob.result.bids)
                {
                    var _ci = cob.result.bids.Where(o => o.price == _qi.price).SingleOrDefault();
                    if (_ci == null)
                    {
                        _nob.result.bids.Add(new SOrderBookItem
                        {
                            action = "delete",
                            id = _qi.id,
                            price = _qi.price,
                            quantity = _qi.quantity,
                            amount = _qi.price * _qi.quantity,
                            count = 1
                        });

                        _nob.result.bidSumQty -= _qi.quantity;

                        _qi.quantity = 0;
                        _qi.amount = 0;
                    }
                }

                qob.result.asks.RemoveAll(o => o.quantity == 0);
                qob.result.bids.RemoveAll(o => o.quantity == 0);
            }

            if (++settings.orderbook_count == GMConfig.SNG.SnapshotSkipCounter)
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
        /// <param name="cco"></param>
        /// <returns></returns>
        private async ValueTask<bool> mergeCompleteOrder(SCompleteOrders cco)
        {
            var _result = false;

            if (GMConfig.SNG.UsePublishTrade == true)
                await publishTrading(cco);

            if (cco.symbol != null)
            {
                SOrderBooks _qob;
                if (__qOrderBooks.TryGetValue(cco.symbol, out _qob) == true)
                {
                    _qob.stream = cco.stream;

                    if (cco.result != null)
                    {
                        var _settings = GetSettings(cco.symbol);
                        _result = await updateCompleteOrder(_qob, cco, _settings);
                    }
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
        private async ValueTask<bool> updateCompleteOrder(SOrderBooks qob, SCompleteOrders cco, Settings settings)
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
                settings.before_trade_ask_size = qob.result.asks.Sum(o => o.quantity);
                settings.before_trade_bid_size = qob.result.bids.Sum(o => o.quantity);

                foreach (var _t in cco.result.OrderBy(t => t.timestamp))
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

                    //cleanOrderbook(qob, _nob, _t.price);
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

        /// <summary>
        /// orderbook에 해당 하는 price-level 보다 안쪽에 위치한 level들은 삭제 해야 한다.
        /// </summary>
        /// <param name="qob"></param>
        /// <param name="nob"></param>
        /// <param name="price"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void cleanOrderbook(SOrderBooks qob, SOrderBooks nob, decimal price)
        {
            var _asks = qob.result.asks.Where(o => o.price < price);
            {
                foreach (var _ask in _asks)
                {
                    nob.result.asks.Add(new SOrderBookItem
                    {
                        action = "delete",
                        id = _ask.id,
                        price = _ask.price,
                        quantity = _ask.quantity,
                        amount = _ask.price * _ask.quantity,
                        count = 1
                    });

                    nob.result.askSumQty -= _ask.quantity;

                    _ask.quantity = 0;
                    _ask.amount = 0;
                }
            }

            var _bids = qob.result.bids.Where(o => o.price > price);
            {
                foreach (var _bid in _bids)
                {
                    nob.result.bids.Add(new SOrderBookItem
                    {
                        action = "delete",
                        id = _bid.id,
                        price = _bid.price,
                        quantity = _bid.quantity,
                        amount = _bid.price * _bid.quantity,
                        count = 1
                    });

                    nob.result.bidSumQty -= _bid.quantity;
                    
                    _bid.quantity = 0;
                    _bid.amount = 0;
                }
            }
        }

        /// <summary>
        /// Json 변환시 표준 클래스로 변환 하기 위해 new create 해야 함
        /// </summary>
        /// <param name="cob"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private async ValueTask<SOrderBooks> createOrderbook(SOrderBooks cob)
        {
            cob.result.asks.ForEach(a => a.action = "insert");
            cob.result.bids.ForEach(a => a.action = "insert");

            return await Task.FromResult(cob);
        }

        private async Task snapshotOrderbook(string symbol)
        {
            if (symbol != null)
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
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private async Task publishOrderbook(SOrderBooks sob)
        {
            await Task.Delay(0);

            var _json_data = JsonConvert.SerializeObject(sob);
            __orderbook.Write(this, GMConfig.DealerName, _json_data);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private async Task publishTrading(SCompleteOrders sco)
        {
            await Task.Delay(0);

            var _json_data = JsonConvert.SerializeObject(sco);
            __trading.Write(this, GMConfig.DealerName, _json_data);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private async Task publishTicker(STickers stk)
        {
            await Task.Delay(0);

            var _json_data = JsonConvert.SerializeObject(stk);
            __ticker.Write(this, GMConfig.DealerName, _json_data);
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