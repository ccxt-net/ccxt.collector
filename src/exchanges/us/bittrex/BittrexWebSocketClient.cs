using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CCXT.Collector.Core.Abstractions;
using CCXT.Collector.Library;
using CCXT.Collector.Service;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

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
                var json = JObject.Parse(message);

                // Handle SignalR specific messages
                var msgType = json["M"]?.ToString();
                if (msgType != null)
                {
                    // SignalR hub messages
                    var messages = json["M"] as JArray;
                    if (messages != null)
                    {
                        foreach (var msg in messages)
                        {
                            await ProcessHubMessage(msg as JObject);
                        }
                    }
                    return;
                }

                // Handle heartbeat
                if (json["R"] != null && json["I"]?.ToString() == "0")
                {
                    // Connection established response
                    return;
                }

                // Handle subscription response
                if (json["R"] != null)
                {
                    var success = json["R"]?.Value<bool>() ?? false;
                    if (!success)
                    {
                        RaiseError($"Subscription failed: {json["E"]}");
                    }
                    return;
                }
            }
            catch (Exception ex)
            {
                RaiseError($"Message processing error: {ex.Message}");
            }
        }

        private async Task ProcessHubMessage(JObject message)
        {
            try
            {
                var hub = message["H"]?.ToString();
                var method = message["M"]?.ToString();
                var args = message["A"] as JArray;

                if (hub == "c3" && args != null && args.Count > 0)
                {
                    foreach (var arg in args)
                    {
                        var data = arg as JObject;
                        if (data == null) continue;

                        switch (method)
                        {
                            case "orderBook":
                                await ProcessOrderbookData(data);
                                break;
                            case "trade":
                                await ProcessTradeData(data);
                                break;
                            case "ticker":
                                await ProcessTickerData(data);
                                break;
                            case "candle":
                                await ProcessCandleData(data);
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

        private async Task ProcessOrderbookData(JObject data)
        {
            try
            {
                var symbol = data["marketSymbol"]?.ToString();
                var sequence = data["sequence"]?.Value<long>() ?? 0;
                var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

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
                var bidDeltas = data["bidDeltas"] as JArray;
                if (bidDeltas != null)
                {
                    foreach (var bid in bidDeltas)
                    {
                        var rate = bid["rate"]?.Value<decimal>() ?? 0;
                        var quantity = bid["quantity"]?.Value<decimal>() ?? 0;
                        
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
                var askDeltas = data["askDeltas"] as JArray;
                if (askDeltas != null)
                {
                    foreach (var ask in askDeltas)
                    {
                        var rate = ask["rate"]?.Value<decimal>() ?? 0;
                        var quantity = ask["quantity"]?.Value<decimal>() ?? 0;
                        
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

        private async Task ProcessTradeData(JObject data)
        {
            try
            {
                var deltas = data["deltas"] as JArray;
                if (deltas == null || deltas.Count == 0)
                    return;

                var symbol = data["marketSymbol"]?.ToString();
                var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

                var trades = new List<STradeItem>();

                foreach (var trade in deltas)
                {
                    var executedAt = trade["executedAt"]?.Value<DateTime>();
                    var tradeTimestamp = executedAt.HasValue ? 
                        new DateTimeOffset(executedAt.Value).ToUnixTimeMilliseconds() : 
                        timestamp;

                    trades.Add(new STradeItem
                    {
                        timestamp = tradeTimestamp,
                        sideType = trade["takerSide"]?.ToString() == "BUY" ? SideType.Bid : SideType.Ask,
                        orderType = OrderType.Limit,
                        price = trade["rate"]?.Value<decimal>() ?? 0,
                        quantity = trade["quantity"]?.Value<decimal>() ?? 0,
                        amount = (trade["rate"]?.Value<decimal>() ?? 0) * (trade["quantity"]?.Value<decimal>() ?? 0)
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

        private async Task ProcessTickerData(JObject data)
        {
            try
            {
                var symbol = data["symbol"]?.ToString();
                var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

                var ticker = new STicker
                {
                    exchange = ExchangeName,
                    symbol = ConvertToStandardSymbol(symbol),
                    timestamp = timestamp,
                    result = new STickerItem
                    {
                        timestamp = timestamp,
                        closePrice = data["lastTradeRate"]?.Value<decimal>() ?? 0,
                        openPrice = data["openRate"]?.Value<decimal>() ?? 0,
                        highPrice = data["highRate"]?.Value<decimal>() ?? 0,
                        lowPrice = data["lowRate"]?.Value<decimal>() ?? 0,
                        volume = data["volume"]?.Value<decimal>() ?? 0,
                        quoteVolume = data["quoteVolume"]?.Value<decimal>() ?? 0
                    }
                };

                InvokeTickerCallback(ticker);
            }
            catch (Exception ex)
            {
                RaiseError($"Ticker processing error: {ex.Message}");
            }
        }

        private async Task ProcessCandleData(JObject data)
        {
            try
            {
                var symbol = data["marketSymbol"]?.ToString();
                var interval = data["interval"]?.ToString();
                var delta = data["delta"] as JObject;
                
                if (delta == null) return;

                var startsAt = delta["startsAt"]?.Value<DateTime>();
                var timestamp = startsAt.HasValue ? 
                    new DateTimeOffset(startsAt.Value).ToUnixTimeMilliseconds() : 
                    DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

                var candles = new List<SCandleItem>
                {
                    new SCandleItem
                    {
                        openTime = timestamp,
                        closeTime = timestamp + 60000, // Assuming 1m interval
                        open = delta["open"]?.Value<decimal>() ?? 0,
                        high = delta["high"]?.Value<decimal>() ?? 0,
                        low = delta["low"]?.Value<decimal>() ?? 0,
                        close = delta["close"]?.Value<decimal>() ?? 0,
                        volume = delta["volume"]?.Value<decimal>() ?? 0
                    }
                };

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
                var bittrexSymbol = ConvertToExchangeSymbol(symbol);
                var request = new
                {
                    H = "c3",
                    M = "Subscribe",
                    A = new[] { new[] { $"orderbook_{bittrexSymbol}_25" } },
                    I = 1
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
                var bittrexSymbol = ConvertToExchangeSymbol(symbol);
                var request = new
                {
                    H = "c3",
                    M = "Subscribe",
                    A = new[] { new[] { $"trade_{bittrexSymbol}" } },
                    I = 2
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
                var bittrexSymbol = ConvertToExchangeSymbol(symbol);
                var request = new
                {
                    H = "c3",
                    M = "Subscribe",
                    A = new[] { new[] { $"ticker_{bittrexSymbol}" } },
                    I = 3
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
                var bittrexSymbol = ConvertToExchangeSymbol(symbol);
                var bittrexInterval = ConvertToExchangeInterval(interval);
                var request = new
                {
                    H = "c3",
                    M = "Subscribe",
                    A = new[] { new[] { $"candle_{bittrexSymbol}_{bittrexInterval}" } },
                    I = 4
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

                await SendMessageAsync(JsonConvert.SerializeObject(request));
                
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
            return JsonConvert.SerializeObject(new { H = "c3", M = "ping", A = new object[0], I = 0 });
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