using CCXT.Collector.Library;
using Microsoft.Extensions.Configuration;
using OdinSdk.BaseLib.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CCXT.Collector.Deribit
{
    public class Polling : KRestClient
    {
        private Public.PublicApi __public_api = null;
        private Public.PublicApi publicApi
        {
            get
            {
                if (__public_api == null)
                    __public_api = new Public.PublicApi(__drconfig.UseLiveServer);
                return __public_api;
            }
        }

        private Private.PrivateApi __private_api = null;
        private Private.PrivateApi privateApi
        {
            get
            {
                if (__private_api == null)
                    __private_api = new Private.PrivateApi(__drconfig.ConnectKey, __drconfig.SecretKey, __drconfig.UseLiveServer);
                return __private_api;
            }
        }
        
        private readonly DRConfig __drconfig;

        public Polling(IConfiguration configuration)
        {
            __drconfig = new DRConfig(configuration);
        }

        public async Task Start(CancellationTokenSource cancelTokenSource, string symbol)
        {
            DRLogger.SNG.WriteO(this, $"polling service start: symbol => {symbol}...");

            var _orderbook_size = 25;
            var _trades_size = 128;
            //var _start_timestamp = CUnixTime.NowMilli;

            var _t_polling = Task.Run(async () =>
            {
                var _client = CreateJsonClient(publicApi.publicClient.ApiUrl);

                var _t_params = new Dictionary<string, object>();
                {
                    _t_params.Add("instrument_name", symbol);
                    _t_params.Add("count", _trades_size);
                    //_t_params.Add("start_timestamp", _start_timestamp);
                    //_t_params.Add("include_old", "true");
                    //_t_params.Add("sorting", "desc");
                }

                var _t_request = CreateJsonRequest($"/api/v2/public/get_last_trades_by_instrument", _t_params);

                while (true)
                {
                    try
                    {
                        await Task.Delay(0);

                        //trades
                        var _t_json_value = await RestExecuteAsync(_client, _t_request);
                        if (_t_json_value.IsSuccessful)
                        {
                            Processing.SendReceiveQ(new QMessage
                            {
                                command = "AP",
                                exchange = DRLogger.SNG.exchange_name,
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
                                DRLogger.SNG.WriteQ(this, $"request-limit: symbol => {symbol}, https_status => {_http_status}");

                                var _waiting = cancelTokenSource.Token.WaitHandle.WaitOne(0);
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
                        DRLogger.SNG.WriteX(this, ex.ToString());
                    }
                    //finally
                    {
                        if (cancelTokenSource.Token.IsCancellationRequested == true)
                            break;
                    }

                    var _cancelled = cancelTokenSource.Token.WaitHandle.WaitOne(0);
                    if (_cancelled == true)
                        break;

                    await Task.Delay(__drconfig.PollingSleep);
                }
            },
            cancelTokenSource.Token
            );

            var _o_polling = Task.Run(async () =>
            {
                var _client = CreateJsonClient(publicApi.publicClient.ApiUrl);

                var _o_params = new Dictionary<string, object>();
                {
                    _o_params.Add("instrument_name", symbol);
                    _o_params.Add("depth", _orderbook_size);
                }

                var _o_request = CreateJsonRequest($"/api/v2/public/get_order_book", _o_params);

                var _use_orderbook = __drconfig.UsePollingOrderboook;
                while (_use_orderbook)
                {
                    try
                    {
                        await Task.Delay(0);

                        // orderbook
                        var _o_json_value = await RestExecuteAsync(_client, _o_request);
                        if (_o_json_value.IsSuccessful)
                        {
                            Processing.SendReceiveQ(new QMessage
                            {
                                command = "AP",
                                exchange = DRLogger.SNG.exchange_name,
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
                                DRLogger.SNG.WriteQ(this, $"request-limit: symbol => {symbol}, https_status => {_http_status}");

                                var _waiting = cancelTokenSource.Token.WaitHandle.WaitOne(0);
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
                        DRLogger.SNG.WriteX(this, ex.ToString());
                    }
                    //finally
                    {
                        if (cancelTokenSource.Token.IsCancellationRequested == true)
                            break;
                    }

                    var _cancelled = cancelTokenSource.Token.WaitHandle.WaitOne(0);
                    if (_cancelled == true)
                        break;

                    await Task.Delay(__drconfig.PollingSleep);
                }
            },
            cancelTokenSource.Token
            );

            await Task.WhenAll(_t_polling, _o_polling);

            DRLogger.SNG.WriteO(this, $"polling service stopped: symbol => {symbol}...");
        }
    }
}