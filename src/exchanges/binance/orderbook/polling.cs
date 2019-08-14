using CCXT.Collector.Binance.Types;
using CCXT.Collector.Library;
using CCXT.Collector.Library.Service;
using CCXT.Collector.Library.Types;
using CCXT.NET.Coin.Public;
using CCXT.NET.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CCXT.Collector.Binance.Orderbook
{
    // "rateLimits": [
    //    {
    //        "rateLimitType": "REQUEST_WEIGHT",
    //        "interval": "MINUTE",
    //        "intervalNum": 1,
    //        "limit": 1200
    //    },
    //    {
    //        "rateLimitType": "ORDERS",
    //        "interval": "SECOND",
    //        "intervalNum": 1,
    //        "limit": 10
    //    },
    //    {
    //        "rateLimitType": "ORDERS",
    //        "interval": "DAY",
    //        "intervalNum": 1,
    //        "limit": 100000
    //    }
    //]

    public class Polling : KRestClient
    {
        private SynchronizedCollection<Task> __polling_tasks;

        /// <summary>
        ///
        /// </summary>
        public SynchronizedCollection<Task> PollingTasks
        {
            get
            {
                if (__polling_tasks == null)
                    __polling_tasks = new SynchronizedCollection<Task>();

                return __polling_tasks;
            }
            set
            {
                __polling_tasks = value;
            }
        }

        public async Task OStart(CancellationTokenSource tokenSource, string symbol)
        {
            BNLogger.WriteO($"polling service start: symbol => {symbol}...");

            if (KConfig.BinanceUsePollingBookticker == false)
            {
                PollingTasks.Add(Task.Run(async () =>
                {
                    var _client = CreateJsonClient(PublicApi.PublicUrl);

                    var _o_params = new Dictionary<string, object>();
                    {
                        _o_params.Add("symbol", symbol.ToUpper());
                        _o_params.Add("limit", 20);
                    }

                    var _o_request = CreateJsonRequest($"/depth", _o_params);
                    var _last_limit_milli_secs = 0L;

                    while (true)
                    {
                        //await __semaphore.WaitAsync(tokenSource.Token);

                        try
                        {
                            await Task.Delay(0);

                            var _waiting_milli_secs = (CUnixTime.NowMilli - KConfig.PollingPrevTime) / KConfig.PollingTermTime;
                            if (_waiting_milli_secs == _last_limit_milli_secs)
                            {
                                var _waiting = tokenSource.Token.WaitHandle.WaitOne(0);
                                if (_waiting == true)
                                    break;

                                await Task.Delay(10);
                            }
                            else
                            {
                                _last_limit_milli_secs = _waiting_milli_secs;

                                // orderbook
                                var _o_json_value = await RestExecuteAsync(_client, _o_request);
                                if (_o_json_value.IsSuccessful && _o_json_value.Content[0] == '{')
                                {
                                    var _o_json_data = JsonConvert.DeserializeObject<BAOrderBookItem>(_o_json_value.Content);
                                    _o_json_data.symbol = symbol;
                                    _o_json_data.lastId = _last_limit_milli_secs;

                                    var _orderbook = new BAOrderBook
                                    {
                                        stream = "arderbook",
                                        data = _o_json_data
                                    };

                                    var _o_json_content = JsonConvert.SerializeObject(_orderbook);
                                    Processing.SendReceiveQ(new QMessage { command = "AP", json = _o_json_content });
                                }
                                else
                                {
                                    var _http_status = (int)_o_json_value.StatusCode;
                                    if (_http_status == 403 || _http_status == 418 || _http_status == 429)
                                    {
                                        BNLogger.WriteQ($"request-limit: symbol => {symbol}, https_status => {_http_status}");

                                        var _waiting = tokenSource.Token.WaitHandle.WaitOne(0);
                                        if (_waiting == true)
                                            break;

                                        await Task.Delay(1000);     // waiting 1 second
                                    }
                                }
                            }
                        }
                        catch (TaskCanceledException)
                        {
                        }
                        catch (Exception ex)
                        {
                            BNLogger.WriteX(ex.ToString());
                        }
                        //finally
                        {
                            //__semaphore.Release();

                            if (tokenSource.IsCancellationRequested == true)
                                break;
                        }

                        var _cancelled = tokenSource.Token.WaitHandle.WaitOne(0);
                        if (_cancelled == true)
                            break;
                    }
                },
                tokenSource.Token
                ));
            }

            await Task.WhenAll(PollingTasks);

            BNLogger.WriteO($"polling service stopped: symbol => {symbol}...");
        }

        public async Task BStart(CancellationTokenSource tokenSource, string[] symbols)
        {
            BNLogger.WriteO($"bpolling service start...");

            if (KConfig.BinanceUsePollingBookticker == true)
            {
                PollingTasks.Add(Task.Run(async () =>
                {
                    var _client = CreateJsonClient(PublicApi.PublicUrl);

                    var _b_params = new Dictionary<string, object>();
                    var _b_request = CreateJsonRequest($"/ticker/bookTicker", _b_params);
                    var _last_limit_milli_secs = 0L;

                    while (true)
                    {
                        try
                        {
                            await Task.Delay(0);

                            var _waiting_milli_secs = (CUnixTime.NowMilli - KConfig.PollingPrevTime) / KConfig.PollingTermTime;
                            if (_waiting_milli_secs == _last_limit_milli_secs)
                            {
                                var _waiting = tokenSource.Token.WaitHandle.WaitOne(0);
                                if (_waiting == true)
                                    break;

                                await Task.Delay(10);
                                continue;
                            }

                            _last_limit_milli_secs = _waiting_milli_secs;

                            // bookticker
                            var _b_json_value = await RestExecuteAsync(_client, _b_request);
                            if (_b_json_value.IsSuccessful && _b_json_value.Content[0] == '[')
                            {
                                var _b_json_data = JsonConvert.DeserializeObject<List<SBookTickerItem>>(_b_json_value.Content);

                                var _bookticker = new SBookTicker
                                {
                                    exchange = BNLogger.exchange_name,
                                    stream = "bookticker",
                                    sequential_id = _last_limit_milli_secs,
                                    data = _b_json_data.Where(t => symbols.Contains(t.symbol)).ToList()
                                };

                                var _b_json_content = JsonConvert.SerializeObject(_bookticker);
                                Processing.SendReceiveQ(new QMessage { command = "AP", json = _b_json_content });
                            }
                            else
                            {
                                var _http_status = (int)_b_json_value.StatusCode;
                                if (_http_status == 403 || _http_status == 418 || _http_status == 429)
                                {
                                    BNLogger.WriteQ($"request-limit: https_status => {_http_status}");

                                    var _waiting = tokenSource.Token.WaitHandle.WaitOne(0);
                                    if (_waiting == true)
                                        break;

                                    await Task.Delay(1000);     // waiting 1 second
                                }
                            }
                        }
                        catch (TaskCanceledException)
                        {
                        }
                        catch (Exception ex)
                        {
                            BNLogger.WriteX(ex.ToString());
                        }
                        //finally
                        {
                            if (tokenSource.IsCancellationRequested == true)
                                break;
                        }

                        var _cancelled = tokenSource.Token.WaitHandle.WaitOne(0);
                        if (_cancelled == true)
                            break;
                    }
                },
                tokenSource.Token
                ));
            }

            await Task.WhenAll(PollingTasks);

            BNLogger.WriteO($"bpolling service stopped...");
        }

        private ConcurrentBag<AQuoteItem> __binance_quotes = new ConcurrentBag<AQuoteItem>();
        private ConcurrentBag<ABookExchange> __binance_markets = new ConcurrentBag<ABookExchange>();

        private void InitMarkets(List<IMarketItem> markets)
        {
            foreach (var _m in markets.Cast<MarketItem>())
            {
                var _quote = __binance_quotes.Where(q => q.quote_id == _m.quoteId).SingleOrDefault();
                if (_quote == null)
                {
                    _quote = new AQuoteItem
                    {
                        exchange = BNLogger.exchange_name,
                        quote_id = _m.quoteId,
                        total_amt = _m.quoteId == "USDT" ? 10000 : 0,
                        invest_amt = _m.quoteId == "USDT" ? 10000 : 0,
                        income = 0
                    };

                    __binance_quotes.Add(_quote);
                }

                __binance_markets.Add(new ABookExchange
                {
                    exchange = BNLogger.exchange_name,

                    symbol = _m.symbol,
                    market_id = _m.marketId,
                    base_id = _m.baseId,
                    quote_id = _m.quoteId,

                    taker_fee = _m.takerFee,
                    maker_fee = _m.makerFee,

                    quote = _quote,
                    quotes = __binance_quotes
                });
            }
        }

        public async Task AStart(CancellationTokenSource tokenSource, List<IMarketItem> markets)
        {
            BNLogger.WriteO($"apolling service start...");

            this.InitMarkets(markets);

            PollingTasks.Add(Task.Run(async () =>
            {
                var _client = CreateJsonClient(Binance.PublicApi.PublicUrl);

                var _a_params = new Dictionary<string, object>();
                var _a_request = CreateJsonRequest($"/ticker/bookTicker", _a_params);
                var _last_limit_milli_secs = 0L;

                while (true)
                {
                    try
                    {
                        await Task.Delay(0);

                        var _waiting_milli_secs = (CUnixTime.NowMilli - KConfig.PollingPrevTime) / KConfig.PollingTermTime;
                        if (_waiting_milli_secs == _last_limit_milli_secs)
                        {
                            var _waiting = tokenSource.Token.WaitHandle.WaitOne(0);
                            if (_waiting == true)
                                break;

                            await Task.Delay(10);
                            continue;
                        }

                        _last_limit_milli_secs = _waiting_milli_secs;

                        // bookticker
                        var _a_json_value = await RestExecuteAsync(_client, _a_request);
                        if (_a_json_value.IsSuccessful && _a_json_value.Content[0] == '[')
                        {
                            var _a_json_data = JsonConvert.DeserializeObject<List<SBookTickerItem>>(_a_json_value.Content);

                            foreach (var _d in _a_json_data.Where(b => __binance_markets.Select(m => m.symbol).Contains(b.symbol)))
                            {
                                var _market = __binance_markets.Where(b => b.symbol == _d.symbol).FirstOrDefault();
                                if (_market == null)
                                    continue;

                                AProcessing.SendReceiveQ(new ABookTickerItem
                                {
                                    market = _market,

                                    symbol = _market.market_id,
                                    sequential_id = _last_limit_milli_secs,

                                    exchangeRate = 1,

                                    askPrice = _d.askPrice,
                                    sellPrice = _d.askPrice,
                                    askQty = _d.askQty,

                                    bidPrice = _d.bidPrice,
                                    buyPrice = _d.bidPrice,
                                    bidQty = _d.bidQty
                                });
                            }
                        }
                        else
                        {
                            var _http_status = (int)_a_json_value.StatusCode;
                            if (_http_status == 403 || _http_status == 418 || _http_status == 429)
                            {
                                BNLogger.WriteQ($"request-limit: https_status => {_http_status}");

                                var _waiting = tokenSource.Token.WaitHandle.WaitOne(0);
                                if (_waiting == true)
                                    break;

                                await Task.Delay(1000);     // waiting 1 second
                            }
                        }
                    }
                    catch (TaskCanceledException)
                    {
                    }
                    catch (Exception ex)
                    {
                        BNLogger.WriteX(ex.ToString());
                    }
                    //finally
                    {
                        if (tokenSource.IsCancellationRequested == true)
                            break;
                    }

                    var _cancelled = tokenSource.Token.WaitHandle.WaitOne(0);
                    if (_cancelled == true)
                        break;
                }
            },
            tokenSource.Token
            ));

            await Task.WhenAll(PollingTasks);

            BNLogger.WriteO($"apolling service stopped...");
        }
    }
}