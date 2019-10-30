using CCXT.Collector.Binance.Public;
using CCXT.Collector.Library;
using CCXT.Collector.Library.Types;
using CCXT.Collector.Service;
using Newtonsoft.Json;
using OdinSdk.BaseLib.Coin.Types;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CCXT.Collector.Binance
{
    public partial class Processing
    {
        private static ConcurrentDictionary<string, SOrderBooks> __qOrderBooks = new ConcurrentDictionary<string, SOrderBooks>();
        private static ConcurrentDictionary<string, Settings> __qSettings = new ConcurrentDictionary<string, Settings>();

        private async ValueTask<bool> mergeTradeItem(BTradeItem tradeItem, string stream = "wsctrades")
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

        private async ValueTask<bool> mergeTradeItems(BATrade tradeItems, string stream = "apitrades")
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

        private async ValueTask<bool> updateTradeItem(SOrderBooks qob, List<BTradeItem> tradeItems, string stream)
        {
            var _rqo = new SOrderBooks
            {
                exchange = BNLogger.exchange_name, 
                stream = stream, 
                symbol = qob.symbol,
                sequentialId = tradeItems.Max(t => t.timestamp),
                result = new SOrderBook()
            };

            var _settings = __qSettings.ContainsKey(qob.symbol) 
                          ? __qSettings[qob.symbol]
                          : __qSettings[qob.symbol] = new Settings();

            lock (__qOrderBooks)
            {
                _settings.before_trade_ask_size = qob.result.asks.Sum(o => o.quantity);
                _settings.before_trade_bid_size = qob.result.bids.Sum(o => o.quantity);

                foreach (var _t in tradeItems.OrderBy(t => t.timestamp))
                {
                    if (_settings.last_trade_time >= _t.timestamp)
                        continue;

                    _settings.last_trade_time = _t.timestamp;
                    
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

                            _rqo.result.asks.Add(_aoi);
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

                            _rqo.result.asks.Add(_aoi);
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

                            _rqo.result.bids.Add(_aoi);
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

                            _rqo.result.bids.Add(_aoi);
                        }
                    }

                    // orderbook에 해당 하는 price-level 보다 안쪽에 위치한 level들은 삭제 해야 한다.
                    var _strange_asks = qob.result.asks.Where(o => o.price < _t.price);
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

                            _rqo.result.asks.Add(_aoi);
                            _qox.quantity = 0;
                        }
                    }

                    var _strange_bids = qob.result.bids.Where(o => o.price > _t.price);
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

                            _rqo.result.bids.Add(_aoi);
                            _qox.quantity = 0;
                        }
                    }
                }

                qob.sequentialId = _rqo.sequentialId;

                qob.result.asks.RemoveAll(o => o.quantity == 0);
                qob.result.bids.RemoveAll(o => o.quantity == 0);
            }

            if (_rqo.result.asks.Count + _rqo.result.bids.Count > 0)
            {
                await publishOrderbook(_rqo);
                //_settings.orderbook_count = 0;
            }

            return true;
        }

        private async ValueTask<bool> mergeOrderbook(BAOrderBook orderBook)
        {
            var _result = false;

            var _settings = __qSettings.ContainsKey(orderBook.data.symbol) 
                          ? __qSettings[orderBook.data.symbol]
                          : __qSettings[orderBook.data.symbol] = new Settings();

            if (__qOrderBooks.ContainsKey(orderBook.data.symbol) == false)
            {
                _settings.last_orderbook_id = orderBook.data.lastId;

                var _sqo = new SOrderBooks
                {
                    exchange = BNLogger.exchange_name,
                    stream = "snapshot",
                    symbol = orderBook.data.symbol,
                    sequentialId = orderBook.data.lastId,
                    result = new SOrderBook()
                };

                foreach (var _oi in orderBook.data.asks)
                {
                    _sqo.result.asks.Add(new SOrderBookItem
                    {
                        action = "insert",
                        price = _oi[0],
                        quantity = _oi[1],
                        amount = _oi[0] * _oi[1],
                        count = 1
                    });
                }

                foreach (var _oi in orderBook.data.bids)
                {
                    _sqo.result.bids.Add(new SOrderBookItem
                    {
                        action = "insert",
                        price = _oi[0],
                        quantity = _oi[1],
                        amount = _oi[0] * _oi[1],
                        count = 1
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

                        _settings.last_order_ask_size = _qob.result.asks.Sum(o => o.quantity);
                        _settings.last_order_bid_size = _qob.result.bids.Sum(o => o.quantity);
#if DEBUG
                        // modified check
                        if (_current_ask_size != _settings.last_order_ask_size || _current_bid_size != _settings.last_order_bid_size)
                            BNLogger.WriteQ($"diffb-{orderBook.stream}: timestamp => {_settings.last_orderbook_id}, symbol => {orderBook.data.symbol}, ask_size => {_current_ask_size}, {_settings.last_order_ask_size}, bid_size => {_current_bid_size}, {_settings.last_order_bid_size}");

                        if (_qob.result.asks.Count + _qob.result.bids.Count != 40)
                        {
                            var _ask_count = _qob.result.asks.Count();
                            var _bid_count = _qob.result.bids.Count();

                            BNLogger.WriteQ($"diffb-{orderBook.stream}: timestamp => {_settings.last_orderbook_id}, symbol => {orderBook.data.symbol}, ask_count => {_ask_count}, bid_count => {_bid_count}");
                        }
#endif
                    }
                    else
                        BNLogger.WriteQ($"trade-{orderBook.stream}: timestamp => {_settings.last_orderbook_id}, symbol => {orderBook.data.symbol}, ask_size => {_current_ask_size}, bid_size => {_current_bid_size}");
                }
                else
                    BNLogger.WriteQ($"equal-{orderBook.stream}: timestamp => {_settings.last_orderbook_id}, symbol => {orderBook.data.symbol}, ask_size => {_current_ask_size}, bid_size => {_current_bid_size}");
            }
            //else
            //    BLogger.WriteQ($"pastb-{orderBook.stream}: timestamp => {_settings.last_orderbook_id}, symbol => {orderBook.data.symbol}, ask_size => {_settings.last_order_ask_size}, bid_size => {_settings.last_order_bid_size}");

            return _result;
        }

        private async ValueTask<bool> updateOrderbook(SOrderBooks qob, Settings settings, BAOrderBook orderBook)
        {
            var _dqo = new SOrderBooks
            {
                exchange = BNLogger.exchange_name,
                stream = "diffbooks",
                symbol = orderBook.data.symbol,
                sequentialId = orderBook.data.lastId,
                result = new SOrderBook()
            };

            lock (__qOrderBooks)
            {
                foreach (var _oi in orderBook.data.asks)
                {
                    var _ask = qob.result.asks.Where(o => o.price == _oi[0]).SingleOrDefault();
                    if (_ask == null)
                    {
                        var _aoi = new SOrderBookItem
                        {
                            action = "insert",
                            price = _oi[0],
                            quantity = _oi[1],
                            amount = _oi[0] * _oi[1],
                            count = 1
                        };

                        _dqo.result.asks.Add(_aoi);
                        qob.result.asks.Add(_aoi);
                    }
                    else if (_ask.quantity != _oi[1])
                    {
                        var _aoi = new SOrderBookItem
                        {
                            action = "update",
                            price = _oi[0],
                            quantity = _oi[1],
                            amount = _oi[0] * _oi[1],
                            count = 1
                        };

                        _dqo.result.asks.Add(_aoi);
                        _ask.quantity = _oi[1];
                    }
                }

                foreach (var _oi in orderBook.data.bids)
                {
                    var _bid = qob.result.bids.Where(o => o.price == _oi[0]).SingleOrDefault();
                    if (_bid == null)
                    {
                        var _boi = new SOrderBookItem
                        {
                            action = "insert",
                            price = _oi[0],
                            quantity = _oi[1],
                            amount = _oi[0] * _oi[1],
                            count = 1
                        };

                        _dqo.result.bids.Add(_boi);
                        qob.result.bids.Add(_boi);
                    }
                    else if (_bid.quantity != _oi[1])
                    {
                        var _boi = new SOrderBookItem
                        {
                            action = "update",
                            price = _oi[0],
                            quantity = _oi[1],
                            amount = _oi[0] * _oi[1],
                            count = 1
                        };

                        _dqo.result.bids.Add(_boi);
                        _bid.quantity = _oi[1];
                    }
                }

                foreach (var _qi in qob.result.asks)
                {
                    var _ask = orderBook.data.asks.Where(o => o[0] == _qi.price).SingleOrDefault();
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
                    var _bid = orderBook.data.bids.Where(o => o[0] == _qi.price).SingleOrDefault();
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

            if (++settings.orderbook_count == KConfig.BinanceOrderBookCounter)
            {
                settings.orderbook_count = 0;

                qob.sequentialId = orderBook.data.lastId;
                await snapshotOrderbook(_dqo.exchange, _dqo.symbol);
            }
            else
                await publishOrderbook(_dqo);

            return true;
        }

        private async Task snapshotOrderbook(string exchange, string symbol)
        {
            if (exchange == BNLogger.exchange_name)
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