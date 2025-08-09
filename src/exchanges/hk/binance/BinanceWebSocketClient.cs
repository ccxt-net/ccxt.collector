using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CCXT.Collector.Core.Abstractions;
using CCXT.Collector.Library;
using CCXT.Collector.Service;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace CCXT.Collector.Binance
{
        /*
         * Binance Support Markets: USDT, BUSD, BTC, ETH, BNB
         *
         * API Documentation:
         *     https://binance-docs.github.io/apidocs/
         *     https://github.com/binance/binance-spot-api-docs
         *
         * WebSocket API:
         *     https://binance-docs.github.io/apidocs/spot/en/#websocket-market-streams
         *     https://github.com/binance/binance-spot-api-docs/blob/master/web-socket-streams.md
         *
         * Fees:
         *     https://www.binance.com/en/fee/schedule
         */
    /// <summary>
    /// Binance WebSocket client for real-time data streaming
    /// </summary>
    public class BinanceWebSocketClient : WebSocketClientBase
    {
        private readonly Dictionary<string, long> _lastUpdateIds;
        private readonly Dictionary<string, SOrderBook> _orderbookCache;
        //private string _listenKey; // For user data stream

        public override string ExchangeName => "Binance";
        protected override string WebSocketUrl => "wss://stream.binance.com:9443/ws";
        protected override string PrivateWebSocketUrl => "wss://stream.binance.com:9443/ws";
        protected override int PingIntervalMs => 180000; // 3 minutes

        public BinanceWebSocketClient()
        {
            _lastUpdateIds = new Dictionary<string, long>();
            _orderbookCache = new Dictionary<string, SOrderBook>();
        }

        protected override async Task ProcessMessageAsync(string message, bool isPrivate = false)
        {
            try
            {
                var json = JObject.Parse(message);

                // Handle different message types
                if (json["e"] != null)
                {
                    var eventType = json["e"].ToString();
                    
                    if (isPrivate)
                    {
                        // Private/User data stream events
                        switch (eventType)
                        {
                            case "outboundAccountPosition":
                                await ProcessAccountUpdate(json);
                                break;
                            case "balanceUpdate":
                                await ProcessBalanceUpdate(json);
                                break;
                            case "executionReport":
                                await ProcessOrderUpdate(json);
                                break;
                            case "listStatus":
                                // OCO order status
                                break;
                        }
                    }
                    else
                    {
                        // Public stream events
                        switch (eventType)
                        {
                            case "depthUpdate":
                                await ProcessOrderbookUpdate(json);
                                break;
                            case "trade":
                                await ProcessTradeData(json);
                                break;
                            case "24hrTicker":
                                await ProcessTickerData(json);
                                break;
                            case "kline":
                                await ProcessKlineData(json);
                                break;
                            case "error":
                                RaiseError($"Binance error: {json["m"]}");
                                break;
                        }
                    }
                }
                else if (json["result"] == null && json["id"] != null)
                {
                    // Response to subscription request
                    // Binance doesn't send explicit subscription confirmations
                }
            }
            catch (Exception ex)
            {
                RaiseError($"Message processing error: {ex.Message}");
            }
        }

        private async Task ProcessOrderbookUpdate(JObject json)
        {
            try
            {
                var symbol = ConvertSymbol(json["s"].ToString());
                var updateId = json["u"].Value<long>();
                
                // Check if we should process this update
                if (_lastUpdateIds.ContainsKey(symbol))
                {
                    if (updateId <= _lastUpdateIds[symbol])
                        return;
                }
                
                _lastUpdateIds[symbol] = updateId;

                var orderbook = new SOrderBook
                {
                    exchange = ExchangeName,
                    symbol = symbol,
                    timestamp = json["E"].Value<long>(),
                    sequentialId = updateId,
                    result = new SOrderBookData
                    {
                        timestamp = json["E"].Value<long>(),
                        asks = new List<SOrderBookItem>(),
                        bids = new List<SOrderBookItem>()
                    }
                };

                // Process bids
                var bids = json["b"] as JArray;
                if (bids != null)
                {
                    foreach (var bid in bids)
                    {
                        var price = bid[0].Value<decimal>();
                        var quantity = bid[1].Value<decimal>();
                        
                        if (quantity > 0)
                        {
                            orderbook.result.bids.Add(new SOrderBookItem
                            {
                                price = price,
                                quantity = quantity,
                                amount = price * quantity,
                                action = "U"  // Update
                            });
                        }
                    }
                }

                // Process asks
                var asks = json["a"] as JArray;
                if (asks != null)
                {
                    foreach (var ask in asks)
                    {
                        var price = ask[0].Value<decimal>();
                        var quantity = ask[1].Value<decimal>();
                        
                        if (quantity > 0)
                        {
                            orderbook.result.asks.Add(new SOrderBookItem
                            {
                                price = price,
                                quantity = quantity,
                                amount = price * quantity,
                                action = "U"  // Update
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

        private async Task ProcessTradeData(JObject json)
        {
            try
            {
                var trade = new STrade
                {
                    exchange = ExchangeName,
                    symbol = ConvertSymbol(json["s"].ToString()),
                    timestamp = json["E"].Value<long>(),
                    result = new List<STradeItem>
                    {
                        new STradeItem
                        {
                            orderId = json["t"].ToString(),
                            timestamp = json["T"].Value<long>(),
                            sideType = json["m"].Value<bool>() ? SideType.Ask : SideType.Bid,
                            orderType = OrderType.Limit,
                            price = json["p"].Value<decimal>(),
                            quantity = json["q"].Value<decimal>(),
                            amount = json["p"].Value<decimal>() * json["q"].Value<decimal>()
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

        private async Task ProcessTickerData(JObject json)
        {
            try
            {
                var ticker = new STicker
                {
                    exchange = ExchangeName,
                    symbol = ConvertSymbol(json["s"].ToString()),
                    timestamp = json["E"].Value<long>(),
                    result = new STickerItem
                    {
                        timestamp = json["E"].Value<long>(),
                        openPrice = json["o"].Value<decimal>(),
                        highPrice = json["h"].Value<decimal>(),
                        lowPrice = json["l"].Value<decimal>(),
                        closePrice = json["c"].Value<decimal>(),
                        volume = json["v"].Value<decimal>(),
                        quoteVolume = json["q"].Value<decimal>(),
                        bidPrice = json["b"].Value<decimal>(),
                        bidQuantity = json["B"].Value<decimal>(),
                        askPrice = json["a"].Value<decimal>(),
                        askQuantity = json["A"].Value<decimal>(),
                        vwap = json["w"].Value<decimal>(),
                        count = json["C"].Value<long>(),
                        change = json["c"].Value<decimal>() - json["o"].Value<decimal>(),
                        percentage = json["o"].Value<decimal>() > 0 
                            ? ((json["c"].Value<decimal>() - json["o"].Value<decimal>()) / json["o"].Value<decimal>()) * 100 
                            : 0
                    }
                };

                InvokeTickerCallback(ticker);
            }
            catch (Exception ex)
            {
                RaiseError($"Ticker processing error: {ex.Message}");
            }
        }

        public override async Task<bool> SubscribeOrderbookAsync(string symbol)
        {
            try
            {
                var binanceSymbol = ConvertToBinanceSymbol(symbol);
                var streamName = $"{binanceSymbol.ToLower()}@depth@100ms";
                
                var subscription = new
                {
                    method = "SUBSCRIBE",
                    @params = new[] { streamName },
                    id = DateTime.UtcNow.Ticks
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
                RaiseError($"Subscribe orderbook error: {ex.Message}");
                return false;
            }
        }

        public override async Task<bool> SubscribeTradesAsync(string symbol)
        {
            try
            {
                var binanceSymbol = ConvertToBinanceSymbol(symbol);
                var streamName = $"{binanceSymbol.ToLower()}@trade";
                
                var subscription = new
                {
                    method = "SUBSCRIBE",
                    @params = new[] { streamName },
                    id = DateTime.UtcNow.Ticks
                };

                await SendMessageAsync(JsonConvert.SerializeObject(subscription));
                
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
                RaiseError($"Subscribe trades error: {ex.Message}");
                return false;
            }
        }

        public override async Task<bool> SubscribeTickerAsync(string symbol)
        {
            try
            {
                var binanceSymbol = ConvertToBinanceSymbol(symbol);
                var streamName = $"{binanceSymbol.ToLower()}@ticker";
                
                var subscription = new
                {
                    method = "SUBSCRIBE",
                    @params = new[] { streamName },
                    id = DateTime.UtcNow.Ticks
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
                RaiseError($"Subscribe ticker error: {ex.Message}");
                return false;
            }
        }

        public override async Task<bool> UnsubscribeAsync(string channel, string symbol)
        {
            try
            {
                var binanceSymbol = ConvertToBinanceSymbol(symbol);
                var streamName = channel switch
                {
                    "orderbook" => $"{binanceSymbol.ToLower()}@depth@100ms",
                    "trades" => $"{binanceSymbol.ToLower()}@trade",
                    "ticker" => $"{binanceSymbol.ToLower()}@ticker",
                    _ => throw new ArgumentException($"Unknown channel: {channel}")
                };

                var unsubscription = new
                {
                    method = "UNSUBSCRIBE",
                    @params = new[] { streamName },
                    id = DateTime.UtcNow.Ticks
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
            return JsonConvert.SerializeObject(new { ping = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() });
        }

        protected override async Task ResubscribeAsync(SubscriptionInfo subscription)
        {
            switch (subscription.Channel)
            {
                case "orderbook":
                    await SubscribeOrderbookAsync(subscription.Symbol);
                    break;
                case "trades":
                    await SubscribeTradesAsync(subscription.Symbol);
                    break;
                case "ticker":
                    await SubscribeTickerAsync(subscription.Symbol);
                    break;
            }
        }

        #region Helper Methods

        private string ConvertToBinanceSymbol(string symbol)
        {
            // Convert from "BTC/USDT" to "BTCUSDT"
            return symbol.Replace("/", "");
        }

        private string ConvertSymbol(string binanceSymbol)
        {
            // Convert from "BTCUSDT" to "BTC/USDT"
            // Common base currencies
            string[] quotes = { "USDT", "BUSD", "USDC", "BTC", "ETH", "BNB" };
            
            foreach (var quote in quotes)
            {
                if (binanceSymbol.EndsWith(quote))
                {
                    var baseSymbol = binanceSymbol.Substring(0, binanceSymbol.Length - quote.Length);
                    return $"{baseSymbol}/{quote}";
                }
            }
            
            return binanceSymbol;
        }

        #endregion

        #region Candlestick/K-Line Implementation

        public override async Task<bool> SubscribeCandlesAsync(string symbol, string interval)
        {
            try
            {
                var binanceSymbol = ConvertToBinanceSymbol(symbol);
                var binanceInterval = ConvertToBinanceInterval(interval);
                var streamName = $"{binanceSymbol.ToLower()}@kline_{binanceInterval}";
                
                var subscription = new
                {
                    method = "SUBSCRIBE",
                    @params = new[] { streamName },
                    id = DateTime.UtcNow.Ticks
                };

                await SendMessageAsync(JsonConvert.SerializeObject(subscription));
                
                var key = CreateSubscriptionKey($"kline:{interval}", symbol);
                _subscriptions[key] = new SubscriptionInfo
                {
                    Channel = "kline",
                    Symbol = symbol,
                    SubscribedAt = DateTime.UtcNow,
                    IsActive = true
                };

                return true;
            }
            catch (Exception ex)
            {
                RaiseError($"Subscribe klines error: {ex.Message}");
                return false;
            }
        }

        private async Task ProcessKlineData(JObject json)
        {
            try
            {
                var klineData = json["k"];
                var symbol = ConvertSymbol(json["s"].ToString());
                var timestamp = json["E"].Value<long>();
                
                var candle = new SCandle
                {
                    exchange = ExchangeName,
                    symbol = symbol,
                    interval = ConvertFromBinanceInterval(klineData["i"].ToString()),
                    timestamp = timestamp,
                    result = new List<SCandleItem>
                    {
                        new SCandleItem
                        {
                            openTime = klineData["t"].Value<long>(),
                            closeTime = klineData["T"].Value<long>(),
                            open = decimal.Parse(klineData["o"].ToString()),
                            high = decimal.Parse(klineData["h"].ToString()),
                            low = decimal.Parse(klineData["l"].ToString()),
                            close = decimal.Parse(klineData["c"].ToString()),
                            volume = decimal.Parse(klineData["v"].ToString()),
                            quoteVolume = decimal.Parse(klineData["q"].ToString()),
                            tradeCount = klineData["n"].Value<long>(),
                            isClosed = klineData["x"].Value<bool>(),
                            buyVolume = decimal.Parse(klineData["V"].ToString()),
                            buyQuoteVolume = decimal.Parse(klineData["Q"].ToString())
                        }
                    }
                };

                InvokeCandleCallback(candle);
            }
            catch (Exception ex)
            {
                RaiseError($"Kline processing error: {ex.Message}");
            }
        }

        private string ConvertToBinanceInterval(string interval)
        {
            return interval?.ToLower() switch
            {
                "1m" => "1m",
                "3m" => "3m",
                "5m" => "5m",
                "15m" => "15m",
                "30m" => "30m",
                "1h" or "60m" => "1h",
                "2h" => "2h",
                "4h" => "4h",
                "6h" => "6h",
                "8h" => "8h",
                "12h" => "12h",
                "1d" or "24h" => "1d",
                "3d" => "3d",
                "1w" or "7d" => "1w",
                "1M" or "30d" => "1M",
                _ => "1h"
            };
        }

        private string ConvertFromBinanceInterval(string interval)
        {
            return interval switch
            {
                "1m" => "1m",
                "3m" => "3m",
                "5m" => "5m",
                "15m" => "15m",
                "30m" => "30m",
                "1h" => "1h",
                "2h" => "2h",
                "4h" => "4h",
                "6h" => "6h",
                "8h" => "8h",
                "12h" => "12h",
                "1d" => "1d",
                "3d" => "3d",
                "1w" => "1w",
                "1M" => "1M",
                _ => interval
            };
        }

        #endregion

        #region Private Channel Implementation

        protected override bool RequiresSeparatePrivateConnection() => true;

        protected override string CreateAuthenticationMessage(string apiKey, string secretKey)
        {
            // Binance uses a REST API call to get a listenKey for user data stream
            // This is a placeholder - real implementation would call REST API
            // POST /api/v3/userDataStream to get listenKey
            //_listenKey = "placeholder_listen_key";
            return null; // Binance doesn't send auth message, uses listenKey in URL
        }

        public override async Task<bool> SubscribeBalanceAsync()
        {
            if (!IsAuthenticated)
            {
                RaiseError("Not authenticated. Please connect with API credentials.");
                return false;
            }

            // Binance user data stream automatically includes balance updates
            // when connected with listenKey
            return true;
        }

        public override async Task<bool> SubscribeOrdersAsync()
        {
            if (!IsAuthenticated)
            {
                RaiseError("Not authenticated. Please connect with API credentials.");
                return false;
            }

            // Binance user data stream automatically includes order updates
            // when connected with listenKey
            return true;
        }

        private async Task ProcessAccountUpdate(JObject json)
        {
            try
            {
                var balances = json["B"] as JArray;
                if (balances != null)
                {
                    var balanceItems = new List<SBalanceItem>();
                    var timestamp = json["E"].Value<long>();
                    
                    foreach (var item in balances)
                    {
                        var free = decimal.Parse(item["f"].ToString());
                        var locked = decimal.Parse(item["l"].ToString());
                        
                        if (free > 0 || locked > 0)
                        {
                            balanceItems.Add(new SBalanceItem
                            {
                                currency = item["a"].ToString(),
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
                            accountId = "spot",
                            timestamp = timestamp,
                            balances = balanceItems
                        };
                        
                        InvokeBalanceCallback(balance);
                    }
                }
            }
            catch (Exception ex)
            {
                RaiseError($"Account update processing error: {ex.Message}");
            }
        }

        private async Task ProcessBalanceUpdate(JObject json)
        {
            try
            {
                // Single balance update
                var balance = new SBalance
                {
                    exchange = ExchangeName,
                    accountId = "spot",
                    timestamp = json["E"].Value<long>(),
                    balances = new List<SBalanceItem>
                    {
                        new SBalanceItem
                        {
                            currency = json["a"].ToString(),
                            free = decimal.Parse(json["f"].ToString()),
                            used = decimal.Parse(json["l"].ToString()),
                            total = decimal.Parse(json["f"].ToString()) + decimal.Parse(json["l"].ToString()),
                            updateTime = json["E"].Value<long>()
                        }
                    }
                };

                InvokeBalanceCallback(balance);
            }
            catch (Exception ex)
            {
                RaiseError($"Balance update processing error: {ex.Message}");
            }
        }

        private async Task ProcessOrderUpdate(JObject json)
        {
            try
            {
                var order = new SOrderItem
                {
                    orderId = json["i"].ToString(),
                    clientOrderId = json["c"].ToString(),
                    symbol = ConvertSymbol(json["s"].ToString()),
                    type = ParseOrderType(json["o"].ToString()),
                    side = json["S"].ToString() == "BUY" ? OrderSide.Buy : OrderSide.Sell,
                    status = ParseOrderStatus(json["X"].ToString()),
                    price = decimal.Parse(json["p"].ToString()),
                    quantity = decimal.Parse(json["q"].ToString()),
                    filledQuantity = decimal.Parse(json["z"].ToString()),
                    createTime = json["O"].Value<long>(),
                    updateTime = json["E"].Value<long>()
                };

                var orders = new SOrder
                {
                    exchange = ExchangeName,
                    timestamp = json["E"].Value<long>(),
                    orders = new List<SOrderItem> { order }
                };

                InvokeOrderCallback(orders);
            }
            catch (Exception ex)
            {
                RaiseError($"Order update processing error: {ex.Message}");
            }
        }

        private OrderType ParseOrderType(string type)
        {
            return type switch
            {
                "LIMIT" => OrderType.Limit,
                "MARKET" => OrderType.Market,
                "STOP" => OrderType.Stop,
                "STOP_LOSS" => OrderType.Stop,
                "STOP_LOSS_LIMIT" => OrderType.StopLimit,
                "TAKE_PROFIT" => OrderType.TakeProfit,
                "TAKE_PROFIT_LIMIT" => OrderType.TakeProfitLimit,
                _ => OrderType.Limit
            };
        }

        private OrderStatus ParseOrderStatus(string status)
        {
            return status switch
            {
                "NEW" => OrderStatus.New,
                "PARTIALLY_FILLED" => OrderStatus.PartiallyFilled,
                "FILLED" => OrderStatus.Filled,
                "CANCELED" => OrderStatus.Canceled,
                "REJECTED" => OrderStatus.Rejected,
                "EXPIRED" => OrderStatus.Expired,
                _ => OrderStatus.Open
            };
        }

        #endregion
    }
}
