using CCXT.Collector.Library;
using CCXT.Collector.Library.Types;
using CCXT.Collector.Service;
using CCXT.Collector.Upbit.Public;
using CCXT.Collector.Upbit.Types;
using Newtonsoft.Json;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CCXT.Collector.Upbit
{
    public partial class Processing
    {
        private static ConcurrentDictionary<string, SOrderBook> __qOrderBooks = new ConcurrentDictionary<string, SOrderBook>();
        private static ConcurrentDictionary<string, Settings> __qSettings = new ConcurrentDictionary<string, Settings>();

        private async Task<bool> mergeTradeItem(UTradeItem tradeItem, string stream = "wscorders")
        {
            var _result = false;

            if (__qOrderBooks.ContainsKey(tradeItem.symbol) == true)
            {
                var _qob = __qOrderBooks[tradeItem.symbol];
                _qob.stream = stream;

                var _trade_items = new List<UTradeItem> { tradeItem };
                _result = await updateTradeItem(_qob, _trade_items, stream);

                if (KConfig.UpbitUsePublishTrade == true)
                {
                    var _str = new STrading(UPLogger.exchange_name, "wsctrades", tradeItem.symbol)
                    {
                        sequential_id = tradeItem.sequential_id
                    };

                    _str.data.Add(tradeItem);

                    //_str.data.Add(new UTradeItem
                    //{
                    //    ask_bid = tradeItem.ask_bid,
                    //    change = tradeItem.change,
                    //    change_price = tradeItem.change_price,
                    //    prev_closing_price = tradeItem.prev_closing_price,
                    //    sequential_id = tradeItem.sequential_id,
                    //    stream_type = tradeItem.stream_type,
                    //    symbol = tradeItem.symbol,
                    //    timestamp = tradeItem.timestamp,
                    //    trade_date = tradeItem.trade_date,
                    //    trade_price = tradeItem.trade_price,
                    //    trade_time = tradeItem.trade_time,
                    //    trade_timestamp = tradeItem.trade_timestamp,
                    //    trade_volume = tradeItem.trade_volume
                    //});

                    await publishTrading(_str);
                }
            }

            return _result;
        }

        private async Task<bool> mergeTradeItems(UATrade tradeItems, string stream = "apiorders")
        {
            var _result = false;

            if (__qOrderBooks.ContainsKey(tradeItems.symbol) == true)
            {
                var _qob = __qOrderBooks[tradeItems.symbol];
                _qob.stream = stream;

                if (tradeItems.data != null)
                    _result = await updateTradeItem(_qob, tradeItems.data.ToList<UTradeItem>(), stream);
            }

            return _result;
        }

        private async Task<bool> updateTradeItem(SOrderBook lob, List<UTradeItem> tradeItems, string stream)
        {
            var _nob = new SOrderBook(UPLogger.exchange_name, stream, lob.symbol)
            {
                sequential_id = tradeItems.Max(t => t.sequential_id)
            };

            var _settings = __qSettings.ContainsKey(lob.symbol) ? __qSettings[lob.symbol]
                          : __qSettings[lob.symbol] = new Settings();

            lock (__qOrderBooks)
            {
                _settings.before_trade_ask_size = lob.data.Where(o => o.side == "ask").Sum(o => o.quantity);
                _settings.before_trade_bid_size = lob.data.Where(o => o.side == "bid").Sum(o => o.quantity);

                foreach (var _t in tradeItems.OrderBy(t => t.sequential_id))
                {
                    if (_settings.last_trade_id >= _t.sequential_id)
                        continue;

                    _settings.last_trade_id = _t.sequential_id;

                    var _qoi = lob.data.Where(o => o.price == _t.trade_price).SingleOrDefault();
                    if (_qoi != null)
                    {
                        if (_qoi.quantity <= _t.trade_volume)
                        {
                            var _aoi = new SOrderBookItem
                            {
                                action = "delete",
                                side = _qoi.side,
                                price = _qoi.price,
                                quantity = _qoi.quantity
                            };

                            _nob.data.Add(_aoi);
                            _qoi.quantity = 0;
                        }
                        else
                        {
                            _qoi.quantity -= _t.trade_volume;

                            var _aoi = new SOrderBookItem
                            {
                                action = "update",
                                side = _qoi.side,
                                price = _qoi.price,
                                quantity = _qoi.quantity
                            };

                            _nob.data.Add(_aoi);
                        }
                    }

                    cleanOrderbook(lob, _nob, _t.trade_volume, _t.trade_price);
                }

                lob.sequential_id = _nob.sequential_id;
                lob.data.RemoveAll(o => o.quantity == 0);
            }

            if (_nob.data.Count > 0)
            {
                await publishOrderbook(_nob);

                _settings.orderbook_count = 0;
                _settings.trades_flag = true;
            }

            return true;
        }

        private async Task<bool> mergeOrderbook(UOrderBook orderbook)
        {
            var _result = false;

            var _settings = __qSettings.ContainsKey(orderbook.symbol) ? __qSettings[orderbook.symbol]
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

                var _current_ask_size = orderbook.total_ask_size;
                var _current_bid_size = orderbook.total_bid_size;

                if (_settings.last_order_ask_size != _current_ask_size || _settings.last_order_bid_size != _current_bid_size)
                {
                    if (_settings.before_trade_ask_size != _current_ask_size || _settings.before_trade_bid_size != _current_bid_size)
                    {
                        var _cob = await convertOrderbook(orderbook, "compbooks");

                        if (_settings.trades_flag == true)
                        {
                            var _t_ask = _tob.data.Where(o => o.side == "ask").OrderByDescending(o => o.price).LastOrDefault();
                            var _t_bid = _tob.data.Where(o => o.side == "bid").OrderByDescending(o => o.price).FirstOrDefault();

                            if (_t_ask != null && _t_bid != null)
                            {
                                _cob.data.RemoveAll(o => (o.side == "ask" && o.price < _t_ask.price) || (o.side == "bid" && o.price > _t_bid.price));

                                var _c_ask = _cob.data.Where(o => o.side == "ask" && o.price == _t_ask.price).SingleOrDefault();
                                var _c_bid = _cob.data.Where(o => o.side == "bid" && o.price == _t_bid.price).SingleOrDefault();

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

                        _settings.last_order_ask_size = _tob.data.Where(o => o.side == "ask").Sum(o => o.quantity);
                        _settings.last_order_bid_size = _tob.data.Where(o => o.side == "bid").Sum(o => o.quantity);
#if DEBUG
                        // modified check
                        if (_current_ask_size != _settings.last_order_ask_size || _current_bid_size != _settings.last_order_bid_size)
                            UPLogger.WriteQ($"diffb-{orderbook.type}: timestamp => {_settings.last_orderbook_time}, symbol => {orderbook.symbol}, ask_size => {_current_ask_size}, {_settings.last_order_ask_size}, bid_size => {_current_bid_size}, {_settings.last_order_bid_size}");

                        if (_tob.data.Count != 30)
                        {
                            var _ask_count = _tob.data.Where(o => o.side == "ask").Count();
                            var _bid_count = _tob.data.Where(o => o.side == "bid").Count();

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

        private async Task<bool> updateOrderbook(SOrderBook lob, Settings settings, SOrderBook cob)
        {
            var _dqo = new SOrderBook(UPLogger.exchange_name, "diffbooks", cob.symbol)
            {
                sequential_id = cob.sequential_id
            };

            lock (__qOrderBooks)
            {
                foreach (var _coi in cob.data)
                {
                    var _oi = lob.data.Where(o => o.side == _coi.side && o.price == _coi.price).SingleOrDefault();
                    if (_oi == null)
                    {
                        var _ioi = new SOrderBookItem
                        {
                            action = "insert",
                            side = _coi.side,
                            price = _coi.price,
                            quantity = _coi.quantity
                        };

                        _dqo.data.Add(_ioi);
                        lob.data.Add(_ioi);
                    }
                    else if (_oi.quantity != _coi.quantity)
                    {
                        _dqo.data.Add(new SOrderBookItem
                        {
                            action = "update",
                            side = _coi.side,
                            price = _coi.price,
                            quantity = _coi.quantity
                        });

                        _oi.quantity = _coi.quantity;
                    }
                }

                foreach (var _loi in lob.data)
                {
                    var _oi = cob.data.Where(o => o.side == _loi.side && o.price == _loi.price).SingleOrDefault();
                    if (_oi == null)
                    {
                        _dqo.data.Add(new SOrderBookItem
                        {
                            action = "delete",
                            side = _loi.side,
                            price = _loi.price,
                            quantity = _loi.quantity
                        });

                        _loi.quantity = 0;
                    }
                }

                lob.data.RemoveAll(o => o.quantity == 0);
            }

            if (++settings.orderbook_count == 2)
            {
                lob.sequential_id = cob.sequential_id;
                await snapshotOrderbook(_dqo.exchange, _dqo.symbol);
            }
            else
                await publishOrderbook(_dqo);

            return true;
        }

        private async Task<bool> updateOrderbook_old(SOrderBook qob, Settings settings, UOrderBook orderBook)
        {
            var _dqo = new SOrderBook(UPLogger.exchange_name, "diffbooks", orderBook.symbol)
            {
                sequential_id = orderBook.timestamp
            };

            lock (__qOrderBooks)
            {
                foreach (var _oi in orderBook.orderbook_units)
                {
                    var _ask = qob.data.Where(o => o.side == "ask" && o.price == _oi.ask_price).SingleOrDefault();
                    if (_ask == null)
                    {
                        var _aoi = new SOrderBookItem
                        {
                            action = "insert",
                            side = "ask",
                            price = _oi.ask_price,
                            quantity = _oi.ask_size
                        };

                        _dqo.data.Add(_aoi);
                        qob.data.Add(_aoi);
                    }
                    else if (_ask.quantity != _oi.ask_size)
                    {
                        var _aoi = new SOrderBookItem
                        {
                            action = "update",
                            side = "ask",
                            price = _oi.ask_price,
                            quantity = _oi.ask_size
                        };

                        _dqo.data.Add(_aoi);
                        _ask.quantity = _oi.ask_size;
                    }

                    var _bid = qob.data.Where(o => o.side == "bid" && o.price == _oi.bid_price).SingleOrDefault();
                    if (_bid == null)
                    {
                        var _boi = new SOrderBookItem
                        {
                            action = "insert",
                            side = "bid",
                            price = _oi.bid_price,
                            quantity = _oi.bid_size
                        };

                        qob.data.Add(_boi);
                        _dqo.data.Add(_boi);
                    }
                    else if (_bid.quantity != _oi.bid_size)
                    {
                        var _boi = new SOrderBookItem
                        {
                            action = "update",
                            side = "bid",
                            price = _oi.bid_price,
                            quantity = _oi.bid_size
                        };

                        _dqo.data.Add(_boi);
                        _bid.quantity = _oi.bid_size;
                    }
                }

                foreach (var _qi in qob.data)
                {
                    if (_qi.side == "ask")
                    {
                        var _ask = orderBook.orderbook_units.Where(o => o.ask_price == _qi.price).SingleOrDefault();
                        if (_ask == null)
                        {
                            _dqo.data.Add(new SOrderBookItem
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
                        var _bid = orderBook.orderbook_units.Where(o => o.bid_price == _qi.price).SingleOrDefault();
                        if (_bid == null)
                        {
                            _dqo.data.Add(new SOrderBookItem
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
                qob.sequential_id = orderBook.timestamp;
                await snapshotOrderbook(_dqo.exchange, _dqo.symbol);
            }
            else
                await publishOrderbook(_dqo);

            return true;
        }

        private void cleanOrderbook(SOrderBook lob, SOrderBook nob, decimal quantity, decimal price)
        {
            // orderbook에 해당 하는 price-level 보다 안쪽에 위치한 level들은 삭제 해야 한다.
            var _strange_levels = lob.data.Where(o => (o.side == "ask" && o.price < price) || (o.side == "bid" && o.price > price));
            if (_strange_levels.Count() > 0)
            {
                foreach (var _qox in _strange_levels)
                {
                    var _aoi = new SOrderBookItem
                    {
                        action = "delete",
                        side = _qox.side,
                        price = _qox.price,
                        quantity = _qox.quantity
                    };

                    nob.data.Add(_aoi);
                    _qox.quantity = 0;
                }

                UPLogger.WriteQ($"clean-orderbook: timestamp => {lob.sequential_id}, symbol => {lob.symbol}, price => {price}, quantity => {quantity}");
            }
        }

        private async Task<SOrderBook> convertOrderbook(UOrderBook orderBook, string stream)
        {
            var _sob = new SOrderBook(UPLogger.exchange_name, stream, orderBook.symbol)
            {
                sequential_id = orderBook.timestamp
            };

            foreach (var _oi in orderBook.orderbook_units)
            {
                _sob.data.Add(new SOrderBookItem
                {
                    action = "insert",
                    side = "ask",
                    price = _oi.ask_price,
                    quantity = _oi.ask_size
                });

                _sob.data.Add(new SOrderBookItem
                {
                    action = "insert",
                    side = "bid",
                    price = _oi.bid_price,
                    quantity = _oi.bid_size
                });
            }

            return await Task.FromResult(_sob);
        }

        private async Task snapshotOrderbook(string exchange, string symbol)
        {
            if (exchange == UPLogger.exchange_name)
            {
                var _sob = (SOrderBook)null;

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

        private async Task publishOrderbook(SOrderBook sob)
        {
            await Task.Delay(0);

            if (sob.data.Count > 0)
            {
                var _json_data = JsonConvert.SerializeObject(sob);
                OrderbookQ.Write(_json_data);
            }
        }

        private async Task publishTrading(STrading str)
        {
            await Task.Delay(0);

            if (str != null)
            {
                var _json_data = JsonConvert.SerializeObject(str);
                TradingQ.Write(_json_data);
            }
        }

        private async Task publishBookticker(SBookTicker sbt)
        {
            await Task.Delay(0);

            if (sbt.data.Count > 0)
            {
                var _json_data = JsonConvert.SerializeObject(sbt);
                BooktickerQ.Write(_json_data);
            }
        }
    }
}