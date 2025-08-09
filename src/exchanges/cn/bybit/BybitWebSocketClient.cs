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
                var json = JObject.Parse(message);

                // Handle ping/pong
                if (json["op"]?.ToString() == "ping")
                {
                    await HandlePingMessage();
                    return;
                }

                // Handle subscription response
                if (json["op"]?.ToString() == "subscribe")
                {
                    if (!json["success"]?.Value<bool>() ?? false)
                    {
                        RaiseError($"Subscription failed: {json["ret_msg"]}");
                    }
                    return;
                }

                // Handle data messages
                if (json["topic"] != null)
                {
                    var topic = json["topic"].ToString();
                    var data = json["data"];
                    
                    if (data == null)
                        return;

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
            await SendMessageAsync(JsonConvert.SerializeObject(pong));
        }

        private async Task ProcessOrderbookData(JObject json)
        {
            try
            {
                var topic = json["topic"].ToString();
                var symbol = ExtractSymbolFromTopic(topic);
                var data = json["data"];
                var type = json["type"]?.ToString(); // snapshot or delta
                var timestamp = json["ts"]?.Value<long>() ?? DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

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
                var asks = data["a"] as JArray;
                if (asks != null)
                {
                    foreach (var ask in asks)
                    {
                        var price = ask[0].Value<decimal>();
                        var quantity = ask[1].Value<decimal>();
                        
                        orderbook.result.asks.Add(new SOrderBookItem
                        {
                            price = price,
                            quantity = quantity,
                            amount = price * quantity
                        });
                    }
                }

                // Process bids
                var bids = data["b"] as JArray;
                if (bids != null)
                {
                    foreach (var bid in bids)
                    {
                        var price = bid[0].Value<decimal>();
                        var quantity = bid[1].Value<decimal>();
                        
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

        private async Task ProcessTradeData(JObject json)
        {
            try
            {
                var topic = json["topic"].ToString();
                var symbol = ExtractSymbolFromTopic(topic);
                var data = json["data"] as JArray;
                
                if (data == null)
                    return;

                var symbolConverted = ConvertToStandardSymbol(symbol);
                var trades = new List<STradeItem>();
                long latestTimestamp = 0;

                foreach (var trade in data)
                {
                    var timestamp = trade["T"]?.Value<long>() ?? DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                    if (timestamp > latestTimestamp)
                        latestTimestamp = timestamp;

                    trades.Add(new STradeItem
                    {
                        orderId = trade["i"]?.ToString(),
                        timestamp = timestamp,
                        price = trade["p"]?.Value<decimal>() ?? 0,
                        quantity = trade["v"]?.Value<decimal>() ?? 0,
                        amount = (trade["p"]?.Value<decimal>() ?? 0) * (trade["v"]?.Value<decimal>() ?? 0),
                        sideType = trade["S"]?.ToString() == "Buy" ? SideType.Bid : SideType.Ask,
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

        private async Task ProcessTickerData(JObject json)
        {
            try
            {
                var topic = json["topic"].ToString();
                var symbol = ExtractSymbolFromTopic(topic);
                var data = json["data"];
                
                if (data == null)
                    return;

                var ticker = new STicker
                {
                    exchange = ExchangeName,
                    symbol = ConvertToStandardSymbol(symbol),
                    timestamp = json["ts"]?.Value<long>() ?? DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                    result = new STickerItem
                    {
                        timestamp = json["ts"]?.Value<long>() ?? DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                        closePrice = data["lastPrice"]?.Value<decimal>() ?? 0,
                        askPrice = data["ask1Price"]?.Value<decimal>() ?? 0,
                        bidPrice = data["bid1Price"]?.Value<decimal>() ?? 0,
                        askQuantity = data["ask1Size"]?.Value<decimal>() ?? 0,
                        bidQuantity = data["bid1Size"]?.Value<decimal>() ?? 0,
                        openPrice = data["prevPrice24h"]?.Value<decimal>() ?? 0,
                        highPrice = data["highPrice24h"]?.Value<decimal>() ?? 0,
                        lowPrice = data["lowPrice24h"]?.Value<decimal>() ?? 0,
                        volume = data["volume24h"]?.Value<decimal>() ?? 0,
                        quoteVolume = data["turnover24h"]?.Value<decimal>() ?? 0,
                        change = data["price24hPcnt"]?.Value<decimal>() ?? 0,
                        percentage = (data["price24hPcnt"]?.Value<decimal>() ?? 0) * 100
                    }
                };

                InvokeTickerCallback(ticker);
            }
            catch (Exception ex)
            {
                RaiseError($"Ticker processing error: {ex.Message}");
            }
        }

        private async Task ProcessKlineData(JObject json)
        {
            try
            {
                var topic = json["topic"].ToString();
                var parts = topic.Split('.');
                var interval = parts[1]; // Extract interval
                var symbol = parts[2]; // Extract symbol
                var data = json["data"] as JArray;
                
                if (data == null)
                    return;

                var candleItems = new List<SCandleItem>();
                long latestTimestamp = 0;
                var convertedInterval = ConvertInterval(interval);

                foreach (var kline in data)
                {
                    var timestamp = kline["start"]?.Value<long>() ?? 0;
                    if (timestamp > latestTimestamp)
                        latestTimestamp = timestamp;

                    candleItems.Add(new SCandleItem
                    {
                        openTime = timestamp,
                        closeTime = kline["end"]?.Value<long>() ?? 0,
                        open = decimal.Parse(kline["open"]?.ToString() ?? "0"),
                        high = decimal.Parse(kline["high"]?.ToString() ?? "0"),
                        low = decimal.Parse(kline["low"]?.ToString() ?? "0"),
                        close = decimal.Parse(kline["close"]?.ToString() ?? "0"),
                        volume = decimal.Parse(kline["volume"]?.ToString() ?? "0"),
                        quoteVolume = decimal.Parse(kline["turnover"]?.ToString() ?? "0")
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

        private async Task ProcessWalletData(JObject json)
        {
            try
            {
                var data = json["data"] as JArray;
                if (data == null) return;

                foreach (var wallet in data)
                {
                    var coins = wallet["coin"] as JArray;
                    if (coins == null) continue;

                    var balanceItems = new List<SBalanceItem>();
                    var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                    var accountType = wallet["accountType"]?.ToString() ?? "spot";

                    foreach (var coin in coins)
                    {
                        var currency = coin["coin"]?.ToString();
                        var free = coin["free"]?.Value<decimal>() ?? 0;
                        var locked = coin["locked"]?.Value<decimal>() ?? 0;
                        
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
                        clientOrderId = order["orderLinkId"]?.ToString(),
                        symbol = ConvertToStandardSymbol(order["symbol"]?.ToString()),
                        side = order["side"]?.ToString() == "Buy" ? OrderSide.Buy : OrderSide.Sell,
                        type = ConvertOrderType(order["orderType"]?.ToString()),
                        status = ConvertOrderStatus(order["orderStatus"]?.ToString()),
                        price = order["price"]?.Value<decimal>() ?? 0,
                        quantity = order["qty"]?.Value<decimal>() ?? 0,
                        filledQuantity = order["cumExecQty"]?.Value<decimal>() ?? 0,
                        createTime = order["createdTime"]?.Value<long>() ?? timestamp,
                        updateTime = order["updatedTime"]?.Value<long>() ?? timestamp
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
                        symbol = ConvertToStandardSymbol(pos["symbol"]?.ToString()),
                        side = pos["side"]?.ToString() == "Buy" ? PositionSide.Long : PositionSide.Short,
                        size = pos["size"]?.Value<decimal>() ?? 0,
                        entryPrice = pos["avgPrice"]?.Value<decimal>() ?? 0,
                        markPrice = pos["markPrice"]?.Value<decimal>() ?? 0,
                        unrealizedPnl = pos["unrealisedPnl"]?.Value<decimal>() ?? 0,
                        realizedPnl = pos["realisedPnl"]?.Value<decimal>() ?? 0
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

                await SendMessageAsync(JsonConvert.SerializeObject(subscription));

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

                await SendMessageAsync(JsonConvert.SerializeObject(subscription));

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

                await SendMessageAsync(JsonConvert.SerializeObject(subscription));

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

                await SendMessageAsync(JsonConvert.SerializeObject(subscription));

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
            return JsonConvert.SerializeObject(new { op = "ping" });
        }

        protected override string CreateAuthenticationMessage(string apiKey, string secretKey)
        {
            var expires = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + 10000;
            var signature = GenerateSignature(secretKey, $"GET/realtime{expires}");
            
            return JsonConvert.SerializeObject(new
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