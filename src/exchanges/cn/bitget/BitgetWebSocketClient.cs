using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CCXT.Collector.Core.Abstractions;
using CCXT.Collector.Library;
using CCXT.Collector.Service;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace CCXT.Collector.Bitget
{
    /*
     * Bitget Support Markets: USDT, USDC, BTC, ETH, BGB
     *
     * API Documentation:
     *     https://www.bitget.com/api-doc/spot/websocket/intro
     *     https://bitgetlimited.github.io/apidoc/en/spot/
     *
     * WebSocket API:
     *     https://www.bitget.com/api-doc/spot/websocket/connect
     *
     * Fees:
     *     https://www.bitget.com/fee
     */
    /// <summary>
    /// Bitget WebSocket client for real-time data streaming
    /// </summary>
    public class BitgetWebSocketClient : WebSocketClientBase
    {
        private readonly Dictionary<string, SOrderBook> _orderbookCache;
        private readonly Dictionary<string, long> _lastUpdateIds;

        public override string ExchangeName => "Bitget";
        protected override string WebSocketUrl => "wss://ws.bitget.com/v2/ws/public";
        protected override string PrivateWebSocketUrl => "wss://ws.bitget.com/v2/ws/private";
        protected override int PingIntervalMs => 30000; // 30 seconds

        public BitgetWebSocketClient()
        {
            _orderbookCache = new Dictionary<string, SOrderBook>();
            _lastUpdateIds = new Dictionary<string, long>();
        }

        protected override async Task ProcessMessageAsync(string message, bool isPrivate = false)
        {
            try
            {
                var json = JObject.Parse(message);

                // Handle ping/pong
                if (json["action"]?.ToString() == "ping")
                {
                    await HandlePingMessage(json);
                    return;
                }

                // Handle subscription responses
                if (json["event"]?.ToString() == "subscribe")
                {
                    var code = json["code"]?.ToString();
                    if (code != "0")
                    {
                        RaiseError($"Subscription failed: {json["msg"]}");
                    }
                    return;
                }

                // Handle data messages
                if (json["action"]?.ToString() == "update" || json["action"]?.ToString() == "snapshot")
                {
                    var arg = json["arg"];
                    if (arg != null)
                    {
                        var channel = arg["channel"]?.ToString();
                        var instId = arg["instId"]?.ToString();
                        
                        if (isPrivate)
                        {
                            // Handle private data
                            switch (channel)
                            {
                                case "account":
                                    await ProcessAccountData(json);
                                    break;
                                case "orders":
                                    await ProcessOrderData(json);
                                    break;
                                case "positions":
                                    await ProcessPositionData(json);
                                    break;
                            }
                        }
                        else
                        {
                            // Handle public data
                            switch (channel)
                            {
                                case "books":
                                case "books5":
                                case "books15":
                                    await ProcessOrderbookData(json);
                                    break;
                                case "trade":
                                    await ProcessTradeData(json);
                                    break;
                                case "ticker":
                                    await ProcessTickerData(json);
                                    break;
                                case "candle1m":
                                case "candle5m":
                                case "candle15m":
                                case "candle30m":
                                case "candle1H":
                                case "candle4H":
                                case "candle1D":
                                    await ProcessCandleData(json);
                                    break;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                RaiseError($"Message processing error: {ex.Message}");
            }
        }

        private async Task HandlePingMessage(JObject json)
        {
            var pong = new
            {
                action = "pong",
                ts = json["ts"]?.ToString()
            };
            await SendMessageAsync(JsonConvert.SerializeObject(pong));
        }

        private async Task ProcessOrderbookData(JObject json)
        {
            try
            {
                var arg = json["arg"];
                var instId = arg["instId"]?.ToString();
                var data = json["data"]?.FirstOrDefault();
                
                if (data == null || string.IsNullOrEmpty(instId))
                    return;

                var symbol = ConvertToStandardSymbol(instId);
                var timestamp = data["ts"]?.Value<long>() ?? DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                var action = json["action"]?.ToString();

                var orderbook = new SOrderBook
                {
                    exchange = ExchangeName,
                    symbol = symbol,
                    timestamp = timestamp,
                    result = new SOrderBookData
                    {
                        timestamp = timestamp,
                        asks = new List<SOrderBookItem>(),
                        bids = new List<SOrderBookItem>()
                    }
                };

                // Process asks
                var asks = data["asks"] as JArray;
                if (asks != null)
                {
                    foreach (var ask in asks)
                    {
                        var price = ask[0].Value<decimal>();
                        var amount = ask[1].Value<decimal>();
                        orderbook.result.asks.Add(new SOrderBookItem
                        {
                            price = price,
                            quantity = amount,
                            amount = price * amount
                        });
                    }
                }

                // Process bids
                var bids = data["bids"] as JArray;
                if (bids != null)
                {
                    foreach (var bid in bids)
                    {
                        var price = bid[0].Value<decimal>();
                        var amount = bid[1].Value<decimal>();
                        orderbook.result.bids.Add(new SOrderBookItem
                        {
                            price = price,
                            quantity = amount,
                            amount = price * amount
                        });
                    }
                }

                // Sort orderbook
                orderbook.result.asks = orderbook.result.asks.OrderBy(a => a.price).ToList();
                orderbook.result.bids = orderbook.result.bids.OrderByDescending(b => b.price).ToList();

                // Handle incremental updates
                if (action == "update" && _orderbookCache.ContainsKey(symbol))
                {
                    var cached = _orderbookCache[symbol];
                    MergeOrderbook(cached, orderbook);
                    InvokeOrderbookCallback(cached);
                }
                else
                {
                    _orderbookCache[symbol] = orderbook;
                    InvokeOrderbookCallback(orderbook);
                }
            }
            catch (Exception ex)
            {
                RaiseError($"Orderbook processing error: {ex.Message}");
            }
        }

        private void MergeOrderbook(SOrderBook cached, SOrderBook update)
        {
            // Merge asks
            foreach (var ask in update.result.asks)
            {
                var existing = cached.result.asks.FirstOrDefault(a => a.price == ask.price);
                if (existing != null)
                {
                    if (ask.quantity == 0)
                        cached.result.asks.Remove(existing);
                    else
                        existing.quantity = ask.quantity;
                }
                else if (ask.quantity > 0)
                {
                    cached.result.asks.Add(ask);
                }
            }

            // Merge bids
            foreach (var bid in update.result.bids)
            {
                var existing = cached.result.bids.FirstOrDefault(b => b.price == bid.price);
                if (existing != null)
                {
                    if (bid.quantity == 0)
                        cached.result.bids.Remove(existing);
                    else
                        existing.quantity = bid.quantity;
                }
                else if (bid.quantity > 0)
                {
                    cached.result.bids.Add(bid);
                }
            }

            cached.result.asks = cached.result.asks.OrderBy(a => a.price).ToList();
            cached.result.bids = cached.result.bids.OrderByDescending(b => b.price).ToList();
            cached.timestamp = update.timestamp;
        }

        private async Task ProcessTradeData(JObject json)
        {
            try
            {
                var arg = json["arg"];
                var instId = arg["instId"]?.ToString();
                var data = json["data"] as JArray;
                
                if (data == null || string.IsNullOrEmpty(instId))
                    return;

                var symbol = ConvertToStandardSymbol(instId);
                var trades = new List<STradeItem>();
                long latestTimestamp = 0;
                
                foreach (var trade in data)
                {
                    var timestamp = trade["ts"]?.Value<long>() ?? DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                    if (timestamp > latestTimestamp)
                        latestTimestamp = timestamp;

                    trades.Add(new STradeItem
                    {
                        orderId = trade["tradeId"]?.ToString(),
                        timestamp = timestamp,
                        price = trade["price"]?.Value<decimal>() ?? 0,
                        quantity = trade["size"]?.Value<decimal>() ?? 0,
                        amount = (trade["price"]?.Value<decimal>() ?? 0) * (trade["size"]?.Value<decimal>() ?? 0),
                        sideType = trade["side"]?.ToString() == "buy" ? SideType.Bid : SideType.Ask,
                        orderType = OrderType.Limit
                    });
                }

                if (trades.Count > 0)
                {
                    var completeOrder = new STrade
                    {
                        exchange = ExchangeName,
                        symbol = symbol,
                        timestamp = latestTimestamp,
                        result = trades
                    };

                    InvokeTradeCallback(completeOrder);
                }
            }
            catch (Exception ex)
            {
                RaiseError($"Trade processing error: {ex.Message}");
            }
        }

        private async Task ProcessTickerData(JObject json)
        {
            try
            {
                var arg = json["arg"];
                var instId = arg["instId"]?.ToString();
                var data = json["data"]?.FirstOrDefault();
                
                if (data == null || string.IsNullOrEmpty(instId))
                    return;

                var symbol = ConvertToStandardSymbol(instId);
                
                var ticker = new STicker
                {
                    exchange = ExchangeName,
                    symbol = symbol,
                    timestamp = data["ts"]?.Value<long>() ?? DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                    result = new STickerItem
                    {
                        timestamp = data["ts"]?.Value<long>() ?? DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                        closePrice = data["lastPr"]?.Value<decimal>() ?? 0,
                        askPrice = data["askPr"]?.Value<decimal>() ?? 0,
                        bidPrice = data["bidPr"]?.Value<decimal>() ?? 0,
                        openPrice = data["open24h"]?.Value<decimal>() ?? 0,
                        highPrice = data["high24h"]?.Value<decimal>() ?? 0,
                        lowPrice = data["low24h"]?.Value<decimal>() ?? 0,
                        volume = data["baseVolume"]?.Value<decimal>() ?? 0,
                        quoteVolume = data["quoteVolume"]?.Value<decimal>() ?? 0,
                        change = data["change24h"]?.Value<decimal>() ?? 0,
                        percentage = data["changeUtc24h"]?.Value<decimal>() ?? 0
                    }
                };

                InvokeTickerCallback(ticker);
            }
            catch (Exception ex)
            {
                RaiseError($"Ticker processing error: {ex.Message}");
            }
        }

        private async Task ProcessCandleData(JObject json)
        {
            try
            {
                var arg = json["arg"];
                var instId = arg["instId"]?.ToString();
                var channel = arg["channel"]?.ToString();
                var data = json["data"] as JArray;
                
                if (data == null || string.IsNullOrEmpty(instId))
                    return;

                var symbol = ConvertToStandardSymbol(instId);
                var interval = ConvertInterval(channel);
                var intervalMs = GetIntervalMilliseconds(interval);
                
                var candleItems = new List<SCandleItem>();
                long latestTimestamp = 0;

                foreach (var candle in data)
                {
                    var timestamp = candle["ts"]?.Value<long>() ?? 0;
                    if (timestamp > latestTimestamp)
                        latestTimestamp = timestamp;

                    candleItems.Add(new SCandleItem
                    {
                        openTime = timestamp,
                        closeTime = timestamp + intervalMs,
                        open = candle["o"]?.Value<decimal>() ?? 0,
                        high = candle["h"]?.Value<decimal>() ?? 0,
                        low = candle["l"]?.Value<decimal>() ?? 0,
                        close = candle["c"]?.Value<decimal>() ?? 0,
                        volume = candle["v"]?.Value<decimal>() ?? 0
                    });
                }

                if (candleItems.Count > 0)
                {
                    var candlestick = new SCandle
                    {
                        exchange = ExchangeName,
                        symbol = symbol,
                        interval = interval,
                        timestamp = latestTimestamp,
                        result = candleItems
                    };

                    InvokeCandleCallback(candlestick);
                }
            }
            catch (Exception ex)
            {
                RaiseError($"Candle processing error: {ex.Message}");
            }
        }

        private async Task ProcessAccountData(JObject json)
        {
            try
            {
                var data = json["data"] as JArray;
                if (data == null) return;

                var balanceItems = new List<SBalanceItem>();
                var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

                foreach (var account in data)
                {
                    var currency = account["ccy"]?.ToString();
                    var free = account["availBal"]?.Value<decimal>() ?? 0;
                    var used = account["frozenBal"]?.Value<decimal>() ?? 0;
                    var total = account["bal"]?.Value<decimal>() ?? 0;
                    
                    if (total > 0 || free > 0 || used > 0)
                    {
                        balanceItems.Add(new SBalanceItem
                        {
                            currency = currency,
                            free = free,
                            used = used,
                            total = total,
                            updateTime = timestamp
                        });
                    }
                }

                if (balanceItems.Count > 0)
                {
                    var balance = new SBalance
                    {
                        exchange = ExchangeName,
                        accountId = "spot",
                        timestamp = timestamp,
                        balances = balanceItems
                    };

                    InvokeBalanceCallback(balance);
                }
            }
            catch (Exception ex)
            {
                RaiseError($"Account processing error: {ex.Message}");
            }
        }

        private async Task ProcessOrderData(JObject json)
        {
            try
            {
                var data = json["data"] as JArray;
                if (data == null) return;

                var orderList = new List<SOrderItem>();
                var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

                foreach (var order in data)
                {
                    orderList.Add(new SOrderItem
                    {
                        orderId = order["orderId"]?.ToString(),
                        clientOrderId = order["clientOid"]?.ToString(),
                        symbol = ConvertToStandardSymbol(order["instId"]?.ToString()),
                        side = order["side"]?.ToString() == "buy" ? OrderSide.Buy : OrderSide.Sell,
                        type = ConvertOrderType(order["orderType"]?.ToString()),
                        status = ConvertOrderStatus(order["status"]?.ToString()),
                        price = order["price"]?.Value<decimal>() ?? 0,
                        quantity = order["size"]?.Value<decimal>() ?? 0,
                        filledQuantity = order["fillSize"]?.Value<decimal>() ?? 0,
                        createTime = order["cTime"]?.Value<long>() ?? timestamp,
                        updateTime = order["uTime"]?.Value<long>() ?? timestamp
                    });
                }

                if (orderList.Count > 0)
                {
                    var orders = new SOrder
                    {
                        exchange = ExchangeName,
                        timestamp = timestamp,
                        orders = orderList
                    };

                    InvokeOrderCallback(orders);
                }
            }
            catch (Exception ex)
            {
                RaiseError($"Order processing error: {ex.Message}");
            }
        }

        private async Task ProcessPositionData(JObject json)
        {
            try
            {
                var data = json["data"] as JArray;
                if (data == null) return;

                var positionList = new List<SPositionItem>();
                var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

                foreach (var pos in data)
                {
                    positionList.Add(new SPositionItem
                    {
                        symbol = ConvertToStandardSymbol(pos["instId"]?.ToString()),
                        side = pos["holdSide"]?.ToString() == "long" ? PositionSide.Long : PositionSide.Short,
                        size = pos["total"]?.Value<decimal>() ?? 0,
                        entryPrice = pos["averageOpenPrice"]?.Value<decimal>() ?? 0,
                        markPrice = pos["markPrice"]?.Value<decimal>() ?? 0,
                        unrealizedPnl = pos["unrealizedPL"]?.Value<decimal>() ?? 0,
                        realizedPnl = pos["achievedProfits"]?.Value<decimal>() ?? 0
                    });
                }

                if (positionList.Count > 0)
                {
                    var positions = new SPosition
                    {
                        exchange = ExchangeName,
                        timestamp = timestamp,
                        positions = positionList
                    };

                    InvokePositionCallback(positions);
                }
            }
            catch (Exception ex)
            {
                RaiseError($"Position processing error: {ex.Message}");
            }
        }

        public override async Task<bool> SubscribeOrderbookAsync(string symbol)
        {
            try
            {
                var instId = ConvertToExchangeSymbol(symbol);
                var subscription = new
                {
                    op = "subscribe",
                    args = new[]
                    {
                        new
                        {
                            instType = "SPOT",
                            channel = "books",
                            instId = instId
                        }
                    }
                };

                await SendMessageAsync(JsonConvert.SerializeObject(subscription));

                var key = CreateSubscriptionKey("orderbook", symbol);
                _subscriptions[key] = new SubscriptionInfo
                {
                    Channel = "books",
                    Symbol = symbol,
                    SubscribedAt = DateTime.UtcNow,
                    IsActive = true
                };

                return true;
            }
            catch (Exception ex)
            {
                RaiseError($"Orderbook subscription error: {ex.Message}");
                return false;
            }
        }

        public override async Task<bool> SubscribeTradesAsync(string symbol)
        {
            try
            {
                var instId = ConvertToExchangeSymbol(symbol);
                var subscription = new
                {
                    op = "subscribe",
                    args = new[]
                    {
                        new
                        {
                            instType = "SPOT",
                            channel = "trade",
                            instId = instId
                        }
                    }
                };

                await SendMessageAsync(JsonConvert.SerializeObject(subscription));

                var key = CreateSubscriptionKey("trades", symbol);
                _subscriptions[key] = new SubscriptionInfo
                {
                    Channel = "trade",
                    Symbol = symbol,
                    SubscribedAt = DateTime.UtcNow,
                    IsActive = true
                };

                return true;
            }
            catch (Exception ex)
            {
                RaiseError($"Trades subscription error: {ex.Message}");
                return false;
            }
        }

        public override async Task<bool> SubscribeTickerAsync(string symbol)
        {
            try
            {
                var instId = ConvertToExchangeSymbol(symbol);
                var subscription = new
                {
                    op = "subscribe",
                    args = new[]
                    {
                        new
                        {
                            instType = "SPOT",
                            channel = "ticker",
                            instId = instId
                        }
                    }
                };

                await SendMessageAsync(JsonConvert.SerializeObject(subscription));

                var key = CreateSubscriptionKey("ticker", symbol);
                _subscriptions[key] = new SubscriptionInfo
                {
                    Channel = "ticker",
                    Symbol = symbol,
                    SubscribedAt = DateTime.UtcNow,
                    IsActive = true
                };

                return true;
            }
            catch (Exception ex)
            {
                RaiseError($"Ticker subscription error: {ex.Message}");
                return false;
            }
        }

        public override async Task<bool> SubscribeCandlesAsync(string symbol, string interval)
        {
            try
            {
                var instId = ConvertToExchangeSymbol(symbol);
                var channelInterval = ConvertToChannelInterval(interval);
                
                var subscription = new
                {
                    op = "subscribe",
                    args = new[]
                    {
                        new
                        {
                            instType = "SPOT",
                            channel = $"candle{channelInterval}",
                            instId = instId
                        }
                    }
                };

                await SendMessageAsync(JsonConvert.SerializeObject(subscription));

                var key = CreateSubscriptionKey($"candle{channelInterval}", symbol);
                _subscriptions[key] = new SubscriptionInfo
                {
                    Channel = $"candle{channelInterval}",
                    Symbol = symbol,
                    SubscribedAt = DateTime.UtcNow,
                    IsActive = true
                };

                return true;
            }
            catch (Exception ex)
            {
                RaiseError($"Candles subscription error: {ex.Message}");
                return false;
            }
        }

        public override async Task<bool> UnsubscribeAsync(string channel, string symbol)
        {
            try
            {
                var instId = ConvertToExchangeSymbol(symbol);
                var unsubscription = new
                {
                    op = "unsubscribe",
                    args = new[]
                    {
                        new
                        {
                            instType = "SPOT",
                            channel = channel,
                            instId = instId
                        }
                    }
                };

                await SendMessageAsync(JsonConvert.SerializeObject(unsubscription));

                var key = CreateSubscriptionKey(channel, symbol);
                if (_subscriptions.TryRemove(key, out var sub))
                {
                    sub.IsActive = false;
                }

                return true;
            }
            catch (Exception ex)
            {
                RaiseError($"Unsubscribe error: {ex.Message}");
                return false;
            }
        }

        protected override string CreatePingMessage()
        {
            return JsonConvert.SerializeObject(new { action = "ping" });
        }

        protected override string CreateAuthenticationMessage(string apiKey, string secretKey)
        {
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
            var method = "GET";
            var requestPath = "/user/verify";
            
            var sign = GenerateSignature(secretKey, timestamp, method, requestPath);
            
            return JsonConvert.SerializeObject(new
            {
                op = "login",
                args = new[]
                {
                    new
                    {
                        apiKey = apiKey,
                        passphrase = "", // Bitget may require passphrase
                        timestamp = timestamp,
                        sign = sign
                    }
                }
            });
        }

        private string GenerateSignature(string secretKey, string timestamp, string method, string requestPath, string body = "")
        {
            var message = timestamp + method + requestPath + body;
            var encoding = new System.Text.ASCIIEncoding();
            byte[] keyByte = encoding.GetBytes(secretKey);
            byte[] messageBytes = encoding.GetBytes(message);
            using (var hmacsha256 = new System.Security.Cryptography.HMACSHA256(keyByte))
            {
                byte[] hashmessage = hmacsha256.ComputeHash(messageBytes);
                return Convert.ToBase64String(hashmessage);
            }
        }

        private string ConvertToExchangeSymbol(string symbol)
        {
            // Convert from standard format (BTC/USDT) to Bitget format (BTCUSDT_SPBL)
            return symbol.Replace("/", "") + "_SPBL";
        }

        private string ConvertToStandardSymbol(string instId)
        {
            // Convert from Bitget format (BTCUSDT_SPBL) to standard format (BTC/USDT)
            if (instId.EndsWith("_SPBL"))
            {
                instId = instId.Replace("_SPBL", "");
            }
            
            // Simple conversion - may need more sophisticated logic
            if (instId.EndsWith("USDT"))
            {
                var baseCurrency = instId.Replace("USDT", "");
                return $"{baseCurrency}/USDT";
            }
            else if (instId.EndsWith("USDC"))
            {
                var baseCurrency = instId.Replace("USDC", "");
                return $"{baseCurrency}/USDC";
            }
            else if (instId.EndsWith("BTC"))
            {
                var baseCurrency = instId.Replace("BTC", "");
                return $"{baseCurrency}/BTC";
            }
            
            return instId;
        }

        private string ConvertToChannelInterval(string interval)
        {
            return interval switch
            {
                "1m" => "1m",
                "5m" => "5m",
                "15m" => "15m",
                "30m" => "30m",
                "1h" => "1H",
                "4h" => "4H",
                "1d" => "1D",
                "1w" => "1W",
                _ => "1m"
            };
        }

        private string ConvertInterval(string channel)
        {
            if (channel == null) return "1m";
            
            return channel.Replace("candle", "") switch
            {
                "1m" => "1m",
                "5m" => "5m",
                "15m" => "15m",
                "30m" => "30m",
                "1H" => "1h",
                "4H" => "4h",
                "1D" => "1d",
                "1W" => "1w",
                _ => "1m"
            };
        }

        private OrderType ConvertOrderType(string type)
        {
            return type?.ToLower() switch
            {
                "limit" => OrderType.Limit,
                "market" => OrderType.Market,
                _ => OrderType.Limit
            };
        }

        private OrderStatus ConvertOrderStatus(string status)
        {
            return status?.ToLower() switch
            {
                "new" => OrderStatus.Open,
                "init" => OrderStatus.Open,
                "partial-fill" => OrderStatus.PartiallyFilled,
                "full-fill" => OrderStatus.Filled,
                "cancelled" => OrderStatus.Canceled,
                _ => OrderStatus.Open
            };
        }

        private long GetIntervalMilliseconds(string interval)
        {
            return interval switch
            {
                "1m" => 60000,
                "5m" => 300000,
                "15m" => 900000,
                "30m" => 1800000,
                "1h" => 3600000,
                "4h" => 14400000,
                "1d" => 86400000,
                "1w" => 604800000,
                _ => 60000
            };
        }
    }
}