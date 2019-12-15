using CCXT.Collector.Library;
using CCXT.Collector.Upbit.Public;
using Newtonsoft.Json;
using OdinSdk.BaseLib.Configuration;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace CCXT.Collector.Bithumb
{
    public class Polling : KRestClient
    {
        private CCXT.Collector.Upbit.Public.PublicApi? __public_api = null;
        private CCXT.Collector.Upbit.Public.PublicApi publicApi
        {
            get
            {
                if (__public_api == null)
                    __public_api = new CCXT.Collector.Upbit.Public.PublicApi();
                return __public_api;
            }
        }

        public async Task OStart(CancellationTokenSource tokenSource, string symbol, int limit = 32)
        {
            GMLogger.WriteO($"polling service start: symbol => {symbol}...");

            var _t_polling = Task.Run(async () =>
            {
                var _client = CreateJsonClient(publicApi.publicClient.ApiUrl);

                var _t_params = new Dictionary<string, object>();
                {
                    _t_params.Add("market", symbol);
                    _t_params.Add("count", limit);
                }

                var _t_request = CreateJsonRequest($"/trades/ticks", _t_params);

                while (true)
                {
                    try
                    {
                        await Task.Delay(KConfig.UpbitPollingSleep);

                        // trades
                        var _t_json_value = await RestExecuteAsync(_client, _t_request);
                        if (_t_json_value.IsSuccessful && _t_json_value.Content[0] == '[')
                        {
                            Processing.SendReceiveQ(new QMessage
                            {
                                command = "AP",
                                exchange = GMLogger.exchange_name,
                                symbol = symbol,
                                stream = "trade",
                                action = "polling",
                                payload = _t_json_value.Content
                            });
                        }
                        else
                        {
                            var _http_status = (int)_t_json_value.StatusCode;
                            if (_http_status == 403 || _http_status == 418)// || _http_status == 429)
                            {
                                GMLogger.WriteQ($"request-limit: symbol => {symbol}, https_status => {_http_status}");

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
                        GMLogger.WriteX(ex.ToString());
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

            var _o_polling = Task.Run(async () =>
            {
                var _client = CreateJsonClient(publicApi.publicClient.ApiUrl);

                var _o_params = new Dictionary<string, object>();
                {
                    _o_params.Add("markets", symbol);
                }

                var _o_request = CreateJsonRequest($"/orderbook", _o_params);

                while (true)
                {
                    try
                    {
                        await Task.Delay(KConfig.UpbitPollingSleep);

                        // orderbook
                        var _o_json_value = await RestExecuteAsync(_client, _o_request);
                        if (_o_json_value.IsSuccessful && _o_json_value.Content[0] == '[')
                        {
                            Processing.SendReceiveQ(new QMessage
                            {
                                command = "AP",
                                exchange = GMLogger.exchange_name,
                                symbol = symbol,
                                stream = "orderbook",
                                action = "polling",
                                payload = _o_json_value.Content
                            });
                        }
                        else
                        {
                            var _http_status = (int)_o_json_value.StatusCode;
                            if (_http_status == 403 || _http_status == 418)// || _http_status == 429)
                            {
                                GMLogger.WriteQ($"request-limit: symbol => {symbol}, https_status => {_http_status}");

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
                        GMLogger.WriteX(ex.ToString());
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

            await Task.WhenAll(_t_polling, _o_polling);

            GMLogger.WriteO($"polling service stopped: symbol => {symbol}...");
        }

        public async Task BStart(CancellationTokenSource tokenSource, string[] symbols)
        {
            GMLogger.WriteO($"bpolling service start..");

            var _b_polling = Task.Run(async () =>
            {
                var _symbols = String.Join(",", symbols);
                var _client = CreateJsonClient(publicApi.publicClient.ApiUrl);

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
                            Processing.SendReceiveQ(new QMessage
                            {
                                command = "AP",
                                exchange = GMLogger.exchange_name,
                                symbol = _symbols,
                                stream = "ticker",
                                action = "polling",
                                sequentialId = _last_limit_milli_secs,
                                payload = _b_json_value.Content
                            });
                        }
                        else
                        {
                            var _http_status = (int)_b_json_value.StatusCode;
                            if (_http_status == 403 || _http_status == 418)// || _http_status == 429)
                            {
                                GMLogger.WriteQ($"request-limit: symbol => {_symbols}, https_status => {_http_status}");

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
                        GMLogger.WriteX(ex.ToString());
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

            GMLogger.WriteO($"bpolling service stopped..");
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
            GMLogger.WriteO($"epolling service start..");

            var _dunamu_url = publicApi.publicClient.ExchangeInfo.GetApiUrl("dunamu");

            var _b_polling = Task.Run(async () =>
            {
                var _client = CreateJsonClient(_dunamu_url);

                var _b_params = new Dictionary<string, object>();
                {
                    _b_params.Add("codes", "FRX.KRWUSD");
                }

                var _b_request = CreateJsonRequest($"/recent", _b_params);
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
                                GMLogger.WriteQ($"https_status => {_http_status}");

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
                        GMLogger.WriteX(ex.ToString());
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

            GMLogger.WriteO($"epolling service stopped..");
        }
    }
}