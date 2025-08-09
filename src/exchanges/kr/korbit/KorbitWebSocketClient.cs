using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CCXT.Collector.Core.Abstractions;
using CCXT.Collector.Service;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

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
                var json = JObject.Parse(message);

                // Check for error messages
                if (json["error"] != null)
                {
                    var error = json["error"].ToString();
                    RaiseError($"Korbit error: {error}");
                    return;
                }

                // Handle different message types
                var eventType = json["event"]?.ToString();
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
                else if (json["data"] != null)
                {
                    // Handle data messages
                    var data = json["data"];
                    var channel = json["channel"]?.ToString();
                    
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

        private async Task ProcessOrderbook(JObject json)
        {
            await ProcessOrderbookData(json);
        }

        private async Task ProcessOrderbookData(JObject json)
        {
            try
            {
                var data = json["data"] ?? json;
                var channel = json["channel"]?.ToString() ?? "";
                
                // Extract symbol from channel name (e.g., "orderbook:btc_krw")
                var korbitSymbol = "";
                if (channel.Contains(":"))
                {
                    korbitSymbol = channel.Split(':')[1];
                }
                else if (data["currency_pair"] != null)
                {
                    korbitSymbol = data["currency_pair"].ToString();
                }
                
                if (string.IsNullOrEmpty(korbitSymbol)) return;

                var symbol = ConvertSymbolBack(korbitSymbol);
                var timestamp = data["timestamp"]?.Value<long>() ?? DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

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
                var bids = data["bids"] as JArray;
                if (bids != null)
                {
                    foreach (var bid in bids)
                    {
                        if (bid is JArray bidArray && bidArray.Count >= 2)
                        {
                            orderbook.result.bids.Add(new SOrderBookItem
                            {
                                price = bidArray[0].Value<decimal>(),
                                quantity = bidArray[1].Value<decimal>()
                            });
                        }
                    }
                }

                // Process asks
                var asks = data["asks"] as JArray;
                if (asks != null)
                {
                    foreach (var ask in asks)
                    {
                        if (ask is JArray askArray && askArray.Count >= 2)
                        {
                            orderbook.result.asks.Add(new SOrderBookItem
                            {
                                price = askArray[0].Value<decimal>(),
                                quantity = askArray[1].Value<decimal>()
                            });
                        }
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

        private async Task ProcessTransaction(JObject json)
        {
            await ProcessTransactionData(json);
        }

        private async Task ProcessTransactionData(JObject json)
        {
            try
            {
                var data = json["data"] ?? json;
                var channel = json["channel"]?.ToString() ?? "";
                
                // Extract symbol from channel name
                var korbitSymbol = "";
                if (channel.Contains(":"))
                {
                    korbitSymbol = channel.Split(':')[1];
                }
                else if (data["currency_pair"] != null)
                {
                    korbitSymbol = data["currency_pair"].ToString();
                }
                
                if (string.IsNullOrEmpty(korbitSymbol)) return;

                var symbol = ConvertSymbolBack(korbitSymbol);
                var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

                var completeOrders = new STrade
                {
                    exchange = ExchangeName,
                    symbol = symbol,
                    timestamp = timestamp,
                    result = new List<STradeItem>()
                };

                // Process transaction list
                var transactions = data["transactions"] as JArray ?? data as JArray;
                if (transactions != null)
                {
                    foreach (var transaction in transactions)
                    {
                        var tx = transaction as JObject ?? transaction;
                        var txTimestamp = tx["timestamp"]?.Value<long>() ?? timestamp;
                        var side = tx["type"]?.ToString() ?? "buy";

                        completeOrders.result.Add(new STradeItem
                        {
                            orderId = tx["tid"]?.ToString() ?? Guid.NewGuid().ToString(),
                            sideType = side.ToLower() == "buy" ? SideType.Bid : SideType.Ask,
                            orderType = OrderType.Limit,
                            price = tx["price"]?.Value<decimal>() ?? 0,
                            quantity = tx["amount"]?.Value<decimal>() ?? 0,
                            amount = (tx["price"]?.Value<decimal>() ?? 0) * (tx["amount"]?.Value<decimal>() ?? 0),
                            timestamp = txTimestamp
                        });
                    }
                }
                else if (data["tid"] != null)
                {
                    // Single transaction
                    var txTimestamp = data["timestamp"]?.Value<long>() ?? timestamp;
                    var side = data["type"]?.ToString() ?? "buy";

                    completeOrders.result.Add(new STradeItem
                    {
                        orderId = data["tid"]?.ToString() ?? Guid.NewGuid().ToString(),
                        sideType = side.ToLower() == "buy" ? SideType.Bid : SideType.Ask,
                        orderType = OrderType.Limit,
                        price = data["price"]?.Value<decimal>() ?? 0,
                        quantity = data["amount"]?.Value<decimal>() ?? 0,
                        amount = (data["price"]?.Value<decimal>() ?? 0) * (data["amount"]?.Value<decimal>() ?? 0),
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

        private async Task ProcessTickerData(JObject json)
        {
            try
            {
                var data = json["data"] ?? json;
                var channel = json["channel"]?.ToString() ?? "";
                
                // Extract symbol from channel name
                var korbitSymbol = "";
                if (channel.Contains(":"))
                {
                    korbitSymbol = channel.Split(':')[1];
                }
                else if (data["currency_pair"] != null)
                {
                    korbitSymbol = data["currency_pair"].ToString();
                }
                
                if (string.IsNullOrEmpty(korbitSymbol)) return;

                var symbol = ConvertSymbolBack(korbitSymbol);
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
                        openPrice = data["open"]?.Value<decimal>() ?? 0,
                        highPrice = data["high"]?.Value<decimal>() ?? 0,
                        lowPrice = data["low"]?.Value<decimal>() ?? 0,
                        volume = data["volume"]?.Value<decimal>() ?? 0,
                        quoteVolume = 0,
                        percentage = data["change_percent"]?.Value<decimal>() ?? 0,
                        change = data["change"]?.Value<decimal>() ?? 0,
                        bidPrice = data["bid"]?.Value<decimal>() ?? 0,
                        askPrice = data["ask"]?.Value<decimal>() ?? 0,
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
                var korbitSymbol = ConvertSymbol(symbol);
                var subscribeMessage = new
                {
                    @event = "korbit:subscribe",
                    data = new
                    {
                        channels = new[] { $"orderbook:{korbitSymbol}" }
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
                var korbitSymbol = FormatSymbol(market);
                var subscribeMessage = new
                {
                    @event = "korbit:subscribe",
                    data = new
                    {
                        channels = new[] { $"transaction:{korbitSymbol}" }
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
                var korbitSymbol = ConvertSymbol(symbol);
                var subscribeMessage = new
                {
                    @event = "korbit:subscribe",
                    data = new
                    {
                        channels = new[] { $"transaction:{korbitSymbol}" }
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
                var korbitSymbol = FormatSymbol(market);
                var subscribeMessage = new
                {
                    @event = "korbit:subscribe",
                    data = new
                    {
                        channels = new[] { $"ticker:{korbitSymbol}" }
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
                var korbitSymbol = ConvertSymbol(symbol);
                var subscribeMessage = new
                {
                    @event = "korbit:subscribe",
                    data = new
                    {
                        channels = new[] { $"ticker:{korbitSymbol}" }
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
                    @event = "korbit:ping"
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
    }
}
