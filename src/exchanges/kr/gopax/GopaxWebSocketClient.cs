using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CCXT.Collector.Core.Abstractions;
using CCXT.Collector.Service;
using System.Text.Json;

namespace CCXT.Collector.Gopax
{
    /*
     * Gopax Exchange (Streami, Korean Exchange by Dunamu)
     * 
     * API Documentation:
     *     https://gopax.github.io/API/
     *     https://api.gopax.co.kr/trading-pairs
     * 
     * WebSocket API:
     *     wss://wsapi.gopax.co.kr
     * 
     * Supported Markets: KRW
     * 
     * Rate Limits:
     *     - REST API: 60 requests per second
     *     - WebSocket: No specific limit
     * 
     * Features:
     *     - Real-time orderbook updates
     *     - Trade stream  
     *     - Ticker updates
     *     - Level 1 and Level 2 orderbook data
     */

    /// <summary>
    /// Gopax WebSocket client for real-time market data streaming
    /// </summary>
    public class GopaxWebSocketClient : WebSocketClientBase
    {
        private readonly Dictionary<string, SOrderBook> _orderbookCache;
        private readonly object _lockObject = new object();

        public override string ExchangeName => "Gopax";
        protected override string WebSocketUrl => "wss://wsapi.gopax.co.kr";
        protected override int PingIntervalMs => 30000; // 30 seconds

        public GopaxWebSocketClient()
        {
            _orderbookCache = new Dictionary<string, SOrderBook>();
        }

        private string ConvertSymbol(string symbol)
        {
            // Convert format: BTC/KRW -> BTC-KRW
            // Gopax uses hyphen separator with uppercase
            var parts = symbol.Split('/');
            if (parts.Length == 2)
            {
                var baseCoin = parts[0].ToUpper();  // BTC
                var quoteCoin = parts[1].ToUpper(); // KRW
                return $"{baseCoin}-{quoteCoin}";
            }

            return symbol.Replace("/", "-").ToUpper();
        }

        private string ConvertSymbolBack(string gopaxSymbol)
        {
            // Convert format: BTC-KRW -> BTC/KRW
            var parts = gopaxSymbol.Split('-');
            if (parts.Length == 2)
            {
                var baseCoin = parts[0];  // BTC
                var quoteCoin = parts[1]; // KRW
                return $"{baseCoin}/{quoteCoin}";
            }

            return gopaxSymbol.Replace("-", "/");
        }

        /// <summary>
        /// Formats a Market object to Gopax-specific symbol format
        /// </summary>
        /// <param name="market">Market to format</param>
        /// <returns>Formatted symbol (e.g., "BTC-KRW")</returns>
        protected override string FormatSymbol(Market market)
        {
            // Gopax uses hyphen separator with uppercase: BASE-QUOTE
            return $"{market.Base.ToUpper()}-{market.Quote.ToUpper()}";
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
                    RaiseError($"Gopax error: {error}");
                    return;
                }

                // Handle different message types
                var messageType = json.GetStringOrDefault("n"); // notification type
                if (!String.IsNullOrEmpty(messageType))
                {
                    switch (messageType)
                    {
                        case "book-level2":
                            await ProcessOrderbook(json);
                            break;
                        case "trades":
                            await ProcessTrades(json);
                            break;
                        case "ticker":
                            await ProcessTickerData(json);
                            break;
                        case "connected":
                            // Connection established
                            break;
                        case "subscribed":
                            // Subscription confirmed
                            break;
                    }
                }
                else if (json.TryGetProperty("o", out var oProp)) // orderbook data
                {
                    await ProcessOrderbookUpdate(json);
                }
                else if (json.TryGetProperty("t", out var tProp)) // trade data
                {
                    await ProcessTradeUpdate(json);
                }
            }
            catch (Exception ex)
            {
                RaiseError($"Error processing message: {ex.Message}");
            }
        }

        private async Task ProcessOrderbook(JsonElement json)
        {
            try
            {
                var gopaxSymbol = json.GetStringOrDefault("i"); // instrument
                if (String.IsNullOrEmpty(gopaxSymbol)) return;

                var symbol = ConvertSymbolBack(gopaxSymbol);
                var timestamp = json.GetInt64OrDefault("t", TimeExtension.UnixTime);

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
                if (json.TryGetArray("b", out var bids))
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
                if (json.TryGetArray("a", out var asks))
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

        private async Task ProcessOrderbookUpdate(JsonElement json)
        {
            // Process incremental orderbook updates
            await ProcessOrderbook(json);
        }

        private async Task ProcessTrades(JsonElement json)
        {
            try
            {
                var gopaxSymbol = json.GetStringOrDefault("i"); // instrument
                if (String.IsNullOrEmpty(gopaxSymbol)) return;

                var symbol = ConvertSymbolBack(gopaxSymbol);
                var timestamp = TimeExtension.UnixTime;

                var completeOrders = new STrade
                {
                    exchange = ExchangeName,
                    symbol = symbol,
                    timestamp = timestamp,
                    result = new List<STradeItem>()
                };

                // Process trades array
                if (json.TryGetArray("trades", out var trades))
                {
                    foreach (var trade in trades.EnumerateArray())
                    {
                        var tradeTimestamp = trade.GetInt64OrDefault("t", timestamp);
                        var side = trade.GetStringOrDefault("s"); // side

                        completeOrders.result.Add(new STradeItem
                        {
                            tradeId = trade.GetStringOrDefault("id", Guid.NewGuid().ToString()),
                            sideType = side == "buy" ? SideType.Bid : SideType.Ask,
                            orderType = OrderType.Limit,
                            price = trade.GetDecimalOrDefault("p"), // price
                            quantity = trade.GetDecimalOrDefault("q"), // quantity
                            amount = (trade.GetDecimalOrDefault("p")) * (trade.GetDecimalOrDefault("q")),
                            timestamp = tradeTimestamp
                        });
                    }
                }

                if (completeOrders.result.Count > 0)
                    InvokeTradeCallback(completeOrders);
            }
            catch (Exception ex)
            {
                RaiseError($"Error processing trades: {ex.Message}");
            }
        }

        private async Task ProcessTradeUpdate(JsonElement json)
        {
            // Process single trade update
            try
            {
                var gopaxSymbol = json.GetStringOrDefault("i"); // instrument
                if (String.IsNullOrEmpty(gopaxSymbol)) return;

                var symbol = ConvertSymbolBack(gopaxSymbol);
                var timestamp = json.GetInt64OrDefault("t", TimeExtension.UnixTime);

                var completeOrders = new STrade
                {
                    exchange = ExchangeName,
                    symbol = symbol,
                    timestamp = timestamp,
                    result = new List<STradeItem>()
                };

                var side = json.GetStringOrDefault("s"); // side
                completeOrders.result.Add(new STradeItem
                {
                    tradeId = json.GetStringOrDefault("id", Guid.NewGuid().ToString()),
                    sideType = side == "buy" ? SideType.Bid : SideType.Ask,
                    orderType = OrderType.Limit,
                    price = json.GetDecimalOrDefault("p"), // price
                    quantity = json.GetDecimalOrDefault("q"), // quantity
                    amount = (json.GetDecimalOrDefault("p")) * (json.GetDecimalOrDefault("q")),
                    timestamp = timestamp
                });

                InvokeTradeCallback(completeOrders);
            }
            catch (Exception ex)
            {
                RaiseError($"Error processing trade update: {ex.Message}");
            }
        }

        private async Task ProcessTickerData(JsonElement json)
        {
            try
            {
                var gopaxSymbol = json.GetStringOrDefault("i"); // instrument
                if (String.IsNullOrEmpty(gopaxSymbol)) return;

                var symbol = ConvertSymbolBack(gopaxSymbol);
                var timestamp = json.GetInt64OrDefault("t", TimeExtension.UnixTime);

                var ticker = new STicker
                {
                    exchange = ExchangeName,
                    symbol = symbol,
                    timestamp = timestamp,
                    result = new STickerItem
                    {
                        timestamp = timestamp,
                        closePrice = json.GetDecimalOrDefault("c"), // close
                        openPrice = json.GetDecimalOrDefault("o"), // open
                        highPrice = json.GetDecimalOrDefault("h"), // high
                        lowPrice = json.GetDecimalOrDefault("l"), // low
                        volume = json.GetDecimalOrDefault("v"), // volume
                        quoteVolume = json.GetDecimalOrDefault("q"), // quote volume
                        percentage = 0,
                        change = 0,
                        bidPrice = json.GetDecimalOrDefault("bp"), // best bid price
                        askPrice = json.GetDecimalOrDefault("ap"), // best ask price
                        vwap = json.GetDecimalOrDefault("vw"), // volume weighted average price
                        prevClosePrice = 0,
                        bidQuantity = json.GetDecimalOrDefault("bq"), // best bid quantity
                        askQuantity = json.GetDecimalOrDefault("aq") // best ask quantity
                    }
                };

                // Calculate percentage and change
                if (ticker.result.openPrice > 0)
                {
                    ticker.result.change = ticker.result.closePrice - ticker.result.openPrice;
                    ticker.result.percentage = (ticker.result.change / ticker.result.openPrice) * 100;
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
                var gopaxSymbol = FormatSymbol(market);
                var subscribeMessage = new
                {
                    n = "subscribe",
                    o = "book-level2",
                    i = gopaxSymbol
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
                var gopaxSymbol = ConvertSymbol(symbol);
                var subscribeMessage = new
                {
                    n = "subscribe",
                    o = "book-level2",
                    i = gopaxSymbol
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
                var gopaxSymbol = FormatSymbol(market);
                var subscribeMessage = new
                {
                    n = "subscribe",
                    o = "trades",
                    i = gopaxSymbol
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
                var gopaxSymbol = ConvertSymbol(symbol);
                var subscribeMessage = new
                {
                    n = "subscribe",
                    o = "trades",
                    i = gopaxSymbol
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
                var gopaxSymbol = FormatSymbol(market);
                var subscribeMessage = new
                {
                    n = "subscribe",
                    o = "ticker",
                    i = gopaxSymbol
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
                var gopaxSymbol = ConvertSymbol(symbol);
                var subscribeMessage = new
                {
                    n = "subscribe",
                    o = "ticker",
                    i = gopaxSymbol
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
                var gopaxSymbol = FormatSymbol(market);
                var channelName = channel.ToLower() switch
                {
                    "orderbook" => "book-level2",
                    "trades" => "trades",
                    "ticker" => "ticker",
                    _ => channel
                };

                var unsubscribeMessage = new
                {
                    n = "unsubscribe",
                    o = channelName,
                    i = gopaxSymbol
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
                var gopaxSymbol = ConvertSymbol(symbol);
                var channelName = channel.ToLower() switch
                {
                    "orderbook" => "book-level2",
                    "trades" => "trades",
                    "ticker" => "ticker",
                    _ => channel
                };

                var unsubscribeMessage = new
                {
                    n = "unsubscribe",
                    o = channelName,
                    i = gopaxSymbol
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
                    n = "ping"
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
                // Gopax doesn't have a specific candles WebSocket channel in their public documentation
                // You would need to construct candles from trades or use REST API
                RaiseError("Gopax doesn't support candles via WebSocket. Use REST API instead.");
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
                // Gopax doesn't have a specific candles WebSocket channel in their public documentation
                // You would need to construct candles from trades or use REST API
                RaiseError("Gopax doesn't support candles via WebSocket. Use REST API instead.");
                return false;
            }
            catch (Exception ex)
            {
                RaiseError($"Subscribe candles error: {ex.Message}");
                return false;
            }
        }
    }
}