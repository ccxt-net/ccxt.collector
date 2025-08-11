using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CCXT.Collector.Core.Abstractions;
using CCXT.Collector.Models.WebSocket;
using CCXT.Collector.Library;
using CCXT.Collector.Service;
using System.Text.Json;

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
     *     https://www.bitget.com/api-doc/spot/websocket/public/Tickers-Channel
     *     https://www.bitget.com/api-doc/spot/websocket/public/Candlesticks-Channel
     *     https://www.bitget.com/api-doc/spot/websocket/public/Trades-Channel
     *     https://www.bitget.com/api-doc/spot/websocket/public/Depth-Channel
     *     https://www.bitget.com/api-doc/spot/websocket/public/Auction-Channel
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
        protected override string WebSocketUrl => "wss://ws.bitget.com/spot/v1/stream";
        protected override string PrivateWebSocketUrl => "wss://ws.bitget.com/spot/v1/stream";
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
                using var doc = JsonDocument.Parse(message); 
                var json = doc.RootElement;

                // Handle ping/pong
                if (json.GetStringOrDefault("action") == "ping")
                {
                    await HandlePingMessage(json);
                    return;
                }

                // Handle subscription responses
                if (json.GetStringOrDefault("event") == "subscribe")
                {
                    var code = json.GetStringOrDefault("code");
                    if (code != "0")
                    {
                        var errorMsg = json.GetStringOrDefault("msg", "Unknown error");
                        RaiseError($"Subscription failed: {errorMsg}");
                    }
                    return;
                }

                // Handle data messages
                if (json.GetStringOrDefault("action") == "update" || json.GetStringOrDefault("action") == "snapshot")
                {
                    if (json.TryGetProperty("arg", out var arg))
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

        private async Task HandlePingMessage(JsonElement json)
        {
            var pong = new
            {
                action = "pong",
                ts = json.GetStringOrDefault("ts")
            };
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

                if (!(json.TryGetArray("data", out var dataProp)))
                    return;

                var data = dataProp.EnumerateArray().FirstOrDefault();                
                if (data.ValueKind == JsonValueKind.Undefined)
                    return;

                var symbol = ConvertToStandardSymbol(instId);
                var timestamp = data.GetInt64OrDefault("ts", TimeExtension.UnixTime);
                var action = json.GetStringOrDefault("action");

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
                        if (ask.GetArrayLength() < 2)
                            continue;
                            
                        var price = ask[0].GetDecimalValue();
                        var amount = ask[1].GetDecimalValue();

                        orderbook.result.asks.Add(new SOrderBookItem
                        {
                            price = price,
                            quantity = amount,
                            amount = price * amount
                        });
                    }
                }

                // Process bids
                if (data.TryGetArray("bids", out var bids))
                {
                    foreach (var bid in bids.EnumerateArray())
                    {
                        if (bid.GetArrayLength() < 2)
                            continue;
                            
                        var price = bid[0].GetDecimalValue();
                        var amount = bid[1].GetDecimalValue();
                        
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

        private async Task ProcessTradeData(JsonElement json)
        {
            try
            {
                if (!json.TryGetProperty("arg", out var arg))
                    return;

                var instId = arg.GetStringOrDefault("instId");
                if (String.IsNullOrEmpty(instId))
                    return;

                if (!(json.TryGetArray("data", out var dataProp)))
                    return;

                var data = dataProp.EnumerateArray().FirstOrDefault();                
                if (data.ValueKind == JsonValueKind.Undefined)
                    return;

                var symbol = ConvertToStandardSymbol(instId);
                var trades = new List<STradeItem>();
                long latestTimestamp = 0;
                
                foreach (var trade in data.EnumerateArray())
                {
                    var timestamp = trade.GetInt64OrDefault("ts", TimeExtension.UnixTime);
                    if (timestamp > latestTimestamp)
                        latestTimestamp = timestamp;

                    trades.Add(new STradeItem
                    {
                        tradeId = trade.GetStringOrDefault("tradeId"),
                        timestamp = timestamp,
                        price = trade.GetDecimalOrDefault("price"),
                        quantity = trade.GetDecimalOrDefault("size"),
                        amount = (trade.GetDecimalOrDefault("price")) * (trade.GetDecimalOrDefault("size")),
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

                if (!(json.TryGetArray("data", out var dataProp)))
                    return;

                var data = dataProp.EnumerateArray().FirstOrDefault();                
                if (data.ValueKind == JsonValueKind.Undefined)
                    return;

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
                        closePrice = data.GetDecimalOrDefault("lastPr"),
                        askPrice = data.GetDecimalOrDefault("askPr"),
                        bidPrice = data.GetDecimalOrDefault("bidPr"),
                        openPrice = data.GetDecimalOrDefault("open24h"),
                        highPrice = data.GetDecimalOrDefault("high24h"),
                        lowPrice = data.GetDecimalOrDefault("low24h"),
                        volume = data.GetDecimalOrDefault("baseVolume"),
                        quoteVolume = data.GetDecimalOrDefault("quoteVolume"),
                        change = data.GetDecimalOrDefault("change24h"),
                        percentage = data.GetDecimalOrDefault("changeUtc24h")
                    }
                };

                InvokeTickerCallback(ticker);
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

                if (!(json.TryGetArray("data", out var data)))
                    return;

                var symbol = ConvertToStandardSymbol(instId);
                var interval = ConvertInterval(channel);
                var intervalMs = GetIntervalMilliseconds(interval);
                
                var candleItems = new List<SCandleItem>();
                long latestTimestamp = 0;

                foreach (var candle in data.EnumerateArray())
                {
                    var timestamp = candle.GetInt64OrDefault("ts");
                    if (timestamp > latestTimestamp)
                        latestTimestamp = timestamp;

                    candleItems.Add(new SCandleItem
                    {
                        openTime = timestamp,
                        closeTime = timestamp + intervalMs,
                        open = candle.GetDecimalOrDefault("o"),
                        high = candle.GetDecimalOrDefault("h"),
                        low = candle.GetDecimalOrDefault("l"),
                        close = candle.GetDecimalOrDefault("c"),
                        volume = candle.GetDecimalOrDefault("v")
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

        private async Task ProcessAccountData(JsonElement json)
        {
            try
            {
                if (!(json.TryGetArray("data", out var data)))
                    return;

                var balanceItems = new List<SBalanceItem>();
                var timestamp = TimeExtension.UnixTime;

                foreach (var account in data.EnumerateArray())
                {
                    var currency = account.GetStringOrDefault("ccy");
                    var free = account.GetDecimalOrDefault("availBal");
                    var used = account.GetDecimalOrDefault("frozenBal");
                    var total = account.GetDecimalOrDefault("bal");
                    
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

        private async Task ProcessOrderData(JsonElement json)
        {
            try
            {
                if (!(json.TryGetArray("data", out var data)))
                    return;

                var orderList = new List<SOrderItem>();
                var timestamp = TimeExtension.UnixTime;

                foreach (var order in data.EnumerateArray())
                {
                    orderList.Add(new SOrderItem
                    {
                        orderId = order.GetStringOrDefault("orderId"),
                        clientOrderId = order.GetStringOrDefault("clientOid"),
                        symbol = ConvertToStandardSymbol(order.GetStringOrDefault("instId")),
                        side = order.GetStringOrDefault("side") == "buy" ? OrderSide.Buy : OrderSide.Sell,
                        type = ConvertOrderType(order.GetStringOrDefault("orderType")),
                        status = ConvertOrderStatus(order.GetStringOrDefault("status")),
                        price = order.GetDecimalOrDefault("price"),
                        quantity = order.GetDecimalOrDefault("size"),
                        filledQuantity = order.GetDecimalOrDefault("fillSize"),
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
                if (!(json.TryGetArray("data", out var data)))
                    return;

                var positionList = new List<SPositionItem>();
                var timestamp = TimeExtension.UnixTime;

                foreach (var pos in data.EnumerateArray())
                {
                    positionList.Add(new SPositionItem
                    {
                        symbol = ConvertToStandardSymbol(pos.GetStringOrDefault("instId")),
                        side = pos.GetStringOrDefault("holdSide") == "long" ? PositionSide.Long : PositionSide.Short,
                        size = pos.GetDecimalOrDefault("total"),
                        entryPrice = pos.GetDecimalOrDefault("averageOpenPrice"),
                        markPrice = pos.GetDecimalOrDefault("markPrice"),
                        unrealizedPnl = pos.GetDecimalOrDefault("unrealizedPL"),
                        realizedPnl = pos.GetDecimalOrDefault("achievedProfits")
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

                await SendMessageAsync(JsonSerializer.Serialize(subscription));

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

                await SendMessageAsync(JsonSerializer.Serialize(subscription));

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
            return JsonSerializer.Serialize(new { action = "ping" });
        }

        protected override string CreateAuthenticationMessage(string apiKey, string secretKey)
        {
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
            var method = "GET";
            var requestPath = "/user/verify";
            
            var sign = GenerateSignature(secretKey, timestamp, method, requestPath);
            
            return JsonSerializer.Serialize(new
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

        #region Batch Subscription Support

        /// <summary>
        /// Bitget supports batch subscription - multiple args in single message
        /// </summary>
        protected override bool SupportsBatchSubscription()
        {
            return true;
        }

        /// <summary>
        /// Send batch subscriptions for Bitget - combines multiple channels/symbols in args array
        /// </summary>
        protected override async Task<bool> SendBatchSubscriptionsAsync(List<KeyValuePair<string, SubscriptionInfo>> subscriptions)
        {
            try
            {
                // Build list of all subscription args
                var args = new List<object>();
                
                foreach (var kvp in subscriptions)
                {
                    var subscription = kvp.Value;
                    var instId = ConvertToExchangeSymbol(subscription.Symbol);
                    
                    // Map channel names to Bitget channel format
                    string channelName = subscription.Channel.ToLower() switch
                    {
                        "orderbook" or "depth" => "books",
                        "trades" or "trade" => "trade",
                        "ticker" => "ticker",
                        "candles" or "kline" or "candlestick" => !string.IsNullOrEmpty(subscription.Extra) 
                            ? $"candle{ConvertToChannelInterval(subscription.Extra)}"
                            : "candle1m",
                        _ => subscription.Channel
                    };

                    // Create subscription arg object
                    var arg = new
                    {
                        instType = "SPOT",
                        channel = channelName,
                        instId = instId
                    };

                    args.Add(arg);
                }

                if (args.Count == 0)
                    return true;

                // Bitget allows multiple args in a single subscription message
                // Typical limit is around 100 subscriptions per connection
                const int maxArgsPerMessage = 100;
                
                if (args.Count <= maxArgsPerMessage)
                {
                    // Send all args in one message
                    var subscriptionMessage = new
                    {
                        op = "subscribe",
                        args = args.ToArray()
                    };

                    await SendMessageAsync(JsonSerializer.Serialize(subscriptionMessage));
                    RaiseError($"Sent Bitget batch subscription with {args.Count} channels");
                }
                else
                {
                    // Split into multiple messages if exceeding limit
                    var messageCount = (args.Count + maxArgsPerMessage - 1) / maxArgsPerMessage;
                    
                    for (int i = 0; i < messageCount; i++)
                    {
                        var batch = args
                            .Skip(i * maxArgsPerMessage)
                            .Take(maxArgsPerMessage)
                            .ToArray();

                        var subscriptionMessage = new
                        {
                            op = "subscribe",
                            args = batch
                        };

                        await SendMessageAsync(JsonSerializer.Serialize(subscriptionMessage));
                        RaiseError($"Sent Bitget batch subscription {i + 1}/{messageCount} with {batch.Length} channels");
                        
                        // Small delay between batches if multiple messages
                        if (i < messageCount - 1)
                            await Task.Delay(100);
                    }
                }

                RaiseError($"Completed Bitget batch subscription for {subscriptions.Count} total subscriptions");

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