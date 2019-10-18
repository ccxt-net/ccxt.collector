using CCXT.Collector.BitMEX.Types;
using CCXT.Collector.Library;
using CCXT.Collector.Library.Types;
using Newtonsoft.Json;
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

        public async Task Start(CancellationTokenSource tokenSource, string symbol, int limit = 32)
        {
            BMLogger.WriteO($"polling service start: symbol => {symbol}");

            var _t_polling = Task.Run(async () =>
            {
                var _client = CreateJsonClient(publicApi.publicClient.ApiUrl);

                var _t_params = new Dictionary<string, object>();
                {
                    _t_params.Add("symbol", symbol);
                    _t_params.Add("limit", limit);
                }

                var _t_request = CreateJsonRequest($"/aggTrades", _t_params);

                while (true)
                {
                    try
                    {
                        await Task.Delay(0);

                        //trades
                        var _t_json_value = await RestExecuteAsync(_client, _t_request);
                        if (_t_json_value.IsSuccessful && _t_json_value.Content[0] == '[')
                        {
                            var _t_json_data = JsonConvert.DeserializeObject<List<BATradeItem>>(_t_json_value.Content);

                            var _trades = new BATrade
                            {
                                exchange = BMLogger.exchange_name,
                                stream = "trades",
                                symbol = symbol,
                                data = _t_json_data
                            };

                            var _t_json_content = JsonConvert.SerializeObject(_trades);
                            Processing.SendReceiveQ(new QMessage { command = "AP", json = _t_json_content });
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
                    _o_params.Add("symbol", symbol.ToUpper());
                    _o_params.Add("limit", 20);
                }

                var _o_request = CreateJsonRequest($"/depth", _o_params);

                while (true)
                {
                    try
                    {
                        await Task.Delay(0);

                        // orderbook
                        var _o_json_value = await RestExecuteAsync(_client, _o_request);
                        if (_o_json_value.IsSuccessful && _o_json_value.Content[0] == '{')
                        {
                            var _o_json_data = JsonConvert.DeserializeObject<BAOrderBookData>(_o_json_value.Content);
                            _o_json_data.symbol = symbol;

                            var _orderbook = new BAOrderBook
                            {
                                stream = "arderbook",
                                data = _o_json_data
                            };

                            var _o_json_content = JsonConvert.SerializeObject(_orderbook);
                            Processing.SendReceiveQ(new QMessage { command = "AP", json = _o_json_content });
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