using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CCXT.Collector.Core.Abstractions;
using CCXT.Collector.Library;
using CCXT.Collector.Service;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace CCXT.Collector.Gateio
{
    /*
     * Gate.io Support Markets: USDT, BTC, ETH, GT
     *
     * API Documentation:
     *     https://www.gate.io/docs/developers/apiv4/en/
     *
     * WebSocket API:
     *     https://www.gate.io/docs/developers/apiv4/ws/en/
     *
     * Fees:
     *     https://www.gate.io/fee
     */
    /// <summary>
    /// Gate.io WebSocket client for real-time data streaming
    /// </summary>
    public class GateioWebSocketClient : WebSocketClientBase
    {
        private readonly Dictionary<string, SOrderBook> _orderbookCache;
        private readonly Dictionary<string, long> _lastUpdateIds;

        public override string ExchangeName => "Gate.io";
        protected override string WebSocketUrl => "wss://api.gateio.ws/ws/v4/";
        protected override int PingIntervalMs => 30000; // 30 seconds

        public GateioWebSocketClient()
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
                if (json["channel"]?.ToString() == "spot.ping")
                {
                    await HandlePingMessage();
                    return;
                }

                // Handle subscription response
                if (json["event"]?.ToString() == "subscribe")
                {
                    var result = json["result"]?["status"]?.ToString();
                    if (result != "success")
                    {
                        RaiseError($"Subscription failed: {json["error"]?["message"]}");
                    }
                    return;
                }

                // Handle data messages
                var channel = json["channel"]?.ToString();
                if (channel != null && json["event"]?.ToString() == "update")
                {
                    var result = json["result"];
                    if (result == null)
                        return;

                    if (channel.StartsWith("spot.order_book"))
                    {
                        await ProcessOrderbookData(json);
                    }
                    else if (channel.StartsWith("spot.trades"))
                    {
                        await ProcessTradeData(json);
                    }
                    else if (channel.StartsWith("spot.tickers"))
                    {
                        await ProcessTickerData(json);
                    }
                    else if (channel.StartsWith("spot.candlesticks"))
                    {
                        await ProcessCandleData(json);
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
                time = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                channel = "spot.pong"
            };
            await SendMessageAsync(JsonConvert.SerializeObject(pong));
        }

        private async Task ProcessOrderbookData(JObject json)
        {
            try
            {
                var result = json["result"];
                var symbol = result["s"]?.ToString(); // Currency pair like "BTC_USDT"
                var timestamp = result["t"]?.Value<long>() ?? DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                var updateId = result["u"]?.Value<long>() ?? 0;
                var isSnapshot = result["U"]?.Value<long>() == updateId; // First update ID equals current for snapshot

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
                var asks = result["asks"] as JArray;
                if (asks != null)
                {
                    foreach (var ask in asks)
                    {
                        var askArray = ask as JArray;
                        if (askArray != null && askArray.Count >= 2)
                        {
                            orderbook.result.asks.Add(new SOrderBookItem
                            {
                                price = askArray[0].Value<decimal>(),
                                quantity = askArray[1].Value<decimal>()
                            });
                        }
                    }
                }

                // Process bids
                var bids = result["bids"] as JArray;
                if (bids != null)
                {
                    foreach (var bid in bids)
                    {
                        var bidArray = bid as JArray;
                        if (bidArray != null && bidArray.Count >= 2)
                        {
                            orderbook.result.bids.Add(new SOrderBookItem
                            {
                                price = bidArray[0].Value<decimal>(),
                                quantity = bidArray[1].Value<decimal>()
                            });
                        }
                    }
                }

                // Sort orderbook
                orderbook.result.asks = orderbook.result.asks.OrderBy(a => a.price).ToList();
                orderbook.result.bids = orderbook.result.bids.OrderByDescending(b => b.price).ToList();

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
                var result = json["result"];
                var symbol = result["currency_pair"]?.ToString();
                var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

                var trades = new List<STradeItem>();
                
                // Gate.io sends array of trades
                if (result is JArray tradeArray)
                {
                    foreach (var trade in tradeArray)
                    {
                        trades.Add(new STradeItem
                        {
                            timestamp = trade["create_time"]?.Value<long>() ?? timestamp,
                            sideType = trade["side"]?.ToString() == "buy" ? SideType.Bid : SideType.Ask,
                            orderType = OrderType.Limit,
                            price = trade["price"]?.Value<decimal>() ?? 0,
                            quantity = trade["amount"]?.Value<decimal>() ?? 0,
                            amount = (trade["price"]?.Value<decimal>() ?? 0) * (trade["amount"]?.Value<decimal>() ?? 0)
                        });
                    }
                }
                else
                {
                    // Single trade
                    trades.Add(new STradeItem
                    {
                        timestamp = result["create_time"]?.Value<long>() ?? timestamp,
                        sideType = result["side"]?.ToString() == "buy" ? SideType.Bid : SideType.Ask,
                        orderType = OrderType.Limit,
                        price = result["price"]?.Value<decimal>() ?? 0,
                        quantity = result["amount"]?.Value<decimal>() ?? 0,
                        amount = (result["price"]?.Value<decimal>() ?? 0) * (result["amount"]?.Value<decimal>() ?? 0)
                    });
                }

                var tradeData = new STrade
                {
                    exchange = ExchangeName,
                    symbol = ConvertToStandardSymbol(symbol),
                    timestamp = timestamp,
                    result = trades
                };

                InvokeTradeCallback(tradeData);
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
                var result = json["result"];
                var symbol = result["currency_pair"]?.ToString();
                var timestamp = result["timestamp"]?.Value<long>() ?? DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

                var ticker = new STicker
                {
                    exchange = ExchangeName,
                    symbol = ConvertToStandardSymbol(symbol),
                    timestamp = timestamp,
                    result = new STickerItem
                    {
                        timestamp = timestamp,
                        closePrice = result["last"]?.Value<decimal>() ?? 0,
                        openPrice = result["open_24h"]?.Value<decimal>() ?? 0,
                        highPrice = result["high_24h"]?.Value<decimal>() ?? 0,
                        lowPrice = result["low_24h"]?.Value<decimal>() ?? 0,
                        volume = result["base_volume"]?.Value<decimal>() ?? 0,
                        quoteVolume = result["quote_volume"]?.Value<decimal>() ?? 0
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
                var result = json["result"];
                var channel = json["channel"]?.ToString();
                var parts = channel.Split('_');
                var interval = parts.Length > 2 ? parts[2] : "1m";
                var symbol = result[0]?["c"]?.ToString(); // Currency pair
                
                var candles = new List<SCandleItem>();
                
                if (result is JArray candleArray)
                {
                    foreach (var candle in candleArray)
                    {
                        if (candle is JArray c && c.Count >= 6)
                        {
                            candles.Add(new SCandleItem
                            {
                                openTime = c[0].Value<long>() * 1000, // Convert to milliseconds
                                closeTime = c[0].Value<long>() * 1000 + 60000, // Assuming 1m interval
                                open = c[5].Value<decimal>(),
                                high = c[3].Value<decimal>(),
                                low = c[4].Value<decimal>(),
                                close = c[2].Value<decimal>(),
                                volume = c[1].Value<decimal>()
                            });
                        }
                    }
                }

                var candleData = new SCandle
                {
                    exchange = ExchangeName,
                    symbol = ConvertToStandardSymbol(symbol),
                    interval = ConvertInterval(interval),
                    timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                    result = candles
                };

                InvokeCandleCallback(candleData);
            }
            catch (Exception ex)
            {
                RaiseError($"Candle processing error: {ex.Message}");
            }
        }

        public override async Task<bool> SubscribeOrderbookAsync(string symbol)
        {
            try
            {
                var gateSymbol = ConvertToExchangeSymbol(symbol);
                var request = new
                {
                    time = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                    channel = "spot.order_book",
                    @event = "subscribe",
                    payload = new[] { gateSymbol, "100", "100ms" } // symbol, depth level, update frequency
                };
                
                await SendMessageAsync(JsonConvert.SerializeObject(request));
                
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
                var gateSymbol = ConvertToExchangeSymbol(symbol);
                var request = new
                {
                    time = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                    channel = "spot.trades",
                    @event = "subscribe",
                    payload = new[] { gateSymbol }
                };
                
                await SendMessageAsync(JsonConvert.SerializeObject(request));
                
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
                var gateSymbol = ConvertToExchangeSymbol(symbol);
                var request = new
                {
                    time = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                    channel = "spot.tickers",
                    @event = "subscribe",
                    payload = new[] { gateSymbol }
                };
                
                await SendMessageAsync(JsonConvert.SerializeObject(request));
                
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

        public override async Task<bool> SubscribeCandlesAsync(string symbol, string interval = "1m")
        {
            try
            {
                var gateSymbol = ConvertToExchangeSymbol(symbol);
                var gateInterval = ConvertToExchangeInterval(interval);
                var request = new
                {
                    time = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                    channel = "spot.candlesticks",
                    @event = "subscribe",
                    payload = new[] { gateInterval, gateSymbol }
                };
                
                await SendMessageAsync(JsonConvert.SerializeObject(request));
                
                var key = CreateSubscriptionKey($"candles:{interval}", symbol);
                _subscriptions[key] = new SubscriptionInfo
                {
                    Channel = "candles",
                    Symbol = symbol,
                    SubscribedAt = DateTime.UtcNow,
                    IsActive = true,
                    Extra = interval
                };

                return true;
            }
            catch (Exception ex)
            {
                RaiseError($"Subscribe candles error: {ex.Message}");
                return false;
            }
        }

        public override async Task<bool> UnsubscribeAsync(string channel, string symbol)
        {
            try
            {
                var gateSymbol = ConvertToExchangeSymbol(symbol);
                var gateChannel = channel switch
                {
                    "orderbook" => "spot.order_book",
                    "trades" => "spot.trades",
                    "ticker" => "spot.tickers",
                    "candles" => "spot.candlesticks",
                    _ => channel
                };

                var unsubscription = new
                {
                    time = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                    channel = gateChannel,
                    @event = "unsubscribe",
                    payload = new[] { gateSymbol }
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
            return JsonConvert.SerializeObject(new 
            { 
                time = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                channel = "spot.ping"
            });
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
                case "candles":
                    await SubscribeCandlesAsync(subscription.Symbol, subscription.Extra ?? "1m");
                    break;
            }
        }

        #region Helper Methods

        private string ConvertToExchangeSymbol(string symbol)
        {
            // Convert BTC/USDT to BTC_USDT
            return symbol.Replace("/", "_");
        }

        private string ConvertToStandardSymbol(string exchangeSymbol)
        {
            // Convert BTC_USDT to BTC/USDT
            return exchangeSymbol?.Replace("_", "/");
        }

        private string ConvertToExchangeInterval(string interval)
        {
            // Convert standard interval to Gate.io format
            return interval switch
            {
                "1m" => "1m",
                "5m" => "5m",
                "15m" => "15m",
                "30m" => "30m",
                "1h" => "1h",
                "4h" => "4h",
                "8h" => "8h",
                "1d" => "1d",
                "1w" => "1w",
                _ => "1m"
            };
        }

        private string ConvertInterval(string gateInterval)
        {
            // Gate.io uses same format as standard
            return gateInterval;
        }

        #endregion
    }
}