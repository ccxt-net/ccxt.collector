using CCXT.Collector.Library;
using CCXT.Collector.Library.Types;
using CCXT.Collector.Upbit.Public;
using CCXT.Collector.Upbit.Types;
using Newtonsoft.Json;
using OdinSdk.BaseLib.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CCXT.Collector.Upbit
{
    public class Polling : KRestClient
    {
        public async Task OStart(CancellationTokenSource tokenSource, string symbol, int limit = 32)
        {
            UPLogger.WriteO($"polling service start: symbol => {symbol}...");

            var _t_polling = Task.Run(async () =>
            {
                var _client = CreateJsonClient(PublicApi.PublicUrl);

                var _t_params = new Dictionary<string, object>();
                {
                    _t_params.Add("market", symbol);
                    _t_params.Add("count", limit);
                }

                var _t_request = CreateJsonRequest($"/trades/ticks", _t_params);

                while (true)
                {
                    //await __semaphore.WaitAsync(tokenSource.Token);

                    try
                    {
                        await Task.Delay(KConfig.UpbitPollingSleep);

                        // trades
                        var _t_json_value = await RestExecuteAsync(_client, _t_request);
                        if (_t_json_value.IsSuccessful && _t_json_value.Content[0] == '[')
                        {
                            var _t_json_data = JsonConvert.DeserializeObject<List<UATrade>>(_t_json_value.Content);

                            var _trades = new STrades
                            {
                                exchange = UPLogger.exchange_name,
                                stream = "trades",
                                symbol = symbol,
                                data = _t_json_data.ToList<STrade>()
                            };

                            var _t_json_content = JsonConvert.SerializeObject(_trades);
                            Processing.SendReceiveQ(new QMessage { command = "AP", json = _t_json_content });
                        }
                        else
                        {
                            var _http_status = (int)_t_json_value.StatusCode;
                            if (_http_status == 403 || _http_status == 418)// || _http_status == 429)
                            {
                                UPLogger.WriteQ($"request-limit: symbol => {symbol}, https_status => {_http_status}");

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
                        UPLogger.WriteX(ex.ToString());
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
            );

            var _o_polling = Task.Run(async () =>
            {
                var _client = CreateJsonClient(PublicApi.PublicUrl);

                var _o_params = new Dictionary<string, object>();
                {
                    _o_params.Add("markets", symbol);
                }

                var _o_request = CreateJsonRequest($"/orderbook", _o_params);

                while (true)
                {
                    //await __semaphore.WaitAsync(tokenSource.Token);

                    try
                    {
                        await Task.Delay(KConfig.UpbitPollingSleep);

                        // orderbook
                        var _o_json_value = await RestExecuteAsync(_client, _o_request);
                        if (_o_json_value.IsSuccessful && _o_json_value.Content[0] == '[')
                        {
                            var _o_json_data = JsonConvert.DeserializeObject<List<UAOrderBook>>(_o_json_value.Content);
                            _o_json_data[0].type = "orderbooks";

                            var _o_json_content = JsonConvert.SerializeObject(_o_json_data[0]);
                            Processing.SendReceiveQ(new QMessage { command = "AP", json = _o_json_content });
                        }
                        else
                        {
                            var _http_status = (int)_o_json_value.StatusCode;
                            if (_http_status == 403 || _http_status == 418)// || _http_status == 429)
                            {
                                UPLogger.WriteQ($"request-limit: symbol => {symbol}, https_status => {_http_status}");

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
                        UPLogger.WriteX(ex.ToString());
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
            );

            await Task.WhenAll(_t_polling, _o_polling);

            UPLogger.WriteO($"polling service stopped: symbol => {symbol}...");
        }

        public async Task BStart(CancellationTokenSource tokenSource, string[] symbols)
        {
            UPLogger.WriteO($"bpolling service start..");

            var _b_polling = Task.Run(async () =>
            {
                var _symbols = String.Join(",", symbols);
                var _client = CreateJsonClient(PublicApi.PublicUrl);

                var _b_params = new Dictionary<string, object>();
                {
                    _b_params.Add("markets", _symbols);
                }

                var _b_request = CreateJsonRequest($"/orderbook", _b_params);
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

                        // orderbook
                        var _b_json_value = await RestExecuteAsync(_client, _b_request);
                        if (_b_json_value.IsSuccessful && _b_json_value.Content[0] == '[')
                        {
                            var _b_json_data = JsonConvert.DeserializeObject<List<UAOrderBook>>(_b_json_value.Content);

                            var _bookticker = new SBookTickers
                            {
                                exchange = UPLogger.exchange_name,
                                stream = "bookticker",
                                sequential_id = _last_limit_milli_secs,
                                data = _b_json_data.Select(o =>
                                {
                                    var _ask = o.orderbook_units.OrderBy(a => a.ask_price).First();
                                    var _bid = o.orderbook_units.OrderBy(a => a.bid_price).Last();

                                    return new SBookTicker
                                    {
                                        symbol = o.symbol,
                                        askPrice = _ask.ask_price,
                                        askQty = _ask.ask_size,
                                        bidPrice = _bid.bid_price,
                                        bidQty = _bid.bid_size
                                    };
                                })
                                .ToList()
                            };

                            var _b_json_content = JsonConvert.SerializeObject(_bookticker);
                            Processing.SendReceiveQ(new QMessage { command = "AP", json = _b_json_content });
                        }
                        else
                        {
                            var _http_status = (int)_b_json_value.StatusCode;
                            if (_http_status == 403 || _http_status == 418)// || _http_status == 429)
                            {
                                UPLogger.WriteQ($"request-limit: symbol => {_symbols}, https_status => {_http_status}");

                                var _waiting = tokenSource.Token.WaitHandle.WaitOne(0);
                                if (_waiting == true)
                                    break;

                                await Task.Delay(KConfig.UpbitPollingSleep);
                            }
                        }
                    }
                    catch (TaskCanceledException)
                    {
                    }
                    catch (Exception ex)
                    {
                        UPLogger.WriteX(ex.ToString());
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
            );

            await Task.WhenAll(_b_polling);

            UPLogger.WriteO($"bpolling service stopped..");
        }

        /// <summary>
        ///
        /// </summary>
        public static UExchangeItem LastExchange
        {
            get;
            set;
        }

        public async Task EStart(CancellationTokenSource tokenSource)
        {
            UPLogger.WriteO($"epolling service start..");

            var _b_polling = Task.Run(async () =>
            {
                var _client = CreateJsonClient(PublicApi.DunamuUrl);

                var _b_params = new Dictionary<string, object>();
                {
                    _b_params.Add("codes", "FRX.KRWUSD");
                }

                var _b_request = CreateJsonRequest($"/forex/recent", _b_params);
                var _last_limit_milli_secs = 0L;

                while (true)
                {
                    try
                    {
                        await Task.Delay(0);

                        var _waiting_milli_secs = (CUnixTime.NowMilli - 0) / (600 * 1000);
                        if (_waiting_milli_secs == _last_limit_milli_secs)
                        {
                            var _waiting = tokenSource.Token.WaitHandle.WaitOne(0);
                            if (_waiting == true)
                                break;

                            await Task.Delay(10);
                            continue;
                        }

                        _last_limit_milli_secs = _waiting_milli_secs;

                        // orderbook
                        var _b_json_value = await RestExecuteAsync(_client, _b_request);
                        if (_b_json_value.IsSuccessful && _b_json_value.Content[0] == '[')
                        {
                            var _b_json_data = JsonConvert.DeserializeObject<List<UExchangeItem>>(_b_json_value.Content);
                            if (_b_json_data.Count > 0)
                                LastExchange = _b_json_data[0];
                            else
                                _last_limit_milli_secs = 0;
                        }
                        else
                        {
                            _last_limit_milli_secs = 0;

                            var _http_status = (int)_b_json_value.StatusCode;
                            if (_http_status == 403 || _http_status == 418)// || _http_status == 429)
                            {
                                UPLogger.WriteQ($"https_status => {_http_status}");

                                var _waiting = tokenSource.Token.WaitHandle.WaitOne(0);
                                if (_waiting == true)
                                    break;

                                await Task.Delay(KConfig.UpbitPollingSleep);
                            }
                        }
                    }
                    catch (TaskCanceledException)
                    {
                    }
                    catch (Exception ex)
                    {
                        UPLogger.WriteX(ex.ToString());
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
            );

            await Task.WhenAll(_b_polling);

            UPLogger.WriteO($"epolling service stopped..");
        }
    }
}