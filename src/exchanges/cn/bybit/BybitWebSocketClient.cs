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

namespace CCXT.Collector.Bybit
{
    /*
     * Bybit Support Markets: USDT, USDC, BTC, ETH
     *
     * API Documentation:
     *     https://bybit-exchange.github.io/docs/v5/intro
     *
     * WebSocket API:
     *     https://bybit-exchange.github.io/docs/v5/ws/connect
     *     https://bybit-exchange.github.io/docs/v5/ws/public/orderbook
     *
     * Fees:
     *     https://www.bybit.com/en-US/help-center/bybitHC_Article?id=360039261154
     */
    /// <summary>
    /// Bybit WebSocket client for real-time data streaming
    /// </summary>
    public class BybitWebSocketClient : WebSocketClientBase
    {
        private readonly Dictionary<string, SOrderBook> _orderbookCache;
        private readonly Dictionary<string, long> _lastUpdateIds;

        public override string ExchangeName => "Bybit";
        protected override string WebSocketUrl => "wss://stream.bybit.com/v5/public/spot";
        protected override string PrivateWebSocketUrl => "wss://stream.bybit.com/v5/private";
        protected override int PingIntervalMs => 20000; // 20 seconds

        public BybitWebSocketClient()
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
                if (json.GetStringOrDefault("op") == "ping")
                {
                    await HandlePingMessage();
                    return;
                }

                // Handle subscription response
                if (json.GetStringOrDefault("op") == "subscribe")
                {
                    if (!json.GetBooleanOrFalse("success"))
                    {
                        var retMsg = json.GetStringOrDefault("ret_msg", "Unknown error");
                        RaiseError($"Subscription failed: {retMsg}");
                    }

                    return;
                }

                // Handle data messages
                if (json.TryGetProperty("topic", out var topicProp))
                {
                    if (!json.TryGetProperty("data", out var data))
                        return;

                    var topic = json.GetStringOrDefault("topic");

                    if (isPrivate)
                    {
                        // Handle private data
                        if (topic.StartsWith("wallet"))
                        {
                            await ProcessWalletData(json);
                        }
                        else if (topic.StartsWith("order"))
                        {
                            await ProcessOrderData(json);
                        }
                        else if (topic.StartsWith("position"))
                        {
                            await ProcessPositionData(json);
                        }
                    }
                    else
                    {
                        // Handle public data
                        if (topic.Contains("orderbook"))
                        {
                            await ProcessOrderbookData(json);
                        }
                        else if (topic.Contains("publicTrade"))
                        {
                            await ProcessTradeData(json);
                        }
                        else if (topic.Contains("tickers"))
                        {
                            await ProcessTickerData(json);
                        }
                        else if (topic.Contains("kline"))
                        {
                            await ProcessKlineData(json);
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
            var pong = new
            {
                op = "pong"
            };
            await SendMessageAsync(JsonSerializer.Serialize(pong));
        }

        private async Task ProcessOrderbookData(JsonElement json)
        {
            try
            {
                if (!json.TryGetProperty("data", out var data))
                    return;

                var topic = json.GetStringOrDefault("topic");
                var symbol = ExtractSymbolFromTopic(topic);
                var type = json.GetStringOrDefault("type"); // snapshot or delta
                var timestamp = json.GetInt64OrDefault("ts", TimeExtension.UnixTime);

                var orderbook = new SOrderBook
                {
                    exchange = ExchangeName,
                    symbol = ConvertToStandardSymbol(symbol),
                    timestamp = timestamp,
                    result = new SOrderBookData
                    {
                        timestamp = timestamp,
                        asks = new List<SOrderBookItem>(),
                        bids = new List<SOrderBookItem>()
                    }
                };

                // Process asks
                if (data.TryGetArray("a", out var asks))
                {
                    foreach (var ask in asks.EnumerateArray())
                    {
                        if (ask.GetArrayLength() < 2)
                            continue;

                        var price = ask[0].GetDecimalValue();
                        var quantity = ask[1].GetDecimalValue();
                        
                        orderbook.result.asks.Add(new SOrderBookItem
                        {
                            price = price,
                            quantity = quantity,
                            amount = price * quantity
                        });
                    }
                }

                // Process bids
                if (data.TryGetArray("b", out var bids))
                {
                    foreach (var bid in bids.EnumerateArray())
                    {
                        if (bid.GetArrayLength() < 2)
                            continue;

                        var price = bid[0].GetDecimalValue();
                        var quantity = bid[1].GetDecimalValue();
                        
                        orderbook.result.bids.Add(new SOrderBookItem
                        {
                            price = price,
                            quantity = quantity,
                            amount = price * quantity
                        });
                    }
                }

                // Sort orderbook
                orderbook.result.asks = orderbook.result.asks.OrderBy(a => a.price).ToList();
                orderbook.result.bids = orderbook.result.bids.OrderByDescending(b => b.price).ToList();

                // Handle incremental updates
                if (type == "delta" && _orderbookCache.ContainsKey(symbol))
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
                var topic = json.GetStringOrDefault("topic");
                var symbol = ExtractSymbolFromTopic(topic);

                if (!(json.TryGetArray("data", out var data)))
                    return;

                var symbolConverted = ConvertToStandardSymbol(symbol);
                var trades = new List<STradeItem>();
                long latestTimestamp = 0;

                foreach (var trade in data.EnumerateArray())
                {
                    var timestamp = trade.GetInt64OrDefault("T", TimeExtension.UnixTime);
                    if (timestamp > latestTimestamp)
                        latestTimestamp = timestamp;

                    trades.Add(new STradeItem
                    {
                        tradeId = trade.GetStringOrDefault("i"),
                        timestamp = timestamp,
                        price = trade.GetDecimalOrDefault("p"),
                        quantity = trade.GetDecimalOrDefault("v"),
                        amount = (trade.GetDecimalOrDefault("p")) * (trade.GetDecimalOrDefault("v")),
                        sideType = trade.GetStringOrDefault("S") == "Buy" ? SideType.Bid : SideType.Ask,
                        orderType = OrderType.Limit
                    });
                }

                if (trades.Count > 0)
                {
                    var completeOrder = new STrade
                    {
                        exchange = ExchangeName,
                        symbol = symbolConverted,
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
                if (!json.TryGetProperty("data", out var data))
                    return;

                var topic = json.GetStringOrDefault("topic");
                var symbol = ExtractSymbolFromTopic(topic);
                var timestamp = json.GetInt64OrDefault("ts", TimeExtension.UnixTime);

                var ticker = new STicker
                {
                    exchange = ExchangeName,
                    symbol = ConvertToStandardSymbol(symbol),
                    timestamp = timestamp,
                    result = new STickerItem
                    {
                        timestamp = timestamp,
                        closePrice = data.GetDecimalOrDefault("lastPrice"),
                        askPrice = data.GetDecimalOrDefault("ask1Price"),
                        bidPrice = data.GetDecimalOrDefault("bid1Price"),
                        askQuantity = data.GetDecimalOrDefault("ask1Size"),
                        bidQuantity = data.GetDecimalOrDefault("bid1Size"),
                        openPrice = data.GetDecimalOrDefault("prevPrice24h"),
                        highPrice = data.GetDecimalOrDefault("highPrice24h"),
                        lowPrice = data.GetDecimalOrDefault("lowPrice24h"),
                        volume = data.GetDecimalOrDefault("volume24h"),
                        quoteVolume = data.GetDecimalOrDefault("turnover24h"),
                        change = data.GetDecimalOrDefault("price24hPcnt"),
                        percentage = (data.GetDecimalOrDefault("price24hPcnt")) * 100
                    }
                };

                InvokeTickerCallback(ticker);
            }
            catch (Exception ex)
            {
                RaiseError($"Ticker processing error: {ex.Message}");
            }
        }

        private async Task ProcessKlineData(JsonElement json)
        {
            try
            {
                var topic = json.GetStringOrDefault("topic");
                var parts = topic.Split('.');
                var interval = parts[1]; // Extract interval
                var symbol = parts[2]; // Extract symbol

                if (!(json.TryGetArray("data", out var data)))
                    return;

                var candleItems = new List<SCandleItem>();
                long latestTimestamp = 0;
                var convertedInterval = ConvertInterval(interval);

                foreach (var kline in data.EnumerateArray())
                {
                    var timestamp = kline.GetInt64OrDefault("start");
                    if (timestamp > latestTimestamp)
                        latestTimestamp = timestamp;

                    candleItems.Add(new SCandleItem
                    {
                        openTime = timestamp,
                        closeTime = kline.GetInt64OrDefault("end"),
                        open = kline.GetDecimalOrDefault("open"),
                        high = kline.GetDecimalOrDefault("high"),
                        low = kline.GetDecimalOrDefault("low"),
                        close = kline.GetDecimalOrDefault("close"),
                        volume = kline.GetDecimalOrDefault("volume"),
                        quoteVolume = kline.GetDecimalOrDefault("turnover")
                    });
                }

                if (candleItems.Count > 0)
                {
                    var candle = new SCandle
                    {
                        exchange = ExchangeName,
                        symbol = ConvertToStandardSymbol(symbol),
                        interval = convertedInterval,
                        timestamp = latestTimestamp,
                        result = candleItems
                    };

                    InvokeCandleCallback(candle);
                }
            }
            catch (Exception ex)
            {
                RaiseError($"Kline processing error: {ex.Message}");
            }
        }

        private async Task ProcessWalletData(JsonElement json)
        {
            try
            {
                if (!(json.TryGetArray("data", out var data)))
                    return;

                foreach (var wallet in data.EnumerateArray())
                {
                    if (!(wallet.TryGetArray("coin", out var coins)))
                        continue;

                    var balanceItems = new List<SBalanceItem>();
                    var timestamp = TimeExtension.UnixTime;
                    var accountType = wallet.GetStringOrDefault("accountType", "spot");

                    foreach (var coin in coins.EnumerateArray())
                    {
                        var currency = coin.GetStringOrDefault("coin");
                        var free = coin.GetDecimalOrDefault("free");
                        var locked = coin.GetDecimalOrDefault("locked");
                        
                        if (free > 0 || locked > 0)
                        {
                            balanceItems.Add(new SBalanceItem
                            {
                                currency = currency,
                                free = free,
                                used = locked,
                                total = free + locked,
                                updateTime = timestamp
                            });
                        }
                    }

                    if (balanceItems.Count > 0)
                    {
                        var balance = new SBalance
                        {
                            exchange = ExchangeName,
                            accountId = accountType,
                            timestamp = timestamp,
                            balances = balanceItems
                        };

                        InvokeBalanceCallback(balance);
                    }
                }
            }
            catch (Exception ex)
            {
                RaiseError($"Wallet processing error: {ex.Message}");
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
                        clientOrderId = order.GetStringOrDefault("orderLinkId"),
                        symbol = ConvertToStandardSymbol(order.GetStringOrDefault("symbol")),
                        side = order.GetStringOrDefault("side") == "Buy" ? OrderSide.Buy : OrderSide.Sell,
                        type = ConvertOrderType(order.GetStringOrDefault("orderType")),
                        status = ConvertOrderStatus(order.GetStringOrDefault("orderStatus")),
                        price = order.GetDecimalOrDefault("price"),
                        quantity = order.GetDecimalOrDefault("qty"),
                        filledQuantity = order.GetDecimalOrDefault("cumExecQty"),
                        createTime = order.GetInt64OrDefault("createdTime", timestamp),
                        updateTime = order.GetInt64OrDefault("updatedTime", timestamp)
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
                        symbol = ConvertToStandardSymbol(pos.GetStringOrDefault("symbol")),
                        side = pos.GetStringOrDefault("side") == "Buy" ? PositionSide.Long : PositionSide.Short,
                        size = pos.GetDecimalOrDefault("size"),
                        entryPrice = pos.GetDecimalOrDefault("avgPrice"),
                        markPrice = pos.GetDecimalOrDefault("markPrice"),
                        unrealizedPnl = pos.GetDecimalOrDefault("unrealisedPnl"),
                        realizedPnl = pos.GetDecimalOrDefault("realisedPnl")
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
                var bybitSymbol = ConvertToBybitSymbol(symbol);
                var subscription = new
                {
                    op = "subscribe",
                    args = new[]
                    {
                        $"orderbook.50.{bybitSymbol}"
                    }
                };

                await SendMessageAsync(JsonSerializer.Serialize(subscription));

                var key = CreateSubscriptionKey("orderbook", symbol);
                _subscriptions[key] = new SubscriptionInfo
                {
                    Channel = "orderbook",
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
                var bybitSymbol = ConvertToBybitSymbol(symbol);
                var subscription = new
                {
                    op = "subscribe",
                    args = new[]
                    {
                        $"publicTrade.{bybitSymbol}"
                    }
                };

                await SendMessageAsync(JsonSerializer.Serialize(subscription));

                var key = CreateSubscriptionKey("trades", symbol);
                _subscriptions[key] = new SubscriptionInfo
                {
                    Channel = "publicTrade",
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
                var bybitSymbol = ConvertToBybitSymbol(symbol);
                var subscription = new
                {
                    op = "subscribe",
                    args = new[]
                    {
                        $"tickers.{bybitSymbol}"
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
                var bybitSymbol = ConvertToBybitSymbol(symbol);
                var bybitInterval = ConvertToBybitInterval(interval);
                
                var subscription = new
                {
                    op = "subscribe",
                    args = new[]
                    {
                        $"kline.{bybitInterval}.{bybitSymbol}"
                    }
                };

                await SendMessageAsync(JsonSerializer.Serialize(subscription));

                var key = CreateSubscriptionKey($"kline.{bybitInterval}", symbol);
                _subscriptions[key] = new SubscriptionInfo
                {
                    Channel = $"kline.{bybitInterval}",
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
                var bybitSymbol = ConvertToBybitSymbol(symbol);
                var topic = channel switch
                {
                    "orderbook" => $"orderbook.50.{bybitSymbol}",
                    "trades" => $"publicTrade.{bybitSymbol}",
                    "ticker" => $"tickers.{bybitSymbol}",
                    _ => channel
                };

                var unsubscription = new
                {
                    op = "unsubscribe",
                    args = new[] { topic }
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
            var expires = TimeExtension.UnixTime + 10000;
            var signature = GenerateSignature(secretKey, $"GET/realtime{expires}");
            
            return JsonSerializer.Serialize(new
            {
                op = "auth",
                args = new[]
                {
                    apiKey,
                    expires.ToString(),
                    signature
                }
            });
        }

        private string GenerateSignature(string secretKey, string message)
        {
            var encoding = new System.Text.ASCIIEncoding();
            byte[] keyByte = encoding.GetBytes(secretKey);
            byte[] messageBytes = encoding.GetBytes(message);
            using (var hmacsha256 = new System.Security.Cryptography.HMACSHA256(keyByte))
            {
                byte[] hashmessage = hmacsha256.ComputeHash(messageBytes);
                return BitConverter.ToString(hashmessage).Replace("-", "").ToLower();
            }
        }

        private string ConvertToBybitSymbol(string symbol)
        {
            // Convert from standard format (BTC/USDT) to Bybit format (BTCUSDT)
            return symbol.Replace("/", "");
        }

        private string ConvertToStandardSymbol(string bybitSymbol)
        {
            // Convert from Bybit format (BTCUSDT) to standard format (BTC/USDT)
            string[] quoteCurrencies = { "USDT", "USDC", "BTC", "ETH", "DAI", "EUR" };
            
            foreach (var quote in quoteCurrencies)
            {
                if (bybitSymbol.EndsWith(quote))
                {
                    var baseCurrency = bybitSymbol.Substring(0, bybitSymbol.Length - quote.Length);
                    return $"{baseCurrency}/{quote}";
                }
            }
            
            return bybitSymbol;
        }

        private string ExtractSymbolFromTopic(string topic)
        {
            var parts = topic.Split('.');
            return parts.Length > 1 ? parts[parts.Length - 1] : "";
        }

        private string ConvertToBybitInterval(string interval)
        {
            return interval switch
            {
                "1m" => "1",
                "3m" => "3",
                "5m" => "5",
                "15m" => "15",
                "30m" => "30",
                "1h" => "60",
                "2h" => "120",
                "4h" => "240",
                "6h" => "360",
                "12h" => "720",
                "1d" => "D",
                "1w" => "W",
                "1M" => "M",
                _ => "1"
            };
        }

        private string ConvertInterval(string bybitInterval)
        {
            return bybitInterval switch
            {
                "1" => "1m",
                "3" => "3m",
                "5" => "5m",
                "15" => "15m",
                "30" => "30m",
                "60" => "1h",
                "120" => "2h",
                "240" => "4h",
                "360" => "6h",
                "720" => "12h",
                "D" => "1d",
                "W" => "1w",
                "M" => "1M",
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
            return status switch
            {
                "New" => OrderStatus.New,
                "PartiallyFilled" => OrderStatus.PartiallyFilled,
                "Filled" => OrderStatus.Filled,
                "Cancelled" => OrderStatus.Canceled,
                "Rejected" => OrderStatus.Rejected,
                _ => OrderStatus.Open
            };
        }
    }
}