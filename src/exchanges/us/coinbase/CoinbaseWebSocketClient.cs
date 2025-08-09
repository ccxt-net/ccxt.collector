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

namespace CCXT.Collector.Coinbase
{
    /*
     * Coinbase Support Markets: USD, EUR, GBP, USDT, USDC
     *
     * API Documentation:
     *     https://docs.cloud.coinbase.com/exchange/docs
     *     https://docs.cloud.coinbase.com/exchange/reference
     *
     * WebSocket API:
     *     https://docs.cloud.coinbase.com/exchange/docs/websocket-feed
     *     https://docs.cloud.coinbase.com/exchange/docs/websocket-channels
     *
     * Fees:
     *     https://help.coinbase.com/en/exchange/trading-and-funding/exchange-fees
     */
    /// <summary>
    /// Coinbase WebSocket client for real-time data streaming
    /// </summary>
    public class CoinbaseWebSocketClient : WebSocketClientBase
    {
        private readonly Dictionary<string, SOrderBook> _orderbookCache;
        private readonly Dictionary<string, long> _lastSequenceNumbers;

        public override string ExchangeName => "Coinbase";
        protected override string WebSocketUrl => "wss://ws-feed.exchange.coinbase.com";
        protected override string PrivateWebSocketUrl => "wss://ws-feed.exchange.coinbase.com";
        protected override int PingIntervalMs => 30000; // 30 seconds

        public CoinbaseWebSocketClient()
        {
            _orderbookCache = new Dictionary<string, SOrderBook>();
            _lastSequenceNumbers = new Dictionary<string, long>();
        }

        protected override async Task ProcessMessageAsync(string message, bool isPrivate = false)
        {
            try
            {
                var json = JObject.Parse(message);
                var type = json["type"]?.ToString();

                if (type == null)
                    return;

                switch (type)
                {
                    case "subscriptions":
                        // Subscription confirmation
                        break;
                    case "ticker":
                        await ProcessTickerData(json);
                        break;
                    case "snapshot":
                        await ProcessOrderbookSnapshot(json);
                        break;
                    case "l2update":
                        await ProcessOrderbookUpdate(json);
                        break;
                    case "match":
                    case "last_match":
                        await ProcessTradeData(json);
                        break;
                    case "heartbeat":
                        // Heartbeat message, ignore
                        break;
                    case "error":
                        RaiseError($"Coinbase error: {json["message"]}");
                        break;
                    case "received":
                    case "open":
                    case "done":
                    case "change":
                        if (isPrivate)
                        {
                            await ProcessOrderUpdate(json);
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                RaiseError($"Message processing error: {ex.Message}");
            }
        }

        private async Task ProcessTickerData(JObject json)
        {
            try
            {
                var productId = json["product_id"]?.ToString();
                if (string.IsNullOrEmpty(productId))
                    return;

                var symbol = ConvertToStandardSymbol(productId);
                var timestamp = ConvertToUnixTimeMillis(json["time"]?.ToString());

                var ticker = new STicker
                {
                    exchange = ExchangeName,
                    symbol = symbol,
                    timestamp = timestamp,
                    result = new STickerItem
                    {
                        timestamp = timestamp,
                        closePrice = json["price"]?.Value<decimal>() ?? 0,
                        openPrice = json["open_24h"]?.Value<decimal>() ?? 0,
                        highPrice = json["high_24h"]?.Value<decimal>() ?? 0,
                        lowPrice = json["low_24h"]?.Value<decimal>() ?? 0,
                        volume = json["volume_24h"]?.Value<decimal>() ?? 0,
                        bidPrice = json["best_bid"]?.Value<decimal>() ?? 0,
                        bidQuantity = json["best_bid_size"]?.Value<decimal>() ?? 0,
                        askPrice = json["best_ask"]?.Value<decimal>() ?? 0,
                        askQuantity = json["best_ask_size"]?.Value<decimal>() ?? 0,
                        vwap = 0, // Not provided by Coinbase ticker
                        count = 0, // Not provided
                        change = (json["price"]?.Value<decimal>() ?? 0) - (json["open_24h"]?.Value<decimal>() ?? 0),
                        percentage = CalculatePercentage(json["open_24h"]?.Value<decimal>() ?? 0, json["price"]?.Value<decimal>() ?? 0)
                    }
                };

                InvokeTickerCallback(ticker);
            }
            catch (Exception ex)
            {
                RaiseError($"Ticker processing error: {ex.Message}");
            }
        }

        private async Task ProcessOrderbookSnapshot(JObject json)
        {
            try
            {
                var productId = json["product_id"]?.ToString();
                if (string.IsNullOrEmpty(productId))
                    return;

                var symbol = ConvertToStandardSymbol(productId);
                var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

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

                // Process bids
                var bids = json["bids"] as JArray;
                if (bids != null)
                {
                    foreach (var bid in bids)
                    {
                        var price = bid[0].Value<decimal>();
                        var size = bid[1].Value<decimal>();
                        
                        orderbook.result.bids.Add(new SOrderBookItem
                        {
                            price = price,
                            quantity = size,
                            amount = price * size
                        });
                    }
                }

                // Process asks
                var asks = json["asks"] as JArray;
                if (asks != null)
                {
                    foreach (var ask in asks)
                    {
                        var price = ask[0].Value<decimal>();
                        var size = ask[1].Value<decimal>();
                        
                        orderbook.result.asks.Add(new SOrderBookItem
                        {
                            price = price,
                            quantity = size,
                            amount = price * size
                        });
                    }
                }

                // Sort orderbook
                orderbook.result.bids = orderbook.result.bids.OrderByDescending(b => b.price).ToList();
                orderbook.result.asks = orderbook.result.asks.OrderBy(a => a.price).ToList();

                _orderbookCache[symbol] = orderbook;
                InvokeOrderbookCallback(orderbook);
            }
            catch (Exception ex)
            {
                RaiseError($"Orderbook snapshot processing error: {ex.Message}");
            }
        }

        private async Task ProcessOrderbookUpdate(JObject json)
        {
            try
            {
                var productId = json["product_id"]?.ToString();
                if (string.IsNullOrEmpty(productId))
                    return;

                var symbol = ConvertToStandardSymbol(productId);
                var timestamp = ConvertToUnixTimeMillis(json["time"]?.ToString());
                
                if (!_orderbookCache.ContainsKey(symbol))
                {
                    // Need snapshot first
                    return;
                }

                var cached = _orderbookCache[symbol];
                cached.timestamp = timestamp;

                // Process changes
                var changes = json["changes"] as JArray;
                if (changes != null)
                {
                    foreach (var change in changes)
                    {
                        var side = change[0].ToString();
                        var price = change[1].Value<decimal>();
                        var size = change[2].Value<decimal>();

                        if (side == "buy")
                        {
                            UpdateOrderbookSide(cached.result.bids, price, size);
                        }
                        else if (side == "sell")
                        {
                            UpdateOrderbookSide(cached.result.asks, price, size);
                        }
                    }
                }

                // Sort orderbook
                cached.result.bids = cached.result.bids.OrderByDescending(b => b.price).ToList();
                cached.result.asks = cached.result.asks.OrderBy(a => a.price).ToList();

                InvokeOrderbookCallback(cached);
            }
            catch (Exception ex)
            {
                RaiseError($"Orderbook update processing error: {ex.Message}");
            }
        }

        private void UpdateOrderbookSide(List<SOrderBookItem> side, decimal price, decimal size)
        {
            var existing = side.FirstOrDefault(item => item.price == price);
            
            if (existing != null)
            {
                if (size == 0)
                {
                    side.Remove(existing);
                }
                else
                {
                    existing.quantity = size;
                    existing.amount = price * size;
                }
            }
            else if (size > 0)
            {
                side.Add(new SOrderBookItem
                {
                    price = price,
                    quantity = size,
                    amount = price * size
                });
            }
        }

        private async Task ProcessTradeData(JObject json)
        {
            try
            {
                var productId = json["product_id"]?.ToString();
                if (string.IsNullOrEmpty(productId))
                    return;

                var symbol = ConvertToStandardSymbol(productId);
                var timestamp = ConvertToUnixTimeMillis(json["time"]?.ToString());

                var trade = new STrade
                {
                    exchange = ExchangeName,
                    symbol = symbol,
                    timestamp = timestamp,
                    result = new List<STradeItem>
                    {
                        new STradeItem
                        {
                            orderId = json["trade_id"]?.ToString() ?? json["sequence"]?.ToString(),
                            timestamp = timestamp,
                            price = json["price"]?.Value<decimal>() ?? 0,
                            quantity = json["size"]?.Value<decimal>() ?? 0,
                            amount = (json["price"]?.Value<decimal>() ?? 0) * (json["size"]?.Value<decimal>() ?? 0),
                            sideType = json["side"]?.ToString() == "buy" ? SideType.Bid : SideType.Ask,
                            orderType = OrderType.Limit
                        }
                    }
                };

                InvokeTradeCallback(trade);
            }
            catch (Exception ex)
            {
                RaiseError($"Trade processing error: {ex.Message}");
            }
        }

        private async Task ProcessOrderUpdate(JObject json)
        {
            try
            {
                var type = json["type"]?.ToString();
                var productId = json["product_id"]?.ToString();
                
                if (string.IsNullOrEmpty(productId))
                    return;

                var symbol = ConvertToStandardSymbol(productId);
                var timestamp = ConvertToUnixTimeMillis(json["time"]?.ToString());

                var order = new SOrderItem
                {
                    orderId = json["order_id"]?.ToString(),
                    clientOrderId = json["client_oid"]?.ToString(),
                    symbol = symbol,
                    side = json["side"]?.ToString() == "buy" ? OrderSide.Buy : OrderSide.Sell,
                    type = ParseOrderType(json["order_type"]?.ToString()),
                    status = ParseOrderStatus(type, json),
                    price = json["price"]?.Value<decimal>() ?? 0,
                    quantity = json["size"]?.Value<decimal>() ?? 0,
                    filledQuantity = json["filled_size"]?.Value<decimal>() ?? 0,
                    remainingQuantity = json["remaining_size"]?.Value<decimal>() ?? 0,
                    createTime = timestamp,
                    updateTime = timestamp
                };

                var orders = new SOrder
                {
                    exchange = ExchangeName,
                    timestamp = timestamp,
                    orders = new List<SOrderItem> { order }
                };

                InvokeOrderCallback(orders);
            }
            catch (Exception ex)
            {
                RaiseError($"Order update processing error: {ex.Message}");
            }
        }

        public override async Task<bool> SubscribeOrderbookAsync(string symbol)
        {
            try
            {
                var productId = ConvertToCoinbaseSymbol(symbol);
                var subscription = new
                {
                    type = "subscribe",
                    product_ids = new[] { productId },
                    channels = new[] { "level2", "heartbeat" }
                };

                await SendMessageAsync(JsonConvert.SerializeObject(subscription));

                var key = CreateSubscriptionKey("orderbook", symbol);
                _subscriptions[key] = new SubscriptionInfo
                {
                    Channel = "level2",
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
                var productId = ConvertToCoinbaseSymbol(symbol);
                var subscription = new
                {
                    type = "subscribe",
                    product_ids = new[] { productId },
                    channels = new[] { "matches", "heartbeat" }
                };

                await SendMessageAsync(JsonConvert.SerializeObject(subscription));

                var key = CreateSubscriptionKey("trades", symbol);
                _subscriptions[key] = new SubscriptionInfo
                {
                    Channel = "matches",
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
                var productId = ConvertToCoinbaseSymbol(symbol);
                var subscription = new
                {
                    type = "subscribe",
                    product_ids = new[] { productId },
                    channels = new[] { "ticker", "heartbeat" }
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
            // Coinbase doesn't support kline/candle data via WebSocket
            // Need to use REST API for historical data
            RaiseError("Coinbase doesn't support candle data via WebSocket. Use REST API instead.");
            return false;
        }

        public override async Task<bool> UnsubscribeAsync(string channel, string symbol)
        {
            try
            {
                var productId = ConvertToCoinbaseSymbol(symbol);
                var channelName = channel switch
                {
                    "orderbook" => "level2",
                    "trades" => "matches",
                    "ticker" => "ticker",
                    _ => channel
                };

                var unsubscription = new
                {
                    type = "unsubscribe",
                    product_ids = new[] { productId },
                    channels = new[] { channelName }
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
            // Coinbase uses heartbeat channel instead of ping/pong
            return null;
        }

        protected override string CreateAuthenticationMessage(string apiKey, string secretKey)
        {
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
            var method = "GET";
            var requestPath = "/users/self/verify";
            
            var signature = GenerateSignature(secretKey, timestamp, method, requestPath);
            
            return JsonConvert.SerializeObject(new
            {
                type = "subscribe",
                product_ids = new string[] { },
                channels = new[]
                {
                    new
                    {
                        name = "user",
                        product_ids = new string[] { }
                    }
                },
                signature = signature,
                key = apiKey,
                passphrase = "", // Coinbase requires passphrase
                timestamp = timestamp
            });
        }

        private string GenerateSignature(string secretKey, string timestamp, string method, string requestPath, string body = "")
        {
            var message = timestamp + method + requestPath + body;
            var encoding = new System.Text.ASCIIEncoding();
            byte[] keyByte = Convert.FromBase64String(secretKey);
            byte[] messageBytes = encoding.GetBytes(message);
            using (var hmacsha256 = new System.Security.Cryptography.HMACSHA256(keyByte))
            {
                byte[] hashmessage = hmacsha256.ComputeHash(messageBytes);
                return Convert.ToBase64String(hashmessage);
            }
        }

        private string ConvertToCoinbaseSymbol(string symbol)
        {
            // Convert from standard format (BTC/USD) to Coinbase format (BTC-USD)
            return symbol.Replace("/", "-");
        }

        private string ConvertToStandardSymbol(string productId)
        {
            // Convert from Coinbase format (BTC-USD) to standard format (BTC/USD)
            return productId.Replace("-", "/");
        }

        private long ConvertToUnixTimeMillis(string timeString)
        {
            if (string.IsNullOrEmpty(timeString))
                return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            if (DateTime.TryParse(timeString, out var dateTime))
            {
                return new DateTimeOffset(dateTime, TimeSpan.Zero).ToUnixTimeMilliseconds();
            }

            return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        }

        private decimal CalculatePercentage(decimal open, decimal close)
        {
            if (open == 0)
                return 0;
            
            return ((close - open) / open) * 100;
        }

        private OrderType ParseOrderType(string type)
        {
            return type?.ToLower() switch
            {
                "limit" => OrderType.Limit,
                "market" => OrderType.Market,
                "stop" => OrderType.Stop,
                _ => OrderType.Limit
            };
        }

        private OrderStatus ParseOrderStatus(string type, JObject json)
        {
            return type switch
            {
                "received" => OrderStatus.New,
                "open" => OrderStatus.Open,
                "done" => ParseDoneReason(json["reason"]?.ToString()),
                "match" => OrderStatus.PartiallyFilled,
                "change" => OrderStatus.Open,
                _ => OrderStatus.Open
            };
        }

        private OrderStatus ParseDoneReason(string reason)
        {
            return reason switch
            {
                "filled" => OrderStatus.Filled,
                "canceled" => OrderStatus.Canceled,
                _ => OrderStatus.Canceled
            };
        }

        #region Helper Methods

        protected override async Task ResubscribeAsync(SubscriptionInfo subscription)
        {
            switch (subscription.Channel)
            {
                case "level2":
                    await SubscribeOrderbookAsync(subscription.Symbol);
                    break;
                case "matches":
                    await SubscribeTradesAsync(subscription.Symbol);
                    break;
                case "ticker":
                    await SubscribeTickerAsync(subscription.Symbol);
                    break;
            }
        }

        #endregion
    }
}

