using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CCXT.Collector.Core.Abstractions;
using CCXT.Collector.Library;
using CCXT.Collector.Service;
using System.Text.Json;

namespace CCXT.Collector.Bittrex
{
    /*
     * Bittrex Support Markets: USD, USDT, BTC, ETH
     *
     * API Documentation:
     *     https://bittrex.github.io/api/v3
     *
     * WebSocket API:
     *     https://bittrex.github.io/api/v3#websockets-overview
     *     wss://socket-v3.bittrex.com/signalr
     *
     * Fees:
     *     https://global.bittrex.com/fees
     */
    /// <summary>
    /// Bittrex WebSocket client for real-time data streaming
    /// </summary>
    public class BittrexWebSocketClient : WebSocketClientBase
    {
        private readonly Dictionary<string, SOrderBook> _orderbookCache;
        private readonly Dictionary<string, long> _sequenceNumbers;

        public override string ExchangeName => "Bittrex";
        protected override string WebSocketUrl => "wss://socket-v3.bittrex.com/signalr";
        protected override int PingIntervalMs => 30000; // 30 seconds

        public BittrexWebSocketClient()
        {
            _orderbookCache = new Dictionary<string, SOrderBook>();
            _sequenceNumbers = new Dictionary<string, long>();
        }

        protected override async Task ProcessMessageAsync(string message, bool isPrivate = false)
        {
            try
            {
                using var doc = JsonDocument.Parse(message); 
                var json = doc.RootElement;

                // Handle SignalR specific messages
                if (json.TryGetArray("M", out var mProp))
                {
                    // SignalR hub messages
                    foreach (var msg in mProp.EnumerateArray())
                    {
                        await ProcessHubMessage(msg);
                    }
                    return;
                }

                // Handle heartbeat
                if (json.TryGetProperty("R", out var rProp) && json.GetStringOrDefault("I") == "0")
                {
                    // Connection established response
                    return;
                }

                // Handle subscription response
                if (json.TryGetProperty("R", out var rProp2))
                {
                    var success = rProp2.ValueKind == JsonValueKind.True;
                    if (!success)
                    {
                        var errorMsg = json.GetStringOrDefault("E", "Unknown error");
                        RaiseError($"Subscription failed: {errorMsg}");
                    }
                    return;
                }
            }
            catch (Exception ex)
            {
                RaiseError($"Message processing error: {ex.Message}");
            }
        }

        private async Task ProcessHubMessage(JsonElement message)
        {
            try
            {
                var hub = message.GetStringOrNull("H");
                var method = message.GetStringOrNull("M");
                
                if (hub == "c3" && message.TryGetArray("A", out var aProp))
                {
                    foreach (var arg in aProp.EnumerateArray())
                    {
                        switch (method)
                        {
                            case "orderBook":
                                await ProcessOrderbookData(arg);
                                break;
                            case "trade":
                                await ProcessTradeData(arg);
                                break;
                            case "ticker":
                                await ProcessTickerData(arg);
                                break;
                            case "candle":
                                await ProcessCandleData(arg);
                                break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                RaiseError($"Hub message processing error: {ex.Message}");
            }
        }

        private async Task ProcessOrderbookData(JsonElement data)
        {
            try
            {
                var symbol = data.GetStringOrNull("marketSymbol");
                var sequence = data.GetInt64OrDefault("sequence", 0L);
                var timestamp = TimeExtension.UnixTime;

                // Check sequence for detecting snapshot vs delta
                var isSnapshot = !_sequenceNumbers.ContainsKey(symbol) || 
                                _sequenceNumbers[symbol] == 0 ||
                                sequence == 1;

                if (!isSnapshot && _sequenceNumbers.ContainsKey(symbol))
                {
                    if (sequence <= _sequenceNumbers[symbol])
                        return; // Old update, ignore
                }

                _sequenceNumbers[symbol] = sequence;

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

                // Process bid updates
                if (data.TryGetArray("bidDeltas", out var bidDeltas))
                {
                    foreach (var bid in bidDeltas.EnumerateArray())
                    {
                        var rate = bid.GetDecimalOrDefault("rate");
                        var quantity = bid.GetDecimalOrDefault("quantity");
                        
                        if (quantity > 0)
                        {
                            orderbook.result.bids.Add(new SOrderBookItem
                            {
                                price = rate,
                                quantity = quantity
                            });
                        }
                    }
                }

                // Process ask updates
                if (data.TryGetArray("askDeltas", out var askDeltas))
                {
                    foreach (var ask in askDeltas.EnumerateArray())
                    {
                        var rate = ask.GetDecimalOrDefault("rate");
                        var quantity = ask.GetDecimalOrDefault("quantity");
                        
                        if (quantity > 0)
                        {
                            orderbook.result.asks.Add(new SOrderBookItem
                            {
                                price = rate,
                                quantity = quantity
                            });
                        }
                    }
                }

                // Sort orderbook
                orderbook.result.asks = orderbook.result.asks.OrderBy(a => a.price).ToList();
                orderbook.result.bids = orderbook.result.bids.OrderByDescending(b => b.price).ToList();

                // Update cache and raise event
                _orderbookCache[symbol] = orderbook;
                InvokeOrderbookCallback(orderbook);
            }
            catch (Exception ex)
            {
                RaiseError($"Orderbook processing error: {ex.Message}");
            }
        }

        private async Task ProcessTradeData(JsonElement data)
        {
            try
            {
                if (!data.TryGetArray("deltas", out var deltas))
                    return;

                var symbol = data.GetStringOrNull("marketSymbol");
                var timestamp = TimeExtension.UnixTime;

                var trades = new List<STradeItem>();

                foreach (var trade in deltas.EnumerateArray())
                {
                    DateTime? executedAt = null;

                    var execProp = trade.GetStringOrNull("executedAt");
                    if (!String.IsNullOrEmpty(execProp))
                    {
                        if (DateTime.TryParse(execProp, out var dt))
                            executedAt = dt;
                    }
                    
                    var tradeTimestamp = executedAt.HasValue ? 
                        new DateTimeOffset(executedAt.Value).ToUnixTimeMilliseconds() : 
                        timestamp;

                    var takerSide = trade.GetStringOrDefault("takerSide", "");
                    var rate = trade.GetDecimalOrDefault("rate");
                    var quantity = trade.GetDecimalOrDefault("quantity");
                    
                    trades.Add(new STradeItem
                    {
                        timestamp = tradeTimestamp,
                        sideType = takerSide == "BUY" ? SideType.Bid : SideType.Ask,
                        orderType = OrderType.Limit,
                        price = rate,
                        quantity = quantity,
                        amount = rate * quantity
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

        private async Task ProcessTickerData(JsonElement data)
        {
            try
            {
                var symbol = data.GetStringOrNull("symbol");
                var timestamp = TimeExtension.UnixTime;

                var ticker = new STicker
                {
                    exchange = ExchangeName,
                    symbol = ConvertToStandardSymbol(symbol),
                    timestamp = timestamp,
                    result = new STickerItem
                    {
                        timestamp = timestamp,
                        closePrice = data.GetDecimalOrDefault("lastTradeRate"),
                        openPrice = data.GetDecimalOrDefault("openRate"),
                        highPrice = data.GetDecimalOrDefault("highRate"),
                        lowPrice = data.GetDecimalOrDefault("lowRate"),
                        volume = data.GetDecimalOrDefault("volume"),
                        quoteVolume = data.GetDecimalOrDefault("quoteVolume")
                    }
                };

                InvokeTickerCallback(ticker);
            }
            catch (Exception ex)
            {
                RaiseError($"Ticker processing error: {ex.Message}");
            }
        }

        private async Task ProcessCandleData(JsonElement data)
        {
            try
            {
                if (!data.TryGetProperty("delta", out var delta))
                    return;

                var symbol = data.GetStringOrNull("marketSymbol");
                var interval = data.GetStringOrNull("interval");
                
                DateTime? startsAt = null;
                var startsProp = delta.GetStringOrNull("startsAt");

                if (!String.IsNullOrEmpty(startsProp))
                {
                    if (DateTime.TryParse(startsProp, out var dt))
                        startsAt = dt;
                }
                
                var timestamp = startsAt.HasValue ? 
                    new DateTimeOffset(startsAt.Value).ToUnixTimeMilliseconds() : 
                    TimeExtension.UnixTime;

                var candles = new List<SCandleItem>
                {
                    new SCandleItem
                    {
                        openTime = timestamp,
                        closeTime = timestamp + 60000, // Assuming 1m interval
                        open = delta.GetDecimalOrDefault("open"),
                        high = delta.GetDecimalOrDefault("high"),
                        low = delta.GetDecimalOrDefault("low"),
                        close = delta.GetDecimalOrDefault("close"),
                        volume = delta.GetDecimalOrDefault("volume")
                    }
                };

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
                var bittrexSymbol = ConvertToExchangeSymbol(symbol);
                var request = new
                {
                    H = "c3",
                    M = "Subscribe",
                    A = new[] { new[] { $"orderbook_{bittrexSymbol}_25" } },
                    I = 1
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
                var bittrexSymbol = ConvertToExchangeSymbol(symbol);
                var request = new
                {
                    H = "c3",
                    M = "Subscribe",
                    A = new[] { new[] { $"trade_{bittrexSymbol}" } },
                    I = 2
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
                var bittrexSymbol = ConvertToExchangeSymbol(symbol);
                var request = new
                {
                    H = "c3",
                    M = "Subscribe",
                    A = new[] { new[] { $"ticker_{bittrexSymbol}" } },
                    I = 3
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
                var bittrexSymbol = ConvertToExchangeSymbol(symbol);
                var bittrexInterval = ConvertToExchangeInterval(interval);
                var request = new
                {
                    H = "c3",
                    M = "Subscribe",
                    A = new[] { new[] { $"candle_{bittrexSymbol}_{bittrexInterval}" } },
                    I = 4
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
                var bittrexSymbol = ConvertToExchangeSymbol(symbol);
                var channelName = channel switch
                {
                    "orderbook" => $"orderbook_{bittrexSymbol}_25",
                    "trades" => $"trade_{bittrexSymbol}",
                    "ticker" => $"ticker_{bittrexSymbol}",
                    "candles" => $"candle_{bittrexSymbol}_MINUTE_1",
                    _ => channel
                };

                var request = new
                {
                    H = "c3",
                    M = "Unsubscribe",
                    A = new[] { new[] { channelName } },
                    I = 5
                };

                await SendMessageAsync(JsonSerializer.Serialize(request));
                
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
            // SignalR uses a different ping mechanism
            return JsonSerializer.Serialize(new { H = "c3", M = "ping", A = new object[0], I = 0 });
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
            // Convert BTC/USDT to BTC-USDT
            return symbol.Replace("/", "-");
        }

        private string ConvertToStandardSymbol(string exchangeSymbol)
        {
            // Convert BTC-USDT to BTC/USDT
            return exchangeSymbol?.Replace("-", "/");
        }

        private string ConvertToExchangeInterval(string interval)
        {
            // Convert standard interval to Bittrex format
            return interval switch
            {
                "1m" => "MINUTE_1",
                "5m" => "MINUTE_5",
                "15m" => "MINUTE_15",
                "30m" => "MINUTE_30",
                "1h" => "HOUR_1",
                "4h" => "HOUR_4",
                "1d" => "DAY_1",
                _ => "MINUTE_1"
            };
        }

        private string ConvertInterval(string bittrexInterval)
        {
            // Convert Bittrex interval to standard format
            return bittrexInterval switch
            {
                "MINUTE_1" => "1m",
                "MINUTE_5" => "5m",
                "MINUTE_15" => "15m",
                "MINUTE_30" => "30m",
                "HOUR_1" => "1h",
                "HOUR_4" => "4h",
                "DAY_1" => "1d",
                _ => "1m"
            };
        }

        #endregion
    }
}