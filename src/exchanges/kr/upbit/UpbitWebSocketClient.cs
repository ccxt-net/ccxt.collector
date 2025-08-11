using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CCXT.Collector.Core.Abstractions;
using CCXT.Collector.Library;
using CCXT.Collector.Service;
using System.Text.Json;

namespace CCXT.Collector.Upbit
{
        /*
         * Upbit Support Markets: KRW, USDT, BTC
         *
         * API Documentation:
         *     Korean: https://docs.upbit.com/docs/%EC%9A%94%EC%B2%AD-%EC%88%98-%EC%A0%9C%ED%95%9C
         *     Global: https://global-docs.upbit.com/reference/today-trades-history
         *
         * WebSocket API:
         *     https://docs.upbit.com/docs/upbit-quotation-websocket
         *     https://docs.upbit.com/reference
         *
         * Fees & Status:
         *     https://upbit.com/service_center/guide
         *     https://upbit.com/service_center/wallet_status
         */
    /// <summary>
    /// Upbit WebSocket client for real-time data streaming
    /// </summary>
    public class UpbitWebSocketClient : WebSocketClientBase
    {
        private readonly Dictionary<string, SOrderBook> _orderbookCache;

        public override string ExchangeName => "Upbit";
        protected override string WebSocketUrl => "wss://api.upbit.com/websocket/v1";
        protected override int PingIntervalMs => 120000; // 2 minutes

        public UpbitWebSocketClient()
        {
            _orderbookCache = new Dictionary<string, SOrderBook>();
        }

        protected override async Task ProcessMessageAsync(string message, bool isPrivate = false)
        {
            try
            {
                // Upbit sends data in binary format first, then JSON
                using var doc = JsonDocument.Parse(message); 
                var json = doc.RootElement;
                
                if (json.TryGetProperty("type", out var typeProp))
                {
                    var messageType = json.GetStringOrDefault("type");
                    
                    if (isPrivate)
                    {
                        // Private channel messages
                        switch (messageType)
                        {
                            case "balance":
                                await ProcessBalance(json);
                                break;
                            case "order":
                                await ProcessOrder(json);
                                break;
                            case "position":
                                await ProcessPosition(json);
                                break;
                            case "error":
                                var privateErrorMessage = json.GetStringOrDefault("message", "Unknown private error");
                                RaiseError($"Upbit private error: {privateErrorMessage}");
                                break;
                        }
                    }
                    else
                    {
                        // Public channel messages
                        switch (messageType)
                        {
                            case "orderbook":
                                await ProcessOrderbook(json);
                                break;
                            case "trade":
                                await ProcessTrade(json);
                                break;
                            case "ticker":
                                await ProcessTickerData(json);
                                break;
                            case "candle":
                                await ProcessCandle(json);
                                break;
                            case "error":
                                var publicErrorMessage = json.GetStringOrDefault("message", "Unknown error");
                                RaiseError($"Upbit error: {publicErrorMessage}");
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

        private async Task ProcessOrderbook(JsonElement json)
        {
            try
            {
                var code = json.GetStringOrDefault("code");
                var symbol = ConvertFromUpbitCode(code);
                
                var orderbook = new SOrderBook
                {
                    exchange = ExchangeName,
                    symbol = symbol,
                    timestamp = json.GetInt64OrDefault("timestamp"),
                    sequentialId = json.GetInt64OrDefault("total_ask_size"),
                    result = new SOrderBookData
                    {
                        timestamp = json.GetInt64OrDefault("timestamp"),
                        asks = new List<SOrderBookItem>(),
                        bids = new List<SOrderBookItem>()
                    }
                };

                // Process orderbook units
                if (json.TryGetArray("orderbook_units", out var units))
                {
                    foreach (var unit in units.EnumerateArray())
                    {
                        // Add ask
                        var askPrice = unit.GetDecimalOrDefault("ask_price");
                        var askSize = unit.GetDecimalOrDefault("ask_size");
                        if (askSize > 0)
                        {
                            orderbook.result.asks.Add(new SOrderBookItem
                            {
                                price = askPrice,
                                quantity = askSize,
                                amount = askPrice * askSize,
                                action = "U"
                            });
                        }

                        // Add bid
                        var bidPrice = unit.GetDecimalOrDefault("bid_price");
                        var bidSize = unit.GetDecimalOrDefault("bid_size");
                        if (bidSize > 0)
                        {
                            orderbook.result.bids.Add(new SOrderBookItem
                            {
                                price = bidPrice,
                                quantity = bidSize,
                                amount = bidPrice * bidSize,
                                action = "U"
                            });
                        }
                    }
                }

                // Sort orderbook
                orderbook.result.bids = orderbook.result.bids.OrderByDescending(b => b.price).ToList();
                orderbook.result.asks = orderbook.result.asks.OrderBy(a => a.price).ToList();

                InvokeOrderbookCallback(orderbook);
            }
            catch (Exception ex)
            {
                RaiseError($"Orderbook processing error: {ex.Message}");
            }
        }

        private async Task ProcessTrade(JsonElement json)
        {
            try
            {
                var code = json.GetStringOrDefault("code");
                var symbol = ConvertFromUpbitCode(code);
                
                var trade = new STrade
                {
                    exchange = ExchangeName,
                    symbol = symbol,
                    timestamp = json.GetInt64OrDefault("timestamp"),
                    result = new List<STradeItem>
                    {
                        new STradeItem
                        {
                            tradeId = json.GetStringOrDefault("sequential_id", Guid.NewGuid().ToString()),
                            timestamp = json.GetInt64OrDefault("timestamp"),
                            sideType = json.GetStringOrDefault("ask_bid") == "ASK" ? SideType.Ask : SideType.Bid,
                            orderType = OrderType.Limit,
                            price = json.GetDecimalOrDefault("trade_price"),
                            quantity = json.GetDecimalOrDefault("trade_volume"),
                            amount = json.GetDecimalOrDefault("trade_price") * json.GetDecimalOrDefault("trade_volume")
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

        private async Task ProcessTickerData(JsonElement json)
        {
            try
            {
                var code = json.GetStringOrDefault("code");
                var symbol = ConvertFromUpbitCode(code);
                
                var ticker = new STicker
                {
                    exchange = ExchangeName,
                    symbol = symbol,
                    timestamp = json.GetInt64OrDefault("timestamp"),
                    result = new STickerItem
                    {
                        timestamp = json.GetInt64OrDefault("timestamp"),
                        openPrice = json.GetDecimalOrDefault("opening_price"),
                        highPrice = json.GetDecimalOrDefault("high_price"),
                        lowPrice = json.GetDecimalOrDefault("low_price"),
                        closePrice = json.GetDecimalOrDefault("trade_price"),
                        volume = json.GetDecimalOrDefault("acc_trade_volume_24h"),
                        quoteVolume = json.GetDecimalOrDefault("acc_trade_price_24h"),
                        prevClosePrice = json.GetDecimalOrDefault("prev_closing_price"),
                        change = json.GetDecimalOrDefault("signed_change_price"),
                        percentage = json.GetDecimalOrDefault("signed_change_rate") * 100
                    }
                };

                // Set bid/ask from orderbook cache if available
                if (_orderbookCache.ContainsKey(symbol))
                {
                    var ob = _orderbookCache[symbol].result;
                    if (ob.bids.Any())
                    {
                        ticker.result.bidPrice = ob.bids[0].price;
                        ticker.result.bidQuantity = ob.bids[0].quantity;
                    }
                    if (ob.asks.Any())
                    {
                        ticker.result.askPrice = ob.asks[0].price;
                        ticker.result.askQuantity = ob.asks[0].quantity;
                    }
                }

                InvokeTickerCallback(ticker);
            }
            catch (Exception ex)
            {
                RaiseError($"Ticker processing error: {ex.Message}");
            }
        }

        public override async Task<bool> SubscribeOrderbookAsync(Market market)
        {
            try
            {
                var upbitCode = FormatSymbol(market);
                
                var subscription = new List<object>
                {
                    new { ticket = Guid.NewGuid().ToString() },
                    new { type = "orderbook", codes = new[] { upbitCode } }
                };

                await SendMessageAsync(JsonSerializer.Serialize(subscription));
                
                var key = CreateSubscriptionKey("orderbook", market.ToString());
                _subscriptions[key] = new SubscriptionInfo
                {
                    Channel = "orderbook",
                    Symbol = market.ToString(),
                    SubscribedAt = DateTime.UtcNow,
                    IsActive = true
                };

                return true;
            }
            catch (Exception ex)
            {
                RaiseError($"Subscribe orderbook error: {ex.Message}");
                return false;
            }
        }

        public override async Task<bool> SubscribeOrderbookAsync(string symbol)
        {
            try
            {
                var upbitCode = ConvertToUpbitCode(symbol);
                
                var subscription = new List<object>
                {
                    new { ticket = Guid.NewGuid().ToString() },
                    new { type = "orderbook", codes = new[] { upbitCode } }
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
                RaiseError($"Subscribe orderbook error: {ex.Message}");
                return false;
            }
        }

        public override async Task<bool> SubscribeTradesAsync(Market market)
        {
            try
            {
                var upbitCode = FormatSymbol(market);
                
                var subscription = new List<object>
                {
                    new { ticket = Guid.NewGuid().ToString() },
                    new { type = "trade", codes = new[] { upbitCode } }
                };

                await SendMessageAsync(JsonSerializer.Serialize(subscription));
                
                var key = CreateSubscriptionKey("trade", market.ToString());
                _subscriptions[key] = new SubscriptionInfo
                {
                    Channel = "trade",
                    Symbol = market.ToString(),
                    SubscribedAt = DateTime.UtcNow,
                    IsActive = true
                };

                return true;
            }
            catch (Exception ex)
            {
                RaiseError($"Subscribe trades error: {ex.Message}");
                return false;
            }
        }

        public override async Task<bool> SubscribeTradesAsync(string symbol)
        {
            try
            {
                var upbitCode = ConvertToUpbitCode(symbol);
                
                var subscription = new List<object>
                {
                    new { ticket = Guid.NewGuid().ToString() },
                    new { type = "trade", codes = new[] { upbitCode } }
                };

                await SendMessageAsync(JsonSerializer.Serialize(subscription));
                
                var key = CreateSubscriptionKey("trade", symbol);
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
                RaiseError($"Subscribe trades error: {ex.Message}");
                return false;
            }
        }

        public override async Task<bool> SubscribeTickerAsync(Market market)
        {
            try
            {
                var upbitCode = FormatSymbol(market);
                
                var subscription = new List<object>
                {
                    new { ticket = Guid.NewGuid().ToString() },
                    new { type = "ticker", codes = new[] { upbitCode } }
                };

                await SendMessageAsync(JsonSerializer.Serialize(subscription));
                
                var key = CreateSubscriptionKey("ticker", market.ToString());
                _subscriptions[key] = new SubscriptionInfo
                {
                    Channel = "ticker",
                    Symbol = market.ToString(),
                    SubscribedAt = DateTime.UtcNow,
                    IsActive = true
                };

                return true;
            }
            catch (Exception ex)
            {
                RaiseError($"Subscribe ticker error: {ex.Message}");
                return false;
            }
        }

        public override async Task<bool> SubscribeTickerAsync(string symbol)
        {
            try
            {
                var upbitCode = ConvertToUpbitCode(symbol);
                
                var subscription = new List<object>
                {
                    new { ticket = Guid.NewGuid().ToString() },
                    new { type = "ticker", codes = new[] { upbitCode } }
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
                RaiseError($"Subscribe ticker error: {ex.Message}");
                return false;
            }
        }

        public override async Task<bool> UnsubscribeAsync(string channel, Market market)
        {
            try
            {
                // Upbit doesn't support explicit unsubscribe
                // We just mark the subscription as inactive
                var key = CreateSubscriptionKey(channel, market.ToString());
                if (_subscriptions.TryGetValue(key, out var sub))
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

        public override async Task<bool> UnsubscribeAsync(string channel, string symbol)
        {
            try
            {
                // Upbit doesn't support explicit unsubscribe
                // We just mark the subscription as inactive
                var key = CreateSubscriptionKey(channel, symbol);
                if (_subscriptions.TryGetValue(key, out var sub))
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
            return "PING";
        }

        protected override async Task ResubscribeAsync(SubscriptionInfo subscription)
        {
            switch (subscription.Channel)
            {
                case "orderbook":
                    await SubscribeOrderbookAsync(subscription.Symbol);
                    break;
                case "trade":
                    await SubscribeTradesAsync(subscription.Symbol);
                    break;
                case "ticker":
                    await SubscribeTickerAsync(subscription.Symbol);
                    break;
            }
        }

        #region Helper Methods

        private string ConvertToUpbitCode(string symbol)
        {
            // Convert from "BTC/KRW" to "KRW-BTC"
            // Upbit uses Quote-Base format (reversed)
            var parts = symbol.Split('/');
            if (parts.Length == 2)
            {
                var baseCoin = parts[0];  // BTC
                var quoteCoin = parts[1];  // KRW
                return $"{quoteCoin}-{baseCoin}";
            }
            
            return symbol;
        }

        private string ConvertFromUpbitCode(string code)
        {
            // Convert from "KRW-BTC" to "BTC/KRW"
            var parts = code.Split('-');
            if (parts.Length == 2)
            {
                var quoteCoin = parts[0];  // KRW
                var baseCoin = parts[1];   // BTC
                return $"{baseCoin}/{quoteCoin}";
            }
            
            return code;
        }

        /// <summary>
        /// Formats a Market object to Upbit-specific symbol format
        /// </summary>
        /// <param name="market">Market to format</param>
        /// <returns>Formatted symbol (e.g., "KRW-BTC")</returns>
        protected override string FormatSymbol(Market market)
        {
            // Upbit uses Quote-Base format (reversed) with hyphen separator and uppercase
            return $"{market.Quote.ToUpper()}-{market.Base.ToUpper()}";
        }

        #endregion

        #region Candle Processing

        private async Task ProcessCandle(JsonElement json)
        {
            try
            {
                var code = json.GetStringOrDefault("code");
                var symbol = ConvertFromUpbitCode(code);
                
                var candle = new SCandle
                {
                    exchange = ExchangeName,
                    symbol = symbol,
                    interval = ConvertUpbitInterval(json.GetStringOrDefault("unit")),
                    timestamp = json.GetInt64OrDefault("timestamp"),
                    result = new List<SCandleItem>
                    {
                        new SCandleItem
                        {
                            openTime = json.GetUnixTimeOrDefault("candle_date_time_kst", json.GetInt64OrDefault("timestamp")),
                            closeTime = json.GetInt64OrDefault("timestamp"),
                            open = json.GetDecimalOrDefault("opening_price"),
                            high = json.GetDecimalOrDefault("high_price"),
                            low = json.GetDecimalOrDefault("low_price"),
                            close = json.GetDecimalOrDefault("trade_price"),
                            volume = json.GetDecimalOrDefault("candle_acc_trade_volume"),
                            quoteVolume = json.GetDecimalOrDefault("candle_acc_trade_price"),
                            tradeCount = json.GetInt64OrDefault("trade_count"),
                            isClosed = true // Upbit sends completed candles
                        }
                    }
                };

                InvokeCandleCallback(candle);
            }
            catch (Exception ex)
            {
                RaiseError($"Candle processing error: {ex.Message}");
            }
        }

        public override async Task<bool> SubscribeCandlesAsync(Market market, string interval)
        {
            try
            {
                var upbitCode = FormatSymbol(market);
                var upbitInterval = ConvertToUpbitInterval(interval);
                
                var subscription = new List<object>
                {
                    new { ticket = Guid.NewGuid().ToString() },
                    new { type = "candle", codes = new[] { upbitCode }, unit = upbitInterval }
                };

                await SendMessageAsync(JsonSerializer.Serialize(subscription));
                
                var key = CreateSubscriptionKey($"candle:{interval}", market.ToString());
                _subscriptions[key] = new SubscriptionInfo
                {
                    Channel = "candle",
                    Symbol = market.ToString(),
                    SubscribedAt = DateTime.UtcNow,
                    IsActive = true
                };

                return true;
            }
            catch (Exception ex)
            {
                RaiseError($"Subscribe candles error: {ex.Message}");
                return false;
            }
        }

        public override async Task<bool> SubscribeCandlesAsync(string symbol, string interval)
        {
            try
            {
                var upbitCode = ConvertToUpbitCode(symbol);
                var upbitInterval = ConvertToUpbitInterval(interval);
                
                var subscription = new List<object>
                {
                    new { ticket = Guid.NewGuid().ToString() },
                    new { type = "candle", codes = new[] { upbitCode }, unit = upbitInterval }
                };

                await SendMessageAsync(JsonSerializer.Serialize(subscription));
                
                var key = CreateSubscriptionKey($"candle:{interval}", symbol);
                _subscriptions[key] = new SubscriptionInfo
                {
                    Channel = "candle",
                    Symbol = symbol,
                    SubscribedAt = DateTime.UtcNow,
                    IsActive = true
                };

                return true;
            }
            catch (Exception ex)
            {
                RaiseError($"Subscribe candles error: {ex.Message}");
                return false;
            }
        }

        private string ConvertToUpbitInterval(string interval)
        {
            // Convert standard intervals to Upbit format
            return interval?.ToLower() switch
            {
                "1m" => "1",
                "3m" => "3",
                "5m" => "5",
                "10m" => "10",
                "15m" => "15",
                "30m" => "30",
                "60m" or "1h" => "60",
                "240m" or "4h" => "240",
                "1d" or "24h" => "D",
                "1w" or "7d" => "W",
                "1M" or "30d" => "M",
                _ => "60" // Default to 1 hour
            };
        }

        private string ConvertUpbitInterval(string unit)
        {
            // Convert Upbit interval format to standard
            return unit switch
            {
                "1" => "1m",
                "3" => "3m",
                "5" => "5m",
                "10" => "10m",
                "15" => "15m",
                "30" => "30m",
                "60" => "1h",
                "240" => "4h",
                "D" => "1d",
                "W" => "1w",
                "M" => "1M",
                _ => "1h"
            };
        }

        #endregion

        #region Private Channel Processing

        private async Task ProcessBalance(JsonElement json)
        {
            try
            {
                var balance = new SBalance
                {
                    exchange = ExchangeName,
                    accountId = json.GetStringOrDefault("account_id", "main"),
                    timestamp = TimeExtension.UnixTime,
                    balances = new List<SBalanceItem>()
                };

                // Parse balance data from Upbit format
                if (json.TryGetArray("balances", out var balances))
                {
                    foreach (var item in balances.EnumerateArray())
                    {
                        var currency = item.GetStringOrDefault("currency");
                        var free = item.GetDecimalOrDefault("balance");
                        var locked = item.GetDecimalOrDefault("locked");
                        
                        balance.balances.Add(new SBalanceItem
                        {
                            currency = currency,
                            free = free,
                            used = locked,
                            total = free + locked,
                            updateTime = TimeExtension.UnixTime
                        });
                    }
                }

                InvokeBalanceCallback(balance);
            }
            catch (Exception ex)
            {
                RaiseError($"Balance processing error: {ex.Message}");
            }
        }

        private async Task ProcessOrder(JsonElement json)
        {
            try
            {
                var order = new SOrderItem
                {
                    orderId = json.GetStringOrDefault("uuid"),
                    symbol = ConvertFromUpbitCode(json.GetStringOrDefault("market")),
                    type = ParseOrderType(json.GetStringOrDefault("ord_type")),
                    side = json.GetStringOrDefault("side") == "bid" ? OrderSide.Buy : OrderSide.Sell,
                    status = ParseOrderStatus(json.GetStringOrDefault("state")),
                    price = json.GetDecimalOrDefault("price"),
                    quantity = json.GetDecimalOrDefault("volume"),
                    filledQuantity = json.GetDecimalOrDefault("executed_volume"),
                    remainingQuantity = json.GetDecimalOrDefault("remaining_volume"),
                    avgFillPrice = json.GetDecimalOrDefault("avg_price"),
                    fee = json.GetDecimalOrDefault("paid_fee"),
                    feeCurrency = json.GetStringOrDefault("fee_currency"),
                    createTime = json.GetUnixTimeOrDefault("created_at"),
                    updateTime = json.GetUnixTimeOrDefault("updated_at")
                };

                order.cost = order.filledQuantity * order.avgFillPrice;

                var orders = new SOrder
                {
                    exchange = ExchangeName,
                    timestamp = order.updateTime,
                    orders = new List<SOrderItem> { order }
                };

                InvokeOrderCallback(orders);
            }
            catch (Exception ex)
            {
                RaiseError($"Order processing error: {ex.Message}");
            }
        }

        private async Task ProcessPosition(JsonElement json)
        {
            // Upbit doesn't support futures/margin trading, so no positions
            // This method is here for interface compatibility
            await Task.CompletedTask;
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

        private OrderStatus ParseOrderStatus(string state)
        {
            return state?.ToLower() switch
            {
                "wait" => OrderStatus.Open,
                "watch" => OrderStatus.Open,
                "done" => OrderStatus.Filled,
                "cancel" => OrderStatus.Canceled,
                "partial" => OrderStatus.PartiallyFilled,
                _ => OrderStatus.New
            };
        }

        #endregion

        #region Authentication

        protected override string CreateAuthenticationMessage(string apiKey, string secretKey)
        {
            // Upbit authentication for private channels
            // This is a simplified version - real implementation would use JWT
            var authData = new
            {
                type = "authentication",
                api_key = apiKey,
                timestamp = TimeExtension.UnixTime,
                // In production, add proper signature generation here
            };

            return JsonSerializer.Serialize(authData);
        }

        public override async Task<bool> SubscribeBalanceAsync()
        {
            if (!IsAuthenticated)
            {
                RaiseError("Not authenticated. Please connect with API credentials.");
                return false;
            }

            try
            {
                var subscription = new List<object>
                {
                    new { ticket = Guid.NewGuid().ToString() },
                    new { type = "balance" }
                };

                var socket = _privateWebSocket ?? _webSocket;
                await SendMessageAsync(JsonSerializer.Serialize(subscription), socket);
                
                _subscriptions["private:balance"] = new SubscriptionInfo
                {
                    Channel = "balance",
                    Symbol = "ALL",
                    SubscribedAt = DateTime.UtcNow,
                    IsActive = true
                };

                return true;
            }
            catch (Exception ex)
            {
                RaiseError($"Subscribe balance error: {ex.Message}");
                return false;
            }
        }

        public override async Task<bool> SubscribeOrdersAsync()
        {
            if (!IsAuthenticated)
            {
                RaiseError("Not authenticated. Please connect with API credentials.");
                return false;
            }

            try
            {
                var subscription = new List<object>
                {
                    new { ticket = Guid.NewGuid().ToString() },
                    new { type = "order" }
                };

                var socket = _privateWebSocket ?? _webSocket;
                await SendMessageAsync(JsonSerializer.Serialize(subscription), socket);
                
                _subscriptions["private:orders"] = new SubscriptionInfo
                {
                    Channel = "order",
                    Symbol = "ALL",
                    SubscribedAt = DateTime.UtcNow,
                    IsActive = true
                };

                return true;
            }
            catch (Exception ex)
            {
                RaiseError($"Subscribe orders error: {ex.Message}");
                return false;
            }
        }

        #endregion
    }
}
