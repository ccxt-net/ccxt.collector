using OdinSdk.BaseLib.Coin;
using OdinSdk.BaseLib.Coin.Public;
using OdinSdk.BaseLib.Coin.Types;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CCXT.Collector.Upbit.Public
{
    public class PublicApi : OdinSdk.BaseLib.Coin.Public.PublicApi, IPublicApi
    {
        /// <summary>
        ///
        /// </summary>
        public PublicApi()
        {
        }

        /// <summary>
        ///
        /// </summary>
        public override XApiClient publicClient
        {
            get
            {
                if (base.publicClient == null)
                    base.publicClient = new UpbitClient("public");

                return base.publicClient;
            }
        }

        /// <summary>
        /// Fetch symbols, market ids and exchanger's information
        /// </summary>
        /// <param name="args">Add additional attributes for each exchange</param>
        /// <returns></returns>
        public override async Task<Markets> FetchMarkets(Dictionary<string, object> args = null)
        {
            var _result = new Markets();

            publicClient.ExchangeInfo.ApiCallWait(TradeType.Public);
            {
                var _params = publicClient.MergeParamsAndArgs(args);

                var _json_value = await publicClient.CallApiGet1Async("/market/all", _params);
#if DEBUG
                _result.rawJson = _json_value.Content;
#endif
                var _json_result = publicClient.GetResponseMessage(_json_value.Response);
                if (_json_result.success == true)
                {
                    var _markets = publicClient.DeserializeObject<List<UMarketItem>>(_json_value.Content);
                    foreach (var _market in _markets)
                    {
                        var _symbol = _market.symbol;

                        _market.baseId = _symbol.Split('-')[1];
                        _market.quoteId = _symbol.Split('-')[0];

                        _market.baseName = publicClient.ExchangeInfo.GetCommonCurrencyName(_market.baseId);
                        _market.quoteName = publicClient.ExchangeInfo.GetCommonCurrencyName(_market.quoteId);

                        _market.marketId = _market.baseName + "/" + _market.quoteName;

                        _market.precision = new MarketPrecision
                        {
                            quantity = 8,
                            price = 8,
                            amount = 8
                        };

                        _market.lot = 1.0m;
                        _market.active = true;

                        _market.takerFee = 0.05m / 100;
                        _market.makerFee = 0.05m / 100;

                        _market.limits = new MarketLimits
                        {
                            quantity = new MarketMinMax
                            {
                                min = (decimal)Math.Pow(10, -_market.precision.quantity),
                                max = decimal.MaxValue
                            },
                            price = new MarketMinMax
                            {
                                min = (decimal)Math.Pow(10, -_market.precision.price),
                                max = decimal.MaxValue
                            },
                            amount = new MarketMinMax
                            {
                                min = _market.lot,
                                max = decimal.MaxValue
                            }
                        };

                        _result.result.Add(_market.marketId, _market);
                    }
                }

                _result.SetResult(_json_result);
            }

            return _result;
        }
    }
}