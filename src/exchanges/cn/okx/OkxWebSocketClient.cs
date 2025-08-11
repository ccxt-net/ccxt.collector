using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CCXT.Collector.Core.Abstractions;
using CCXT.Collector.Models.WebSocket;
using CCXT.Collector.Library;
using CCXT.Collector.Service;
using System.Text.Json;

namespace CCXT.Collector.Okx
{
    /*
     * OKX Support Markets: USDT, USDC, BTC, ETH
     *
     * API Documentation:
     *     https://www.okx.com/docs-v5/en/
     *
     * WebSocket API:
     *     https://www.okx.com/docs-v5/en/#websocket-api
     *     https://www.okx.com/docs-v5/en/#websocket-api-public-channel
     *
     * Fees:
     *     https://www.okx.com/fees
     */
    /// <summary>
    /// OKX WebSocket client for real-time data streaming
    /// </summary>
    public class OkxWebSocketClient : WebSocketClientBase
    {
        private readonly Dictionary<string, SOrderBook> _orderbookCache;
        private readonly Dictionary<string, long> _lastUpdateIds;

        public override string ExchangeName => "OKX";
        protected override string WebSocketUrl => "wss://ws.okx.com:8443/ws/v5/public";
        protected override string PrivateWebSocketUrl => "wss://ws.okx.com:8443/ws/v5/private";
        protected override int PingIntervalMs => 30000; // 30 seconds

        public OkxWebSocketClient()
        {
            _orderbookCache = new Dictionary<string, SOrderBook>();
            _lastUpdateIds = new Dictionary<string, long>();
        }

        protected override async Task ProcessMessageAsync(string message, bool isPrivate = false)
        {
            try
            {
                using var doc = JsonDocument.Parse(message);
                var json = doc.RootElement;

                // Handle ping/pong
                if (json.TryGetProperty("op", out var op) && op.GetString() == "ping")
                {
                    await HandlePingMessage();
                    return;
                }

                // Handle subscription response
                if (json.TryGetProperty("event", out var eventProp))
                {
                    var eventType = eventProp.GetString();
                    if (eventType == "subscribe")
                    {
                        var code = json.GetStringOrDefault("code");
                        if (code != "0")
                        {
                            RaiseError($"Subscription failed: {json.GetStringOrDefault("msg")}");
                        }
                        return;
                    }
                    else if (eventType == "error")
                    {
                        RaiseError($"OKX error: {json.GetStringOrDefault("msg")}");
                        return;
                    }
                }

                // Handle data messages
                if (json.TryGetProperty("arg", out var arg) && json.TryGetProperty("data", out var data))
                {
                    var channel = arg.GetStringOrDefault("channel");
                    var instId = arg.GetStringOrDefault("instId");

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
                            case "books-l2-tbt":
                            case "books50-l2-tbt":
                                await ProcessOrderbookData(json);
                                break;
                            case "trades":
                                await ProcessTradeData(json);
                                break;
                            case "tickers":
                                await ProcessTickerData(json);
                                break;
                            case "candle1m":
                            case "candle3m":
                            case "candle5m":
                            case "candle15m":
                            case "candle30m":
                            case "candle1H":
                            case "candle2H":
                            case "candle4H":
                            case "candle6H":
                            case "candle12H":
                            case "candle1D":
                            case "candle1W":
                            case "candle1M":
                                await ProcessCandleData(json);
                                break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                RaiseError($"Message processing error: {ex.Message}");
            }
        }

        private async Task HandlePingMessage()
        {
            var pong = new { op = "pong" };
            await SendMessageAsync(JsonSerializer.Serialize(pong));
        }

        private async Task ProcessOrderbookData(JsonElement json)
        {
            try
            {
                if (!json.TryGetProperty("arg", out var arg))
                    return;

                var instId = arg.GetStringOrDefault("instId");
                if (String.IsNullOrEmpty(instId))
                    return;

                if (!json.TryGetArray("data", out var dataArray))
                    return;

                foreach (var data in dataArray.EnumerateArray())
                {
                    var symbol = ConvertToStandardSymbol(instId);
                    var timestamp = data.GetInt64OrDefault("ts", TimeExtension.UnixTime);
                    var action = json.GetStringOrDefault("action", "snapshot");

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
                    if (data.TryGetArray("asks", out var asks))
                    {
                        foreach (var ask in asks.EnumerateArray())
                        {
                            if (ask.GetArrayLength() < 4)
                                continue;

                            var price = ask[0].GetDecimalValue();
                            var quantity = ask[1].GetDecimalValue();
                            var orders = ask[3].GetInt32Value();

                            if (quantity > 0)
                            {
                                orderbook.result.asks.Add(new SOrderBookItem
                                {
                                    price = price,
                                    quantity = quantity,
                                    amount = price * quantity,
                                    count = orders
                                });
                            }
                        }
                    }

                    // Process bids
                    if (data.TryGetArray("bids", out var bids))
                    {
                        foreach (var bid in bids.EnumerateArray())
                        {
                            if (bid.GetArrayLength() < 4)
                                continue;

                            var price = bid[0].GetDecimalValue();
                            var quantity = bid[1].GetDecimalValue();
                            var orders = bid[3].GetInt32Value();

                            if (quantity > 0)
                            {
                                orderbook.result.bids.Add(new SOrderBookItem
                                {
                                    price = price,
                                    quantity = quantity,
                                    amount = price * quantity,
                                    count = orders
                                });
                            }
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
                    {
                        existing.quantity = ask.quantity;
                        existing.amount = ask.amount;
                        existing.count = ask.count;
                    }
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
                    {
                        existing.quantity = bid.quantity;
                        existing.amount = bid.amount;
                        existing.count = bid.count;
                    }
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

        private async Task ProcessTradeData(JsonElement json)
        {
            try
            {
                if (!json.TryGetProperty("arg", out var arg))
                    return;

                var instId = arg.GetStringOrDefault("instId");
                if (String.IsNullOrEmpty(instId))
                    return;

                if (!json.TryGetArray("data", out var dataArray))
                    return;

                var symbol = ConvertToStandardSymbol(instId);
                var trades = new List<STradeItem>();
                long latestTimestamp = 0;

                foreach (var trade in dataArray.EnumerateArray())
                {
                    var timestamp = trade.GetInt64OrDefault("ts", TimeExtension.UnixTime);
                    if (timestamp > latestTimestamp)
                        latestTimestamp = timestamp;

                    trades.Add(new STradeItem
                    {
                        tradeId = trade.GetStringOrDefault("tradeId"),
                        timestamp = timestamp,
                        price = trade.GetDecimalOrDefault("px"),
                        quantity = trade.GetDecimalOrDefault("sz"),
                        amount = trade.GetDecimalOrDefault("px") * trade.GetDecimalOrDefault("sz"),
                        sideType = trade.GetStringOrDefault("side") == "buy" ? SideType.Bid : SideType.Ask,
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

        private async Task ProcessTickerData(JsonElement json)
        {
            try
            {
                if (!json.TryGetProperty("arg", out var arg))
                    return;

                var instId = arg.GetStringOrDefault("instId");
                if (String.IsNullOrEmpty(instId))
                    return;

                if (!json.TryGetArray("data", out var dataArray))
                    return;

                foreach (var data in dataArray.EnumerateArray())
                {
                    var symbol = ConvertToStandardSymbol(instId);
                    var timestamp = data.GetInt64OrDefault("ts", TimeExtension.UnixTime);

                    var ticker = new STicker
                    {
                        exchange = ExchangeName,
                        symbol = symbol,
                        timestamp = timestamp,
                        result = new STickerItem
                        {
                            timestamp = timestamp,
                            closePrice = data.GetDecimalOrDefault("last"),
                            askPrice = data.GetDecimalOrDefault("askPx"),
                            bidPrice = data.GetDecimalOrDefault("bidPx"),
                            askQuantity = data.GetDecimalOrDefault("askSz"),
                            bidQuantity = data.GetDecimalOrDefault("bidSz"),
                            openPrice = data.GetDecimalOrDefault("open24h"),
                            highPrice = data.GetDecimalOrDefault("high24h"),
                            lowPrice = data.GetDecimalOrDefault("low24h"),
                            volume = data.GetDecimalOrDefault("vol24h"),
                            quoteVolume = data.GetDecimalOrDefault("volCcy24h"),
                            change = data.GetDecimalOrDefault("last") - data.GetDecimalOrDefault("open24h"),
                            percentage = CalculatePercentage(data.GetDecimalOrDefault("open24h"), data.GetDecimalOrDefault("last"))
                        }
                    };

                    InvokeTickerCallback(ticker);
                }
            }
            catch (Exception ex)
            {
                RaiseError($"Ticker processing error: {ex.Message}");
            }
        }

        private async Task ProcessCandleData(JsonElement json)
        {
            try
            {
                if (!json.TryGetProperty("arg", out var arg))
                    return;

                var channel = arg.GetStringOrDefault("channel");
                var instId = arg.GetStringOrDefault("instId");
                if (String.IsNullOrEmpty(instId))
                    return;

                if (!json.TryGetArray("data", out var dataArray))
                    return;

                var symbol = ConvertToStandardSymbol(instId);
                var interval = ConvertInterval(channel);
                var candleItems = new List<SCandleItem>();
                long latestTimestamp = 0;

                foreach (var candle in dataArray.EnumerateArray())
                {
                    if (candle.GetArrayLength() >= 9)
                    {
                        var timestamp = candle[0].GetInt64Value();
                        if (timestamp > latestTimestamp)
                            latestTimestamp = timestamp;

                        candleItems.Add(new SCandleItem
                        {
                            openTime = timestamp,
                            closeTime = timestamp + GetIntervalMilliseconds(interval),
                            open = candle[1].GetDecimalValue(),
                            high = candle[2].GetDecimalValue(),
                            low = candle[3].GetDecimalValue(),
                            close = candle[4].GetDecimalValue(),
                            volume = candle[5].GetDecimalValue(),
                            quoteVolume = candle[6].GetDecimalValue(),
                            isClosed = candle[8].GetInt32Value() == 1
                        });
                    }
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

        private async Task ProcessAccountData(JsonElement json)
        {
            try
            {
                if (!json.TryGetArray("data", out var dataArray))
                    return;

                foreach (var data in dataArray.EnumerateArray())
                {
                    if (data.TryGetArray("details", out var details))
                    {
                        var balanceItems = new List<SBalanceItem>();
                        var timestamp = data.GetInt64OrDefault("uTime", TimeExtension.UnixTime);

                        foreach (var detail in details.EnumerateArray())
                        {
                            var currency = detail.GetStringOrDefault("ccy");
                            var cashBal = detail.GetDecimalOrDefault("cashBal");
                            var frozenBal = detail.GetDecimalOrDefault("frozenBal");

                            if (cashBal > 0 || frozenBal > 0)
                            {
                                balanceItems.Add(new SBalanceItem
                                {
                                    currency = currency,
                                    free = cashBal - frozenBal,
                                    used = frozenBal,
                                    total = cashBal,
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
                }
            }
            catch (Exception ex)
            {
                RaiseError($"Account processing error: {ex.Message}");
            }
        }

        private async Task ProcessOrderData(JsonElement json)
        {
            try
            {
                if (!json.TryGetArray("data", out var dataArray))
                    return;

                var orderList = new List<SOrderItem>();
                var timestamp = TimeExtension.UnixTime;

                foreach (var order in dataArray.EnumerateArray())
                {
                    orderList.Add(new SOrderItem
                    {
                        orderId = order.GetStringOrDefault("ordId"),
                        clientOrderId = order.GetStringOrDefault("clOrdId"),
                        symbol = ConvertToStandardSymbol(order.GetStringOrDefault("instId")),
                        side = order.GetStringOrDefault("side") == "buy" ? OrderSide.Buy : OrderSide.Sell,
                        type = ConvertOrderType(order.GetStringOrDefault("ordType")),
                        status = ConvertOrderStatus(order.GetStringOrDefault("state")),
                        price = order.GetDecimalOrDefault("px"),
                        quantity = order.GetDecimalOrDefault("sz"),
                        filledQuantity = order.GetDecimalOrDefault("fillSz"),
                        createTime = order.GetInt64OrDefault("cTime", timestamp),
                        updateTime = order.GetInt64OrDefault("uTime", timestamp)
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

        private async Task ProcessPositionData(JsonElement json)
        {
            try
            {
                if (!json.TryGetArray("data", out var dataArray))
                    return;

                var positionList = new List<SPositionItem>();
                var timestamp = TimeExtension.UnixTime;

                foreach (var pos in dataArray.EnumerateArray())
                {
                    positionList.Add(new SPositionItem
                    {
                        symbol = ConvertToStandardSymbol(pos.GetStringOrDefault("instId")),
                        side = pos.GetStringOrDefault("posSide") == "long" ? PositionSide.Long : PositionSide.Short,
                        size = pos.GetDecimalOrDefault("pos"),
                        entryPrice = pos.GetDecimalOrDefault("avgPx"),
                        markPrice = pos.GetDecimalOrDefault("markPx"),
                        unrealizedPnl = pos.GetDecimalOrDefault("upl"),
                        realizedPnl = pos.GetDecimalOrDefault("realizedPnl")
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
                var instId = ConvertToOkxSymbol(symbol);
                var subscription = new
                {
                    op = "subscribe",
                    args = new[]
                    {
                        new
                        {
                            channel = "books",
                            instId = instId
                        }
                    }
                };

                await SendMessageAsync(JsonSerializer.Serialize(subscription));

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
                var instId = ConvertToOkxSymbol(symbol);
                var subscription = new
                {
                    op = "subscribe",
                    args = new[]
                    {
                        new
                        {
                            channel = "trades",
                            instId = instId
                        }
                    }
                };

                await SendMessageAsync(JsonSerializer.Serialize(subscription));

                var key = CreateSubscriptionKey("trades", symbol);
                _subscriptions[key] = new SubscriptionInfo
                {
                    Channel = "trades",
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
                var instId = ConvertToOkxSymbol(symbol);
                var subscription = new
                {
                    op = "subscribe",
                    args = new[]
                    {
                        new
                        {
                            channel = "tickers",
                            instId = instId
                        }
                    }
                };

                await SendMessageAsync(JsonSerializer.Serialize(subscription));

                var key = CreateSubscriptionKey("ticker", symbol);
                _subscriptions[key] = new SubscriptionInfo
                {
                    Channel = "tickers",
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
                var instId = ConvertToOkxSymbol(symbol);
                var channelInterval = ConvertToChannelInterval(interval);

                var subscription = new
                {
                    op = "subscribe",
                    args = new[]
                    {
                        new
                        {
                            channel = $"candle{channelInterval}",
                            instId = instId
                        }
                    }
                };

                await SendMessageAsync(JsonSerializer.Serialize(subscription));

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
                var instId = ConvertToOkxSymbol(symbol);
                var unsubscription = new
                {
                    op = "unsubscribe",
                    args = new[]
                    {
                        new
                        {
                            channel = channel,
                            instId = instId
                        }
                    }
                };

                await SendMessageAsync(JsonSerializer.Serialize(unsubscription));

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
            return JsonSerializer.Serialize(new { op = "ping" });
        }

        protected override string CreateAuthenticationMessage(string apiKey, string secretKey)
        {
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
            var method = "GET";
            var requestPath = "/users/self/verify";

            var sign = GenerateSignature(secretKey, timestamp, method, requestPath);

            return JsonSerializer.Serialize(new
            {
                op = "login",
                args = new[]
                {
                    new
                    {
                        apiKey = apiKey,
                        passphrase = "", // OKX may require passphrase
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

        private string ConvertToOkxSymbol(string symbol)
        {
            // Convert from standard format (BTC/USDT) to OKX format (BTC-USDT)
            return symbol.Replace("/", "-");
        }

        private string ConvertToStandardSymbol(string instId)
        {
            // Convert from OKX format (BTC-USDT) to standard format (BTC/USDT)
            return instId.Replace("-", "/");
        }

        private string ConvertToChannelInterval(string interval)
        {
            return interval switch
            {
                "1m" => "1m",
                "3m" => "3m",
                "5m" => "5m",
                "15m" => "15m",
                "30m" => "30m",
                "1h" => "1H",
                "2h" => "2H",
                "4h" => "4H",
                "6h" => "6H",
                "12h" => "12H",
                "1d" => "1D",
                "1w" => "1W",
                "1M" => "1M",
                _ => "1m"
            };
        }

        private string ConvertInterval(string channel)
        {
            if (channel == null) return "1m";

            return channel.Replace("candle", "") switch
            {
                "1m" => "1m",
                "3m" => "3m",
                "5m" => "5m",
                "15m" => "15m",
                "30m" => "30m",
                "1H" => "1h",
                "2H" => "2h",
                "4H" => "4h",
                "6H" => "6h",
                "12H" => "12h",
                "1D" => "1d",
                "1W" => "1w",
                "1M" => "1M",
                _ => "1m"
            };
        }

        private OrderType ConvertOrderType(string type)
        {
            return type?.ToLower() switch
            {
                "limit" => OrderType.Limit,
                "market" => OrderType.Market,
                "post_only" => OrderType.Limit,
                "fok" => OrderType.Limit,
                "ioc" => OrderType.Limit,
                _ => OrderType.Limit
            };
        }

        private OrderStatus ConvertOrderStatus(string status)
        {
            return status?.ToLower() switch
            {
                "live" => OrderStatus.Open,
                "partially_filled" => OrderStatus.PartiallyFilled,
                "filled" => OrderStatus.Filled,
                "canceled" => OrderStatus.Canceled,
                _ => OrderStatus.Open
            };
        }

        private long GetIntervalMilliseconds(string interval)
        {
            return interval switch
            {
                "1m" => 60000,
                "3m" => 180000,
                "5m" => 300000,
                "15m" => 900000,
                "30m" => 1800000,
                "1h" => 3600000,
                "2h" => 7200000,
                "4h" => 14400000,
                "6h" => 21600000,
                "12h" => 43200000,
                "1d" => 86400000,
                "1w" => 604800000,
                "1M" => 2592000000,
                _ => 60000
            };
        }

        private decimal CalculatePercentage(decimal open, decimal close)
        {
            if (open == 0)
                return 0;

            return ((close - open) / open) * 100;
        }

        #region Batch Subscription Support

        /// <summary>
        /// OKX supports batch subscription - multiple channels and symbols in one message
        /// </summary>
        protected override bool SupportsBatchSubscription()
        {
            return true;
        }

        /// <summary>
        /// Send batch subscriptions for OKX - all channels and symbols in single args array
        /// </summary>
        protected override async Task<bool> SendBatchSubscriptionsAsync(List<KeyValuePair<string, SubscriptionInfo>> subscriptions)
        {
            try
            {
                // Build args array with all subscriptions
                var args = new List<object>();
                
                foreach (var kvp in subscriptions)
                {
                    var subscription = kvp.Value;
                    var instId = ConvertToOkxSymbol(subscription.Symbol);
                    
                    // Map channel names to OKX channel types
                    string okxChannel = subscription.Channel.ToLower() switch
                    {
                        "orderbook" or "depth" => "books",
                        "trades" or "trade" => "trades",
                        "ticker" => "tickers",
                        "candles" or "kline" => !string.IsNullOrEmpty(subscription.Extra) 
                            ? $"candle{ConvertToChannelInterval(subscription.Extra)}" 
                            : "candle1m",
                        _ => subscription.Channel
                    };

                    // Add to args array
                    args.Add(new
                    {
                        channel = okxChannel,
                        instId = instId
                    });
                }

                if (args.Count == 0)
                    return true;

                // Create subscription message with all channels and symbols
                var subscriptionMessage = new
                {
                    op = "subscribe",
                    args = args
                };

                // Send the batch subscription
                var json = JsonSerializer.Serialize(subscriptionMessage);
                await SendMessageAsync(json);
                
                // Log the batch subscription
                RaiseError($"Sent OKX batch subscription with {args.Count} items");

                return true;
            }
            catch (Exception ex)
            {
                RaiseError($"Batch subscription failed: {ex.Message}");
                return false;
            }
        }

        #endregion
    }
}