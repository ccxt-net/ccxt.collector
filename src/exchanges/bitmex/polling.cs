﻿using CCXT.Collector.BitMEX.Public;
using CCXT.Collector.Library;
using Newtonsoft.Json;
using OdinSdk.BaseLib.Coin.Public;
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
                                stream = "trade",
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
                            
                            var _asks = new List<OrderBookItem>();
                            var _bids = new List<OrderBookItem>();

                            foreach (var _o in _orderbooks)
                            {
                                _o.amount = _o.quantity * _o.price;
                                _o.count = 1;

                                if (_o.side.ToLower() == "sell")
                                    _asks.Add(_o);
                                else
                                    _bids.Add(_o);
                            }

                            var _orderbook = new BAOrderBook
                            {
                                stream = "orderbook",
                                data = new BAOrderBookData
                                {
                                    symbol = symbol,
                                    
                                }
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