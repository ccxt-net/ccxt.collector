using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CCXT.Collector.Core.Abstractions;
using CCXT.Collector.Library;
using CCXT.Collector.Service;
using System.Text.Json;
using CCXT.Collector.Models.WebSocket;

namespace CCXT.Collector.Cryptocom
{
    /*
     * Crypto.com Support Markets: USDT, USDC, BTC, CRO
     *
     * API Documentation:
     *     https://exchange-docs.crypto.com/exchange/v1/rest-ws/index.html
     *
     * WebSocket API:
     *     https://exchange-docs.crypto.com/exchange/v1/rest-ws/index.html#websocket-api
     *
     * Fees:
     *     https://crypto.com/exchange/fees
     */
    /// <summary>
    /// Cryptocom WebSocket client for real-time data streaming
    /// </summary>
    public class CryptocomWebSocketClient : WebSocketClientBase
    {
        private readonly Dictionary<string, SOrderBook> _orderbookCache;
        private int _messageId = 1;

        public override string ExchangeName => "Cryptocom";
        protected override string WebSocketUrl => "wss://stream.crypto.com/exchange/v1/market";
        protected override int PingIntervalMs => 30000; // 30 seconds

        public CryptocomWebSocketClient()
        {
            _orderbookCache = new Dictionary<string, SOrderBook>();
        }

        protected override async Task ProcessMessageAsync(string message, bool isPrivate = false)
        {
            try
            {
                using var doc = JsonDocument.Parse(message); 
                var json = doc.RootElement;
                
                
                // Handle heartbeat/pong
                if (json.TryGetProperty("method", out var method) && method.GetString() == "public/heartbeat")
                {
                    return; // Heartbeat response, ignore
                }
                
                // Handle subscription responses and data messages
                if (json.TryGetProperty("method", out var methodProp) && methodProp.GetString() == "subscribe")
                {
                    // This is a subscription confirmation with initial data
                    if (json.TryGetProperty("code", out var code) && code.GetInt32() != 0)
                    {
                        var errorMsg = json.TryGetProperty("message", out var msg) ? msg.GetString() : "Unknown error";
                        RaiseError($"Subscription failed with code {code.GetInt32()}: {errorMsg}");
                        return;
                    }
                }
                
                // Handle data messages - they come in the result field
                if (json.TryGetProperty("result", out var result))
                {
                    if (result.TryGetProperty("channel", out var channel))
                    {
                        var channelName = channel.GetString();
                        
                        if (channelName?.Contains("book") == true)
                        {
                            ProcessOrderbookMessage(result);
                        }
                        else if (channelName?.Contains("trade") == true)
                        {
                            ProcessTradeMessage(result);
                        }
                        else if (channelName?.Contains("ticker") == true)
                        {
                            ProcessTickerMessage(result);
                        }
                    }
                }
                else if (json.TryGetProperty("id", out var id) && json.TryGetProperty("code", out var code))
                {
                    // Error response
                    if (code.GetInt32() != 0)
                    {
                        var errorMsg = json.TryGetProperty("message", out var msg) ? msg.GetString() : "Unknown error";
                        RaiseError($"Request {id.GetInt32()} failed with code {code.GetInt32()}: {errorMsg}");
                    }
                }
            }
            catch (Exception ex)
            {
                RaiseError($"Message processing error: {ex.Message}");
            }
        }
        
        private void ProcessOrderbookMessage(JsonElement data)
        {
            try
            {
                var symbol = data.GetStringOrDefault("instrument_name", "");
                var normalizedSymbol = NormalizeSymbol(symbol);
                var isSnapshot = data.GetStringOrDefault("channel", "").Contains("snapshot");
                
                if (data.TryGetProperty("data", out var bookDataArray))
                {
                    foreach (var bookData in bookDataArray.EnumerateArray())
                    {
                        var orderbook = new SOrderBook
                        {
                            exchange = ExchangeName,
                            symbol = normalizedSymbol,
                            timestamp = bookData.GetInt64OrDefault("t"),
                            result = new SOrderBookData
                            {
                                timestamp = bookData.GetInt64OrDefault("t"),
                                asks = new List<SOrderBookItem>(),
                                bids = new List<SOrderBookItem>()
                            }
                        };
                        
                        // For updates, the data is in "update" field, for snapshots it's directly in the object
                        var bookContent = bookData.TryGetProperty("update", out var update) ? update : bookData;
                        
                        if (bookContent.TryGetProperty("asks", out var asks))
                        {
                            foreach (var ask in asks.EnumerateArray())
                            {
                                if (ask.GetArrayLength() >= 2)
                                {
                                    var price = ask[0].GetDecimalValue();
                                    var quantity = ask[1].GetDecimalValue();
                                    
                                    // Skip entries with 0 quantity (removed from orderbook)
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
                        
                        if (bookContent.TryGetProperty("bids", out var bids))
                        {
                            foreach (var bid in bids.EnumerateArray())
                            {
                                if (bid.GetArrayLength() >= 2)
                                {
                                    var price = bid[0].GetDecimalValue();
                                    var quantity = bid[1].GetDecimalValue();
                                    
                                    // Skip entries with 0 quantity (removed from orderbook)
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
                        
                        // Calculate sum quantities
                        orderbook.result.askSumQty = orderbook.result.asks.Sum(a => a.quantity);
                        orderbook.result.bidSumQty = orderbook.result.bids.Sum(b => b.quantity);
                        
                        InvokeOrderbookCallback(orderbook);
                    }
                }
            }
            catch (Exception ex)
            {
                RaiseError($"Orderbook processing error: {ex.Message}");
            }
        }
        
        private void ProcessTradeMessage(JsonElement data)
        {
            try
            {
                var symbol = data.GetStringOrDefault("instrument_name", "");
                var normalizedSymbol = NormalizeSymbol(symbol);
                
                if (data.TryGetProperty("data", out var tradesData))
                {
                    var trades = new List<STradeItem>();
                    
                    foreach (var trade in tradesData.EnumerateArray())
                    {
                        trades.Add(new STradeItem
                        {
                            tradeId = trade.GetStringOrDefault("d", ""),
                            price = trade.GetDecimalOrDefault("p"),
                            quantity = trade.GetDecimalOrDefault("q"),
                            timestamp = trade.GetInt64OrDefault("t"),
                            sideType = trade.GetStringOrDefault("s") == "BUY" ? SideType.Bid : SideType.Ask
                        });
                    }
                    
                    if (trades.Any())
                    {
                        var strade = new STrade
                        {
                            exchange = ExchangeName,
                            symbol = normalizedSymbol,
                            timestamp = trades.First().timestamp,
                            result = trades
                        };
                        
                        InvokeTradeCallback(strade);
                    }
                }
            }
            catch (Exception ex)
            {
                RaiseError($"Trade processing error: {ex.Message}");
            }
        }
        
        private void ProcessTickerMessage(JsonElement data)
        {
            try
            {
                var symbol = data.GetStringOrDefault("instrument_name", "");
                var normalizedSymbol = NormalizeSymbol(symbol);
                
                if (data.TryGetProperty("data", out var tickerDataArray))
                {
                    foreach (var tickData in tickerDataArray.EnumerateArray())
                    {
                        var ticker = new STicker
                        {
                            exchange = ExchangeName,
                            symbol = normalizedSymbol,
                            timestamp = tickData.GetInt64OrDefault("t"),
                            result = new STickerItem
                            {
                                timestamp = tickData.GetInt64OrDefault("t"),
                                closePrice = tickData.GetDecimalOrDefault("a"),
                                bidPrice = tickData.GetDecimalOrDefault("b"),
                                askPrice = tickData.GetDecimalOrDefault("k"),
                                highPrice = tickData.GetDecimalOrDefault("h"),
                                lowPrice = tickData.GetDecimalOrDefault("l"),
                                volume = tickData.GetDecimalOrDefault("v"),
                                openPrice = tickData.GetDecimalOrDefault("o"),
                                change = tickData.GetDecimalOrDefault("c")
                            }
                        };
                        
                        InvokeTickerCallback(ticker);
                    }
                }
            }
            catch (Exception ex)
            {
                RaiseError($"Ticker processing error: {ex.Message}");
            }
        }

        public override async Task<bool> SubscribeOrderbookAsync(string symbol)
        {
            try
            {
                var instrumentName = ConvertSymbol(symbol);
                var subscription = new
                {
                    id = _messageId++,
                    method = "subscribe",
                    @params = new
                    {
                        channels = new[] { $"book.{instrumentName}.10" }, // 10 levels depth
                        book_subscription_type = "SNAPSHOT_AND_UPDATE"
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
                var instrumentName = ConvertSymbol(symbol);
                var subscription = new
                {
                    id = _messageId++,
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
                var instrumentName = ConvertSymbol(symbol);
                var subscription = new
                {
                    id = _messageId++,
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
                var instrumentName = ConvertSymbol(symbol);
                var channelName = channel switch
                {
                    "orderbook" => $"book.{instrumentName}.10",
                    "trades" => $"trade.{instrumentName}",
                    "ticker" => $"ticker.{instrumentName}",
                    _ => channel
                };
                
                var unsubscription = new
                {
                    id = _messageId++,
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
                id = _messageId++,
                method = "public/heartbeat",
                nonce = TimeExtension.UnixTime
            });
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
                var instrumentName = ConvertSymbol(symbol);
                var subscription = new
                {
                    id = _messageId++,
                    method = "subscribe",
                    @params = new
                    {
                        channels = new[] { $"candlestick.{interval}.{instrumentName}" }
                    },
                    nonce = TimeExtension.UnixTime
                };

                await SendMessageAsync(JsonSerializer.Serialize(subscription));
                
                MarkSubscriptionActive("candles", symbol, interval);

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

        private string ConvertSymbol(string symbol)
        {
            // Convert from "BTC/USDT" to "BTC_USDT" format for Crypto.com
            return symbol.Replace("/", "_");
        }
        
        private string NormalizeSymbol(string symbol)
        {
            // Convert from "BTC_USDT" back to "BTC/USDT" format
            return symbol.Replace("_", "/");
        }

        #endregion
    }
}