﻿using CCXT.Collector.Binance.Public;
using CCXT.Collector.Library;
using CCXT.Collector.Service;
using CCXT.NET.Shared.Configuration;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
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
        
        private CCXT.Collector.Binance.Public.PublicApi __public_api = null;
        private CCXT.Collector.Binance.Public.PublicApi publicApi
        {
            get
            {
                if (__public_api == null)
                    __public_api = new CCXT.Collector.Binance.Public.PublicApi();
                return __public_api;
            }
        }
        
        private readonly BNConfig __bnconfig;

        public Polling(IConfiguration configuration)
        {
            __bnconfig = new BNConfig(configuration);
        }

        public async ValueTask OStart(CancellationToken cancelToken, string symbol)
        {
            BNLogger.SNG.WriteO(this, $"polling service start: symbol => {symbol}...");

            if (__bnconfig.UsePollingTicker == false)
            {
                PollingTasks.Add(Task.Run(async () =>
                {
                    var _client = CreateJsonClient(publicApi.publicClient.ApiUrl);

                    var _o_params = new Dictionary<string, object>();
                    {
                        _o_params.Add("symbol", symbol.ToUpper());
                        _o_params.Add("limit", 20);
                    }

                    var _o_request = CreateJsonRequest($"/depth", _o_params);
                    var _last_limit_milli_secs = 0L;

                    while (true)
                    {
                        try
                        {
                            await Task.Delay(0);

                            var _waiting_milli_secs = (CUnixTime.NowMilli - __bnconfig.PollingPrevTime) / __bnconfig.PollingTermTime;
                            if (_waiting_milli_secs == _last_limit_milli_secs)
                            {
                                var _waiting = cancelToken.WaitHandle.WaitOne(0);
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
                                        stream = "orderbook",
                                        data = _o_json_data
                                    };

                                    var _o_json_content = JsonConvert.SerializeObject(_orderbook);
                                    Processing.SendReceiveQ(new QMessage { command = "AP", payload = _o_json_content });
                                }
                                else
                                {
                                    var _http_status = (int)_o_json_value.StatusCode;
                                    if (_http_status == 403 || _http_status == 418 || _http_status == 429)
                                    {
                                        BNLogger.SNG.WriteQ(this, $"request-limit: symbol => {symbol}, https_status => {_http_status}");

                                        var _waiting = cancelToken.WaitHandle.WaitOne(0);
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
                            BNLogger.SNG.WriteX(this, ex.ToString());
                        }
                        //finally
                        {
                            //__semaphore.Release();

                            if (cancelToken.IsCancellationRequested == true)
                                break;
                        }

                        var _cancelled = cancelToken.WaitHandle.WaitOne(0);
                        if (_cancelled == true)
                            break;
                    }
                },
                cancelToken
                ));
            }

            await Task.WhenAll(PollingTasks);

            BNLogger.SNG.WriteO(this, $"polling service stopped: symbol => {symbol}...");
        }

        public async ValueTask BStart(CancellationToken cancelToken, string symbol)
        {
            BNLogger.SNG.WriteO(this, $"bpolling service start...");

            if (__bnconfig.UsePollingTicker == true)
            {
                PollingTasks.Add(Task.Run(async () =>
                {
                    var _client = CreateJsonClient(publicApi.publicClient.ApiUrl);

                    var _b_params = new Dictionary<string, object>();
                    var _b_request = CreateJsonRequest($"/ticker/bookTicker", _b_params);
                    var _last_limit_milli_secs = 0L;

                    while (true)
                    {
                        try
                        {
                            await Task.Delay(0);

                            var _waiting_milli_secs = (CUnixTime.NowMilli - __bnconfig.PollingPrevTime) / __bnconfig.PollingTermTime;
                            if (_waiting_milli_secs == _last_limit_milli_secs)
                            {
                                var _waiting = cancelToken.WaitHandle.WaitOne(0);
                                if (_waiting == true)
                                    break;

                                await Task.Delay(10);
                                continue;
                            }

                            _last_limit_milli_secs = _waiting_milli_secs;

                            // ticker
                            var _b_json_value = await RestExecuteAsync(_client, _b_request);
                            if (_b_json_value.IsSuccessful && _b_json_value.Content[0] == '[')
                            {
                                var _b_json_data = JsonConvert.DeserializeObject<List<BTickerItem>>(_b_json_value.Content);

                                var _tickers = new STickers
                                {
                                    exchange = BNLogger.SNG.exchange_name,
                                    stream = "ticker",
                                    sequentialId = _last_limit_milli_secs,
                                    result = _b_json_data.Where(t => t.symbol == symbol).ToList<STickerItem>()
                                };

                                var _b_json_content = JsonConvert.SerializeObject(_tickers);
                                Processing.SendReceiveQ(new QMessage { command = "AP", payload = _b_json_content });
                            }
                            else
                            {
                                var _http_status = (int)_b_json_value.StatusCode;
                                if (_http_status == 403 || _http_status == 418 || _http_status == 429)
                                {
                                    BNLogger.SNG.WriteQ(this, $"request-limit: https_status => {_http_status}");

                                    var _waiting = cancelToken.WaitHandle.WaitOne(0);
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
                            BNLogger.SNG.WriteX(this, ex.ToString());
                        }
                        //finally
                        {
                            if (cancelToken.IsCancellationRequested == true)
                                break;
                        }

                        var _cancelled = cancelToken.WaitHandle.WaitOne(0);
                        if (_cancelled == true)
                            break;
                    }
                },
                cancelToken
                ));
            }

            await Task.WhenAll(PollingTasks);

            BNLogger.SNG.WriteO(this, $"bpolling service stop...");
        }
    }
}