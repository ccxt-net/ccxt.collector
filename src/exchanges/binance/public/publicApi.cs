using Newtonsoft.Json.Linq;
using OdinSdk.BaseLib.Coin;
using OdinSdk.BaseLib.Coin.Public;
using OdinSdk.BaseLib.Coin.Types;
using OdinSdk.BaseLib.Converter;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CCXT.Collector.Binance.Public
{
    /// <summary>
    /// exchange's public API implement class
    /// </summary>
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
                    base.publicClient = new BinanceClient("public");

                return base.publicClient;
            }
        }

        /// <summary>
        /// Fetch symbols, market ids and exchanger's information
        /// </summary>
        /// <param name="args">Add additional attributes for each exchange</param>
        /// <returns></returns>
        public override async ValueTask<Markets> FetchMarketsAsync(Dictionary<string, object> args = null)
        {
            var _result = new Markets();

            publicClient.ExchangeInfo.ApiCallWait(TradeType.Public);
            {
                var _params = publicClient.MergeParamsAndArgs(args);

                var _json_value = await publicClient.CallApiGet1Async("/exchangeInfo", _params);
#if RAWJSON
                _result.rawJson = _json_value.Content;
#endif
                var _json_result = publicClient.GetResponseMessage(_json_value.Response);
                if (_json_result.success == true)
                {
                    var _exchange_info = publicClient.DeserializeObject<JObject>(_json_value.Content);

                    var _symbols = _exchange_info["symbols"].ToObject<JArray>();
                    foreach (var _market in _symbols)
                    {
                        var _symbol = _market["symbol"]?.ToString();
                        if (_symbol == "123456")     // "123456" is a "test symbol/market"
                            continue;

                        var _base_id = _market["baseAsset"]?.ToString();
                        var _quote_id = _market["quoteAsset"]?.ToString();

                        var _base_name = publicClient.ExchangeInfo.GetCommonCurrencyName(_base_id ?? "");
                        var _quote_name = publicClient.ExchangeInfo.GetCommonCurrencyName(_quote_id ?? "");
                        var _market_id = _base_name + "/" + _quote_name;

                        var _precision = new MarketPrecision
                        {
                            quantity = _market["baseAssetPrecision"].Value<int>(),
                            price = _market["quotePrecision"].Value<int>(),
                            amount = _market["quotePrecision"].Value<int>()
                        };

                        var _lot = (decimal)(-1.0 * Math.Log10((double)_precision.quantity));
                        var _active = _market["status"].ToString().ToUpper() == "TRADING";

                        var _limits = new MarketLimits
                        {
                            quantity = new MarketMinMax
                            {
                                min = (decimal)Math.Pow(10, -(double)_precision.quantity),
                                max = decimal.MaxValue
                            },
                            price = new MarketMinMax
                            {
                                min = (decimal)Math.Pow(10, -(double)_precision.price),
                                max = decimal.MaxValue
                            },
                            amount = new MarketMinMax
                            {
                                min = _lot,
                                max = decimal.MaxValue
                            }
                        };

                        var _entry = new MarketItem
                        {
                            marketId = _market_id,

                            symbol = _symbol ?? "",
                            baseId = _base_id ?? "",
                            quoteId = _quote_id ?? "",
                            baseName = _base_name,
                            quoteName = _quote_name,

                            lot = _lot,
                            active = _active,

                            precision = _precision,
                            limits = _limits
                        };

                        JToken _filters = _market["filters"];
                        if (_filters != null)
                        {
                            var _price_filter = _filters.SingleOrDefault(f => f["filterType"]?.ToString() == "PRICE_FILTER");
                            if (_price_filter != null)
                            {
                                _entry.precision.price = Numerical.PrecisionFromString(_price_filter["tickSize"].ToString());
                                _entry.limits.price.min = _price_filter["minPrice"].Value<decimal>();
                                _entry.limits.price.max = _price_filter["maxPrice"].Value<decimal>();
                            }

                            var _lot_size = _filters.SingleOrDefault(f => f["filterType"]?.ToString() == "LOT_SIZE");
                            if (_lot_size != null)
                            {
                                _entry.precision.quantity = Numerical.PrecisionFromString(_lot_size["stepSize"].ToString());
                                _entry.limits.quantity.min = _lot_size["minQty"].Value<decimal>();
                                _entry.limits.quantity.max = _lot_size["maxQty"].Value<decimal>();
                            }

                            var _min_notional = _filters.SingleOrDefault(f => f["filterType"]?.ToString() == "MIN_NOTIONAL");
                            if (_min_notional != null)
                            {
                                _entry.limits.amount.min = _min_notional["minNotional"].Value<decimal>();
                            }
                        }

                        _result.result.Add(_entry.marketId, _entry);
                    }
                }

                _result.SetResult(_json_result);
            }

            return _result;
        }
    }
}