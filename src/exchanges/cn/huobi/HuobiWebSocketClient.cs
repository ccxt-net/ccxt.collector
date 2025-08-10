using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CCXT.Collector.Core.Abstractions;
using CCXT.Collector.Library;
using CCXT.Collector.Service;
using System.Text.Json;

namespace CCXT.Collector.Huobi
{
    /*
     * Huobi (HTX) Support Markets: USDT, BTC, ETH, HT
     *
     * API Documentation:
     *     https://huobiapi.github.io/docs/spot/v1/en/
     *
     * WebSocket API:
     *     https://huobiapi.github.io/docs/spot/v1/en/#websocket-market-data
     *     https://huobiapi.github.io/docs/spot/v1/en/#websocket-asset-and-order
     *
     * Important: Huobi uses GZIP compression for WebSocket messages
     *
     * Fees:
     *     https://www.htx.com/en-us/fee/
     */
    /// <summary>
    /// Huobi WebSocket client for real-time data streaming with GZIP decompression
    /// </summary>
    public class HuobiWebSocketClient : WebSocketClientBase
    {
        private readonly Dictionary<string, SOrderBook> _orderbookCache;
        private readonly Dictionary<string, long> _lastUpdateIds;
        private long _nextId = 1;

        public override string ExchangeName => "Huobi";
        protected override string WebSocketUrl => "wss://api.huobi.pro/ws";
        protected override string PrivateWebSocketUrl => "wss://api.huobi.pro/ws/v2";
        protected override int PingIntervalMs => 20000; // 20 seconds

        public HuobiWebSocketClient()
        {
            _orderbookCache = new Dictionary<string, SOrderBook>();
            _lastUpdateIds = new Dictionary<string, long>();
        }

        // Override the base ReceiveLoop to handle GZIP compression
        protected override async Task ReceiveLoop(ClientWebSocket socket, bool isPrivate)
        {
            var bufferSize = 1024 * 16;  // 16KB initial buffer
            var buffer = new ArraySegment<byte>(new byte[bufferSize]);
            var binaryData = new List<byte>();

            try
            {
                while (socket?.State == WebSocketState.Open && !_cancellationTokenSource.Token.IsCancellationRequested)
                {
                    WebSocketReceiveResult result;
                    binaryData.Clear();

                    do
                    {
                        result = await socket.ReceiveAsync(buffer, _cancellationTokenSource.Token);
                        
                        if (result.MessageType == WebSocketMessageType.Binary || 
                            result.MessageType == WebSocketMessageType.Text)
                        {
                            binaryData.AddRange(buffer.Array.Take(result.Count));
                            
                            // Resize buffer if nearly full 
                            if (result.Count == buffer.Count && !result.EndOfMessage)
                            {
                                bufferSize *= 2;
                                buffer = new ArraySegment<byte>(new byte[bufferSize]);
                            }
                        }
                        else if (result.MessageType == WebSocketMessageType.Close)
                        {
                            await HandleDisconnectAsync();
                            return;
                        }
                    }
                    while (!result.EndOfMessage);

                    if (binaryData.Count > 0)
                    {
                        // Decompress GZIP data
                        var decompressedMessage = DecompressGzip(binaryData.ToArray());
                        if (!string.IsNullOrEmpty(decompressedMessage))
                        {
                            await ProcessMessageAsync(decompressedMessage, isPrivate);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                RaiseError($"Receive error: {ex.Message}");
                await HandleReconnectAsync();
            }
        }

        private string DecompressGzip(byte[] data)
        {
            try
            {
                using (var compressedStream = new MemoryStream(data))
                using (var decompressor = new GZipStream(compressedStream, CompressionMode.Decompress))
                using (var decompressedStream = new MemoryStream())
                {
                    decompressor.CopyTo(decompressedStream);
                    return Encoding.UTF8.GetString(decompressedStream.ToArray());
                }
            }
            catch (Exception ex)
            {
                // If decompression fails, try treating it as plain text
                try
                {
                    return Encoding.UTF8.GetString(data);
                }
                catch
                {
                    RaiseError($"Failed to decompress message: {ex.Message}");
                    return null;
                }
            }
        }

        protected override async Task ProcessMessageAsync(string message, bool isPrivate = false)
        {
            try
            {
                using var doc = JsonDocument.Parse(message);
                var json = doc.RootElement;

                // Handle ping/pong
                if (json.TryGetProperty("ping", out var ping))
                {
                    await HandlePingMessage(ping.GetInt64());
                    return;
                }

                // Handle subscription response
                if (json.TryGetProperty("status", out var status))
                {
                    if (status.GetString() == "error")
                    {
                        RaiseError($"Subscription error: {json.GetStringOrDefault("err-msg")}");
                    }
                    return;
                }

                // Handle data messages
                if (json.TryGetProperty("ch", out var channel))
                {
                    var ch = channel.GetString();
                    
                    if (ch.Contains("depth"))
                    {
                        await ProcessOrderbookData(json);
                    }
                    else if (ch.Contains("trade.detail"))
                    {
                        await ProcessTradeData(json);
                    }
                    else if (ch.Contains("detail"))
                    {
                        await ProcessTickerData(json);
                    }
                    else if (ch.Contains("kline"))
                    {
                        await ProcessKlineData(json);
                    }
                }
                else if (json.TryGetProperty("action", out var action))
                {
                    // Private channel messages
                    var act = action.GetString();
                    if (act == "push" && json.TryGetProperty("data", out var data))
                    {
                        if (json.TryGetProperty("ch", out var privateCh))
                        {
                            var privateChannel = privateCh.GetString();
                            if (privateChannel.Contains("accounts.update"))
                            {
                                await ProcessAccountData(json);
                            }
                            else if (privateChannel.Contains("orders"))
                            {
                                await ProcessOrderData(json);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                RaiseError($"Message processing error: {ex.Message}");
            }
        }

        private async Task HandlePingMessage(long pingValue)
        {
            var pong = new
            {
                pong = pingValue
            };
            await SendMessageAsync(JsonSerializer.Serialize(pong));
        }

        private async Task ProcessOrderbookData(JsonElement json)
        {
            try
            {
                var channel = json.GetStringOrDefault("ch");
                var symbol = ExtractSymbolFromChannel(channel);
                
                if (string.IsNullOrEmpty(symbol))
                    return;

                var standardSymbol = ConvertToStandardSymbol(symbol);
                var timestamp = json.GetInt64OrDefault("ts", TimeExtension.UnixTime);

                if (!json.TryGetProperty("tick", out var tick))
                    return;

                var orderbook = new SOrderBook
                {
                    exchange = ExchangeName,
                    symbol = standardSymbol,
                    timestamp = timestamp,
                    result = new SOrderBookData
                    {
                        timestamp = timestamp,
                        asks = new List<SOrderBookItem>(),
                        bids = new List<SOrderBookItem>()
                    }
                };

                // Process asks
                if (tick.TryGetArray("asks", out var asks))
                {
                    foreach (var ask in asks.EnumerateArray())
                    {
                        if (ask.GetArrayLength() >= 2)
                        {
                            var price = ask[0].GetDecimalValue();
                            var quantity = ask[1].GetDecimalValue();
                            
                            if (quantity > 0)
                            {
                                orderbook.result.asks.Add(new SOrderBookItem
                                {
                                    price = price,
                                    quantity = quantity,
                                    amount = price * quantity
                                });
                            }
                        }
                    }
                }

                // Process bids
                if (tick.TryGetArray("bids", out var bids))
                {
                    foreach (var bid in bids.EnumerateArray())
                    {
                        if (bid.GetArrayLength() >= 2)
                        {
                            var price = bid[0].GetDecimalValue();
                            var quantity = bid[1].GetDecimalValue();
                            
                            if (quantity > 0)
                            {
                                orderbook.result.bids.Add(new SOrderBookItem
                                {
                                    price = price,
                                    quantity = quantity,
                                    amount = price * quantity
                                });
                            }
                        }
                    }
                }

                // Sort orderbook
                orderbook.result.asks = orderbook.result.asks.OrderBy(a => a.price).ToList();
                orderbook.result.bids = orderbook.result.bids.OrderByDescending(b => b.price).ToList();

                _orderbookCache[standardSymbol] = orderbook;
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
                var channel = json.GetStringOrDefault("ch");
                var symbol = ExtractSymbolFromChannel(channel);
                
                if (string.IsNullOrEmpty(symbol))
                    return;

                var standardSymbol = ConvertToStandardSymbol(symbol);
                var timestamp = json.GetInt64OrDefault("ts", TimeExtension.UnixTime);

                if (!json.TryGetProperty("tick", out var tick))
                    return;

                var trades = new List<STradeItem>();

                if (tick.TryGetArray("data", out var data))
                {
                    foreach (var trade in data.EnumerateArray())
                    {
                        trades.Add(new STradeItem
                        {
                            tradeId = trade.GetStringOrDefault("tradeId", trade.GetStringOrDefault("id")),
                            timestamp = trade.GetInt64OrDefault("ts", timestamp),
                            price = trade.GetDecimalOrDefault("price"),
                            quantity = trade.GetDecimalOrDefault("amount"),
                            amount = trade.GetDecimalOrDefault("price") * trade.GetDecimalOrDefault("amount"),
                            sideType = trade.GetStringOrDefault("direction") == "buy" ? SideType.Bid : SideType.Ask,
                            orderType = OrderType.Limit
                        });
                    }
                }

                if (trades.Count > 0)
                {
                    var tradeData = new STrade
                    {
                        exchange = ExchangeName,
                        symbol = standardSymbol,
                        timestamp = timestamp,
                        result = trades
                    };

                    InvokeTradeCallback(tradeData);
                }
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
                var channel = json.GetStringOrDefault("ch");
                var symbol = ExtractSymbolFromChannel(channel);
                
                if (string.IsNullOrEmpty(symbol))
                    return;

                var standardSymbol = ConvertToStandardSymbol(symbol);
                var timestamp = json.GetInt64OrDefault("ts", TimeExtension.UnixTime);

                if (!json.TryGetProperty("tick", out var tick))
                    return;

                var ticker = new STicker
                {
                    exchange = ExchangeName,
                    symbol = standardSymbol,
                    timestamp = timestamp,
                    result = new STickerItem
                    {
                        timestamp = timestamp,
                        closePrice = tick.GetDecimalOrDefault("close"),
                        openPrice = tick.GetDecimalOrDefault("open"),
                        highPrice = tick.GetDecimalOrDefault("high"),
                        lowPrice = tick.GetDecimalOrDefault("low"),
                        volume = tick.GetDecimalOrDefault("amount"),
                        quoteVolume = tick.GetDecimalOrDefault("vol"),
                        count = tick.GetInt64OrDefault("count"),
                        bidPrice = tick.GetDecimalOrDefault("bid"),
                        askPrice = tick.GetDecimalOrDefault("ask"),
                        change = tick.GetDecimalOrDefault("close") - tick.GetDecimalOrDefault("open"),
                        percentage = CalculatePercentage(tick.GetDecimalOrDefault("open"), tick.GetDecimalOrDefault("close"))
                    }
                };

                InvokeTickerCallback(ticker);
            }
            catch (Exception ex)
            {
                RaiseError($"Ticker processing error: {ex.Message}");
            }
        }

        private async Task ProcessKlineData(JsonElement json)
        {
            try
            {
                var channel = json.GetStringOrDefault("ch");
                var parts = channel.Split('.');
                if (parts.Length < 4)
                    return;

                var symbol = parts[1];
                var interval = parts[3];
                
                var standardSymbol = ConvertToStandardSymbol(symbol);
                var standardInterval = ConvertInterval(interval);
                var timestamp = json.GetInt64OrDefault("ts", TimeExtension.UnixTime);

                if (!json.TryGetProperty("tick", out var tick))
                    return;

                var candle = new SCandle
                {
                    exchange = ExchangeName,
                    symbol = standardSymbol,
                    interval = standardInterval,
                    timestamp = timestamp,
                    result = new List<SCandleItem>
                    {
                        new SCandleItem
                        {
                            openTime = tick.GetInt64OrDefault("id") * 1000, // Convert to milliseconds
                            closeTime = (tick.GetInt64OrDefault("id") * 1000) + GetIntervalMilliseconds(standardInterval),
                            open = tick.GetDecimalOrDefault("open"),
                            high = tick.GetDecimalOrDefault("high"),
                            low = tick.GetDecimalOrDefault("low"),
                            close = tick.GetDecimalOrDefault("close"),
                            volume = tick.GetDecimalOrDefault("amount"),
                            quoteVolume = tick.GetDecimalOrDefault("vol"),
                            tradeCount = tick.GetInt64OrDefault("count")
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

        private async Task ProcessAccountData(JsonElement json)
        {
            try
            {
                if (!json.TryGetProperty("data", out var data))
                    return;

                var balanceItems = new List<SBalanceItem>();
                var timestamp = json.GetInt64OrDefault("ts", TimeExtension.UnixTime);

                if (data.TryGetProperty("list", out var list))
                {
                    foreach (var item in list.EnumerateArray())
                    {
                        var currency = item.GetStringOrDefault("currency");
                        var balance = item.GetDecimalOrDefault("balance");
                        var available = item.GetDecimalOrDefault("available");

                        if (balance > 0 || available > 0)
                        {
                            balanceItems.Add(new SBalanceItem
                            {
                                currency = currency.ToUpper(),
                                free = available,
                                used = balance - available,
                                total = balance,
                                updateTime = timestamp
                            });
                        }
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
            catch (Exception ex)
            {
                RaiseError($"Account processing error: {ex.Message}");
            }
        }

        private async Task ProcessOrderData(JsonElement json)
        {
            try
            {
                if (!json.TryGetProperty("data", out var data))
                    return;

                var orderList = new List<SOrderItem>();
                var timestamp = json.GetInt64OrDefault("ts", TimeExtension.UnixTime);

                var order = new SOrderItem
                {
                    orderId = data.GetStringOrDefault("orderId"),
                    clientOrderId = data.GetStringOrDefault("clientOrderId"),
                    symbol = ConvertToStandardSymbol(data.GetStringOrDefault("symbol")),
                    side = data.GetStringOrDefault("type").Contains("buy") ? OrderSide.Buy : OrderSide.Sell,
                    type = ConvertOrderType(data.GetStringOrDefault("type")),
                    status = ConvertOrderStatus(data.GetStringOrDefault("orderStatus")),
                    price = data.GetDecimalOrDefault("orderPrice"),
                    quantity = data.GetDecimalOrDefault("orderSize"),
                    filledQuantity = data.GetDecimalOrDefault("tradeVolume"),
                    createTime = data.GetInt64OrDefault("orderCreateTime", timestamp),
                    updateTime = timestamp
                };

                orderList.Add(order);

                var orders = new SOrder
                {
                    exchange = ExchangeName,
                    timestamp = timestamp,
                    orders = orderList
                };

                InvokeOrderCallback(orders);
            }
            catch (Exception ex)
            {
                RaiseError($"Order processing error: {ex.Message}");
            }
        }

        public override async Task<bool> SubscribeOrderbookAsync(string symbol)
        {
            try
            {
                var huobiSymbol = ConvertToHuobiSymbol(symbol);
                var subscription = new
                {
                    sub = $"market.{huobiSymbol}.depth.step0",
                    id = $"id{_nextId++}"
                };

                await SendMessageAsync(JsonSerializer.Serialize(subscription));

                var key = CreateSubscriptionKey("orderbook", symbol);
                _subscriptions[key] = new SubscriptionInfo
                {
                    Channel = $"market.{huobiSymbol}.depth.step0",
                    Symbol = symbol,
                    SubscribedAt = DateTime.UtcNow,
                    IsActive = true
                };

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
                var huobiSymbol = ConvertToHuobiSymbol(symbol);
                var subscription = new
                {
                    sub = $"market.{huobiSymbol}.trade.detail",
                    id = $"id{_nextId++}"
                };

                await SendMessageAsync(JsonSerializer.Serialize(subscription));

                var key = CreateSubscriptionKey("trades", symbol);
                _subscriptions[key] = new SubscriptionInfo
                {
                    Channel = $"market.{huobiSymbol}.trade.detail",
                    Symbol = symbol,
                    SubscribedAt = DateTime.UtcNow,
                    IsActive = true
                };

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
                var huobiSymbol = ConvertToHuobiSymbol(symbol);
                var subscription = new
                {
                    sub = $"market.{huobiSymbol}.detail",
                    id = $"id{_nextId++}"
                };

                await SendMessageAsync(JsonSerializer.Serialize(subscription));

                var key = CreateSubscriptionKey("ticker", symbol);
                _subscriptions[key] = new SubscriptionInfo
                {
                    Channel = $"market.{huobiSymbol}.detail",
                    Symbol = symbol,
                    SubscribedAt = DateTime.UtcNow,
                    IsActive = true
                };

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
            try
            {
                var huobiSymbol = ConvertToHuobiSymbol(symbol);
                var huobiInterval = ConvertToHuobiInterval(interval);
                
                var subscription = new
                {
                    sub = $"market.{huobiSymbol}.kline.{huobiInterval}",
                    id = $"id{_nextId++}"
                };

                await SendMessageAsync(JsonSerializer.Serialize(subscription));

                var key = CreateSubscriptionKey($"kline.{huobiInterval}", symbol);
                _subscriptions[key] = new SubscriptionInfo
                {
                    Channel = $"market.{huobiSymbol}.kline.{huobiInterval}",
                    Symbol = symbol,
                    SubscribedAt = DateTime.UtcNow,
                    IsActive = true
                };

                return true;
            }
            catch (Exception ex)
            {
                RaiseError($"Candles subscription error: {ex.Message}");
                return false;
            }
        }

        public override async Task<bool> UnsubscribeAsync(string channel, string symbol)
        {
            try
            {
                var huobiSymbol = ConvertToHuobiSymbol(symbol);
                var unsubscription = new
                {
                    unsub = channel.Contains("market.") ? channel : $"market.{huobiSymbol}.{channel}",
                    id = $"id{_nextId++}"
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
            return JsonSerializer.Serialize(new { ping = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() });
        }

        protected override string CreateAuthenticationMessage(string apiKey, string secretKey)
        {
            // Huobi authentication is more complex and requires signing
            // This is a simplified version - actual implementation would need proper signing
            var timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss");
            
            return JsonSerializer.Serialize(new
            {
                action = "req",
                ch = "auth",
                @params = new
                {
                    authType = "api",
                    accessKey = apiKey,
                    signatureMethod = "HmacSHA256",
                    signatureVersion = "2.1",
                    timestamp = timestamp,
                    signature = GenerateSignature(secretKey, timestamp)
                }
            });
        }

        private string GenerateSignature(string secretKey, string timestamp)
        {
            // Simplified signature generation - actual implementation would be more complex
            var message = $"GET\napi.huobi.pro\n/ws/v2\nAccessKeyId={timestamp}";
            var encoding = new System.Text.ASCIIEncoding();
            byte[] keyByte = encoding.GetBytes(secretKey);
            byte[] messageBytes = encoding.GetBytes(message);
            using (var hmacsha256 = new System.Security.Cryptography.HMACSHA256(keyByte))
            {
                byte[] hashmessage = hmacsha256.ComputeHash(messageBytes);
                return Convert.ToBase64String(hashmessage);
            }
        }

        private string ConvertToHuobiSymbol(string symbol)
        {
            // Convert from standard format (BTC/USDT) to Huobi format (btcusdt)
            return symbol.Replace("/", "").ToLower();
        }

        private string ConvertToStandardSymbol(string huobiSymbol)
        {
            // Convert from Huobi format (btcusdt) to standard format (BTC/USDT)
            huobiSymbol = huobiSymbol.ToUpper();
            
            string[] quoteCurrencies = { "USDT", "HUSD", "BTC", "ETH", "HT", "TRX", "USDC" };
            
            foreach (var quote in quoteCurrencies)
            {
                if (huobiSymbol.EndsWith(quote))
                {
                    var baseCurrency = huobiSymbol.Substring(0, huobiSymbol.Length - quote.Length);
                    return $"{baseCurrency}/{quote}";
                }
            }
            
            return huobiSymbol;
        }

        private string ExtractSymbolFromChannel(string channel)
        {
            // Extract symbol from channel string like "market.btcusdt.depth.step0"
            if (string.IsNullOrEmpty(channel))
                return null;

            var parts = channel.Split('.');
            if (parts.Length >= 2)
                return parts[1];

            return null;
        }

        private string ConvertToHuobiInterval(string interval)
        {
            return interval switch
            {
                "1m" => "1min",
                "5m" => "5min",
                "15m" => "15min",
                "30m" => "30min",
                "1h" => "60min",
                "4h" => "4hour",
                "1d" => "1day",
                "1w" => "1week",
                "1M" => "1mon",
                _ => "1min"
            };
        }

        private string ConvertInterval(string huobiInterval)
        {
            return huobiInterval switch
            {
                "1min" => "1m",
                "5min" => "5m",
                "15min" => "15m",
                "30min" => "30m",
                "60min" => "1h",
                "4hour" => "4h",
                "1day" => "1d",
                "1week" => "1w",
                "1mon" => "1M",
                _ => "1m"
            };
        }

        private OrderType ConvertOrderType(string type)
        {
            if (type == null) return OrderType.Limit;
            
            if (type.Contains("market"))
                return OrderType.Market;
            
            return OrderType.Limit;
        }

        private OrderStatus ConvertOrderStatus(string status)
        {
            return status switch
            {
                "created" => OrderStatus.New,
                "submitted" => OrderStatus.Open,
                "partial-filled" => OrderStatus.PartiallyFilled,
                "filled" => OrderStatus.Filled,
                "partial-canceled" => OrderStatus.Canceled,
                "canceled" => OrderStatus.Canceled,
                _ => OrderStatus.Open
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
                "1d" => 86400000,
                "1w" => 604800000,
                "1M" => 2592000000,
                _ => 60000
            };
        }

        private decimal CalculatePercentage(decimal open, decimal close)
        {
            if (open == 0)
                return 0;

            return ((close - open) / open) * 100;
        }
    }
}