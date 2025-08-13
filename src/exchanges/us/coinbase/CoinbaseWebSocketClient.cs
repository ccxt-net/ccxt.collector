using CCXT.Collector.Core.Abstractions;
using CCXT.Collector.Models.WebSocket;
using CCXT.Collector.Service;
using System;
using CCXT.Collector.Core.Infrastructure; 
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text.Json;
using System.Threading.Tasks;

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
     *     https://docs.cdp.coinbase.com/exchange/websocket-feed/overview
     *     https://docs.cdp.coinbase.com/exchange/websocket-feed/best-practices
     *     https://docs.cdp.coinbase.com/exchange/websocket-feed/authentication
     *     https://docs.cdp.coinbase.com/exchange/websocket-feed/channels
     *     https://docs.cdp.coinbase.com/exchange/websocket-feed/rate-limits
     *     https://docs.cdp.coinbase.com/exchange/websocket-feed/errors
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
        protected override string PrivateWebSocketUrl => "wss://ws-direct.exchange.coinbase.com";
        protected override int PingIntervalMs => 30000; // 30 seconds

        public CoinbaseWebSocketClient()
        {
            _orderbookCache = new Dictionary<string, SOrderBook>();
            _lastSequenceNumbers = new Dictionary<string, long>();
        }

        protected override void ConfigureWebSocket(ClientWebSocket webSocket)
        {
            // Coinbase might require specific headers
            webSocket.Options.SetRequestHeader("User-Agent", "CCXT.Collector/1.0");
            webSocket.Options.KeepAliveInterval = TimeSpan.FromSeconds(30);
        }

        protected override async Task ProcessMessageAsync(string message, bool isPrivate = false)
        {
            try
            {
                using var doc = JsonDocument.Parse(message);
                var json = doc.RootElement;
                var type = json.GetStringOrDefault("type");

                if (type == null)
                    return;

                switch (type)
                {
                    case "subscriptions":
                        // Subscription confirmation - log channels for debugging
                        if (json.TryGetProperty("channels", out var channels))
                        {
                            // Successfully subscribed
                        }
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
                        var errorMsg = json.GetStringOrDefault("message", "Unknown error");
                        var reason = json.GetStringOrDefault("reason", "");
                        if (!string.IsNullOrEmpty(reason))
                            errorMsg = $"{errorMsg}: {reason}";
                        RaiseError($"Coinbase error: {errorMsg}");
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

        private async Task ProcessTickerData(JsonElement json)
        {
            try
            {
                var productId = json.GetStringOrDefault("product_id");
                if (string.IsNullOrEmpty(productId))
                    return;

                var symbol = ConvertToStandardSymbol(productId);
                var timestamp = ConvertToUnixTimeMillis(json.GetStringOrDefault("time"));

                var ticker = new STicker
                {
                    exchange = ExchangeName,
                    symbol = symbol,
                    timestamp = timestamp,
                    result = new STickerItem
                    {
                        timestamp = timestamp,
                        closePrice = json.GetDecimalOrDefault("price"),
                        openPrice = json.GetDecimalOrDefault("open_24h"),
                        highPrice = json.GetDecimalOrDefault("high_24h"),
                        lowPrice = json.GetDecimalOrDefault("low_24h"),
                        volume = json.GetDecimalOrDefault("volume_24h"),
                        bidPrice = json.GetDecimalOrDefault("best_bid"),
                        bidQuantity = json.GetDecimalOrDefault("best_bid_size"),
                        askPrice = json.GetDecimalOrDefault("best_ask"),
                        askQuantity = json.GetDecimalOrDefault("best_ask_size"),
                        vwap = 0, // Not provided by Coinbase ticker
                        count = 0, // Not provided
                        change = (json.GetDecimalOrDefault("price")) - (json.GetDecimalOrDefault("open_24h")),
                        percentage = CalculatePercentage(json.GetDecimalOrDefault("open_24h"), json.GetDecimalOrDefault("price"))
                    }
                };

                InvokeTickerCallback(ticker);
            }
            catch (Exception ex)
            {
                RaiseError($"Ticker processing error: {ex.Message}");
            }
        }

        private async Task ProcessOrderbookSnapshot(JsonElement json)
        {
            try
            {
                var productId = json.GetStringOrDefault("product_id");
                if (string.IsNullOrEmpty(productId))
                    return;

                var symbol = ConvertToStandardSymbol(productId);
                var timestamp = TimeExtension.UnixTime;

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
                if (json.TryGetArray("bids", out var bids))
                {
                    foreach (var bid in bids.EnumerateArray())
                    {
                        if (bid.GetArrayLength() < 2)
                            continue;

                        var price = bid[0].GetDecimalValue();
                        var size = bid[1].GetDecimalValue();

                        orderbook.result.bids.Add(new SOrderBookItem
                        {
                            price = price,
                            quantity = size,
                            amount = price * size
                        });
                    }
                }

                // Process asks
                if (json.TryGetArray("asks", out var asks))
                {
                    foreach (var ask in asks.EnumerateArray())
                    {
                        if (ask.GetArrayLength() < 2)
                            continue;

                        var price = ask[0].GetDecimalValue();
                        var size = ask[1].GetDecimalValue();

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

        private async Task ProcessOrderbookUpdate(JsonElement json)
        {
            try
            {
                var productId = json.GetStringOrDefault("product_id");
                if (string.IsNullOrEmpty(productId))
                    return;

                var symbol = ConvertToStandardSymbol(productId);
                var timestamp = ConvertToUnixTimeMillis(json.GetStringOrDefault("time"));

                if (!_orderbookCache.ContainsKey(symbol))
                {
                    // Need snapshot first
                    return;
                }

                var cached = _orderbookCache[symbol];
                cached.timestamp = timestamp;

                // Process changes
                if (json.TryGetArray("changes", out var changes))
                {
                    foreach (var change in changes.EnumerateArray())
                    {
                        if (change.GetArrayLength() < 3)
                            continue;

                        var side = change[0].ToString();
                        var price = change[1].GetDecimalValue();
                        var size = change[2].GetDecimalValue();

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

        private async Task ProcessTradeData(JsonElement json)
        {
            try
            {
                var productId = json.GetStringOrDefault("product_id");
                if (string.IsNullOrEmpty(productId))
                    return;

                var symbol = ConvertToStandardSymbol(productId);
                var timestamp = ConvertToUnixTimeMillis(json.GetStringOrDefault("time"));

                var trade = new STrade
                {
                    exchange = ExchangeName,
                    symbol = symbol,
                    timestamp = timestamp,
                    result = new List<STradeItem>
                    {
                        new STradeItem
                        {
                            tradeId = json.GetStringOrDefault("trade_id", json.GetStringOrDefault("sequence")),
                            timestamp = timestamp,
                            price = json.GetDecimalOrDefault("price"),
                            quantity = json.GetDecimalOrDefault("size"),
                            amount = (json.GetDecimalOrDefault("price")) * (json.GetDecimalOrDefault("size")),
                            sideType = json.GetStringOrDefault("side") == "buy" ? SideType.Bid : SideType.Ask,
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

        private async Task ProcessOrderUpdate(JsonElement json)
        {
            try
            {
                var type = json.GetStringOrDefault("type");
                var productId = json.GetStringOrDefault("product_id");

                if (string.IsNullOrEmpty(productId))
                    return;

                var symbol = ConvertToStandardSymbol(productId);
                var timestamp = ConvertToUnixTimeMillis(json.GetStringOrDefault("time"));

                var order = new SOrderItem
                {
                    orderId = json.GetStringOrDefault("order_id"),
                    clientOrderId = json.GetStringOrDefault("client_oid"),
                    symbol = symbol,
                    side = json.GetStringOrDefault("side") == "buy" ? OrderSide.Buy : OrderSide.Sell,
                    type = ParseOrderType(json.GetStringOrDefault("order_type")),
                    status = ParseOrderStatus(type, json),
                    price = json.GetDecimalOrDefault("price"),
                    quantity = json.GetDecimalOrDefault("size"),
                    filledQuantity = json.GetDecimalOrDefault("filled_size"),
                    remainingQuantity = json.GetDecimalOrDefault("remaining_size"),
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
                    channels = new[] { "level2_batch", "heartbeat" }
                };

                await SendMessageAsync(JsonSerializer.Serialize(subscription));

                MarkSubscriptionActive("orderbook", symbol);

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

                await SendMessageAsync(JsonSerializer.Serialize(subscription));

                MarkSubscriptionActive("trades", symbol);

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

                await SendMessageAsync(JsonSerializer.Serialize(subscription));

                MarkSubscriptionActive("ticker", symbol);

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
                    "orderbook" => "level2_batch",
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
            // Coinbase uses heartbeat channel instead of ping/pong
            return null;
        }

        protected override string CreateAuthenticationMessage(string apiKey, string secretKey)
        {
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
            var method = "GET";
            var requestPath = "/users/self/verify";

            var signature = GenerateSignature(secretKey, timestamp, method, requestPath);

            return JsonSerializer.Serialize(new
            {
                type = "subscribe",
                product_ids = Array.Empty<string>(),
                channels = new[]
                {
                    new
                    {
                        name = "user",
                        product_ids = Array.Empty<string>()
                    }
                },
                signature,
                key = apiKey,
                passphrase = "", // Coinbase requires passphrase
                timestamp
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
            return ParsingHelpers.ToDashSymbol(symbol);
        }

        private string ConvertToStandardSymbol(string productId)
        {
            // Convert from Coinbase format (BTC-USD) to standard format (BTC/USD)
            return ParsingHelpers.FromDashSymbol(productId);
        }

        private long ConvertToUnixTimeMillis(string timeString)
        {
            if (string.IsNullOrEmpty(timeString))
                return TimeExtension.UnixTime;

            // Try parsing as ISO 8601 with timezone info first
            if (DateTimeOffset.TryParse(timeString, out var dateTimeOffset))
            {
                return dateTimeOffset.ToUnixTimeMilliseconds();
            }

            // If that fails, try parsing as DateTime and assume UTC
            if (DateTime.TryParse(timeString, out var dateTime))
            {
                // Specify that the DateTime is in UTC to avoid offset issues
                var utcDateTime = DateTime.SpecifyKind(dateTime, DateTimeKind.Utc);
                return new DateTimeOffset(utcDateTime).ToUnixTimeMilliseconds();
            }

            return TimeExtension.UnixTime;
        }

        private decimal CalculatePercentage(decimal open, decimal close)
        {
            if (open == 0)
                return 0;

            return ((close - open) / open) * 100;
        }

        private OrderType ParseOrderType(string type)
        {
            return ParsingHelpers.ParseGenericOrderType(type);
        }

        private OrderStatus ParseOrderStatus(string type, JsonElement json)
        {
            if (type == "done")
            {
                var reason = json.GetStringOrDefault("reason");
                if (string.Equals(reason, "filled", StringComparison.OrdinalIgnoreCase))
                    return OrderStatus.Filled;
                if (string.Equals(reason, "canceled", StringComparison.OrdinalIgnoreCase))
                    return OrderStatus.Canceled;
            }
            // 나머지는 일반 매핑 재사용
            return ParsingHelpers.ParseGenericOrderStatus(type);
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
                case "level2_batch":
                case "orderbook":
                    await SubscribeOrderbookAsync(subscription.Symbol);
                    break;
                case "matches":
                case "trades":
                    await SubscribeTradesAsync(subscription.Symbol);
                    break;
                case "ticker":
                    await SubscribeTickerAsync(subscription.Symbol);
                    break;
            }
        }

        #endregion

        #region Batch Subscription Support

        /// <summary>
        /// Coinbase supports batch subscription - multiple product IDs and channels in single message
        /// </summary>
        protected override bool SupportsBatchSubscription()
        {
            return true;
        }

        /// <summary>
        /// Send batch subscriptions for Coinbase - combines product IDs and channels efficiently
        /// </summary>
        protected override async Task<bool> SendBatchSubscriptionsAsync(List<KeyValuePair<string, SubscriptionInfo>> subscriptions)
        {
            try
            {
                // Group subscriptions by channel type
                var channelGroups = subscriptions
                    .GroupBy(s => s.Value.Channel.ToLower())
                    .ToList();

                var channels = new List<object>();
                var allProductIds = new List<string>();

                foreach (var channelGroup in channelGroups)
                {
                    var channel = channelGroup.Key;
                    var productIds = channelGroup
                        .Select(s => ConvertToCoinbaseSymbol(s.Value.Symbol))
                        .Distinct()
                        .ToArray();

                    if (productIds.Length == 0)
                        continue;

                    // Map channel names to Coinbase channel format
                    string channelName = channel switch
                    {
                        "orderbook" or "depth" => "level2_batch",
                        "trades" or "trade" => "matches",
                        "ticker" => "ticker",
                        "candles" or "kline" or "candlestick" => null, // Not supported via WebSocket
                        _ => channel
                    };

                    if (channelName == null)
                    {
                        continue; // Skip unsupported channels
                    }

                    // Add channel with specific product IDs
                    channels.Add(new
                    {
                        name = channelName,
                        product_ids = productIds
                    });

                    // Collect all product IDs for global subscription
                    allProductIds.AddRange(productIds);
                }

                if (channels.Count == 0)
                    return true;

                // Always include heartbeat channel
                channels.Add("heartbeat");

                // Create Coinbase subscription message
                // Can specify product_ids globally or per channel
                var subscriptionMessage = new
                {
                    type = "subscribe",
                    product_ids = allProductIds.Distinct().ToArray(),
                    channels = channels.ToArray()
                };

                await SendMessageAsync(JsonSerializer.Serialize(subscriptionMessage));

                RaiseError($"Sent Coinbase batch subscription for {subscriptions.Count} subscriptions across {channels.Count - 1} channels");

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