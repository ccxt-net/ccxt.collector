using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CCXT.Collector.Core.Abstractions;
using CCXT.Collector.Service;
using System.Text.Json;

namespace CCXT.Collector.Coinone
{
    /*
     * Coinone Exchange
     * 
     * API Documentation:
     *     https://docs.coinone.co.kr/
     *     https://api.coinone.co.kr/
     * 
     * WebSocket API:
     *     wss://stream.coinone.co.kr
     * 
     * Supported Markets: KRW
     * 
     * Rate Limits:
     *     - Public API: 90 requests per minute
     *     - Private API: 10 requests per second
     * 
     * Features:
     *     - Real-time orderbook updates
     *     - Trade stream
     *     - Ticker updates
     */

    /// <summary>
    /// Coinone WebSocket client for real-time market data streaming
    /// </summary>
    public class CoinoneWebSocketClient : WebSocketClientBase
    {
        private readonly Dictionary<string, SOrderBook> _orderbookCache;

        public override string ExchangeName => "Coinone";
        protected override string WebSocketUrl => "wss://stream.coinone.co.kr";
        protected override int PingIntervalMs => 30000; // 30 seconds

        public CoinoneWebSocketClient()
        {
            _orderbookCache = new Dictionary<string, SOrderBook>();
        }

        private string ConvertSymbol(string symbol)
        {
            // Convert format: BTC/KRW -> btc_krw
            // Coinone uses lowercase with underscore separator
            var parts = symbol.Split('/');
            if (parts.Length == 2)
            {
                var baseCoin = parts[0].ToLower();  // btc
                var quoteCoin = parts[1].ToLower(); // krw
                return $"{baseCoin}_{quoteCoin}";
            }

            return symbol.Replace("/", "_").ToLower();
        }

        private string ConvertSymbolBack(string coinoneSymbol)
        {
            // Convert format: btc_krw -> BTC/KRW
            var parts = coinoneSymbol.Split('_');
            if (parts.Length == 2)
            {
                var baseCoin = parts[0].ToUpper();  // BTC
                var quoteCoin = parts[1].ToUpper(); // KRW
                return $"{baseCoin}/{quoteCoin}";
            }

            return coinoneSymbol.Replace("_", "/").ToUpper();
        }

        /// <summary>
        /// Formats a Market object to Coinone-specific symbol format
        /// </summary>
        /// <param name="market">Market to format</param>
        /// <returns>Formatted symbol (e.g., "btc_krw")</returns>
        protected override string FormatSymbol(Market market)
        {
            // Coinone uses underscore separator with lowercase: base_quote
            return $"{market.Base.ToLower()}_{market.Quote.ToLower()}";
        }

        protected override async Task ProcessMessageAsync(string message, bool isPrivate = false)
        {
            try
            {
                using var doc = JsonDocument.Parse(message); 
                var json = doc.RootElement;

                // Check for error messages
                if (json.TryGetProperty("error_code", out var error_codeProp))
                {
                    var errorCode = json.GetStringOrDefault("error_code");
                    var errorMsg = json.GetStringOrDefault("error_msg", "Unknown error");
                    RaiseError($"Coinone error {errorCode}: {errorMsg}");
                    return;
                }

                // Handle different message types
                if (json.TryGetProperty("response_type", out var response_typeProp))
                {
                    var responseType = json.GetStringOrDefault("response_type");

                    switch (responseType)
                    {
                        case "ORDERBOOK":
                            await ProcessOrderbook(json);
                            break;
                        case "TRADE":
                            await ProcessTrades(json);
                            break;
                        case "TICKER":
                            await ProcessTickerData(json);
                            break;
                        case "SUBSCRIBE":
                            // Subscription confirmation
                            break;
                        case "PONG":
                            // Pong response
                            break;
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
            try
            {
                if (!json.TryGetProperty("data", out var data))
                    return;

                var coinoneSymbol = data.GetStringOrDefault("target_currency");
                if (String.IsNullOrEmpty(coinoneSymbol)) return;

                var symbol = ConvertSymbolBack(coinoneSymbol);
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
                        orderbook.result.bids.Add(new SOrderBookItem
                        {
                            price = bid.GetDecimalOrDefault("price"),
                            quantity = bid.GetDecimalOrDefault("qty")
                        });
                    }
                }

                // Process asks
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

                // Cache and invoke callback
                _orderbookCache[symbol] = orderbook;
                InvokeOrderbookCallback(orderbook);
            }
            catch (Exception ex)
            {
                RaiseError($"Error processing orderbook: {ex.Message}");
            }
        }

        private async Task ProcessTrades(JsonElement json)
        {
            try
            {
                if (!json.TryGetProperty("data", out var data))
                    return;

                if (!(data.TryGetArray("trades", out var trades)))
                    return;

                var coinoneSymbol = data.GetStringOrDefault("target_currency");
                if (String.IsNullOrEmpty(coinoneSymbol)) return;

                var symbol = ConvertSymbolBack(coinoneSymbol);
                var timestamp = TimeExtension.UnixTime;

                var completeOrders = new STrade
                {
                    exchange = ExchangeName,
                    symbol = symbol,
                    timestamp = timestamp,
                    result = new List<STradeItem>()
                };

                foreach (var trade in trades.EnumerateArray())
                {
                    var tradeTimestamp = trade.GetInt64OrDefault("timestamp", timestamp);
                    var isBuy = trade.GetBooleanOrFalse("is_buyer_maker");

                    completeOrders.result.Add(new STradeItem
                    {
                        tradeId = trade.GetStringOrDefault("id", Guid.NewGuid().ToString()),
                        sideType = isBuy ? SideType.Bid : SideType.Ask,
                        orderType = OrderType.Limit,
                        price = trade.GetDecimalOrDefault("price"),
                        quantity = trade.GetDecimalOrDefault("qty"),
                        amount = (trade.GetDecimalOrDefault("price")) * (trade.GetDecimalOrDefault("qty")),
                        timestamp = tradeTimestamp
                    });
                }

                InvokeTradeCallback(completeOrders);
            }
            catch (Exception ex)
            {
                RaiseError($"Error processing trades: {ex.Message}");
            }
        }

        private async Task ProcessTickerData(JsonElement json)
        {
            try
            {
                if (!json.TryGetProperty("data", out var data))
                    return;

                var coinoneSymbol = data.GetStringOrDefault("target_currency");
                if (String.IsNullOrEmpty(coinoneSymbol)) return;

                var symbol = ConvertSymbolBack(coinoneSymbol);
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
                        openPrice = data.GetDecimalOrDefault("first"),
                        highPrice = data.GetDecimalOrDefault("high"),
                        lowPrice = data.GetDecimalOrDefault("low"),
                        volume = data.GetDecimalOrDefault("volume"),
                        quoteVolume = data.GetDecimalOrDefault("quote_volume"),
                        percentage = 0,
                        change = 0,
                        bidPrice = data.GetDecimalOrDefault("best_bid"),
                        askPrice = data.GetDecimalOrDefault("best_ask"),
                        vwap = 0,
                        prevClosePrice = data.GetDecimalOrDefault("yesterday_last"),
                        bidQuantity = data.GetDecimalOrDefault("best_bid_qty"),
                        askQuantity = data.GetDecimalOrDefault("best_ask_qty")
                    }
                };

                // Calculate percentage and change
                if (ticker.result.prevClosePrice > 0)
                {
                    ticker.result.change = ticker.result.closePrice - ticker.result.prevClosePrice;
                    ticker.result.percentage = (ticker.result.change / ticker.result.prevClosePrice) * 100;
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
                var coinoneSymbol = FormatSymbol(market);
                var subscribeMessage = new
                {
                    request_type = "SUBSCRIBE",
                    channel = "ORDERBOOK",
                    topic = new
                    {
                        quote_currency = market.Quote.ToLower(),
                        target_currency = market.Base.ToLower()
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
                var coinoneSymbol = ConvertSymbol(symbol);
                var subscribeMessage = new
                {
                    request_type = "SUBSCRIBE",
                    channel = "ORDERBOOK",
                    topic = new
                    {
                        quote_currency = "krw",
                        target_currency = coinoneSymbol.Split('_')[0]
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
                var coinoneSymbol = FormatSymbol(market);
                var subscribeMessage = new
                {
                    request_type = "SUBSCRIBE",
                    channel = "TRADE",
                    topic = new
                    {
                        quote_currency = market.Quote.ToLower(),
                        target_currency = market.Base.ToLower()
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
                var coinoneSymbol = ConvertSymbol(symbol);
                var subscribeMessage = new
                {
                    request_type = "SUBSCRIBE",
                    channel = "TRADE",
                    topic = new
                    {
                        quote_currency = "krw",
                        target_currency = coinoneSymbol.Split('_')[0]
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
                var coinoneSymbol = FormatSymbol(market);
                var subscribeMessage = new
                {
                    request_type = "SUBSCRIBE",
                    channel = "TICKER",
                    topic = new
                    {
                        quote_currency = market.Quote.ToLower(),
                        target_currency = market.Base.ToLower()
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
                var coinoneSymbol = ConvertSymbol(symbol);
                var subscribeMessage = new
                {
                    request_type = "SUBSCRIBE",
                    channel = "TICKER",
                    topic = new
                    {
                        quote_currency = "krw",
                        target_currency = coinoneSymbol.Split('_')[0]
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
                var coinoneSymbol = FormatSymbol(market);
                var unsubscribeMessage = new
                {
                    request_type = "UNSUBSCRIBE",
                    channel = channel.ToUpper(),
                    topic = new
                    {
                        quote_currency = market.Quote.ToLower(),
                        target_currency = market.Base.ToLower()
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
                var coinoneSymbol = ConvertSymbol(symbol);
                var unsubscribeMessage = new
                {
                    request_type = "UNSUBSCRIBE",
                    channel = channel.ToUpper(),
                    topic = new
                    {
                        quote_currency = "krw",
                        target_currency = coinoneSymbol.Split('_')[0]
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
                    request_type = "PING"
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
                // Coinone doesn't have a specific candles WebSocket channel in their public documentation
                // You would need to construct candles from trades or use REST API
                RaiseError("Coinone doesn't support candles via WebSocket. Use REST API instead.");
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
                // Coinone doesn't have a specific candles WebSocket channel in their public documentation
                // You would need to construct candles from trades or use REST API
                RaiseError("Coinone doesn't support candles via WebSocket. Use REST API instead.");
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
