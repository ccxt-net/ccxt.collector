﻿using CCXT.Collector.Library;
using CCXT.NET.Shared.Configuration;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CCXT.Collector.BitMEX
{
    public class Polling : KRestClient
    {
        private Public.PublicApi __public_api = null;
        private Public.PublicApi publicApi
        {
            get
            {
                if (__public_api == null)
                    __public_api = new Public.PublicApi(__bmconfig.UseLiveServer);
                return __public_api;
            }
        }

        private Private.PrivateApi __private_api = null;
        private Private.PrivateApi privateApi
        {
            get
            {
                if (__private_api == null)
                    __private_api = new Private.PrivateApi(__bmconfig.ConnectKey, __bmconfig.SecretKey, __bmconfig.UseLiveServer);
                return __private_api;
            }
        }

        private readonly BMConfig __bmconfig;

        public Polling(IConfiguration configuration)
        {
            __bmconfig = new BMConfig(configuration);
        }

        public async ValueTask Start(CancellationToken cancelToken, string symbol, int limits = 25)
        {
            BMLogger.SNG.WriteO(this, $"polling service start: symbol => {symbol}...");

            var _m_polling = Task.Run(async () =>
            {
                var _use_myorder = __bmconfig.UseMyOrderStream;
                while (_use_myorder)
                {
                    try
                    {
                        await Task.Delay(0);

                        // my orders
                        var _orders = await privateApi.GetOrders(symbol);

                        if (_orders.success == true)
                        {
                            Processing.SendReceiveQ(new QMessage
                            {
                                command = "AP",
                                exchange = BMLogger.SNG.exchange_name,
                                symbol = symbol,
                                stream = "order",
                                action = "polling",
                                payload = JsonConvert.SerializeObject(_orders.result)
                            });
                        }
                    }
                    catch (TaskCanceledException)
                    {
                    }
                    catch (Exception ex)
                    {
                        BMLogger.SNG.WriteX(this, ex.ToString());
                    }
                    //finally
                    {
                        if (cancelToken.IsCancellationRequested == true)
                            break;
                    }

                    var _cancelled = cancelToken.WaitHandle.WaitOne(0);
                    if (_cancelled == true)
                        break;

                    await Task.Delay(__bmconfig.PollingSleep * 3);
                }
            },
            cancelToken
            );

            var _t_polling = Task.Run(async () =>
            {
                var _client = CreateJsonClient(publicApi.publicClient.ApiUrl);

                var _t_params = new Dictionary<string, object>();
                {
                    _t_params.Add("symbol", symbol);
                    _t_params.Add("count", limits);
                    _t_params.Add("reverse", true);
                }

                var _t_request = CreateJsonRequest($"/api/v1/trade", _t_params);

                while (true)
                {
                    try
                    {
                        await Task.Delay(0);

                        //trades
                        var _t_json_value = await RestExecuteAsync(_client, _t_request);
                        if (_t_json_value.IsSuccessful && _t_json_value.Content[0] == '[')
                        {
                            Processing.SendReceiveQ(new QMessage
                            {
                                command = "AP",
                                exchange = BMLogger.SNG.exchange_name,
                                symbol = symbol,
                                stream = "trade",
                                action = "polling",
                                payload = _t_json_value.Content
                            });
                        }
                        else
                        {
                            var _http_status = (int)_t_json_value.StatusCode;
                            if (_http_status == 403 || _http_status == 418 || _http_status == 429)
                            {
                                BMLogger.SNG.WriteQ(this, $"request-limit: symbol => {symbol}, https_status => {_http_status}");

                                var _waiting = cancelToken.WaitHandle.WaitOne(0);
                                if (_waiting == true)
                                    break;

                                var _seconds = 1;
                                {
                                    var _limit_reset = _t_json_value.Headers.Where(h => h.Name.ToLower() == "x-ratelimit-reset").FirstOrDefault();
                                    if (_limit_reset != null)
                                    {
                                        var _diff_seconds = Convert.ToInt64(_limit_reset.Value) - CUnixTime.Now;
                                        if (_diff_seconds > 0)
                                            _seconds += (int)_diff_seconds;
                                    }

                                    await Task.Delay(_seconds * 1000);
                                }
                            }
                        }
                    }
                    catch (TaskCanceledException)
                    {
                    }
                    catch (Exception ex)
                    {
                        BMLogger.SNG.WriteX(this, ex.ToString());
                    }
                    //finally
                    {
                        if (cancelToken.IsCancellationRequested == true)
                            break;
                    }

                    var _cancelled = cancelToken.WaitHandle.WaitOne(0);
                    if (_cancelled == true)
                        break;

                    await Task.Delay(__bmconfig.PollingSleep * 2);
                }
            },
            cancelToken
            );

            var _o_polling = Task.Run(async () =>
            {
                var _client = CreateJsonClient(publicApi.publicClient.ApiUrl);

                var _o_params = new Dictionary<string, object>();
                {
                    _o_params.Add("symbol", symbol);
                    _o_params.Add("depth", 25);
                }

                var _o_request = CreateJsonRequest($"/api/v1/orderBook/L2", _o_params);

                var _use_orderbook = __bmconfig.UsePollingOrderboook;
                while (_use_orderbook)
                {
                    try
                    {
                        await Task.Delay(0);

                        // orderbook
                        var _o_json_value = await RestExecuteAsync(_client, _o_request);
                        if (_o_json_value.IsSuccessful && _o_json_value.Content[0] == '[')
                        {
                            Processing.SendReceiveQ(new QMessage
                            {
                                command = "AP",
                                exchange = BMLogger.SNG.exchange_name,
                                symbol = symbol,
                                stream = "orderbook",
                                action = "polling",
                                payload = _o_json_value.Content
                            });
                        }
                        else
                        {
                            var _http_status = (int)_o_json_value.StatusCode;
                            if (_http_status == 403 || _http_status == 418 || _http_status == 429)
                            {
                                BMLogger.SNG.WriteQ(this, $"request-limit: symbol => {symbol}, https_status => {_http_status}");

                                var _waiting = cancelToken.WaitHandle.WaitOne(0);
                                if (_waiting == true)
                                    break;

                                var _seconds = 1;
                                {
                                    var _limit_reset = _o_json_value.Headers.Where(h => h.Name.ToLower() == "x-ratelimit-reset").FirstOrDefault();
                                    if (_limit_reset != null)
                                    {
                                        var _diff_seconds = Convert.ToInt64(_limit_reset.Value) - CUnixTime.Now;
                                        if (_diff_seconds > 0)
                                            _seconds += (int)_diff_seconds;
                                    }

                                    await Task.Delay(_seconds * 1000);
                                }
                            }
                        }
                    }
                    catch (TaskCanceledException)
                    {
                    }
                    catch (Exception ex)
                    {
                        BMLogger.SNG.WriteX(this, ex.ToString());
                    }
                    //finally
                    {
                        if (cancelToken.IsCancellationRequested == true)
                            break;
                    }

                    var _cancelled = cancelToken.WaitHandle.WaitOne(0);
                    if (_cancelled == true)
                        break;

                    await Task.Delay(__bmconfig.PollingSleep * 2);
                }
            },
            cancelToken
            );

            await Task.WhenAll(_m_polling, _t_polling, _o_polling);

            BMLogger.SNG.WriteO(this, $"polling service stopped: symbol => {symbol}...");
        }
    }
}