using CCXT.Collector.BitMEX.Public;
using CCXT.Collector.Library;
using CCXT.Collector.Library.Types;
using Newtonsoft.Json;
using OdinSdk.BaseLib.Coin.Types;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace CCXT.Collector.BitMEX
{
    public class Polling : KRestClient
    {
        private CCXT.Collector.BitMEX.Public.PublicApi __public_api = null;
        private CCXT.Collector.BitMEX.Public.PublicApi publicApi
        {
            get
            {
                if (__public_api == null)
                    __public_api = new CCXT.Collector.BitMEX.Public.PublicApi();
                return __public_api;
            }
        }

        public async Task Start(CancellationTokenSource tokenSource, string symbol, int limits = 25)
        {
            BMLogger.WriteO($"polling service start: symbol => {symbol}");

            var _t_polling = Task.Run(async () =>
            {
                var _client = CreateJsonClient(publicApi.publicClient.ApiUrl);

                var _t_params = new Dictionary<string, object>();
                {
                    _t_params.Add("symbol", symbol);
                    _t_params.Add("count", limits);
                    _t_params.Add("reverse", true);
                }

                var _t_request = CreateJsonRequest($"trade", _t_params);

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
                                exchange = BMLogger.exchange_name,
                                stream = "trade",
                                symbol = symbol,
                                payload = _t_json_value.Content
                            });
                        }
                    }
                    catch (TaskCanceledException)
                    {
                    }
                    catch (Exception ex)
                    {
                        BMLogger.WriteX(ex.ToString());
                    }
                    //finally
                    {
                        if (tokenSource.IsCancellationRequested == true)
                            break;
                    }

                    var _cancelled = tokenSource.Token.WaitHandle.WaitOne(0);
                    if (_cancelled == true)
                        break;

                    await Task.Delay(KConfig.UpbitPollingSleep);
                }
            },
            tokenSource.Token
            );

            var _o_polling = Task.Run(async () =>
            {
                var _client = CreateJsonClient(publicApi.publicClient.ApiUrl);

                var _o_params = new Dictionary<string, object>();
                {
                    _o_params.Add("symbol", symbol);
                    _o_params.Add("depth", 25);
                }

                var _o_request = CreateJsonRequest($"orderBook/L2", _o_params);

                while (true)
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
                                exchange = BMLogger.exchange_name,
                                stream = "orderbook",
                                symbol = symbol,
                                payload = _o_json_value.Content
                            });
                        }
                    }
                    catch (TaskCanceledException)
                    {
                    }
                    catch (Exception ex)
                    {
                        BMLogger.WriteX(ex.ToString());
                    }
                    //finally
                    {
                        if (tokenSource.IsCancellationRequested == true)
                            break;
                    }

                    var _cancelled = tokenSource.Token.WaitHandle.WaitOne(0);
                    if (_cancelled == true)
                        break;

                    await Task.Delay(KConfig.UpbitPollingSleep);
                }
            },
            tokenSource.Token
            );

            await Task.WhenAll(_t_polling, _o_polling);

            BMLogger.WriteO($"polling service stopped: symbol => {symbol}");
        }
    }
}