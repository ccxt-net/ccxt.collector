﻿using CCXT.Collector.BitMEX.Public;
using CCXT.Collector.BitMEX.Types;
using CCXT.Collector.Library.Types;
using CCXT.Collector.Service;
using Newtonsoft.Json;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CCXT.Collector.BitMEX
{
    public partial class Processing
    {
        private static ConcurrentDictionary<string, SOrderBooks> __qOrderBooks = new ConcurrentDictionary<string, SOrderBooks>();
        private static ConcurrentDictionary<string, Settings> __qSettings = new ConcurrentDictionary<string, Settings>();

        private async Task<bool> mergeTradeItem(BTradeItem tradeItem, string stream = "wsctrades")
        {
            var _result = false;

            if (__qOrderBooks.ContainsKey(tradeItem.symbol) == true)
            {
                var _qob = __qOrderBooks[tradeItem.symbol];
                _qob.stream = stream;

                _result = await updateTradeItem(_qob, new List<BTradeItem> { tradeItem }, stream);
            }

            return _result;
        }

        private async Task<bool> mergeTradeItems(BATrade tradeItems, string stream = "apitrades")
        {
            var _result = false;

            if (__qOrderBooks.ContainsKey(tradeItems.symbol) == true)
            {
                var _qob = __qOrderBooks[tradeItems.symbol];
                _qob.stream = stream;

                if (tradeItems.data != null)
                    _result = await updateTradeItem(_qob, tradeItems.data.ToList<BTradeItem>(), stream);
            }

            return _result;
        }

        private async Task<bool> updateTradeItem(SOrderBooks qob, List<BTradeItem> tradeItems, string stream)
        {
            var _rqo = new SOrderBooks(BMLogger.exchange_name, stream, qob.symbol)
            {
                sequential_id = tradeItems.Max(t => t.timestamp)
            };

            var _settings = __qSettings.ContainsKey(qob.symbol) ? __qSettings[qob.symbol]
                          : __qSettings[qob.symbol] = new Settings();

            lock (__qOrderBooks)
            {
                _settings.before_trade_ask_size = qob.data.Where(o => o.side == "ask").Sum(o => o.quantity);
                _settings.before_trade_bid_size = qob.data.Where(o => o.side == "bid").Sum(o => o.quantity);

                foreach (var _t in tradeItems.OrderBy(t => t.timestamp))
                {
                    if (_settings.last_trade_time >= _t.timestamp)
                        continue;

                    _settings.last_trade_time = _t.timestamp;

                    var _qoi = qob.data.Where(o => o.price == _t.price).SingleOrDefault();
                    if (_qoi != null)
                    {
                        if (_qoi.quantity <= _t.quantity)
                        {
                            var _aoi = new SOrderBook
                            {
                                action = "delete",
                                side = _qoi.side,
                                price = _qoi.price,
                                quantity = _qoi.quantity
                            };

                            _rqo.data.Add(_aoi);
                            _qoi.quantity = 0;
                        }
                        else
                        {
                            _qoi.quantity -= _t.quantity;

                            var _aoi = new SOrderBook
                            {
                                action = "update",
                                side = _qoi.side,
                                price = _qoi.price,
                                quantity = _qoi.quantity
                            };

                            _rqo.data.Add(_aoi);
                        }
                    }

                    // orderbook에 해당 하는 price-level 보다 안쪽에 위치한 level들은 삭제 해야 한다.
                    var _strange_levels = qob.data.Where(o => (o.side == "ask" && o.price < _t.price) || (o.side == "bid" && o.price > _t.price));
                    if (_strange_levels.Count() > 0)
                    {
                        foreach (var _qox in _strange_levels)
                        {
                            var _aoi = new SOrderBook
                            {
                                action = "delete",
                                side = _qox.side,
                                price = _qox.price,
                                quantity = _qox.quantity
                            };

                            _rqo.data.Add(_aoi);
                            _qox.quantity = 0;
                        }

                        BMLogger.WriteQ($"nofnd-{stream}: timestamp => {_settings.last_trade_time}, symbol => {qob.symbol}, price => {_t.price}, quantity => {_t.quantity}");
                    }
                }

                qob.sequential_id = _rqo.sequential_id;
                qob.data.RemoveAll(o => o.quantity == 0);
            }

            if (_rqo.data.Count > 0)
            {
                await publishOrderbook(_rqo);
                _settings.orderbook_count = 0;
            }

            return true;
        }

        private async Task<bool> mergeOrderbook(BAOrderBook orderBook)
        {
            var _result = false;

            var _settings = __qSettings.ContainsKey(orderBook.data.symbol) ? __qSettings[orderBook.data.symbol]
              : __qSettings[orderBook.data.symbol] = new Settings();

            if (__qOrderBooks.ContainsKey(orderBook.data.symbol) == false)
            {
                _settings.last_orderbook_id = orderBook.data.lastId;

                var _sqo = new SOrderBooks(BMLogger.exchange_name, "snapshot", orderBook.data.symbol)
                {
                    sequential_id = orderBook.data.lastId
                };

                foreach (var _oi in orderBook.data.asks)
                {
                    _sqo.data.Add(new SOrderBook
                    {
                        action = "insert",
                        side = "ask",
                        price = _oi[0],
                        quantity = _oi[1]
                    });
                }

                foreach (var _oi in orderBook.data.bids)
                {
                    _sqo.data.Add(new SOrderBook
                    {
                        action = "insert",
                        side = "bid",
                        price = _oi[0],
                        quantity = _oi[1]
                    });
                }

                __qOrderBooks[_sqo.symbol] = _sqo;

                await publishOrderbook(_sqo);
                _settings.orderbook_count = 0;

                _result = true;
            }
            else if (_settings.last_orderbook_id < orderBook.data.lastId)
            {
                _settings.last_orderbook_id = orderBook.data.lastId;

                var _qob = __qOrderBooks[orderBook.data.symbol];

                var _current_ask_size = orderBook.data.asks.Sum(o => o[1]);
                var _current_bid_size = orderBook.data.bids.Sum(o => o[1]);

                if (_settings.last_order_ask_size != _current_ask_size || _settings.last_order_bid_size != _current_bid_size)
                {
                    if (_settings.before_trade_ask_size != _current_ask_size || _settings.before_trade_bid_size != _current_bid_size)
                    {
                        _result = await updateOrderbook(_qob, _settings, orderBook);

                        _settings.last_order_ask_size = _qob.data.Where(o => o.side == "ask").Sum(o => o.quantity);
                        _settings.last_order_bid_size = _qob.data.Where(o => o.side == "bid").Sum(o => o.quantity);
#if DEBUG
                        // modified check
                        if (_current_ask_size != _settings.last_order_ask_size || _current_bid_size != _settings.last_order_bid_size)
                            BMLogger.WriteQ($"diffb-{orderBook.stream}: timestamp => {_settings.last_orderbook_id}, symbol => {orderBook.data.symbol}, ask_size => {_current_ask_size}, {_settings.last_order_ask_size}, bid_size => {_current_bid_size}, {_settings.last_order_bid_size}");

                        if (_qob.data.Count != 200)
                        {
                            var _ask_count = _qob.data.Where(o => o.side == "ask").Count();
                            var _bid_count = _qob.data.Where(o => o.side == "bid").Count();

                            BMLogger.WriteQ($"diffb-{orderBook.stream}: timestamp => {_settings.last_orderbook_id}, symbol => {orderBook.data.symbol}, ask_count => {_ask_count}, bid_count => {_bid_count}");
                        }
#endif
                    }
                    else
                        BMLogger.WriteQ($"trade-{orderBook.stream}: timestamp => {_settings.last_orderbook_id}, symbol => {orderBook.data.symbol}, ask_size => {_current_ask_size}, bid_size => {_current_bid_size}");
                }
                else
                    BMLogger.WriteQ($"equal-{orderBook.stream}: timestamp => {_settings.last_orderbook_id}, symbol => {orderBook.data.symbol}, ask_size => {_current_ask_size}, bid_size => {_current_bid_size}");
            }
            //else
            //    BLogger.WriteQ($"pastb-{orderBook.stream}: timestamp => {_settings.last_orderbook_id}, symbol => {orderBook.data.symbol}, ask_size => {_settings.last_order_ask_size}, bid_size => {_settings.last_order_bid_size}");

            return _result;
        }

        private async Task<bool> updateOrderbook(SOrderBooks qob, Settings settings, BAOrderBook orderBook)
        {
            var _dqo = new SOrderBooks(BMLogger.exchange_name, "diffbooks", orderBook.data.symbol)
            {
                sequential_id = orderBook.data.lastId
            };

            lock (__qOrderBooks)
            {
                foreach (var _oi in orderBook.data.asks)
                {
                    var _ask = qob.data.Where(o => o.side == "ask" && o.price == _oi[0]).SingleOrDefault();
                    if (_ask == null)
                    {
                        var _aoi = new SOrderBook
                        {
                            action = "insert",
                            side = "ask",
                            price = _oi[0],
                            quantity = _oi[1]
                        };

                        _dqo.data.Add(_aoi);
                        qob.data.Add(_aoi);
                    }
                    else if (_ask.quantity != _oi[1])
                    {
                        var _aoi = new SOrderBook
                        {
                            action = "update",
                            side = "ask",
                            price = _oi[0],
                            quantity = _oi[1]
                        };

                        _dqo.data.Add(_aoi);
                        _ask.quantity = _oi[1];
                    }
                }

                foreach (var _oi in orderBook.data.bids)
                {
                    var _bid = qob.data.Where(o => o.side == "bid" && o.price == _oi[0]).SingleOrDefault();
                    if (_bid == null)
                    {
                        var _boi = new SOrderBook
                        {
                            action = "insert",
                            side = "bid",
                            price = _oi[0],
                            quantity = _oi[1]
                        };

                        qob.data.Add(_boi);
                        _dqo.data.Add(_boi);
                    }
                    else if (_bid.quantity != _oi[1])
                    {
                        var _boi = new SOrderBook
                        {
                            action = "update",
                            side = "bid",
                            price = _oi[0],
                            quantity = _oi[1]
                        };

                        _dqo.data.Add(_boi);
                        _bid.quantity = _oi[1];
                    }
                }

                foreach (var _qi in qob.data)
                {
                    if (_qi.side == "ask")
                    {
                        var _ask = orderBook.data.asks.Where(o => o[0] == _qi.price).SingleOrDefault();
                        if (_ask == null)
                        {
                            _dqo.data.Add(new SOrderBook
                            {
                                action = "delete",
                                side = _qi.side,
                                price = _qi.price,
                                quantity = _qi.quantity
                            });

                            _qi.quantity = 0;
                        }
                    }
                    else
                    {
                        var _bid = orderBook.data.bids.Where(o => o[0] == _qi.price).SingleOrDefault();
                        if (_bid == null)
                        {
                            _dqo.data.Add(new SOrderBook
                            {
                                action = "delete",
                                side = _qi.side,
                                price = _qi.price,
                                quantity = _qi.quantity
                            });

                            _qi.quantity = 0;
                        }
                    }
                }

                qob.data.RemoveAll(o => o.quantity == 0);
            }

            if (++settings.orderbook_count == 2)
            {
                qob.sequential_id = orderBook.data.lastId;
                await snapshotOrderbook(_dqo.exchange, _dqo.symbol);
            }
            else
                await publishOrderbook(_dqo);

            return true;
        }

        private async Task snapshotOrderbook(string exchange, string symbol)
        {
            if (exchange == BMLogger.exchange_name)
            {
                var _sob = (SOrderBooks)null;

                lock (__qOrderBooks)
                {
                    if (__qOrderBooks.ContainsKey(symbol) == true)
                    {
                        _sob = __qOrderBooks[symbol];
                        _sob.stream = "snapshot";
                    }
                }

                if (_sob != null)
                    await publishOrderbook(_sob);
            }
        }

        private async Task publishOrderbook(SOrderBooks qob)
        {
            await Task.Delay(0);

            if (qob.data.Count > 0)
            {
                var _json_data = JsonConvert.SerializeObject(qob);
                OrderbookQ.Write(_json_data);
            }
        }
    }
}