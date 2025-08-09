using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CCXT.Collector.Core.Abstractions;
using CCXT.Collector.Service;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

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
        private readonly Dictionary<string, SOrderBooks> _orderbookCache;

        public override string ExchangeName => "Coinone";
        protected override string WebSocketUrl => "wss://stream.coinone.co.kr";
        protected override int PingIntervalMs => 30000; // 30 seconds

        public CoinoneWebSocketClient()
        {
            _orderbookCache = new Dictionary<string, SOrderBooks>();
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
                var json = JObject.Parse(message);

                // Check for error messages
                if (json["error_code"] != null)
                {
                    var errorCode = json["error_code"].ToString();
                    var errorMsg = json["error_msg"]?.ToString() ?? "Unknown error";
                    RaiseError($"Coinone error {errorCode}: {errorMsg}");
                    return;
                }

                // Handle different message types
                if (json["response_type"] != null)
                {
                    var responseType = json["response_type"].ToString();

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

        private async Task ProcessOrderbook(JObject json)
        {
            try
            {
                var data = json["data"];
                if (data == null) return;

                var coinoneSymbol = data["target_currency"]?.ToString();
                if (string.IsNullOrEmpty(coinoneSymbol)) return;

                var symbol = ConvertSymbolBack(coinoneSymbol);
                var timestamp = data["timestamp"]?.Value<long>() ?? DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

                var orderbook = new SOrderBooks
                {
                    exchange = ExchangeName,
                    symbol = symbol,
                    timestamp = timestamp,
                    result = new SOrderBook
                    {
                        timestamp = timestamp,
                        bids = new List<SOrderBookItem>(),
                        asks = new List<SOrderBookItem>()
                    }
                };

                // Process bids
                var bids = data["bids"] as JArray;
                if (bids != null)
                {
                    foreach (var bid in bids)
                    {
                        orderbook.result.bids.Add(new SOrderBookItem
                        {
                            price = bid["price"]?.Value<decimal>() ?? 0,
                            quantity = bid["qty"]?.Value<decimal>() ?? 0
                        });
                    }
                }

                // Process asks
                var asks = data["asks"] as JArray;
                if (asks != null)
                {
                    foreach (var ask in asks)
                    {
                        orderbook.result.asks.Add(new SOrderBookItem
                        {
                            price = ask["price"]?.Value<decimal>() ?? 0,
                            quantity = ask["qty"]?.Value<decimal>() ?? 0
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

        private async Task ProcessTrades(JObject json)
        {
            try
            {
                var data = json["data"];
                if (data == null) return;

                var trades = data["trades"] as JArray;
                if (trades == null || trades.Count == 0) return;

                var coinoneSymbol = data["target_currency"]?.ToString();
                if (string.IsNullOrEmpty(coinoneSymbol)) return;

                var symbol = ConvertSymbolBack(coinoneSymbol);
                var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

                var completeOrders = new SCompleteOrders
                {
                    exchange = ExchangeName,
                    symbol = symbol,
                    timestamp = timestamp,
                    result = new List<SCompleteOrderItem>()
                };

                foreach (var trade in trades)
                {
                    var tradeTimestamp = trade["timestamp"]?.Value<long>() ?? timestamp;
                    var isBuy = trade["is_buyer_maker"]?.Value<bool>() ?? false;

                    completeOrders.result.Add(new SCompleteOrderItem
                    {
                        orderId = trade["id"]?.ToString() ?? Guid.NewGuid().ToString(),
                        sideType = isBuy ? SideType.Bid : SideType.Ask,
                        orderType = OrderType.Limit,
                        price = trade["price"]?.Value<decimal>() ?? 0,
                        quantity = trade["qty"]?.Value<decimal>() ?? 0,
                        amount = (trade["price"]?.Value<decimal>() ?? 0) * (trade["qty"]?.Value<decimal>() ?? 0),
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

        private async Task ProcessTickerData(JObject json)
        {
            try
            {
                var data = json["data"];
                if (data == null) return;

                var coinoneSymbol = data["target_currency"]?.ToString();
                if (string.IsNullOrEmpty(coinoneSymbol)) return;

                var symbol = ConvertSymbolBack(coinoneSymbol);
                var timestamp = data["timestamp"]?.Value<long>() ?? DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

                var ticker = new STicker
                {
                    exchange = ExchangeName,
                    symbol = symbol,
                    timestamp = timestamp,
                    result = new STickerItem
                    {
                        timestamp = timestamp,
                        closePrice = data["last"]?.Value<decimal>() ?? 0,
                        openPrice = data["first"]?.Value<decimal>() ?? 0,
                        highPrice = data["high"]?.Value<decimal>() ?? 0,
                        lowPrice = data["low"]?.Value<decimal>() ?? 0,
                        volume = data["volume"]?.Value<decimal>() ?? 0,
                        quoteVolume = data["quote_volume"]?.Value<decimal>() ?? 0,
                        percentage = 0,
                        change = 0,
                        bidPrice = data["best_bid"]?.Value<decimal>() ?? 0,
                        askPrice = data["best_ask"]?.Value<decimal>() ?? 0,
                        vwap = 0,
                        prevClosePrice = data["yesterday_last"]?.Value<decimal>() ?? 0,
                        bidQuantity = data["best_bid_qty"]?.Value<decimal>() ?? 0,
                        askQuantity = data["best_ask_qty"]?.Value<decimal>() ?? 0
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

                var json = JsonConvert.SerializeObject(subscribeMessage);
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

                var json = JsonConvert.SerializeObject(subscribeMessage);
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

                var json = JsonConvert.SerializeObject(subscribeMessage);
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

                var json = JsonConvert.SerializeObject(subscribeMessage);
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

                var json = JsonConvert.SerializeObject(subscribeMessage);
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

                var json = JsonConvert.SerializeObject(subscribeMessage);
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

                var json = JsonConvert.SerializeObject(unsubscribeMessage);
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

                var json = JsonConvert.SerializeObject(unsubscribeMessage);
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

                var json = JsonConvert.SerializeObject(pingMessage);
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
