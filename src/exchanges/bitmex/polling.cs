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
                            var _t_json_data = JsonConvert.DeserializeObject<List<BCompleteOrderItem>>(_t_json_value.Content);

                            var _trades = new BCompleteOrder
                            {
                                exchange = BMLogger.exchange_name,
                                stream = "trade",
                                symbol = symbol,
                                data = _t_json_data
                            };

                            var _t_json_content = JsonConvert.SerializeObject(_trades);
                            Processing.SendReceiveQ(new QMessage { command = "AP", payload = _t_json_content });
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

                var _o_request = CreateJsonRequest($"/api/v1/orderBook/L2", _o_params);

                while (true)
                {
                    try
                    {
                        await Task.Delay(0);

                        // orderbook
                        var _o_json_value = await RestExecuteAsync(_client, _o_request);
                        if (_o_json_value.IsSuccessful && _o_json_value.Content[0] == '{')
                        {
                            var _orderbooks = JsonConvert.DeserializeObject<List<BOrderBookItem>>(_o_json_value.Content);
                            
                            var _asks = new List<SOrderBookItem>();
                            var _bids = new List<SOrderBookItem>();

                            var _timestamp = 0L;

                            foreach (var _o in _orderbooks)
                            {
                                if (_timestamp < _o.id)
                                    _timestamp = _o.id;

                                var _ob = new SOrderBookItem
                                {
                                    quantity = _o.quantity,
                                    price = _o.price,
                                    amount = _o.quantity * _o.price,
                                    count = 1
                                };

                                if (_o.sideType == SideType.Ask)
                                    _asks.Add(_ob);
                                else
                                    _bids.Add(_ob);
                            }

                            var _orderbook = new BOrderBook
                            {
                                stream = "orderbook",
                                symbol = symbol,
                                data = new SOrderBook
                                {
                                    timestamp = _timestamp,
                                    asks = _asks,
                                    bids = _bids
                                }
                            };

                            var _o_json_content = JsonConvert.SerializeObject(_orderbook);
                            Processing.SendReceiveQ(new QMessage { command = "AP", payload = _o_json_content });
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