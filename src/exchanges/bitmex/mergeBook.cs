using CCXT.Collector.Library;
using CCXT.Collector.Library.Types;
using CCXT.Collector.Service;
using Newtonsoft.Json;
using System.Collections.Concurrent;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace CCXT.Collector.BitMEX
{
    public partial class Processing
    {
        private static ConcurrentDictionary<string, SOrderBooks> __qOrderBooks = new ConcurrentDictionary<string, SOrderBooks>();
        private static ConcurrentDictionary<string, Settings> __qSettings = new ConcurrentDictionary<string, Settings>();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="trades"></param>
        /// <returns></returns>
        private async ValueTask<bool> mergeTradeItems(SCompleteOrders trades)
        {
            var _result = false;

            SOrderBooks _qob;
            if (__qOrderBooks.TryGetValue(trades.symbol, out _qob) == true)
            {
                _qob.stream = trades.stream;

                if (trades.result != null)
                {
                    var _settings = GetSettings(trades.symbol);
                    _result = await updateTradeItems(_qob, trades, _settings);
                }
            }

            return _result;
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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="qob"></param>
        /// <param name="trades"></param>
        /// <param name="settings"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private async ValueTask<bool> updateTradeItems(SOrderBooks qob, SCompleteOrders trades, Settings settings)
        {
            var _nob = new SOrderBooks
            {
                exchange = trades.exchange,
                stream = "difftrade",
                symbol = trades.symbol,
                sequentialId = trades.sequentialId,
                result = new SOrderBook
                {
                    timestamp = trades.result.Max(o => o.timestamp),
                    askSumQty = 0,
                    bidSumQty = 0
                }
            };

            lock (__qOrderBooks)
            {
                settings.before_trade_ask_size = qob.result.asks.Sum(o => o.quantity);
                settings.before_trade_bid_size = qob.result.bids.Sum(o => o.quantity);

                foreach (var _t in trades.result.OrderBy(t => t.timestamp))
                {
                    if (settings.last_trade_id >= _t.timestamp)
                        continue;

                    settings.last_trade_id = _t.timestamp;

                    var _ask = qob.result.asks.Where(o => o.price == _t.price).SingleOrDefault();
                    if (_ask != null)
                    {
                        if (_ask.quantity <= _t.quantity)
                        {
                            var _aoi = new SOrderBookItem
                            {
                                action = "delete",
                                price = _ask.price,
                                quantity = _ask.quantity,
                                amount = _ask.price * _ask.quantity,
                                count = 1
                            };

                            _nob.result.asks.Add(_aoi);

                            _nob.result.askSumQty -= _ask.quantity;
                            _ask.quantity = 0;
                        }
                        else
                        {
                            _ask.quantity -= _t.quantity;

                            var _aoi = new SOrderBookItem
                            {
                                action = "update",
                                price = _ask.price,
                                quantity = _ask.quantity,
                                amount = _ask.price * _ask.quantity,
                                count = 1
                            };

                            _nob.result.asks.Add(_aoi);
                            _nob.result.askSumQty += _ask.quantity;
                        }
                    }

                    var _bid = qob.result.bids.Where(o => o.price == _t.price).SingleOrDefault();
                    if (_bid != null)
                    {
                        if (_bid.quantity <= _t.quantity)
                        {
                            var _aoi = new SOrderBookItem
                            {
                                action = "delete",
                                price = _bid.price,
                                quantity = _bid.quantity,
                                amount = _bid.price * _bid.quantity,
                                count = 1
                            };

                            _nob.result.bids.Add(_aoi);

                            _nob.result.bidSumQty -= _bid.quantity;
                            _bid.quantity = 0;
                        }
                        else
                        {
                            _bid.quantity -= _t.quantity;

                            var _aoi = new SOrderBookItem
                            {
                                action = "update",
                                price = _bid.price,
                                quantity = _bid.quantity,
                                amount = _bid.price * _bid.quantity,
                                count = 1
                            };

                            _nob.result.bids.Add(_aoi);
                            _nob.result.bidSumQty += _bid.quantity;
                        }
                    }

                    cleanOrderbook(qob, _nob, _t.price);
                }

                qob.sequentialId = _nob.sequentialId;

                qob.result.asks.RemoveAll(o => o.quantity == 0);
                qob.result.bids.RemoveAll(o => o.quantity == 0);
            }

            if (_nob.result.asks.Count + _nob.result.bids.Count > 0)
            {
                await publishOrderbook(_nob);

                settings.orderbook_count = 0;
                settings.trades_flag = true;
            }

            return true;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="orderbook"></param>
        /// <param name="symbol"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private async ValueTask<bool> insertOrderbook(SOrderBook orderbook, string exchange, string symbol)
        {
            var _result = false;

            var _sqo = await createOrderbooks(orderbook, exchange, symbol);
            if (__qOrderBooks.TryAdd(symbol, _sqo) == true)
            {
                var _settings = GetSettings(symbol);

                _settings.last_orderbook_time = orderbook.timestamp;
                _settings.orderbook_count = 0;

                await snapshotOrderbook(symbol);
                _result = true;
            }

            return _result;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="qob"></param>
        /// <param name="orderbook"></param>
        /// <param name="exchange"></param>
        /// <param name="symbol"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private async ValueTask<bool> modifyOrderbook(SOrderBooks qob, SOrderBook orderbook, string exchange, string symbol)
        {
            var _result = false;

            var _settings = GetSettings(symbol);
            if (_settings.last_orderbook_time < orderbook.timestamp)
            {
                _settings.last_orderbook_time = orderbook.timestamp;

                var _current_ask_size = orderbook.askSumQty;
                var _current_bid_size = orderbook.bidSumQty;

                if (_settings.last_order_ask_size != _current_ask_size || _settings.last_order_bid_size != _current_bid_size)
                {
                    if (_settings.before_trade_ask_size != _current_ask_size || _settings.before_trade_bid_size != _current_bid_size)
                    {
                        var _cob = await createOrderbooks(orderbook, exchange, symbol);

                        if (_settings.trades_flag == true)
                        {
                            var _t_ask = qob.result.asks.OrderByDescending(o => o.price).LastOrDefault();
                            var _t_bid = qob.result.bids.OrderByDescending(o => o.price).FirstOrDefault();

                            if (_t_ask != null && _t_bid != null)
                            {
                                _cob.result.asks.RemoveAll(o => o.price < _t_ask.price);
                                _cob.result.bids.RemoveAll(o => o.price > _t_bid.price);

                                var _c_ask = _cob.result.asks.Where(o => o.price == _t_ask.price).SingleOrDefault();
                                var _c_bid = _cob.result.bids.Where(o => o.price == _t_bid.price).SingleOrDefault();

                                if (_c_ask != null && _c_bid != null)
                                {
                                    if (_t_ask.quantity != _c_ask.quantity || _t_bid.quantity != _c_bid.quantity)
                                    {
                                        _t_ask.quantity = _c_ask.quantity;
                                        _t_bid.quantity = _c_bid.quantity;
                                    }
                                }
                            }

                            _settings.trades_flag = false;
                        }

                        _result = await updateOrderbook(qob, _cob, _settings);

                        _settings.last_order_ask_size = qob.result.asks.Sum(o => o.quantity);
                        _settings.last_order_bid_size = qob.result.bids.Sum(o => o.quantity);
#if DEBUG
                        // modified check
                        if (_current_ask_size != _settings.last_order_ask_size || _current_bid_size != _settings.last_order_bid_size)
                            BMLogger.WriteQ($"diffb: timestamp => {_settings.last_orderbook_time}, symbol => {symbol}, ask_size => {_current_ask_size}, {_settings.last_order_ask_size}, bid_size => {_current_bid_size}, {_settings.last_order_bid_size}");

                        var _ask_count = qob.result.asks.Count;
                        var _bid_count = qob.result.bids.Count;
                        if (_ask_count + _bid_count != 30)
                            BMLogger.WriteQ($"diffb: timestamp => {_settings.last_orderbook_time}, symbol => {symbol}, ask_count => {_ask_count}, bid_count => {_bid_count}");
#endif
                    }
                    else
                        BMLogger.WriteQ($"trade: timestamp => {_settings.last_orderbook_time}, symbol => {symbol}, ask_size => {_current_ask_size}, bid_size => {_current_bid_size}");
                }
                else
                    BMLogger.WriteQ($"equal: timestamp => {_settings.last_orderbook_time}, symbol => {symbol}, ask_size => {_current_ask_size}, bid_size => {_current_bid_size}");
            }

            return _result;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="orderbook"></param>
        /// <param name="symbol"></param>
        /// <returns></returns>
        private async ValueTask<bool> mergeOrderbook(SOrderBook orderbook, string exchange, string symbol)
        {
            var _result = false;

            SOrderBooks _qob;
            {
                if (__qOrderBooks.TryGetValue(symbol, out _qob) == false)
                    _result = await insertOrderbook(orderbook, exchange, symbol);
                else
                    _result = await modifyOrderbook(_qob, orderbook, exchange, symbol);
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
            var _dqo = new SOrderBooks
            {
                exchange = cob.exchange,
                stream = "diffbooks",
                symbol = cob.symbol,
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
                foreach (var _coi in cob.result.asks)
                {
                    var _oi = qob.result.asks.Where(o => o.price == _coi.price).SingleOrDefault();
                    if (_oi == null)
                    {
                        var _ioi = new SOrderBookItem
                        {
                            action = "insert",
                            price = _coi.price,
                            quantity = _coi.quantity,
                            amount = _coi.price * _coi.quantity,
                            count = 1
                        };

                        _dqo.result.asks.Add(_ioi);
                        qob.result.asks.Add(_ioi);

                        _dqo.result.askSumQty += _coi.quantity;
                    }
                    else if (_oi.quantity != _coi.quantity)
                    {
                        _dqo.result.asks.Add(new SOrderBookItem
                        {
                            action = "update",
                            price = _coi.price,
                            quantity = _coi.quantity,
                            amount = _coi.price * _coi.quantity,
                            count = 1
                        });

                        _oi.quantity = _coi.quantity;
                        _dqo.result.askSumQty += _coi.quantity;
                    }
                }

                foreach (var _coi in cob.result.bids)
                {
                    var _oi = qob.result.bids.Where(o => o.price == _coi.price).SingleOrDefault();
                    if (_oi == null)
                    {
                        var _ioi = new SOrderBookItem
                        {
                            action = "insert",
                            price = _coi.price,
                            quantity = _coi.quantity,
                            amount = _coi.price * _coi.quantity,
                            count = 1
                        };

                        _dqo.result.bids.Add(_ioi);
                        qob.result.bids.Add(_ioi);

                        _dqo.result.bidSumQty += _coi.quantity;
                    }
                    else if (_oi.quantity != _coi.quantity)
                    {
                        _dqo.result.bids.Add(new SOrderBookItem
                        {
                            action = "update",
                            price = _coi.price,
                            quantity = _coi.quantity,
                            amount = _coi.price * _coi.quantity,
                            count = 1
                        });

                        _oi.quantity = _coi.quantity;
                        _dqo.result.bidSumQty += _coi.quantity;
                    }
                }

                foreach (var _loi in qob.result.asks)
                {
                    var _oi = cob.result.asks.Where(o => o.price == _loi.price).SingleOrDefault();
                    if (_oi == null)
                    {
                        _dqo.result.asks.Add(new SOrderBookItem
                        {
                            action = "delete",
                            price = _loi.price,
                            quantity = _loi.quantity,
                            amount = _loi.price * _loi.quantity,
                            count = 1
                        });

                        _dqo.result.askSumQty -= _loi.quantity;
                        _loi.quantity = 0;
                    }
                }

                foreach (var _loi in qob.result.bids)
                {
                    var _oi = cob.result.bids.Where(o => o.price == _loi.price).SingleOrDefault();
                    if (_oi == null)
                    {
                        _dqo.result.bids.Add(new SOrderBookItem
                        {
                            action = "delete",
                            price = _loi.price,
                            quantity = _loi.quantity,
                            amount = _loi.price * _loi.quantity,
                            count = 1
                        });

                        _dqo.result.bidSumQty -= _loi.quantity;
                        _loi.quantity = 0;
                    }
                }

                qob.result.asks.RemoveAll(o => o.quantity == 0);
                qob.result.bids.RemoveAll(o => o.quantity == 0);
            }

            if (++settings.orderbook_count == KConfig.SnapshotSkipCounter)
            {
                qob.sequentialId = cob.sequentialId;
                await snapshotOrderbook(_dqo.symbol);
            }
            else
                await publishOrderbook(_dqo);

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
            //if (_asks.Count() > 0)
            {
                foreach (var _qox in _asks)
                {
                    nob.result.asks.Add(new SOrderBookItem
                    {
                        action = "delete",
                        price = _qox.price,
                        quantity = _qox.quantity,
                        amount = _qox.price * _qox.quantity,
                        count = 1
                    });

                    nob.result.askSumQty -= _qox.quantity;
                    _qox.quantity = 0;
                }
            }

            var _bids = qob.result.bids.Where(o => o.price > price);
            //if (_bids.Count() > 0)
            {
                foreach (var _qox in _bids)
                {
                    nob.result.bids.Add(new SOrderBookItem
                    {
                        action = "delete",
                        price = _qox.price,
                        quantity = _qox.quantity,
                        amount = _qox.price * _qox.quantity,
                        count = 1
                    });

                    nob.result.bidSumQty -= _qox.quantity;
                    _qox.quantity = 0;
                }
            }
        }

        /// <summary>
        /// Json 변환시 표준 클래스로 변환 하기 위해 new create 해야 함
        /// </summary>
        /// <param name="orderbook"></param>
        /// <param name="exchange"></param>
        /// <param name="symbol"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private async ValueTask<SOrderBooks> createOrderbooks(SOrderBook orderbook, string exchange, string symbol)
        {
            var _sob = new SOrderBooks
            {
                exchange = exchange,
                symbol = symbol,
                sequentialId = orderbook.timestamp,
                result = new SOrderBook
                {
                    timestamp = orderbook.timestamp,
                    askSumQty = orderbook.asks.Sum(o => o.quantity),
                    bidSumQty = orderbook.bids.Sum(o => o.quantity)
                }
            };

            foreach (var _oi in orderbook.asks)
            {
                _sob.result.asks.Add(new SOrderBookItem
                {
                    action = "insert",
                    price = _oi.price,
                    quantity = _oi.quantity,
                    amount = _oi.price * _oi.quantity,
                    count = 1
                });
            }

            foreach (var _oi in orderbook.bids)
            {
                _sob.result.bids.Add(new SOrderBookItem
                {
                    action = "insert",
                    price = _oi.price,
                    quantity = _oi.quantity,
                    amount = _oi.price * _oi.quantity,
                    count = 1
                });
            }

            return await Task.FromResult(_sob);
        }

        private async Task snapshotOrderbook(string symbol)
        {
            SOrderBooks _qob;

            lock (__qOrderBooks)
            {
                if (__qOrderBooks.TryGetValue(symbol, out _qob) == true)
                {
                    _qob.stream = "snapshot";
                }
            }

            if (_qob != null)
                await publishOrderbook(_qob);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private async Task publishOrderbook(SOrderBooks sob)
        {
            await Task.Delay(0);
            {
                var _json_data = JsonConvert.SerializeObject(sob);
                OrderbookQ.Write(_json_data);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private async Task publishTrading(SCompleteOrders sco)
        {
            await Task.Delay(0);
            {
                var _json_data = JsonConvert.SerializeObject(sco);
                TradingQ.Write(_json_data);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private async Task publishTicker(STickers stk)
        {
            await Task.Delay(0);
            {
                var _json_data = JsonConvert.SerializeObject(stk);
                TickerQ.Write(_json_data);
            }
        }
    }
}