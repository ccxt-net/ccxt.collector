using CCXT.Collector.Library;
using CCXT.Collector.Library.Types;
using CCXT.Collector.Service;
using CCXT.Collector.Upbit.Public;
using Newtonsoft.Json;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;

namespace CCXT.Collector.Upbit
{
    public partial class Processing
    {
        private static ConcurrentDictionary<string, SOrderBooks> __qOrderBooks = new ConcurrentDictionary<string, SOrderBooks>();
        private static ConcurrentDictionary<string, Settings> __qSettings = new ConcurrentDictionary<string, Settings>();

        private async Task<bool> mergeTradeItem(SCompleteOrders trades)
        {
            var _result = false;

            if (__qOrderBooks.ContainsKey(trades.symbol) == true)
            {
                var _qob = __qOrderBooks[trades.symbol];
                _qob.stream = trades.stream;

                _result = await updateTradeItem(_qob, trades);

                if (KConfig.UpbitUsePublishTrade == true)
                    await publishTrading(trades);
            }

            return _result;
        }

        private async Task<bool> mergeTradeItems(SCompleteOrders trades)
        {
            var _result = false;

            if (__qOrderBooks.ContainsKey(trades.symbol) == true)
            {
                var _qob = __qOrderBooks[trades.symbol];
                _qob.stream = trades.stream;

                if (trades.result != null)
                    _result = await updateTradeItem(_qob, trades);
            }

            return _result;
        }

        private async Task<bool> updateTradeItem(SOrderBooks lob, SCompleteOrders trades)
        {
            var _nob = new SOrderBooks
            {
                exchange = trades.exchange,
                stream = trades.stream,
                symbol = lob.symbol,
                sequentialId = trades.sequentialId,
                result = new SOrderBook()
            };

            var _settings = __qSettings.ContainsKey(lob.symbol) 
                          ? __qSettings[lob.symbol]
                          : __qSettings[lob.symbol] = new Settings();

            lock (__qOrderBooks)
            {
                _settings.before_trade_ask_size = lob.result.asks.Sum(o => o.quantity);
                _settings.before_trade_bid_size = lob.result.bids.Sum(o => o.quantity);

                foreach (var _t in trades.result.OrderBy(t => t.timestamp))
                {
                    if (_settings.last_trade_id >= _t.timestamp)
                        continue;

                    _settings.last_trade_id = _t.timestamp;

                    var _ask = lob.result.asks.Where(o => o.price == _t.price).SingleOrDefault();
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
                        }
                    }

                    var _bid = lob.result.bids.Where(o => o.price == _t.price).SingleOrDefault();
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
                        }
                    }

                    cleanOrderbook(lob, _nob, _t.quantity, _t.price);
                }

                lob.sequentialId = _nob.sequentialId;
                lob.result.asks.RemoveAll(o => o.quantity == 0);
                lob.result.bids.RemoveAll(o => o.quantity == 0);
            }

            if (_nob.result.asks.Count + _nob.result.bids.Count > 0)
            {
                await publishOrderbook(_nob);

                _settings.orderbook_count = 0;
                _settings.trades_flag = true;
            }

            return true;
        }

        private async Task<bool> mergeOrderbook(SOrderBook orderbook)
        {
            var _result = false;

            var _settings = __qSettings.ContainsKey(orderbook.symbol) 
                          ? __qSettings[orderbook.symbol]
                          : __qSettings[orderbook.symbol] = new Settings();

            if (__qOrderBooks.ContainsKey(orderbook.symbol) == false)
            {
                _settings.last_orderbook_time = orderbook.timestamp;

                var _sqo = await convertOrderbook(orderbook, "snapshot");
                __qOrderBooks[_sqo.symbol] = _sqo;

                await publishOrderbook(_sqo);
                _settings.orderbook_count = 0;

                _result = true;
            }
            else if (_settings.last_orderbook_time < orderbook.timestamp)
            {
                _settings.last_orderbook_time = orderbook.timestamp;

                var _tob = __qOrderBooks[orderbook.symbol];

                var _current_ask_size = orderbook.totalAskQuantity;
                var _current_bid_size = orderbook.totalBidQuantity;

                if (_settings.last_order_ask_size != _current_ask_size || _settings.last_order_bid_size != _current_bid_size)
                {
                    if (_settings.before_trade_ask_size != _current_ask_size || _settings.before_trade_bid_size != _current_bid_size)
                    {
                        var _cob = await convertOrderbook(orderbook, "compbooks");

                        if (_settings.trades_flag == true)
                        {
                            var _t_ask = _tob.result.asks.OrderByDescending(o => o.price).LastOrDefault();
                            var _t_bid = _tob.result.bids.OrderByDescending(o => o.price).FirstOrDefault();

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

                        _result = await updateOrderbook(_tob, _settings, _cob);

                        _settings.last_order_ask_size = _tob.result.asks.Sum(o => o.quantity);
                        _settings.last_order_bid_size = _tob.result.bids.Sum(o => o.quantity);
#if DEBUG
                        // modified check
                        if (_current_ask_size != _settings.last_order_ask_size || _current_bid_size != _settings.last_order_bid_size)
                            UPLogger.WriteQ($"diffb-{orderbook.type}: timestamp => {_settings.last_orderbook_time}, symbol => {orderbook.symbol}, ask_size => {_current_ask_size}, {_settings.last_order_ask_size}, bid_size => {_current_bid_size}, {_settings.last_order_bid_size}");

                        if (_tob.result.asks.Count + _tob.result.bids.Count != 30)
                        {
                            var _ask_count = _tob.result.asks.Count();
                            var _bid_count = _tob.result.bids.Count();

                            UPLogger.WriteQ($"diffb-{orderbook.type}: timestamp => {_settings.last_orderbook_time}, symbol => {orderbook.symbol}, ask_count => {_ask_count}, bid_count => {_bid_count}");
                        }
#endif
                    }
                    else
                        UPLogger.WriteQ($"trade-{orderbook.type}: timestamp => {_settings.last_orderbook_time}, symbol => {orderbook.symbol}, ask_size => {_current_ask_size}, bid_size => {_current_bid_size}");
                }
                else
                    UPLogger.WriteQ($"equal-{orderbook.type}: timestamp => {_settings.last_orderbook_time}, symbol => {orderbook.symbol}, ask_size => {_current_ask_size}, bid_size => {_current_bid_size}");
            }
            //else
            //    ULogger.WriteQ($"pastb-{orderBook.type}: timestamp => {_settings.last_orderbook_time}, symbol => {orderBook.symbol}, ask_size => {_settings.last_order_ask_size}, bid_size => {_settings.last_order_bid_size}");

            return _result;
        }

        private async Task<bool> updateOrderbook(SOrderBooks lob, Settings settings, SOrderBooks cob)
        {
            var _dqo = new SOrderBooks
            {
                exchange = UPLogger.exchange_name,
                stream = "diffbooks",
                symbol = cob.symbol,
                sequentialId = cob.sequentialId,
                result = new SOrderBook()
            };

            lock (__qOrderBooks)
            {
                foreach (var _coi in cob.result.asks)
                {
                    var _oi = lob.result.asks.Where(o => o.price == _coi.price).SingleOrDefault();
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
                        lob.result.asks.Add(_ioi);
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
                    }
                }

                foreach (var _coi in cob.result.bids)
                {
                    var _oi = lob.result.bids.Where(o => o.price == _coi.price).SingleOrDefault();
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
                        lob.result.bids.Add(_ioi);
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
                    }
                }

                foreach (var _loi in lob.result.asks)
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

                        _loi.quantity = 0;
                    }
                }

                foreach (var _loi in lob.result.bids)
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

                        _loi.quantity = 0;
                    }
                }

                lob.result.asks.RemoveAll(o => o.quantity == 0);
                lob.result.bids.RemoveAll(o => o.quantity == 0);
            }

            if (++settings.orderbook_count == 2)
            {
                lob.sequentialId = cob.sequentialId;
                await snapshotOrderbook(_dqo.exchange, _dqo.symbol);
            }
            else
                await publishOrderbook(_dqo);

            return true;
        }

        private async Task<bool> updateOrderbook_old(SOrderBooks qob, Settings settings, UOrderBook orderBook)
        {
            var _dqo = new SOrderBooks
            {
                exchange = UPLogger.exchange_name,
                stream = "diffbooks",
                symbol = orderBook.symbol,
                sequentialId = orderBook.timestamp,
                result = new SOrderBook()
            };

            lock (__qOrderBooks)
            {
                foreach (var _oi in orderBook.asks)
                {
                    var _ask = qob.result.asks.Where(o => o.price == _oi.price).SingleOrDefault();
                    if (_ask == null)
                    {
                        var _aoi = new SOrderBookItem
                        {
                            action = "insert",
                            price = _oi.price,
                            quantity = _oi.quantity,
                            amount = _oi.price * _oi.quantity,
                            count = 1
                        };

                        _dqo.result.asks.Add(_aoi);
                        qob.result.asks.Add(_aoi);
                    }
                    else if (_ask.quantity != _oi.quantity)
                    {
                        var _aoi = new SOrderBookItem
                        {
                            action = "update",
                            price = _oi.price,
                            quantity = _oi.quantity,
                            amount = _oi.price * _oi.quantity,
                            count = 1
                        };

                        _dqo.result.asks.Add(_aoi);
                        _ask.quantity = _oi.quantity;
                    }
                }

                foreach (var _oi in orderBook.bids)
                {
                    var _bid = qob.result.bids.Where(o => o.price == _oi.price).SingleOrDefault();
                    if (_bid == null)
                    {
                        var _boi = new SOrderBookItem
                        {
                            action = "insert",
                            price = _oi.price,
                            quantity = _oi.quantity,
                            amount = _oi.price * _oi.quantity,
                            count = 1
                        };

                        qob.result.bids.Add(_boi);
                        _dqo.result.bids.Add(_boi);
                    }
                    else if (_bid.quantity != _oi.quantity)
                    {
                        var _boi = new SOrderBookItem
                        {
                            action = "update",
                            price = _oi.price,
                            quantity = _oi.quantity,
                            amount = _oi.price * _oi.quantity,
                            count = 1
                        };

                        _dqo.result.bids.Add(_boi);
                        _bid.quantity = _oi.quantity;
                    }
                }

                foreach (var _qi in qob.result.asks)
                {
                    var _ask = orderBook.asks.Where(o => o.price == _qi.price).SingleOrDefault();
                    if (_ask == null)
                    {
                        _dqo.result.asks.Add(new SOrderBookItem
                        {
                            action = "delete",
                            price = _qi.price,
                            quantity = _qi.quantity,
                            amount = _qi.price * _qi.quantity,
                            count = 1
                        });

                        _qi.quantity = 0;
                    }
                }

                foreach (var _qi in qob.result.bids)
                {
                    var _bid = orderBook.bids.Where(o => o.price == _qi.price).SingleOrDefault();
                    if (_bid == null)
                    {
                        _dqo.result.bids.Add(new SOrderBookItem
                        {
                            action = "delete",
                            price = _qi.price,
                            quantity = _qi.quantity,
                            amount = _qi.price * _qi.quantity,
                            count = 1
                        });

                        _qi.quantity = 0;
                    }
                }

                qob.result.asks.RemoveAll(o => o.quantity == 0);
                qob.result.bids.RemoveAll(o => o.quantity == 0);
            }

            if (++settings.orderbook_count == 2)
            {
                qob.sequentialId = orderBook.timestamp;
                await snapshotOrderbook(_dqo.exchange, _dqo.symbol);
            }
            else
                await publishOrderbook(_dqo);

            return true;
        }

        private void cleanOrderbook(SOrderBooks lob, SOrderBooks nob, decimal quantity, decimal price)
        {
            // orderbook에 해당 하는 price-level 보다 안쪽에 위치한 level들은 삭제 해야 한다.
            var _strange_asks = lob.result.asks.Where(o => o.price < price);
            if (_strange_asks.Count() > 0)
            {
                foreach (var _qox in _strange_asks)
                {
                    var _aoi = new SOrderBookItem
                    {
                        action = "delete",
                        price = _qox.price,
                        quantity = _qox.quantity,
                        amount = _qox.price * _qox.quantity,
                        count = 1
                    };

                    nob.result.asks.Add(_aoi);
                    _qox.quantity = 0;
                }
            }

            var _strange_bids = lob.result.bids.Where(o => o.price > price);
            if (_strange_bids.Count() > 0)
            {
                foreach (var _qox in _strange_bids)
                {
                    var _aoi = new SOrderBookItem
                    {
                        action = "delete",
                        price = _qox.price,
                        quantity = _qox.quantity,
                        amount = _qox.price * _qox.quantity,
                        count = 1
                    };

                    nob.result.bids.Add(_aoi);
                    _qox.quantity = 0;
                }
            }
        }

        private async Task<SOrderBooks> convertOrderbook(SOrderBook orderBook, string stream)
        {
            var _sob = new SOrderBooks
            {
                exchange = UPLogger.exchange_name,
                stream = stream,
                symbol = orderBook.symbol,
                sequentialId = orderBook.timestamp,
                result = new SOrderBook()
            };

            foreach (var _oi in orderBook.asks)
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

            foreach (var _oi in orderBook.bids)
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

        private async Task snapshotOrderbook(string exchange, string symbol)
        {
            if (exchange == UPLogger.exchange_name)
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

        private async Task publishOrderbook(SOrderBooks sob)
        {
            await Task.Delay(0);

            if (sob.result.asks.Count + sob.result.bids.Count > 0)
            {
                var _json_data = JsonConvert.SerializeObject(sob);
                OrderbookQ.Write(_json_data);
            }
        }

        private async Task publishTrading(SCompleteOrders str)
        {
            await Task.Delay(0);

            if (str != null)
            {
                var _json_data = JsonConvert.SerializeObject(str);
                TradingQ.Write(_json_data);
            }
        }

        private async Task publishTicker(STickers sbt)
        {
            await Task.Delay(0);

            if (sbt.result.Count > 0)
            {
                var _json_data = JsonConvert.SerializeObject(sbt);
                TickerQ.Write(_json_data);
            }
        }
    }
}