using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CCXT.Collector.Core.Abstractions;
using CCXT.Collector.Library;
using CCXT.Collector.Service;
using System.Text.Json;
using CCXT.Collector.Models.WebSocket;

namespace CCXT.Collector.Crypto
{
    /*
     * Crypto.com Support Markets: USDT, USDC, USD, CRO
     *
     * API Documentation:
     *     https://exchange-docs.crypto.com/exchange/v1/rest-ws/index.html
     *     https://exchange-docs.crypto.com/spot/index.html
     *
     * WebSocket API:
     *     https://exchange-docs.crypto.com/exchange/v1/rest-ws/index.html#websocket-subscriptions
     *
     * Fees:
     *     https://crypto.com/exchange/document/fees-limits
     */
    /// <summary>
    /// Crypto.com WebSocket client for real-time data streaming
    /// </summary>
    public class CryptoWebSocketClient : WebSocketClientBase
    {
        private readonly Dictionary<string, SOrderBook> _orderbookCache;
        private long _nextId = 1;

        public override string ExchangeName => "Crypto.com";
        protected override string WebSocketUrl => "wss://stream.crypto.com/v2/market";
        protected override string PrivateWebSocketUrl => "wss://stream.crypto.com/v2/user";
        protected override int PingIntervalMs => 30000; // 30 seconds

        public CryptoWebSocketClient()
        {
            _orderbookCache = new Dictionary<string, SOrderBook>();
        }

        protected override async Task ProcessMessageAsync(string message, bool isPrivate = false)
        {
            try
            {
                using var doc = JsonDocument.Parse(message); 
                var json = doc.RootElement;
                
                // Handle response to subscription
                if (json.TryGetProperty("id", out var idProp) && json.TryGetProperty("code", out var codeProp))
                {
                    var code = json.GetInt32OrDefault("code", -1);
                    if (code != 0)
                    {
                        var errorMessage = json.GetStringOrDefault("message", "Unknown error");
                        RaiseError($"Subscription error: {errorMessage}");
                    }
                    return;
                }

                // Handle method messages
                var method = json.GetStringOrDefault("method");
                if (method == "subscribe")
                {
                    if (json.TryGetProperty("result", out var result))
                    {
                        var channel = result.GetStringOrDefault("channel");
                        
                        if (channel != null)
                        {
                            if (channel.StartsWith("ticker."))
                            {
                                await ProcessTickerData(json);
                            }
                            else if (channel.StartsWith("book."))
                            {
                                await ProcessOrderbookData(json);
                            }
                            else if (channel.StartsWith("trade."))
                            {
                                await ProcessTradeData(json);
                            }
                            else if (channel.StartsWith("candlestick."))
                            {
                                await ProcessCandleData(json);
                            }
                        }
                    }
                }
                else if (method == "public/heartbeat")
                {
                    await HandleHeartbeat(json);
                }
            }
            catch (Exception ex)
            {
                RaiseError($"Message processing error: {ex.Message}");
            }
        }

        private async Task HandleHeartbeat(JsonElement json)
        {
            var id = json.GetStringOrDefault("id");
            var response = new
            {
                id = id,
                method = "public/heartbeat"
            };
            await SendMessageAsync(JsonSerializer.Serialize(response));
        }

        private async Task ProcessTickerData(JsonElement json)
        {
            try
            {
                if (!json.TryGetProperty("result", out var result))
                    return;

                var instrumentName = result.GetStringOrDefault("instrument_name");
                if (String.IsNullOrEmpty(instrumentName))
                    return;

                var symbol = ConvertToStandardSymbol(instrumentName);
                if (!(result.TryGetArray("data", out var data)))
                    return;

                var tickerData = data[0];
                var timestamp = tickerData.GetInt64OrDefault("t", TimeExtension.UnixTime);

                var ticker = new STicker
                {
                    exchange = ExchangeName,
                    symbol = symbol,
                    timestamp = timestamp,
                    result = new STickerItem
                    {
                        timestamp = timestamp,
                        closePrice = tickerData.GetDecimalOrDefault("a"), // last trade price
                        openPrice = tickerData.GetDecimalOrDefault("o"), // 24h open
                        highPrice = tickerData.GetDecimalOrDefault("h"), // 24h high
                        lowPrice = tickerData.GetDecimalOrDefault("l"), // 24h low
                        volume = tickerData.GetDecimalOrDefault("v"), // 24h volume
                        quoteVolume = tickerData.GetDecimalOrDefault("vv"), // 24h quote volume
                        bidPrice = tickerData.GetDecimalOrDefault("b"),
                        bidQuantity = tickerData.GetDecimalOrDefault("bs"),
                        askPrice = tickerData.GetDecimalOrDefault("k"),
                        askQuantity = tickerData.GetDecimalOrDefault("ks"),
                        change = tickerData.GetDecimalOrDefault("c"), // 24h change
                        percentage = (tickerData.GetDecimalOrDefault("c")) * 100 // convert to percentage
                    }
                };

                InvokeTickerCallback(ticker);
            }
            catch (Exception ex)
            {
                RaiseError($"Ticker processing error: {ex.Message}");
            }
        }

        private async Task ProcessOrderbookData(JsonElement json)
        {
            try
            {
                if (!json.TryGetProperty("result", out var result))
                    return;

                if (!result.TryGetProperty("data", out var data))
                    return;

                var instrumentName = result.GetStringOrDefault("instrument_name");
                if (String.IsNullOrEmpty(instrumentName))
                    return;

                var symbol = ConvertToStandardSymbol(instrumentName);

                var timestamp = (data.ValueKind == JsonValueKind.Array && data.GetArrayLength() > 0)
                    ? data[0].GetInt64OrDefault("t", TimeExtension.UnixTime)
                    : TimeExtension.UnixTime;

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

                // Process asks
                if (data[0].TryGetArray("asks", out var asks))
                {
                    foreach (var ask in asks.EnumerateArray())
                    {
                        if (ask.GetArrayLength() < 3)
                            continue;
                            
                        var price = ask[0].GetDecimalValue();
                        var quantity = ask[1].GetDecimalValue();
                        var numOrders = ask[2].GetInt32Value();
                        
                        orderbook.result.asks.Add(new SOrderBookItem
                        {
                            price = price,
                            quantity = quantity,
                            amount = price * quantity,
                            count = numOrders
                        });
                    }
                }

                // Process bids
                if (data[0].TryGetArray("bids", out var bids))
                {
                    foreach (var bid in bids.EnumerateArray())
                    {
                        if (bid.GetArrayLength() < 3)
                            continue;
                            
                        var price = bid[0].GetDecimalValue();
                        var quantity = bid[1].GetDecimalValue();
                        var numOrders = bid[2].GetInt32Value();
                        
                        orderbook.result.bids.Add(new SOrderBookItem
                        {
                            price = price,
                            quantity = quantity,
                            amount = price * quantity,
                            count = numOrders
                        });
                    }
                }

                // Sort orderbook
                orderbook.result.asks = orderbook.result.asks.OrderBy(a => a.price).ToList();
                orderbook.result.bids = orderbook.result.bids.OrderByDescending(b => b.price).ToList();

                _orderbookCache[symbol] = orderbook;
                InvokeOrderbookCallback(orderbook);
            }
            catch (Exception ex)
            {
                RaiseError($"Orderbook processing error: {ex.Message}");
            }
        }

        private async Task ProcessTradeData(JsonElement json)
        {
            try
            {
                if (!json.TryGetProperty("result", out var result))
                    return;

                var instrumentName = result.GetStringOrDefault("instrument_name");
                if (String.IsNullOrEmpty(instrumentName))
                    return;

                var symbol = ConvertToStandardSymbol(instrumentName);

                if (!(result.TryGetArray("data", out var data)))
                    return;

                var trades = new List<STradeItem>();
                long latestTimestamp = 0;

                foreach (var trade in data.EnumerateArray())
                {
                    var timestamp = trade.GetInt64OrDefault("t", TimeExtension.UnixTime);
                    if (timestamp > latestTimestamp)
                        latestTimestamp = timestamp;
                    
                    trades.Add(new STradeItem
                    {
                        tradeId = trade.GetStringOrDefault("d"), // trade id
                        timestamp = timestamp,
                        price = trade.GetDecimalOrDefault("p"),
                        quantity = trade.GetDecimalOrDefault("q"),
                        amount = (trade.GetDecimalOrDefault("p")) * (trade.GetDecimalOrDefault("q")),
                        sideType = trade.GetStringOrDefault("s") == "BUY" ? SideType.Bid : SideType.Ask,
                        orderType = OrderType.Limit
                    });
                }

                if (trades.Count > 0)
                {
                    var completeOrder = new STrade
                    {
                        exchange = ExchangeName,
                        symbol = symbol,
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

        private async Task ProcessCandleData(JsonElement json)
        {
            try
            {
                if (!json.TryGetProperty("result", out var result))
                    return;

                var instrumentName = result.GetStringOrDefault("instrument_name");
                var interval = result.GetStringOrDefault("interval");
                
                if (String.IsNullOrEmpty(instrumentName) || String.IsNullOrEmpty(interval))
                    return;

                var symbol = ConvertToStandardSymbol(instrumentName);

                if (!(result.TryGetArray("data", out var data)))
                    return;

                var candleItems = new List<SCandleItem>();
                long latestTimestamp = 0;
                var convertedInterval = ConvertInterval(interval);
                var intervalMs = GetIntervalMilliseconds(convertedInterval);

                foreach (var candle in data.EnumerateArray())
                {
                    var timestamp = candle.GetInt64OrDefault("t");
                    if (timestamp > latestTimestamp)
                        latestTimestamp = timestamp;
                    
                    candleItems.Add(new SCandleItem
                    {
                        openTime = timestamp,
                        closeTime = timestamp + intervalMs,
                        open = candle.GetDecimalOrDefault("o"),
                        high = candle.GetDecimalOrDefault("h"),
                        low = candle.GetDecimalOrDefault("l"),
                        close = candle.GetDecimalOrDefault("c"),
                        volume = candle.GetDecimalOrDefault("v")
                    });
                }

                if (candleItems.Count > 0)
                {
                    var candlestick = new SCandle
                    {
                        exchange = ExchangeName,
                        symbol = symbol,
                        interval = convertedInterval,
                        timestamp = latestTimestamp,
                        result = candleItems
                    };

                    InvokeCandleCallback(candlestick);
                }
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
                var instrumentName = ConvertToCryptoSymbol(symbol);
                var subscription = new
                {
                    id = _nextId++,
                    method = "subscribe",
                    @params = new
                    {
                        channels = new[] { $"book.{instrumentName}.10" }
                    },
                    nonce = TimeExtension.UnixTime
                };

                await SendMessageAsync(JsonSerializer.Serialize(subscription));
                
                MarkSubscriptionActive("orderbook", symbol);

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
                var instrumentName = ConvertToCryptoSymbol(symbol);
                var subscription = new
                {
                    id = _nextId++,
                    method = "subscribe",
                    @params = new
                    {
                        channels = new[] { $"trade.{instrumentName}" }
                    },
                    nonce = TimeExtension.UnixTime
                };

                await SendMessageAsync(JsonSerializer.Serialize(subscription));
                
                MarkSubscriptionActive("trades", symbol);

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
                var instrumentName = ConvertToCryptoSymbol(symbol);
                var subscription = new
                {
                    id = _nextId++,
                    method = "subscribe",
                    @params = new
                    {
                        channels = new[] { $"ticker.{instrumentName}" }
                    },
                    nonce = TimeExtension.UnixTime
                };

                await SendMessageAsync(JsonSerializer.Serialize(subscription));
                
                MarkSubscriptionActive("ticker", symbol);

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
                var instrumentName = ConvertToCryptoSymbol(symbol);
                string channelName = channel switch
                {
                    "orderbook" => $"book.{instrumentName}.10",
                    "trades" => $"trade.{instrumentName}",
                    "ticker" => $"ticker.{instrumentName}",
                    _ => channel
                };

                var unsubscription = new
                {
                    id = _nextId++,
                    method = "unsubscribe",
                    @params = new
                    {
                        channels = new[] { channelName }
                    },
                    nonce = TimeExtension.UnixTime
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
                id = _nextId++,
                method = "public/heartbeat"
            });
        }

        protected override string CreateAuthenticationMessage(string apiKey, string secretKey)
        {
            var nonce = TimeExtension.UnixTime;
            var method = "public/auth";
            var signaturePayload = $"{method}{nonce}{apiKey}";
            var signature = GenerateSignature(secretKey, signaturePayload);
            
            return JsonSerializer.Serialize(new
            {
                id = _nextId++,
                method = method,
                @params = new
                {
                    api_key = apiKey,
                    sig = signature
                },
                nonce = nonce
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

        #region Candlestick/K-Line Implementation

        public override async Task<bool> SubscribeCandlesAsync(string symbol, string interval)
        {
            try
            {
                var instrumentName = ConvertToCryptoSymbol(symbol);
                var cryptoInterval = ConvertToCryptoInterval(interval);
                
                var subscription = new
                {
                    id = _nextId++,
                    method = "subscribe",
                    @params = new
                    {
                        channels = new[] { $"candlestick.{cryptoInterval}.{instrumentName}" }
                    },
                    nonce = TimeExtension.UnixTime
                };

                await SendMessageAsync(JsonSerializer.Serialize(subscription));
                
                var key = CreateSubscriptionKey($"candlestick.{cryptoInterval}", symbol);
                _subscriptions[key] = new SubscriptionInfo
                {
                    Channel = $"candlestick.{cryptoInterval}",
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

        #endregion

        #region Helper Methods

        private string ConvertToCryptoSymbol(string symbol)
        {
            // Convert from standard format (BTC/USDT) to Crypto.com format (BTC_USDT)
            return symbol.Replace("/", "_");
        }

        private string ConvertToStandardSymbol(string instrumentName)
        {
            // Convert from Crypto.com format (BTC_USDT) to standard format (BTC/USDT)
            return instrumentName.Replace("_", "/");
        }

        private string ConvertToCryptoInterval(string interval)
        {
            return interval switch
            {
                "1m" => "1M",
                "5m" => "5M",
                "15m" => "15M",
                "30m" => "30M",
                "1h" => "1H",
                "4h" => "4H",
                "6h" => "6H",
                "12h" => "12H",
                "1d" => "1D",
                "1w" => "7D",
                "2w" => "14D",
                "1M" => "1M",
                _ => "1M"
            };
        }

        private string ConvertInterval(string cryptoInterval)
        {
            return cryptoInterval switch
            {
                "1M" => "1m",
                "5M" => "5m",
                "15M" => "15m",
                "30M" => "30m",
                "1H" => "1h",
                "4H" => "4h",
                "6H" => "6h",
                "12H" => "12h",
                "1D" => "1d",
                "7D" => "1w",
                "14D" => "2w",
                _ => "1m"
            };
        }

        private long GetIntervalMilliseconds(string interval)
        {
            return interval switch
            {
                "1m" => 60000,
                "5m" => 300000,
                "15m" => 900000,
                "30m" => 1800000,
                "1h" => 3600000,
                "4h" => 14400000,
                "6h" => 21600000,
                "12h" => 43200000,
                "1d" => 86400000,
                "1w" => 604800000,
                "2w" => 1209600000,
                "1M" => 2592000000,
                _ => 60000
            };
        }

        #endregion

        #region Batch Subscription Support

        /// <summary>
        /// Crypto.com supports batch subscription - up to 100 channels per message
        /// </summary>
        protected override bool SupportsBatchSubscription()
        {
            return true;
        }

        /// <summary>
        /// Send batch subscriptions for Crypto.com - batches up to 100 channels per message
        /// </summary>
        protected override async Task<bool> SendBatchSubscriptionsAsync(List<KeyValuePair<string, SubscriptionInfo>> subscriptions)
        {
            try
            {
                // Build list of all channels
                var channels = new List<string>();
                
                foreach (var kvp in subscriptions)
                {
                    var subscription = kvp.Value;
                    var instrumentName = ConvertToCryptoSymbol(subscription.Symbol);
                    
                    // Map channel names to Crypto.com channel format
                    string channelName = subscription.Channel.ToLower() switch
                    {
                        "orderbook" or "depth" => $"book.{instrumentName}.10",
                        "trades" or "trade" => $"trade.{instrumentName}",
                        "ticker" => $"ticker.{instrumentName}",
                        "candles" or "kline" or "candlestick" => !string.IsNullOrEmpty(subscription.Extra) 
                            ? $"candlestick.{ConvertToCryptoInterval(subscription.Extra)}.{instrumentName}"
                            : $"candlestick.1M.{instrumentName}",
                        _ => $"{subscription.Channel}.{instrumentName}"
                    };

                    channels.Add(channelName);
                }

                if (channels.Count == 0)
                    return true;

                // Crypto.com supports up to 100 channels per subscription message
                const int maxChannelsPerMessage = 100;
                var messageCount = (channels.Count + maxChannelsPerMessage - 1) / maxChannelsPerMessage;

                for (int i = 0; i < messageCount; i++)
                {
                    // Take up to 100 channels for this message
                    var batchChannels = channels
                        .Skip(i * maxChannelsPerMessage)
                        .Take(maxChannelsPerMessage)
                        .ToArray();

                    // Create subscription message
                    var subscriptionMessage = new
                    {
                        id = _nextId++,
                        method = "subscribe",
                        @params = new
                        {
                            channels = batchChannels
                        },
                        nonce = TimeExtension.UnixTime
                    };

                    // Send the batch subscription
                    var json = JsonSerializer.Serialize(subscriptionMessage);
                    await SendMessageAsync(json);
                    
                    RaiseError($"Sent Crypto.com batch subscription {i + 1}/{messageCount} with {batchChannels.Length} channels");

                    // Small delay between batches if multiple messages
                    if (i < messageCount - 1)
                        await Task.Delay(100);
                }

                RaiseError($"Completed Crypto.com batch subscription: {channels.Count} total channels in {messageCount} message(s)");

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
