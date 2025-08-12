using CCXT.Collector.Core.Abstractions;
using CCXT.Collector.Models.WebSocket;
using CCXT.Collector.Service;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace CCXT.Collector.Korbit
{
    /*
     * Korbit Exchange (Korea's First Bitcoin Exchange)
     * 
     * API Documentation:
     *     https://apidocs.korbit.co.kr/
     *     https://apidocs.korbit.co.kr/ko/#websocket-api
     * 
     * WebSocket API:
     *     https://docs.korbit.co.kr/#WebSocket
     *     https://docs.korbit.co.kr/#WS-method-subscribe_type-ticker
     *     https://docs.korbit.co.kr/#WS-method-subscribe_type-myOrder
     * 
     * Supported Markets: KRW
     * 
     * Rate Limits:
     *     - REST API: 60 requests per minute
     *     - WebSocket: No specific limit
     * 
     * Features:
     *     - Real-time orderbook updates
     *     - Trade stream
     *     - Ticker updates
     *     - Transaction data
     */

    /// <summary>
    /// Korbit WebSocket client for real-time market data streaming
    /// </summary>
    public class KorbitWebSocketClient : WebSocketClientBase
    {
        private readonly Dictionary<string, SOrderBook> _orderbookCache;
        private readonly object _lockObject = new object();

        public override string ExchangeName => "Korbit";
        protected override string WebSocketUrl => "wss://ws-api.korbit.co.kr/v2/public";
        protected override int PingIntervalMs => 60000; // 60 seconds

        public KorbitWebSocketClient()
        {
            _orderbookCache = new Dictionary<string, SOrderBook>();
        }

        private string ConvertSymbol(string symbol)
        {
            // Convert format: BTC/KRW -> btc_krw
            // Korbit uses lowercase with underscore separator
            var parts = symbol.Split('/');
            if (parts.Length == 2)
            {
                var baseCoin = parts[0].ToLower();  // btc
                var quoteCoin = parts[1].ToLower(); // krw
                return $"{baseCoin}_{quoteCoin}";
            }

            return symbol.Replace("/", "_").ToLower();
        }

        private string ConvertSymbolBack(string korbitSymbol)
        {
            // Convert format: btc_krw -> BTC/KRW
            var parts = korbitSymbol.Split('_');
            if (parts.Length == 2)
            {
                var baseCoin = parts[0].ToUpper();  // BTC
                var quoteCoin = parts[1].ToUpper(); // KRW
                return $"{baseCoin}/{quoteCoin}";
            }

            return korbitSymbol.Replace("_", "/").ToUpper();
        }

        /// <summary>
        /// Formats a Market object to Korbit-specific symbol format
        /// </summary>
        /// <param name="market">Market to format</param>
        /// <returns>Formatted symbol (e.g., "btc_krw")</returns>
        protected override string FormatSymbol(Market market)
        {
            // Korbit uses underscore separator with lowercase: base_quote
            return $"{market.Base.ToLower()}_{market.Quote.ToLower()}";
        }

        protected override async Task ProcessMessageAsync(string message, bool isPrivate = false)
        {
            try
            {
                using var doc = JsonDocument.Parse(message); 
                var json = doc.RootElement;

                // Check for error messages
                if (json.TryGetProperty("error", out var errorProp))
                {
                    var error = json.GetStringOrDefault("error");
                    RaiseError($"Korbit error: {error}");
                    return;
                }

                // Handle different message types based on 'type' field
                var messageType = json.GetStringOrDefault("type");
                if (!string.IsNullOrEmpty(messageType))
                {
                    switch (messageType)
                    {
                        case "orderbook":
                            await ProcessOrderbookData(json);
                            break;
                        case "trade":
                            await ProcessTransactionData(json);
                            break;
                        case "ticker":
                            await ProcessTickerData(json);
                            break;
                        case "subscribe":
                            // Subscription confirmed
                            HandleSubscriptionConfirmation(json);
                            break;
                        case "pong":
                            // Pong received, connection is alive
                            break;
                        default:
                            // Unknown message type, ignore
                            break;
                    }
                }
                else
                {
                    // Check for success/error responses
                    if (json.TryGetProperty("success", out var success))
                    {
                        var isSuccess = success.ValueKind == JsonValueKind.True;
                        if (!isSuccess && json.TryGetProperty("message", out var msg))
                        {
                            RaiseError($"Korbit error: {msg.GetString()}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                RaiseError($"Error processing message: {ex.Message}");
            }
        }

        private void HandleSubscriptionConfirmation(JsonElement json)
        {
            try
            {
                // v2 API returns success and message
                if (json.TryGetProperty("success", out var success))
                {
                    var isSuccess = success.ValueKind == JsonValueKind.True;
                    RaiseError($"[INFO] Subscription confirmed: {isSuccess}");
                    
                    if (!isSuccess && json.TryGetProperty("message", out var msg))
                    {
                        RaiseError($"[ERROR] Subscription failed: {msg.GetString()}");
                    }
                }
                
                // Get type and symbols from the subscription response
                var type = json.GetStringOrDefault("type");
                if (json.TryGetArray("symbols", out var symbols))
                {
                    foreach (var korbitSymbol in symbols.EnumerateArray())
                    {
                        var symStr = korbitSymbol.GetString();
                        if (!string.IsNullOrEmpty(symStr))
                        {
                            var symbol = ConvertSymbolBack(symStr);
                            var channelName = type switch
                            {
                                "orderbook" => "orderbook",
                                "trade" => "trades",
                                "ticker" => "ticker",
                                _ => type
                            };
                            MarkSubscriptionActive(channelName, symbol);
                            RaiseError($"[INFO] Marked {channelName} subscription active for {symbol}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                RaiseError($"Error handling subscription confirmation: {ex.Message}");
            }
        }

        private async Task ProcessOrderbook(JsonElement json)
        {
            await ProcessOrderbookData(json);
        }

        private async Task ProcessOrderbookData(JsonElement json)
        {
            try
            {
                // v2 API format: {"type":"orderbook","timestamp":1700000027754,"symbol":"btc_krw","snapshot":true,"data":{...}}
                var korbitSymbol = json.GetStringOrDefault("symbol");
                if (string.IsNullOrEmpty(korbitSymbol)) return;

                var symbol = ConvertSymbolBack(korbitSymbol);
                var timestamp = json.GetInt64OrDefault("timestamp", TimeExtension.UnixTime);
                var isSnapshot = json.GetBooleanOrFalse("snapshot");
                
                // Get the data object
                if (!json.TryGetProperty("data", out var data))
                {
                    RaiseError($"[ERROR] No data property in orderbook message");
                    return;
                }

                var orderbook = new SOrderBook
                {
                    exchange = ExchangeName,
                    symbol = symbol,
                    timestamp = timestamp,
                    result = new SOrderBookData
                    {
                        timestamp = timestamp,
                        bids = new List<SOrderBookItem>(),
                        asks = new List<SOrderBookItem>()
                    }
                };

                // Process bids - v2 API returns objects with price and qty properties
                if (data.TryGetArray("bids", out var bids))
                {
                    foreach (var bid in bids.EnumerateArray())
                    {
                        orderbook.result.bids.Add(new SOrderBookItem
                        {
                            price = bid.GetDecimalOrDefault("price"),
                            quantity = bid.GetDecimalOrDefault("qty")
                        });
                    }
                }

                // Process asks - v2 API returns objects with price and qty properties
                if (data.TryGetArray("asks", out var asks))
                {
                    foreach (var ask in asks.EnumerateArray())
                    {
                        orderbook.result.asks.Add(new SOrderBookItem
                        {
                            price = ask.GetDecimalOrDefault("price"),
                            quantity = ask.GetDecimalOrDefault("qty")
                        });
                    }
                }

                // Sort orderbook
                orderbook.result.bids = orderbook.result.bids.OrderByDescending(x => x.price).ToList();
                orderbook.result.asks = orderbook.result.asks.OrderBy(x => x.price).ToList();

                // Cache and invoke callback
                lock (_lockObject)
                {
                    _orderbookCache[symbol] = orderbook;
                }
                InvokeOrderbookCallback(orderbook);
            }
            catch (Exception ex)
            {
                RaiseError($"Error processing orderbook: {ex.Message}");
            }
        }

        private async Task ProcessTransaction(JsonElement json)
        {
            await ProcessTransactionData(json);
        }

        private async Task ProcessTransactionData(JsonElement json)
        {
            try
            {
                // v2 API format: {"type":"trade","timestamp":1700000027754,"symbol":"btc_krw","data":{...}}
                var korbitSymbol = json.GetStringOrDefault("symbol");
                if (string.IsNullOrEmpty(korbitSymbol)) return;

                var symbol = ConvertSymbolBack(korbitSymbol);
                var timestamp = json.GetInt64OrDefault("timestamp", TimeExtension.UnixTime);
                
                // Get the data object/array
                if (!json.TryGetProperty("data", out var data))
                {
                    RaiseError($"[ERROR] No data property in trade message");
                    return;
                }

                var completeOrders = new STrade
                {
                    exchange = ExchangeName,
                    symbol = symbol,
                    timestamp = timestamp,
                    result = new List<STradeItem>()
                };

                // Process trade data - v2 API single trade format
                // Data contains: {"id":"12345","price":"99000000","amount":"0.01","type":"buy"|"sell","timestamp":1700000027754}
                var tradeId = data.GetStringOrDefault("id", Guid.NewGuid().ToString());
                var price = data.GetDecimalOrDefault("price");
                var amount = data.GetDecimalOrDefault("amount");
                var side = data.GetStringOrDefault("type", "");
                var tradeTimestamp = data.GetInt64OrDefault("timestamp", timestamp);
                
                var sideType = side.ToLower() switch
                {
                    "buy" => SideType.Bid,
                    "sell" => SideType.Ask,
                    _ => SideType.Unknown
                };

                completeOrders.result.Add(new STradeItem
                {
                    tradeId = tradeId,
                    sideType = sideType,
                    orderType = OrderType.Limit,
                    price = price,
                    quantity = amount,
                    amount = price * amount,
                    timestamp = tradeTimestamp
                });

                if (completeOrders.result.Count > 0)
                    InvokeTradeCallback(completeOrders);
            }
            catch (Exception ex)
            {
                RaiseError($"Error processing transaction: {ex.Message}");
            }
        }

        private async Task ProcessTickerData(JsonElement json)
        {
            try
            {
                // v2 API format: {"type":"ticker","timestamp":1700000027754,"symbol":"btc_krw","snapshot":true,"data":{...}}
                var korbitSymbol = json.GetStringOrDefault("symbol");
                if (string.IsNullOrEmpty(korbitSymbol)) return;

                var symbol = ConvertSymbolBack(korbitSymbol);
                var timestamp = json.GetInt64OrDefault("timestamp", TimeExtension.UnixTime);
                
                // Get the data object
                if (!json.TryGetProperty("data", out var data))
                {
                    RaiseError($"[ERROR] No data property in ticker message");
                    return;
                }

                // v2 API ticker data contains: close, open, high, low, volume, etc.
                var ticker = new STicker
                {
                    exchange = ExchangeName,
                    symbol = symbol,
                    timestamp = timestamp,
                    result = new STickerItem
                    {
                        timestamp = timestamp,
                        closePrice = data.GetDecimalOrDefault("close"),
                        openPrice = data.GetDecimalOrDefault("open"),
                        highPrice = data.GetDecimalOrDefault("high"),
                        lowPrice = data.GetDecimalOrDefault("low"),
                        volume = data.GetDecimalOrDefault("volume"),
                        quoteVolume = data.GetDecimalOrDefault("value", 0), // KRW trading value
                        percentage = data.GetDecimalOrDefault("changePercent", 0),
                        change = data.GetDecimalOrDefault("change", 0),
                        bidPrice = data.GetDecimalOrDefault("bid"),
                        askPrice = data.GetDecimalOrDefault("ask"),
                        vwap = 0,
                        prevClosePrice = 0,
                        bidQuantity = data.GetDecimalOrDefault("bidVolume", 0),
                        askQuantity = data.GetDecimalOrDefault("askVolume", 0)
                    }
                };

                // Calculate previous close price if not provided
                if (ticker.result.change != 0)
                {
                    ticker.result.prevClosePrice = ticker.result.closePrice - ticker.result.change;
                }

                InvokeTickerCallback(ticker);
            }
            catch (Exception ex)
            {
                RaiseError($"Error processing ticker: {ex.Message}");
            }
        }

        public override async Task<bool> SubscribeOrderbookAsync(Market market)
        {
            try
            {
                var korbitSymbol = FormatSymbol(market);
                // v2 API requires array format
                var subscribeMessage = new[]
                {
                    new
                    {
                        method = "subscribe",
                        type = "orderbook",
                        symbols = new[] { korbitSymbol }
                    }
                };

                var json = JsonSerializer.Serialize(subscribeMessage);
                await SendMessageAsync(json);
                MarkSubscriptionActive("orderbook", market.ToString());
                return true;
            }
            catch (Exception ex)
            {
                RaiseError($"Error subscribing to orderbook: {ex.Message}");
                return false;
            }
        }

        public override async Task<bool> SubscribeOrderbookAsync(string symbol)
        {
            try
            {
                var korbitSymbol = ConvertSymbol(symbol);
                // v2 API requires array format
                var subscribeMessage = new[]
                {
                    new
                    {
                        method = "subscribe",
                        type = "orderbook",
                        symbols = new[] { korbitSymbol }
                    }
                };

                var json = JsonSerializer.Serialize(subscribeMessage);
                await SendMessageAsync(json);
                MarkSubscriptionActive("orderbook", symbol);
                return true;
            }
            catch (Exception ex)
            {
                RaiseError($"Error subscribing to orderbook: {ex.Message}");
                return false;
            }
        }

        public override async Task<bool> SubscribeTradesAsync(Market market)
        {
            try
            {
                var korbitSymbol = FormatSymbol(market);
                // v2 API requires array format with 'trade' type
                var subscribeMessage = new[]
                {
                    new
                    {
                        method = "subscribe",
                        type = "trade",
                        symbols = new[] { korbitSymbol }
                    }
                };

                var json = JsonSerializer.Serialize(subscribeMessage);
                await SendMessageAsync(json);
                MarkSubscriptionActive("trades", market.ToString());
                return true;
            }
            catch (Exception ex)
            {
                RaiseError($"Error subscribing to trades: {ex.Message}");
                return false;
            }
        }

        public override async Task<bool> SubscribeTradesAsync(string symbol)
        {
            try
            {
                var korbitSymbol = ConvertSymbol(symbol);
                // v2 API requires array format with 'trade' type
                var subscribeMessage = new[]
                {
                    new
                    {
                        method = "subscribe",
                        type = "trade",
                        symbols = new[] { korbitSymbol }
                    }
                };

                var json = JsonSerializer.Serialize(subscribeMessage);
                await SendMessageAsync(json);
                MarkSubscriptionActive("trades", symbol);
                return true;
            }
            catch (Exception ex)
            {
                RaiseError($"Error subscribing to trades: {ex.Message}");
                return false;
            }
        }

        public override async Task<bool> SubscribeTickerAsync(Market market)
        {
            try
            {
                var korbitSymbol = FormatSymbol(market);
                // v2 API requires array format
                var subscribeMessage = new[]
                {
                    new
                    {
                        method = "subscribe",
                        type = "ticker",
                        symbols = new[] { korbitSymbol }
                    }
                };

                var json = JsonSerializer.Serialize(subscribeMessage);
                await SendMessageAsync(json);
                MarkSubscriptionActive("ticker", market.ToString());
                return true;
            }
            catch (Exception ex)
            {
                RaiseError($"Error subscribing to ticker: {ex.Message}");
                return false;
            }
        }

        public override async Task<bool> SubscribeTickerAsync(string symbol)
        {
            try
            {
                var korbitSymbol = ConvertSymbol(symbol);
                // v2 API requires array format
                var subscribeMessage = new[]
                {
                    new
                    {
                        method = "subscribe",
                        type = "ticker",
                        symbols = new[] { korbitSymbol }
                    }
                };

                var json = JsonSerializer.Serialize(subscribeMessage);
                await SendMessageAsync(json);
                MarkSubscriptionActive("ticker", symbol);
                return true;
            }
            catch (Exception ex)
            {
                RaiseError($"Error subscribing to ticker: {ex.Message}");
                return false;
            }
        }

        public override async Task<bool> UnsubscribeAsync(string channel, Market market)
        {
            try
            {
                var korbitSymbol = FormatSymbol(market);
                var type = channel.ToLower() switch
                {
                    "orderbook" => "orderbook",
                    "trades" or "trade" => "trade",
                    "ticker" => "ticker",
                    _ => channel
                };

                // v2 API unsubscribe format
                var unsubscribeMessage = new[]
                {
                    new
                    {
                        method = "unsubscribe",
                        type = type,
                        symbols = new[] { korbitSymbol }
                    }
                };

                var json = JsonSerializer.Serialize(unsubscribeMessage);
                await SendMessageAsync(json);
                return true;
            }
            catch (Exception ex)
            {
                RaiseError($"Error unsubscribing: {ex.Message}");
                return false;
            }
        }

        public override async Task<bool> UnsubscribeAsync(string channel, string symbol)
        {
            try
            {
                var korbitSymbol = ConvertSymbol(symbol);
                var type = channel.ToLower() switch
                {
                    "orderbook" => "orderbook",
                    "trades" or "trade" => "trade",
                    "ticker" => "ticker",
                    _ => channel
                };

                // v2 API unsubscribe format
                var unsubscribeMessage = new[]
                {
                    new
                    {
                        method = "unsubscribe",
                        type = type,
                        symbols = new[] { korbitSymbol }
                    }
                };

                var json = JsonSerializer.Serialize(unsubscribeMessage);
                await SendMessageAsync(json);
                return true;
            }
            catch (Exception ex)
            {
                RaiseError($"Error unsubscribing: {ex.Message}");
                return false;
            }
        }

        protected override async Task SendPingAsync()
        {
            try
            {
                // v2 API ping format
                var pingMessage = new[]
                {
                    new
                    {
                        method = "ping"
                    }
                };

                var json = JsonSerializer.Serialize(pingMessage);
                await SendMessageAsync(json);
            }
            catch (Exception ex)
            {
                RaiseError($"Error sending ping: {ex.Message}");
            }
        }

        public override async Task<bool> SubscribeCandlesAsync(Market market, string interval)
        {
            try
            {
                // Korbit doesn't have a specific candles WebSocket channel in their public documentation
                // You would need to construct candles from trades or use REST API
                RaiseError("Korbit doesn't support candles via WebSocket. Use REST API instead.");
                return false;
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
                // Korbit doesn't have a specific candles WebSocket channel in their public documentation
                // You would need to construct candles from trades or use REST API
                RaiseError("Korbit doesn't support candles via WebSocket. Use REST API instead.");
                return false;
            }
            catch (Exception ex)
            {
                RaiseError($"Subscribe candles error: {ex.Message}");
                return false;
            }
        }

        #region Batch Subscription Support

        /// <summary>
        /// Korbit supports batch subscription - multiple symbols with comma separation in channel strings
        /// </summary>
        protected override bool SupportsBatchSubscription()
        {
            return true;
        }

        /// <summary>
        /// Send batch subscriptions for Korbit v2 API - multiple symbols per subscription type
        /// </summary>
        protected override async Task<bool> SendBatchSubscriptionsAsync(List<KeyValuePair<string, SubscriptionInfo>> subscriptions)
        {
            try
            {
                // Group subscriptions by channel type
                var groupedByChannel = subscriptions
                    .GroupBy(s => s.Value.Channel.ToLower())
                    .ToList();

                // Build subscription messages array
                var messages = new List<object>();

                foreach (var channelGroup in groupedByChannel)
                {
                    var channel = channelGroup.Key;
                    var symbols = channelGroup
                        .Select(s => ConvertSymbol(s.Value.Symbol))
                        .Distinct()
                        .ToList();

                    if (symbols.Count == 0)
                        continue;

                    // Map channel names to Korbit v2 types
                    string type = channel switch
                    {
                        "orderbook" or "depth" => "orderbook",
                        "trades" or "trade" => "trade",
                        "ticker" => "ticker",
                        _ => channel
                    };

                    // Create subscription message for this type with all symbols
                    messages.Add(new
                    {
                        method = "subscribe",
                        type = type,
                        symbols = symbols.ToArray()
                    });

                    RaiseError($"Added {type} subscription for {symbols.Count} markets: {string.Join(", ", symbols.Take(3))}{(symbols.Count > 3 ? "..." : "")}");
                }

                if (messages.Count == 0)
                    return true;

                // Send all subscriptions in one array
                var json = JsonSerializer.Serialize(messages.ToArray());
                await SendMessageAsync(json);

                RaiseError($"Sent Korbit batch subscription with {messages.Count} message groups");

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
