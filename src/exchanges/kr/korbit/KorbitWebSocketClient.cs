using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CCXT.Collector.Core.Abstractions;
using CCXT.Collector.Service;
using System.Text.Json;
using CCXT.Collector.Library;
using CCXT.Collector.Models.WebSocket;

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
     *     wss://ws.korbit.co.kr/v1/user/push
     *     wss://ws2.korbit.co.kr/v1/user/push (Alternative)
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
        protected override string WebSocketUrl => "wss://ws.korbit.co.kr/v1/user/push";
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

                // Handle different message types
                var eventType = json.GetStringOrDefault("event");
                if (!string.IsNullOrEmpty(eventType))
                {
                    switch (eventType)
                    {
                        case "korbit:connected":
                            // Connection established
                            break;
                        case "korbit:subscribe":
                            // Subscription confirmed
                            break;
                        case "korbit:push-orderbook":
                            await ProcessOrderbook(json);
                            break;
                        case "korbit:push-transaction":
                            await ProcessTransaction(json);
                            break;
                        case "korbit:push-ticker":
                            await ProcessTickerData(json);
                            break;
                    }
                }
                else if (json.TryGetProperty("data", out var dataProp))
                {
                    var channel = json.GetStringOrDefault("channel");
                    
                    if (!string.IsNullOrEmpty(channel))
                    {
                        if (channel.Contains("orderbook"))
                            await ProcessOrderbookData(json);
                        else if (channel.Contains("transaction"))
                            await ProcessTransactionData(json);
                        else if (channel.Contains("ticker"))
                            await ProcessTickerData(json);
                    }
                }
            }
            catch (Exception ex)
            {
                RaiseError($"Error processing message: {ex.Message}");
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
                var data = json.TryGetProperty("data", out var dataProp) ? dataProp : json;
                var channel = json.GetStringOrDefault("channel", "");
                
                // Extract symbol from channel name (e.g., "orderbook:btc_krw")
                var korbitSymbol = "";
                if (channel.Contains(":"))
                {
                    korbitSymbol = channel.Split(':')[1];
                }
                else 
                {
                    korbitSymbol = data.GetStringOrDefault("currency_pair");
                }
                
                if (string.IsNullOrEmpty(korbitSymbol)) return;

                var symbol = ConvertSymbolBack(korbitSymbol);
                var timestamp = data.GetInt64OrDefault("timestamp", TimeExtension.UnixTime);

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

                // Process bids
                if (data.TryGetArray("bids", out var bids))
                {
                    foreach (var bid in bids.EnumerateArray())
                    {
                        if (bid.EnumerateArray().Count() < 2)
                            continue;

                        orderbook.result.bids.Add(new SOrderBookItem
                        {
                            price = bid[0].GetDecimalValue(),
                            quantity = bid[1].GetDecimalValue()
                        });
                    }
                }

                // Process asks
                if (data.TryGetArray("asks", out var asks))
                {
                    foreach (var ask in asks.EnumerateArray())
                    {
                        if (ask.EnumerateArray().Count() < 2)
                            continue;

                        orderbook.result.asks.Add(new SOrderBookItem
                        {
                            price = ask[0].GetDecimalValue(),
                            quantity = ask[1].GetDecimalValue()
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
                var data = json.TryGetProperty("data", out var dataProp) ? dataProp : json;
                var channel = json.GetStringOrDefault("channel", "");
                
                // Extract symbol from channel name
                var korbitSymbol = "";
                if (channel.Contains(":"))
                {
                    korbitSymbol = channel.Split(':')[1];
                }
                else
                {
                    korbitSymbol = data.GetStringOrDefault("currency_pair");
                }
                
                if (string.IsNullOrEmpty(korbitSymbol)) return;

                var symbol = ConvertSymbolBack(korbitSymbol);
                var timestamp = TimeExtension.UnixTime;

                var completeOrders = new STrade
                {
                    exchange = ExchangeName,
                    symbol = symbol,
                    timestamp = timestamp,
                    result = new List<STradeItem>()
                };

                // Process transaction list
                if (data.TryGetArray("transactions", out var transactions))
                {
                    foreach (var tx in transactions.EnumerateArray())
                    {
                        var txTimestamp = tx.GetInt64OrDefault("timestamp", timestamp);
                        var side = tx.GetStringOrDefault("type", "buy");

                        completeOrders.result.Add(new STradeItem
                        {
                            tradeId = tx.GetStringOrDefault("tid", Guid.NewGuid().ToString()),
                            sideType = side.ToLower() == "buy" ? SideType.Bid : SideType.Ask,
                            orderType = OrderType.Limit,
                            price = tx.GetDecimalOrDefault("price"),
                            quantity = tx.GetDecimalOrDefault("amount"),
                            amount = (tx.GetDecimalOrDefault("price")) * (tx.GetDecimalOrDefault("amount")),
                            timestamp = txTimestamp
                        });
                    }
                }
                else if (data.TryGetProperty("tid", out var _))
                {
                    // Single transaction
                    var txTimestamp = data.GetInt64OrDefault("timestamp", timestamp);
                    var side = data.GetStringOrDefault("type", "buy");

                    completeOrders.result.Add(new STradeItem
                    {
                        tradeId = data.GetStringOrDefault("tid", Guid.NewGuid().ToString()),
                        sideType = side.ToLower() == "buy" ? SideType.Bid : SideType.Ask,
                        orderType = OrderType.Limit,
                        price = data.GetDecimalOrDefault("price"),
                        quantity = data.GetDecimalOrDefault("amount"),
                        amount = (data.GetDecimalOrDefault("price")) * (data.GetDecimalOrDefault("amount")),
                        timestamp = txTimestamp
                    });
                }

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
                var data = json.TryGetProperty("data", out var dataProp) ? dataProp : json;
                var channel = json.GetStringOrDefault("channel", "");
                
                // Extract symbol from channel name
                var korbitSymbol = "";

                if (channel.Contains(":"))
                {
                    korbitSymbol = channel.Split(':')[1];
                }
                else
                {
                    korbitSymbol = data.GetStringOrDefault("currency_pair");
                }
                
                if (string.IsNullOrEmpty(korbitSymbol)) return;

                var symbol = ConvertSymbolBack(korbitSymbol);
                var timestamp = data.GetInt64OrDefault("timestamp", TimeExtension.UnixTime);

                var ticker = new STicker
                {
                    exchange = ExchangeName,
                    symbol = symbol,
                    timestamp = timestamp,
                    result = new STickerItem
                    {
                        timestamp = timestamp,
                        closePrice = data.GetDecimalOrDefault("last"),
                        openPrice = data.GetDecimalOrDefault("open"),
                        highPrice = data.GetDecimalOrDefault("high"),
                        lowPrice = data.GetDecimalOrDefault("low"),
                        volume = data.GetDecimalOrDefault("volume"),
                        quoteVolume = 0,
                        percentage = data.GetDecimalOrDefault("change_percent"),
                        change = data.GetDecimalOrDefault("change"),
                        bidPrice = data.GetDecimalOrDefault("bid"),
                        askPrice = data.GetDecimalOrDefault("ask"),
                        vwap = 0,
                        prevClosePrice = 0,
                        bidQuantity = 0,
                        askQuantity = 0
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
                var subscribeMessage = new
                {
                    @event = "korbit:subscribe",
                    data = new
                    {
                        channels = new[] { $"orderbook:{korbitSymbol}" }
                    }
                };

                var json = JsonSerializer.Serialize(subscribeMessage);
                await SendMessageAsync(json);
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
                var subscribeMessage = new
                {
                    @event = "korbit:subscribe",
                    data = new
                    {
                        channels = new[] { $"orderbook:{korbitSymbol}" }
                    }
                };

                var json = JsonSerializer.Serialize(subscribeMessage);
                await SendMessageAsync(json);
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
                var subscribeMessage = new
                {
                    @event = "korbit:subscribe",
                    data = new
                    {
                        channels = new[] { $"transaction:{korbitSymbol}" }
                    }
                };

                var json = JsonSerializer.Serialize(subscribeMessage);
                await SendMessageAsync(json);
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
                var subscribeMessage = new
                {
                    @event = "korbit:subscribe",
                    data = new
                    {
                        channels = new[] { $"transaction:{korbitSymbol}" }
                    }
                };

                var json = JsonSerializer.Serialize(subscribeMessage);
                await SendMessageAsync(json);
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
                var subscribeMessage = new
                {
                    @event = "korbit:subscribe",
                    data = new
                    {
                        channels = new[] { $"ticker:{korbitSymbol}" }
                    }
                };

                var json = JsonSerializer.Serialize(subscribeMessage);
                await SendMessageAsync(json);
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
                var subscribeMessage = new
                {
                    @event = "korbit:subscribe",
                    data = new
                    {
                        channels = new[] { $"ticker:{korbitSymbol}" }
                    }
                };

                var json = JsonSerializer.Serialize(subscribeMessage);
                await SendMessageAsync(json);
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
                var channelName = channel.ToLower() switch
                {
                    "orderbook" => $"orderbook:{korbitSymbol}",
                    "trades" => $"transaction:{korbitSymbol}",
                    "ticker" => $"ticker:{korbitSymbol}",
                    _ => $"{channel}:{korbitSymbol}"
                };

                var unsubscribeMessage = new
                {
                    @event = "korbit:unsubscribe",
                    data = new
                    {
                        channels = new[] { channelName }
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
                var channelName = channel.ToLower() switch
                {
                    "orderbook" => $"orderbook:{korbitSymbol}",
                    "trades" => $"transaction:{korbitSymbol}",
                    "ticker" => $"ticker:{korbitSymbol}",
                    _ => $"{channel}:{korbitSymbol}"
                };

                var unsubscribeMessage = new
                {
                    @event = "korbit:unsubscribe",
                    data = new
                    {
                        channels = new[] { channelName }
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
                var pingMessage = new
                {
                    @event = "korbit:ping"
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
        /// Send batch subscriptions for Korbit - multiple symbols comma-separated per channel type
        /// Example: 'ticker:btc_krw,eth_krw,xrp_krw'
        /// </summary>
        protected override async Task<bool> SendBatchSubscriptionsAsync(List<KeyValuePair<string, SubscriptionInfo>> subscriptions)
        {
            try
            {
                // Group subscriptions by channel type
                var groupedByChannel = subscriptions
                    .GroupBy(s => s.Value.Channel.ToLower())
                    .ToList();

                // Build channels array with all subscriptions
                var channels = new List<string>();

                foreach (var channelGroup in groupedByChannel)
                {
                    var channel = channelGroup.Key;
                    var symbols = channelGroup
                        .Select(s => ConvertSymbol(s.Value.Symbol))
                        .Distinct()
                        .ToList();

                    if (symbols.Count == 0)
                        continue;

                    // Map channel names to Korbit channel types
                    string korbitChannel = channel switch
                    {
                        "orderbook" or "depth" => "orderbook",
                        "trades" or "trade" => "transaction",
                        "ticker" => "ticker",
                        _ => channel
                    };

                    // Create channel string with comma-separated symbols
                    // Example: "ticker:btc_krw,eth_krw,xrp_krw"
                    var channelString = $"{korbitChannel}:{string.Join(",", symbols)}";
                    channels.Add(channelString);

                    RaiseError($"Added {korbitChannel} subscription for {symbols.Count} markets: {string.Join(", ", symbols.Take(3))}{(symbols.Count > 3 ? "..." : "")}");
                }

                if (channels.Count == 0)
                    return true;

                // Create subscription message with all channels
                var subscribeMessage = new
                {
                    accessToken = (string)null,  // null for public channels
                    timestamp = TimeExtension.UnixTime,
                    @event = "korbit:subscribe",
                    data = new
                    {
                        channels = channels.ToArray()
                    }
                };

                // Send the batch subscription
                var json = JsonSerializer.Serialize(subscribeMessage);
                await SendMessageAsync(json);

                RaiseError($"Sent Korbit batch subscription with {channels.Count} channel groups");

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
