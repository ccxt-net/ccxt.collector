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
     *     https://exchange-docs.crypto.com/exchange/v1/rest-ws/index.html#websocket-api
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
                var json = JObject.Parse(message);
                
                // Handle response to subscription
                if (json["id"] != null && json["code"] != null)
                {
                    var code = json["code"]?.Value<int>() ?? -1;
                    if (code != 0)
                    {
                        RaiseError($"Subscription error: {json["message"]}");
                    }
                    return;
                }

                // Handle method messages
                var method = json["method"]?.ToString();
                if (method == "subscribe")
                {
                    var result = json["result"];
                    if (result != null)
                    {
                        var channel = result["channel"]?.ToString();
                        
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

        private async Task HandleHeartbeat(JObject json)
        {
            var id = json["id"]?.ToString();
            var response = new
            {
                id = id,
                method = "public/heartbeat"
            };
            await SendMessageAsync(JsonConvert.SerializeObject(response));
        }

        private async Task ProcessTickerData(JObject json)
        {
            try
            {
                var result = json["result"];
                if (result == null)
                    return;

                var instrumentName = result["instrument_name"]?.ToString();
                if (string.IsNullOrEmpty(instrumentName))
                    return;

                var symbol = ConvertToStandardSymbol(instrumentName);
                var data = result["data"];
                
                if (data == null || !data.HasValues)
                    return;

                var tickerData = data[0];
                var timestamp = tickerData["t"]?.Value<long>() ?? DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

                var ticker = new STicker
                {
                    exchange = ExchangeName,
                    symbol = symbol,
                    timestamp = timestamp,
                    result = new STickerItem
                    {
                        timestamp = timestamp,
                        closePrice = tickerData["a"]?.Value<decimal>() ?? 0, // last trade price
                        openPrice = tickerData["o"]?.Value<decimal>() ?? 0, // 24h open
                        highPrice = tickerData["h"]?.Value<decimal>() ?? 0, // 24h high
                        lowPrice = tickerData["l"]?.Value<decimal>() ?? 0, // 24h low
                        volume = tickerData["v"]?.Value<decimal>() ?? 0, // 24h volume
                        quoteVolume = tickerData["vv"]?.Value<decimal>() ?? 0, // 24h quote volume
                        bidPrice = tickerData["b"]?.Value<decimal>() ?? 0,
                        bidQuantity = tickerData["bs"]?.Value<decimal>() ?? 0,
                        askPrice = tickerData["k"]?.Value<decimal>() ?? 0,
                        askQuantity = tickerData["ks"]?.Value<decimal>() ?? 0,
                        change = tickerData["c"]?.Value<decimal>() ?? 0, // 24h change
                        percentage = (tickerData["c"]?.Value<decimal>() ?? 0) * 100 // convert to percentage
                    }
                };

                InvokeTickerCallback(ticker);
            }
            catch (Exception ex)
            {
                RaiseError($"Ticker processing error: {ex.Message}");
            }
        }

        private async Task ProcessOrderbookData(JObject json)
        {
            try
            {
                var result = json["result"];
                if (result == null)
                    return;

                var instrumentName = result["instrument_name"]?.ToString();
                if (string.IsNullOrEmpty(instrumentName))
                    return;

                var symbol = ConvertToStandardSymbol(instrumentName);
                var data = result["data"];
                var timestamp = data[0]?["t"]?.Value<long>() ?? DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

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
                var asks = data[0]?["asks"] as JArray;
                if (asks != null)
                {
                    foreach (var ask in asks)
                    {
                        var price = ask[0].Value<decimal>();
                        var quantity = ask[1].Value<decimal>();
                        var numOrders = ask[2].Value<int>();
                        
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
                var bids = data[0]?["bids"] as JArray;
                if (bids != null)
                {
                    foreach (var bid in bids)
                    {
                        var price = bid[0].Value<decimal>();
                        var quantity = bid[1].Value<decimal>();
                        var numOrders = bid[2].Value<int>();
                        
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

        private async Task ProcessTradeData(JObject json)
        {
            try
            {
                var result = json["result"];
                if (result == null)
                    return;

                var instrumentName = result["instrument_name"]?.ToString();
                if (string.IsNullOrEmpty(instrumentName))
                    return;

                var symbol = ConvertToStandardSymbol(instrumentName);
                var data = result["data"] as JArray;
                
                if (data == null || !data.HasValues)
                    return;

                var trades = new List<STradeItem>();
                long latestTimestamp = 0;

                foreach (var trade in data)
                {
                    var timestamp = trade["t"]?.Value<long>() ?? DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                    if (timestamp > latestTimestamp)
                        latestTimestamp = timestamp;
                    
                    trades.Add(new STradeItem
                    {
                        orderId = trade["d"]?.ToString(), // trade id
                        timestamp = timestamp,
                        price = trade["p"]?.Value<decimal>() ?? 0,
                        quantity = trade["q"]?.Value<decimal>() ?? 0,
                        amount = (trade["p"]?.Value<decimal>() ?? 0) * (trade["q"]?.Value<decimal>() ?? 0),
                        sideType = trade["s"]?.ToString() == "BUY" ? SideType.Bid : SideType.Ask,
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

        private async Task ProcessCandleData(JObject json)
        {
            try
            {
                var result = json["result"];
                if (result == null)
                    return;

                var instrumentName = result["instrument_name"]?.ToString();
                var interval = result["interval"]?.ToString();
                
                if (string.IsNullOrEmpty(instrumentName) || string.IsNullOrEmpty(interval))
                    return;

                var symbol = ConvertToStandardSymbol(instrumentName);
                var data = result["data"] as JArray;
                
                if (data == null || !data.HasValues)
                    return;

                var candleItems = new List<SCandleItem>();
                long latestTimestamp = 0;
                var convertedInterval = ConvertInterval(interval);
                var intervalMs = GetIntervalMilliseconds(convertedInterval);

                foreach (var candle in data)
                {
                    var timestamp = candle["t"]?.Value<long>() ?? 0;
                    if (timestamp > latestTimestamp)
                        latestTimestamp = timestamp;
                    
                    candleItems.Add(new SCandleItem
                    {
                        openTime = timestamp,
                        closeTime = timestamp + intervalMs,
                        open = candle["o"]?.Value<decimal>() ?? 0,
                        high = candle["h"]?.Value<decimal>() ?? 0,
                        low = candle["l"]?.Value<decimal>() ?? 0,
                        close = candle["c"]?.Value<decimal>() ?? 0,
                        volume = candle["v"]?.Value<decimal>() ?? 0
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
                    nonce = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                };

                await SendMessageAsync(JsonConvert.SerializeObject(subscription));
                
                var key = CreateSubscriptionKey("orderbook", symbol);
                _subscriptions[key] = new SubscriptionInfo
                {
                    Channel = "book",
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
                var instrumentName = ConvertToCryptoSymbol(symbol);
                var subscription = new
                {
                    id = _nextId++,
                    method = "subscribe",
                    @params = new
                    {
                        channels = new[] { $"trade.{instrumentName}" }
                    },
                    nonce = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                };

                await SendMessageAsync(JsonConvert.SerializeObject(subscription));
                
                var key = CreateSubscriptionKey("trades", symbol);
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
                    nonce = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
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
                    nonce = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
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
                id = _nextId++,
                method = "public/heartbeat"
            });
        }

        protected override string CreateAuthenticationMessage(string apiKey, string secretKey)
        {
            var nonce = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var method = "public/auth";
            var signaturePayload = $"{method}{nonce}{apiKey}";
            var signature = GenerateSignature(secretKey, signaturePayload);
            
            return JsonConvert.SerializeObject(new
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
                    nonce = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                };

                await SendMessageAsync(JsonConvert.SerializeObject(subscription));
                
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
    }
}
