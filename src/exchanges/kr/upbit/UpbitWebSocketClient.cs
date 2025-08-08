using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CCXT.Collector.Library;
using CCXT.Collector.Service;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

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
        private readonly Dictionary<string, SOrderBooks> _orderbookCache;
        private readonly Dictionary<string, string> _marketCodeMap;

        public override string ExchangeName => "Upbit";
        protected override string WebSocketUrl => "wss://api.upbit.com/websocket/v1";
        protected override int PingIntervalMs => 120000; // 2 minutes

        public UpbitWebSocketClient()
        {
            _orderbookCache = new Dictionary<string, SOrderBooks>();
            _marketCodeMap = new Dictionary<string, string>();
            InitializeMarketCodes();
        }

        private void InitializeMarketCodes()
        {
            // Common market code mappings
            _marketCodeMap["BTC/KRW"] = "KRW-BTC";
            _marketCodeMap["ETH/KRW"] = "KRW-ETH";
            _marketCodeMap["XRP/KRW"] = "KRW-XRP";
            _marketCodeMap["ADA/KRW"] = "KRW-ADA";
            _marketCodeMap["SOL/KRW"] = "KRW-SOL";
            _marketCodeMap["DOGE/KRW"] = "KRW-DOGE";
            _marketCodeMap["BTC/USDT"] = "USDT-BTC";
            _marketCodeMap["ETH/USDT"] = "USDT-ETH";
        }

        protected override async Task ProcessMessageAsync(string message, bool isPrivate = false)
        {
            try
            {
                // Upbit sends data in binary format first, then JSON
                var json = JObject.Parse(message);
                
                if (json["type"] != null)
                {
                    var messageType = json["type"].ToString();
                    
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
                                OnError?.Invoke($"Upbit private error: {json["message"]}");
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
                                await ProcessTicker(json);
                                break;
                            case "candle":
                                await ProcessCandle(json);
                                break;
                            case "error":
                                OnError?.Invoke($"Upbit error: {json["message"]}");
                                break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                OnError?.Invoke($"Message processing error: {ex.Message}");
            }
        }

        private async Task ProcessOrderbook(JObject json)
        {
            try
            {
                var code = json["code"].ToString();
                var symbol = ConvertFromUpbitCode(code);
                
                var orderbook = new SOrderBooks
                {
                    exchange = ExchangeName,
                    symbol = symbol,
                    timestamp = json["timestamp"].Value<long>(),
                    sequentialId = json["total_ask_size"]?.ToString() ?? "0",
                    result = new SOrderBook
                    {
                        timestamp = json["timestamp"].Value<long>(),
                        asks = new List<SOrderBookItem>(),
                        bids = new List<SOrderBookItem>()
                    }
                };

                // Process orderbook units
                var units = json["orderbook_units"] as JArray;
                if (units != null)
                {
                    foreach (var unit in units)
                    {
                        // Add ask
                        var askPrice = unit["ask_price"].Value<decimal>();
                        var askSize = unit["ask_size"].Value<decimal>();
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
                        var bidPrice = unit["bid_price"].Value<decimal>();
                        var bidSize = unit["bid_size"].Value<decimal>();
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
                OnError?.Invoke($"Orderbook processing error: {ex.Message}");
            }
        }

        private async Task ProcessTrade(JObject json)
        {
            try
            {
                var code = json["code"].ToString();
                var symbol = ConvertFromUpbitCode(code);
                
                var trade = new SCompleteOrders
                {
                    exchange = ExchangeName,
                    symbol = symbol,
                    timestamp = json["timestamp"].Value<long>(),
                    result = new SCompleteOrder
                    {
                        orderId = json["sequential_id"]?.ToString() ?? Guid.NewGuid().ToString(),
                        timestamp = json["timestamp"].Value<long>(),
                        sideType = json["ask_bid"].ToString() == "ASK" ? SideType.Ask : SideType.Bid,
                        orderType = OrderType.Limit,
                        price = json["trade_price"].Value<decimal>(),
                        quantity = json["trade_volume"].Value<decimal>(),
                        amount = json["trade_price"].Value<decimal>() * json["trade_volume"].Value<decimal>()
                    }
                };

                InvokeTradeCallback(trade);
            }
            catch (Exception ex)
            {
                OnError?.Invoke($"Trade processing error: {ex.Message}");
            }
        }

        private async Task ProcessTicker(JObject json)
        {
            try
            {
                var code = json["code"].ToString();
                var symbol = ConvertFromUpbitCode(code);
                
                var ticker = new STicker
                {
                    exchange = ExchangeName,
                    symbol = symbol,
                    timestamp = json["timestamp"].Value<long>(),
                    result = new STickerItem
                    {
                        timestamp = json["timestamp"].Value<long>(),
                        openPrice = json["opening_price"].Value<decimal>(),
                        highPrice = json["high_price"].Value<decimal>(),
                        lowPrice = json["low_price"].Value<decimal>(),
                        closePrice = json["trade_price"].Value<decimal>(),
                        volume = json["acc_trade_volume_24h"].Value<decimal>(),
                        quoteVolume = json["acc_trade_price_24h"].Value<decimal>(),
                        prevClosePrice = json["prev_closing_price"].Value<decimal>(),
                        change = json["signed_change_price"].Value<decimal>(),
                        percentage = json["signed_change_rate"].Value<decimal>() * 100
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
                OnError?.Invoke($"Ticker processing error: {ex.Message}");
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
                OnError?.Invoke($"Subscribe orderbook error: {ex.Message}");
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

                await SendMessageAsync(JsonConvert.SerializeObject(subscription));
                
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
                OnError?.Invoke($"Subscribe trades error: {ex.Message}");
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
                OnError?.Invoke($"Subscribe ticker error: {ex.Message}");
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
                OnError?.Invoke($"Unsubscribe error: {ex.Message}");
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
            // Try to find in map first
            if (_marketCodeMap.ContainsKey(symbol))
                return _marketCodeMap[symbol];

            // Convert from "BTC/KRW" to "KRW-BTC"
            var parts = symbol.Split('/');
            if (parts.Length == 2)
            {
                var quote = parts[1];
                var baseSymbol = parts[0];
                return $"{quote}-{baseSymbol}";
            }
            
            return symbol;
        }

        private string ConvertFromUpbitCode(string code)
        {
            // Find in reverse map
            var entry = _marketCodeMap.FirstOrDefault(x => x.Value == code);
            if (entry.Key != null)
                return entry.Key;

            // Convert from "KRW-BTC" to "BTC/KRW"
            var parts = code.Split('-');
            if (parts.Length == 2)
            {
                var quote = parts[0];
                var baseSymbol = parts[1];
                return $"{baseSymbol}/{quote}";
            }
            
            return code;
        }

        #endregion

        #region Candle Processing

        private async Task ProcessCandle(JObject json)
        {
            try
            {
                var code = json["code"].ToString();
                var symbol = ConvertFromUpbitCode(code);
                
                var candle = new SCandlestick
                {
                    exchange = ExchangeName,
                    symbol = symbol,
                    interval = ConvertUpbitInterval(json["unit"]?.ToString()),
                    timestamp = json["timestamp"].Value<long>(),
                    result = new SCandleItem
                    {
                        openTime = json["candle_date_time_kst"] != null ? 
                            DateTimeOffset.Parse(json["candle_date_time_kst"].ToString()).ToUnixTimeMilliseconds() : 
                            json["timestamp"].Value<long>(),
                        closeTime = json["timestamp"].Value<long>(),
                        open = json["opening_price"].Value<decimal>(),
                        high = json["high_price"].Value<decimal>(),
                        low = json["low_price"].Value<decimal>(),
                        close = json["trade_price"].Value<decimal>(),
                        volume = json["candle_acc_trade_volume"].Value<decimal>(),
                        quoteVolume = json["candle_acc_trade_price"].Value<decimal>(),
                        tradeCount = json["trade_count"]?.Value<long>() ?? 0,
                        isClosed = true // Upbit sends completed candles
                    }
                };

                InvokeCandleCallback(candle);
            }
            catch (Exception ex)
            {
                OnError?.Invoke($"Candle processing error: {ex.Message}");
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

                await SendMessageAsync(JsonConvert.SerializeObject(subscription));
                
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
                OnError?.Invoke($"Subscribe candles error: {ex.Message}");
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

        private async Task ProcessBalance(JObject json)
        {
            try
            {
                var balance = new SBalance
                {
                    exchange = ExchangeName,
                    accountId = json["account_id"]?.ToString() ?? "main",
                    timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                    balances = new List<SBalanceItem>()
                };

                // Parse balance data from Upbit format
                if (json["balances"] is JArray balances)
                {
                    foreach (var item in balances)
                    {
                        var currency = item["currency"].ToString();
                        var free = item["balance"].Value<decimal>();
                        var locked = item["locked"].Value<decimal>();
                        
                        balance.balances.Add(new SBalanceItem
                        {
                            currency = currency,
                            free = free,
                            used = locked,
                            total = free + locked,
                            updateTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                        });
                    }
                }

                InvokeBalanceCallback(balance);
            }
            catch (Exception ex)
            {
                OnError?.Invoke($"Balance processing error: {ex.Message}");
            }
        }

        private async Task ProcessOrder(JObject json)
        {
            try
            {
                var order = new SOrder
                {
                    exchange = ExchangeName,
                    orderId = json["uuid"].ToString(),
                    symbol = ConvertFromUpbitCode(json["market"].ToString()),
                    type = ParseOrderType(json["ord_type"]?.ToString()),
                    side = json["side"].ToString() == "bid" ? OrderSide.Buy : OrderSide.Sell,
                    status = ParseOrderStatus(json["state"]?.ToString()),
                    price = json["price"]?.Value<decimal>() ?? 0,
                    quantity = json["volume"]?.Value<decimal>() ?? 0,
                    filledQuantity = json["executed_volume"]?.Value<decimal>() ?? 0,
                    remainingQuantity = json["remaining_volume"]?.Value<decimal>() ?? 0,
                    avgFillPrice = json["avg_price"]?.Value<decimal>() ?? 0,
                    fee = json["paid_fee"]?.Value<decimal>() ?? 0,
                    feeCurrency = json["fee_currency"]?.ToString(),
                    createTime = json["created_at"] != null ? 
                        DateTimeOffset.Parse(json["created_at"].ToString()).ToUnixTimeMilliseconds() : 0,
                    updateTime = json["updated_at"] != null ? 
                        DateTimeOffset.Parse(json["updated_at"].ToString()).ToUnixTimeMilliseconds() : 0
                };

                order.cost = order.filledQuantity * order.avgFillPrice;

                InvokeOrderCallback(order);
            }
            catch (Exception ex)
            {
                OnError?.Invoke($"Order processing error: {ex.Message}");
            }
        }

        private async Task ProcessPosition(JObject json)
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
                timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                // In production, add proper signature generation here
            };

            return JsonConvert.SerializeObject(authData);
        }

        public override async Task<bool> SubscribeBalanceAsync()
        {
            if (!IsAuthenticated)
            {
                OnError?.Invoke("Not authenticated. Please connect with API credentials.");
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
                await SendMessageAsync(JsonConvert.SerializeObject(subscription), socket);
                
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
                OnError?.Invoke($"Subscribe balance error: {ex.Message}");
                return false;
            }
        }

        public override async Task<bool> SubscribeOrdersAsync()
        {
            if (!IsAuthenticated)
            {
                OnError?.Invoke("Not authenticated. Please connect with API credentials.");
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
                await SendMessageAsync(JsonConvert.SerializeObject(subscription), socket);
                
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
                OnError?.Invoke($"Subscribe orders error: {ex.Message}");
                return false;
            }
        }

        #endregion
    }
}
