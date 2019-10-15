using CCXT.Collector.Binance.Public;
using CCXT.Collector.Binance.Types;
using CCXT.Collector.Library;
using CCXT.Collector.Library.Types;
using Newtonsoft.Json;
using OdinSdk.BaseLib.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CCXT.Collector.Binance
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
                                var _b_json_data = JsonConvert.DeserializeObject<List<SBookTicker>>(_b_json_value.Content);

                                var _bookticker = new SBookTickers
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
    }
}