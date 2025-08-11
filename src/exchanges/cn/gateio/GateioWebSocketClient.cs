using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CCXT.Collector.Core.Abstractions;
using CCXT.Collector.Library;
using CCXT.Collector.Service;
using System.Text.Json;

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
                using var doc = JsonDocument.Parse(message); 
                var json = doc.RootElement;
                
                // Handle ping/pong
                if (json.GetStringOrDefault("channel") == "spot.ping")
                {
                    await HandlePingMessage();
                    return;
                }

                // Handle subscription response
                if (json.GetStringOrDefault("event") == "subscribe")
                {
                    var status = json.GetNestedString("result", "status");
                    if (!string.Equals(status, "success", StringComparison.OrdinalIgnoreCase))
                    {
                        var errMsg = json.GetNestedString("error", "message");
                        RaiseError($"Subscription failed: {errMsg}");
                    }
                    return;
                }

                // Handle data messages
                var channel = json.GetStringOrNull("channel");
                if (channel != null && json.GetStringOrDefault("event") == "update")
                {
                    if (json.TryGetProperty("result", out var result))
                    {
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
            await SendMessageAsync(JsonSerializer.Serialize(pong));
        }

        private async Task ProcessOrderbookData(JsonElement root)
        {
            try
            {
                if (!root.TryGetProperty("result", out var result))
                    return;
                    
                var symbol = result.GetStringOrDefault("s"); // Currency pair like "BTC_USDT)"
                var timestamp = result.GetInt64OrDefault("t", TimeExtension.UnixTime);
                var updateId = result.GetInt64OrDefault("u", 0L);
                var firstUpdateId = result.GetInt64OrDefault("U", 0L);
                var isSnapshot = firstUpdateId == updateId; // First update ID equals current for snapshot

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
                if (result.TryGetArray("asks", out var asks))
                {
                    foreach (var ask in asks.EnumerateArray())
                    {
                        if (ask.GetArrayLength() < 2)
                            continue;

                        var price = ask[0].GetDecimalValue();
                        var quantity = ask[1].GetDecimalValue();

                        if (quantity > 0)
                        {
                            orderbook.result.asks.Add(new SOrderBookItem
                            {
                                price = price,
                                quantity = quantity
                            });
                        }
                    }
                }

                // Process bids
                if (result.TryGetArray("bids", out var bidsArray))
                {
                    foreach (var bid in bidsArray.EnumerateArray())
                    {
                        if (bid.GetArrayLength() < 2)
                            continue;

                        var price = bid[0].GetDecimalValue();
                        var quantity = bid[1].GetDecimalValue();

                        if (quantity > 0)
                        {
                            orderbook.result.bids.Add(new SOrderBookItem
                            {
                                price = price,
                                quantity = quantity
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

        private async Task ProcessTradeData(JsonElement root)
        {
            try
            {
                if (!root.TryGetProperty("result", out var result))
                    return;

                var symbol = result.GetStringOrNull("currency_pair");
                var timestamp = TimeExtension.UnixTime;

                var trades = new List<STradeItem>();
                
                // Gate.io sends array of trades
                if (result.ValueKind == JsonValueKind.Array)
                {
                    foreach (var trade in result.EnumerateArray())
                    {
                        var createTime = trade.GetInt64OrDefault("create_time", timestamp);
                        var side = trade.GetStringOrDefault("side", "");
                        var price = trade.GetDecimalOrDefault("price");
                        var amount = trade.GetDecimalOrDefault("amount");
                        
                        trades.Add(new STradeItem
                        {
                            timestamp = createTime,
                            sideType = side == "buy" ? SideType.Bid : SideType.Ask,
                            orderType = OrderType.Limit,
                            price = price,
                            quantity = amount,
                            amount = price * amount
                        });
                    }
                }
                else
                {
                    // Single trade
                    var createTime = result.GetInt64OrDefault("create_time", timestamp);
                    var side = result.GetStringOrDefault("side", "");
                    var price = result.GetDecimalOrDefault("price");
                    var amount = result.GetDecimalOrDefault("amount");
                    
                    trades.Add(new STradeItem
                    {
                        timestamp = createTime,
                        sideType = side == "buy" ? SideType.Bid : SideType.Ask,
                        orderType = OrderType.Limit,
                        price = price,
                        quantity = amount,
                        amount = price * amount
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

        private async Task ProcessTickerData(JsonElement root)
        {
            try
            {
                if (!root.TryGetProperty("result", out var result))
                    return;

                var symbol = result.GetStringOrNull("currency_pair");
                var timestamp = result.GetInt64OrDefault("timestamp", TimeExtension.UnixTime);

                var ticker = new STicker
                {
                    exchange = ExchangeName,
                    symbol = ConvertToStandardSymbol(symbol),
                    timestamp = timestamp,
                    result = new STickerItem
                    {
                        timestamp = timestamp,
                        closePrice = result.GetDecimalOrDefault("last"),
                        openPrice = result.GetDecimalOrDefault("open_24h"),
                        highPrice = result.GetDecimalOrDefault("high_24h"),
                        lowPrice = result.GetDecimalOrDefault("low_24h"),
                        volume = result.GetDecimalOrDefault("base_volume"),
                        quoteVolume = result.GetDecimalOrDefault("quote_volume")
                    }
                };

                InvokeTickerCallback(ticker);
            }
            catch (Exception ex)
            {
                RaiseError($"Ticker processing error: {ex.Message}");
            }
        }

        private async Task ProcessCandleData(JsonElement root)
        {
            try
            {
                if (!root.TryGetProperty("result", out var result))
                    return;

                var channel = root.GetStringOrDefault("channel", "");
                var parts = channel.Split('_');
                var interval = parts.Length > 2 ? parts[2] : "1m";
                var symbol = "";
                
                var candles = new List<SCandleItem>();

                if (result.ValueKind == JsonValueKind.Array)
                {
                    foreach (var candle in result.EnumerateArray())
                    {
                        if (candle.GetArrayLength() < 6)
                            continue;

                        var openTime = candle[0].GetInt64Value();
                        var volume = candle[1].GetDecimalValue();
                        var close = candle[2].GetDecimalValue();
                        var high = candle[3].GetDecimalValue();
                        var low = candle[4].GetDecimalValue();
                        var open = candle[5].GetDecimalValue();

                        candles.Add(new SCandleItem
                        {
                            openTime = openTime * 1000, // Convert to milliseconds
                            closeTime = openTime * 1000 + 60000, // Assuming 1m interval
                            open = open,
                            high = high,
                            low = low,
                            close = close,
                            volume = volume
                        });

                        // Extract symbol from first candle if available
                        if (String.IsNullOrEmpty(symbol) && candle.GetArrayLength() > 6)
                        {
                            symbol = candle[6].ValueKind == JsonValueKind.String ? candle[6].GetString() : "";
                        }
                    }
                }

                var candleData = new SCandle
                {
                    exchange = ExchangeName,
                    symbol = ConvertToStandardSymbol(symbol),
                    interval = ConvertInterval(interval),
                    timestamp = TimeExtension.UnixTime,
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
                
                await SendMessageAsync(JsonSerializer.Serialize(request));
                
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
                
                await SendMessageAsync(JsonSerializer.Serialize(request));
                
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
                
                await SendMessageAsync(JsonSerializer.Serialize(request));
                
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
                
                await SendMessageAsync(JsonSerializer.Serialize(request));
                
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
            return JsonSerializer.Serialize(new 
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