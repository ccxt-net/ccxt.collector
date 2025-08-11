using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using CCXT.Collector.Core.Abstractions;
using CCXT.Collector.Library;
using CCXT.Collector.Service;
using CCXT.Collector.Models.WebSocket;
using System.Text.Json;

namespace CCXT.Collector.Bithumb
{
    /*
     * Bithumb Exchange (One of Korea's Largest Exchanges)
     * 
     * API Documentation:
     *     https://apidocs.bithumb.com/
     *     https://api.bithumb.com/
     * 
     * WebSocket API:
     *     https://apidocs.bithumb.com/v2.1.5/reference/%EA%B8%B0%EB%B3%B8-%EC%A0%95%EB%B3%B4
     *     https://apidocs.bithumb.com/v2.1.5/reference/%EC%9A%94%EC%B2%AD-%ED%8F%AC%EB%A7%B7
     *     https://apidocs.bithumb.com/v2.1.5/reference/%ED%98%84%EC%9E%AC%EA%B0%80-ticker
     *     https://apidocs.bithumb.com/v2.1.5/reference/%EC%B2%B4%EA%B2%B0-trade
     *     https://apidocs.bithumb.com/v2.1.5/reference/%ED%98%B8%EA%B0%80-orderbook
     *     https://apidocs.bithumb.com/v2.1.5/reference/%EB%82%B4-%EC%A3%BC%EB%AC%B8-%EB%B0%8F-%EC%B2%B4%EA%B2%B0-myorder
     *     https://apidocs.bithumb.com/v2.1.5/reference/%EB%82%B4-%EC%9E%90%EC%82%B0-myasset
     * 
     * Supported Markets: KRW
     * 
     * Rate Limits:
     *     - REST API: 1,400 requests per second
     *     - WebSocket: No specific limit
     * 
     * Features:
     *     - Real-time orderbook depth updates
     *     - Trade stream (transaction)
     *     - Ticker updates (24H, 30M, 1H, 12H, MID)
     *     - Incremental orderbook updates
     */

    /// <summary>
    /// Bithumb WebSocket client for real-time market data streaming
    /// </summary>
    public class BithumbWebSocketClient : WebSocketClientBase
    {
        private readonly Dictionary<string, SOrderBook> _orderbookCache;
        private readonly Dictionary<string, List<SOrderBookItem>> _localOrderbook;
        private readonly object _lockObject = new object();

        public override string ExchangeName => "Bithumb";
        protected override string WebSocketUrl => "wss://pubwss.bithumb.com/pub/ws";
        protected override int PingIntervalMs => 30000; // 30 seconds

        public BithumbWebSocketClient()
        {
            _orderbookCache = new Dictionary<string, SOrderBook>();
            _localOrderbook = new Dictionary<string, List<SOrderBookItem>>();
        }

        private string ConvertSymbol(string symbol)
        {
            // Convert format: BTC/KRW -> BTC_KRW
            // Bithumb uses underscore separator with uppercase
            var parts = symbol.Split('/');
            if (parts.Length == 2)
            {
                var baseCoin = parts[0].ToUpper();  // BTC
                var quoteCoin = parts[1].ToUpper(); // KRW
                return $"{baseCoin}_{quoteCoin}";
            }

            return symbol.Replace("/", "_").ToUpper();
        }

        private string ConvertSymbolBack(string bithumbSymbol)
        {
            // Convert format: BTC_KRW -> BTC/KRW
            var parts = bithumbSymbol.Split('_');
            if (parts.Length == 2)
            {
                var baseCoin = parts[0];  // BTC
                var quoteCoin = parts[1]; // KRW
                return $"{baseCoin}/{quoteCoin}";
            }

            return bithumbSymbol.Replace("_", "/");
        }

        /// <summary>
        /// Formats a Market object to Bithumb-specific symbol format
        /// </summary>
        /// <param name="market">Market to format</param>
        /// <returns>Formatted symbol (e.g., "BTC_KRW")</returns>
        protected override string FormatSymbol(Market market)
        {
            // Bithumb uses underscore separator with uppercase: BASE_QUOTE
            return $"{market.Base.ToUpper()}_{market.Quote.ToUpper()}";
        }

        protected override async Task ProcessMessageAsync(string message, bool isPrivate = false)
        {
            try
            {
                using var doc = JsonDocument.Parse(message); 
                var json = doc.RootElement;

                // Check for status messages
                if (json.TryGetProperty("status", out var statusProp))
                {
                    var status = json.GetStringOrDefault("status");
                    if (status != "0000")
                    {
                        var errorMsg = json.GetStringOrDefault("msg", "Unknown error");
                        RaiseError($"Bithumb error status: {status}, message: {errorMsg}");
                        return;
                    }
                }

                // Handle different message types
                if (json.TryGetProperty("type", out var typeProp))
                {
                    if (!json.TryGetProperty("content", out var content))
                        return;

                    var messageType = json.GetStringOrDefault("type");

                    switch (messageType)
                    {
                        case "ticker":
                            await ProcessTickerData(content);
                            break;
                        case "orderbookdepth":
                            await ProcessOrderbookDepth(content);
                            break;
                        case "transaction":
                            await ProcessTransaction(content);
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                RaiseError($"Error processing message: {ex.Message}");
            }
        }

        private async Task ProcessTickerData(JsonElement content)
        {
            try
            {
                var bithumbSymbol = content.GetStringOrDefault("symbol");
                if (String.IsNullOrEmpty(bithumbSymbol)) return;

                var symbol = ConvertSymbolBack(bithumbSymbol);
                
                // Parse date and time to timestamp
                var date = content.GetStringOrDefault("date");
                var time = content.GetStringOrDefault("time");
                var timestamp = TimeExtension.UnixTime;
                
                if (!String.IsNullOrEmpty(date) && !String.IsNullOrEmpty(time))
                {
                    try
                    {
                        var dateTime = DateTime.ParseExact(
                            date + time,
                            "yyyyMMddHHmmss",
                            CultureInfo.InvariantCulture,
                            DateTimeStyles.AssumeLocal
                        );
                        timestamp = dateTime.ToUnixTime();
                    }
                    catch { }
                }

                var ticker = new STicker
                {
                    exchange = ExchangeName,
                    symbol = symbol,
                    timestamp = timestamp,
                    result = new STickerItem
                    {
                        timestamp = timestamp,
                        closePrice = content.GetDecimalOrDefault("closePrice"),
                        openPrice = content.GetDecimalOrDefault("openPrice"),
                        highPrice = content.GetDecimalOrDefault("highPrice"),
                        lowPrice = content.GetDecimalOrDefault("lowPrice"),
                        volume = content.GetDecimalOrDefault("volume"),
                        quoteVolume = content.GetDecimalOrDefault("value"),
                        percentage = content.GetDecimalOrDefault("chgRate"),
                        change = content.GetDecimalOrDefault("chgAmt"),
                        bidPrice = 0, // Will be updated from orderbook
                        askPrice = 0, // Will be updated from orderbook
                        vwap = 0,
                        prevClosePrice = content.GetDecimalOrDefault("prevClosePrice"),
                        bidQuantity = 0,
                        askQuantity = 0
                    }
                };

                // Try to get bid/ask from orderbook cache
                lock (_lockObject)
                {
                    if (_orderbookCache.ContainsKey(symbol))
                    {
                        var ob = _orderbookCache[symbol].result;
                        if (ob.bids.Count > 0)
                        {
                            ticker.result.bidPrice = ob.bids[0].price;
                            ticker.result.bidQuantity = ob.bids[0].quantity;
                        }
                        if (ob.asks.Count > 0)
                        {
                            ticker.result.askPrice = ob.asks[0].price;
                            ticker.result.askQuantity = ob.asks[0].quantity;
                        }
                    }
                }

                InvokeTickerCallback(ticker);
            }
            catch (Exception ex)
            {
                RaiseError($"Error processing ticker: {ex.Message}");
            }
        }

        private async Task ProcessOrderbookDepth(JsonElement content)
        {
            try
            {
                if (!(content.TryGetArray("list", out var list)))
                    return;

                // Group by symbol
                var symbolGroups = list.EnumerateArray().GroupBy(item => item.GetStringOrDefault("symbol"));

                foreach (var group in symbolGroups)
                {
                    var bithumbSymbol = group.Key;
                    if (String.IsNullOrEmpty(bithumbSymbol)) continue;

                    var symbol = ConvertSymbolBack(bithumbSymbol);
                    var timestamp = TimeExtension.UnixTime;

                    // Get or create local orderbook for incremental updates
                    if (!_localOrderbook.ContainsKey(symbol + "_asks"))
                        _localOrderbook[symbol + "_asks"] = new List<SOrderBookItem>();
                    if (!_localOrderbook.ContainsKey(symbol + "_bids"))
                        _localOrderbook[symbol + "_bids"] = new List<SOrderBookItem>();

                    var localAsks = _localOrderbook[symbol + "_asks"];
                    var localBids = _localOrderbook[symbol + "_bids"];

                    // Process incremental updates
                    foreach (var item in group)
                    {
                        var orderType = item.GetStringOrDefault("orderType");
                        var price = item.GetDecimalOrDefault("price");
                        var quantity = item.GetDecimalOrDefault("quantity");

                        if (orderType == "ask")
                        {
                            var existing = localAsks.FirstOrDefault(x => x.price == price);
                            if (existing != null)
                            {
                                if (quantity == 0)
                                    localAsks.Remove(existing);
                                else
                                    existing.quantity = quantity;
                            }
                            else if (quantity > 0)
                            {
                                localAsks.Add(new SOrderBookItem { price = price, quantity = quantity });
                            }
                        }
                        else if (orderType == "bid")
                        {
                            var existing = localBids.FirstOrDefault(x => x.price == price);
                            if (existing != null)
                            {
                                if (quantity == 0)
                                    localBids.Remove(existing);
                                else
                                    existing.quantity = quantity;
                            }
                            else if (quantity > 0)
                            {
                                localBids.Add(new SOrderBookItem { price = price, quantity = quantity });
                            }
                        }
                    }

                    // Sort and trim orderbook
                    localAsks.Sort((a, b) => a.price.CompareTo(b.price));
                    localBids.Sort((a, b) => b.price.CompareTo(a.price));

                    if (localAsks.Count > 30)
                        localAsks.RemoveRange(30, localAsks.Count - 30);
                    if (localBids.Count > 30)
                        localBids.RemoveRange(30, localBids.Count - 30);

                    // Create orderbook snapshot
                    var orderbook = new SOrderBook
                    {
                        exchange = ExchangeName,
                        symbol = symbol,
                        timestamp = timestamp,
                        result = new SOrderBookData
                        {
                            timestamp = timestamp,
                            bids = new List<SOrderBookItem>(localBids),
                            asks = new List<SOrderBookItem>(localAsks)
                        }
                    };

                    // Cache and invoke callback
                    lock (_lockObject)
                    {
                        _orderbookCache[symbol] = orderbook;
                    }
                    InvokeOrderbookCallback(orderbook);
                }
            }
            catch (Exception ex)
            {
                RaiseError($"Error processing orderbook: {ex.Message}");
            }
        }

        private async Task ProcessTransaction(JsonElement content)
        {
            try
            {
                if (!(content.TryGetArray("list", out var list)))
                    return;

                // Group by symbol
                var symbolGroups = list.EnumerateArray().GroupBy(item => item.GetStringOrDefault("symbol"));

                foreach (var group in symbolGroups)
                {
                    var bithumbSymbol = group.Key;
                    if (String.IsNullOrEmpty(bithumbSymbol)) continue;

                    var symbol = ConvertSymbolBack(bithumbSymbol);
                    var timestamp = TimeExtension.UnixTime;

                    var completeOrders = new STrade
                    {
                        exchange = ExchangeName,
                        symbol = symbol,
                        timestamp = timestamp,
                        result = new List<STradeItem>()
                    };

                    foreach (var item in group)
                    {
                        var txTimestamp = timestamp;
                        
                        // Try to parse transaction date/time
                        var contDate = item.GetStringOrDefault("contDtm");
                        if (!String.IsNullOrEmpty(contDate))
                        {
                            try
                            {
                                var dateTime = DateTime.ParseExact(
                                    contDate,
                                    "yyyy-MM-dd HH:mm:ss.ffffff",
                                    CultureInfo.InvariantCulture,
                                    DateTimeStyles.AssumeLocal
                                );
                                txTimestamp = dateTime.ToUnixTime();
                            }
                            catch { }
                        }

                        var buySellGb = item.GetStringOrDefault("buySellGb");
                        var sideType = (buySellGb == "1") ? SideType.Bid : SideType.Ask;

                        completeOrders.result.Add(new STradeItem
                        {
                            tradeId = item.GetStringOrDefault("contNo", Guid.NewGuid().ToString()),
                            sideType = sideType,
                            orderType = OrderType.Limit,
                            price = item.GetDecimalOrDefault("contPrice"),
                            quantity = item.GetDecimalOrDefault("contQty"),
                            amount = (item.GetDecimalOrDefault("contPrice")) * (item.GetDecimalOrDefault("contQty")),
                            timestamp = txTimestamp
                        });
                    }

                    if (completeOrders.result.Count > 0)
                        InvokeTradeCallback(completeOrders);
                }
            }
            catch (Exception ex)
            {
                RaiseError($"Error processing transaction: {ex.Message}");
            }
        }

        public override async Task<bool> SubscribeOrderbookAsync(Market market)
        {
            try
            {
                var bithumbSymbol = FormatSymbol(market);
                var subscribeMessage = new
                {
                    type = "orderbookdepth",
                    symbols = new[] { bithumbSymbol }
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
                var bithumbSymbol = ConvertSymbol(symbol);
                var subscribeMessage = new
                {
                    type = "orderbookdepth",
                    symbols = new[] { bithumbSymbol }
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
                var bithumbSymbol = FormatSymbol(market);
                var subscribeMessage = new
                {
                    type = "transaction",
                    symbols = new[] { bithumbSymbol }
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
                var bithumbSymbol = ConvertSymbol(symbol);
                var subscribeMessage = new
                {
                    type = "transaction",
                    symbols = new[] { bithumbSymbol }
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
                var bithumbSymbol = FormatSymbol(market);
                var subscribeMessage = new
                {
                    type = "ticker",
                    symbols = new[] { bithumbSymbol },
                    tickTypes = new[] { "24H" } // Can also use 30M, 1H, 12H, MID
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
                var bithumbSymbol = ConvertSymbol(symbol);
                var subscribeMessage = new
                {
                    type = "ticker",
                    symbols = new[] { bithumbSymbol },
                    tickTypes = new[] { "24H" } // Can also use 30M, 1H, 12H, MID
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
                // Bithumb doesn't support explicit unsubscribe
                // Just remove from local tracking
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
                // Bithumb doesn't support explicit unsubscribe
                // Just remove from local tracking
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
            // Bithumb doesn't require explicit ping messages
            // WebSocket connection is maintained automatically
            await Task.CompletedTask;
        }

        public async Task<bool> SubscribeMultipleAsync(List<string> symbols, List<string> channels)
        {
            try
            {
                var bithumbSymbols = symbols.Select(s => ConvertSymbol(s)).ToArray();

                foreach (var channel in channels)
                {
                    object subscribeMessage = null;

                    switch (channel.ToLower())
                    {
                        case "orderbook":
                            subscribeMessage = new
                            {
                                type = "orderbookdepth",
                                symbols = bithumbSymbols
                            };
                            break;
                        case "ticker":
                            subscribeMessage = new
                            {
                                type = "ticker",
                                symbols = bithumbSymbols,
                                tickTypes = new[] { "24H" }
                            };
                            break;
                        case "trades":
                            subscribeMessage = new
                            {
                                type = "transaction",
                                symbols = bithumbSymbols
                            };
                            break;
                    }

                    if (subscribeMessage != null)
                    {
                        var json = JsonSerializer.Serialize(subscribeMessage);
                        await SendMessageAsync(json);
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                RaiseError($"Error subscribing to multiple channels: {ex.Message}");
                return false;
            }
        }

        public override async Task<bool> SubscribeCandlesAsync(Market market, string interval)
        {
            try
            {
                // Bithumb doesn't have a specific candles WebSocket channel
                // You would need to construct candles from trades or use REST API
                RaiseError("Bithumb doesn't support candles via WebSocket. Use REST API instead.");
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
                // Bithumb doesn't have a specific candles WebSocket channel
                // You would need to construct candles from trades or use REST API
                RaiseError("Bithumb doesn't support candles via WebSocket. Use REST API instead.");
                return false;
            }
            catch (Exception ex)
            {
                RaiseError($"Subscribe candles error: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Bithumb supports batch subscription - multiple markets in one message
        /// Note: Unlike Upbit, Bithumb allows separate subscription messages per channel
        /// </summary>
        protected override bool SupportsBatchSubscription()
        {
            return true;
        }

        /// <summary>
        /// Send batch subscriptions for Bithumb - can send separate messages per channel
        /// </summary>
        protected override async Task<bool> SendBatchSubscriptionsAsync(List<KeyValuePair<string, SubscriptionInfo>> subscriptions)
        {
            try
            {
                // Group subscriptions by channel type
                var groupedByChannel = subscriptions
                    .GroupBy(s => s.Value.Channel.ToLower())
                    .ToList();

                foreach (var channelGroup in groupedByChannel)
                {
                    var channel = channelGroup.Key;
                    var symbols = channelGroup
                        .Select(s => ConvertSymbol(s.Value.Symbol))
                        .Distinct()
                        .ToArray();

                    if (symbols.Length == 0)
                        continue;

                    // Map channel names to Bithumb message types
                    string messageType = channel switch
                    {
                        "orderbook" or "depth" => "orderbookdepth",
                        "trades" or "trade" => "transaction",
                        "ticker" => "ticker",
                        _ => channel
                    };

                    // Create subscription message with all symbols for this channel
                    var subscribeMessage = new
                    {
                        type = messageType,
                        symbols = symbols  // Send all symbols at once
                    };

                    // Send the batch subscription for this channel
                    var json = JsonSerializer.Serialize(subscribeMessage);
                    await SendMessageAsync(json);
                    
                    // Log the batch subscription
                    RaiseError($"Subscribed to {messageType} for {symbols.Length} markets: {string.Join(", ", symbols.Take(5))}{(symbols.Length > 5 ? "..." : "")}");
                }

                return true;
            }
            catch (Exception ex)
            {
                RaiseError($"Batch subscription failed: {ex.Message}");
                return false;
            }
        }
    }
}

