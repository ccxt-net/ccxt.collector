using CCXT.Collector.Library;
using CCXT.NET.Coin.Public;
using CCXT.NET.Converter;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CCXT.Collector.Binance
{
    public class BPublicApi : KRestClient
    {
        public const string PublicUrl = "https://api.binance.com/api/v1";

        public async Task<Markets> LoadMarkets()
        {
            var _result = new Markets();

            var _client = CreateJsonClient(PublicUrl);

            var _m_params = new Dictionary<string, object>();
            var _m_request = CreateJsonRequest($"/exchangeInfo", _m_params);

            var _m_json_value = await RestExecuteAsync(_client, _m_request);
            if (_m_json_value.IsSuccessful && _m_json_value.Content[0] == '{')
            {
                var _exchange_info = JsonConvert.DeserializeObject<JObject>(_m_json_value.Content);

                var _symbols = _exchange_info["symbols"].ToObject<JArray>();
                foreach (var _market in _symbols)
                {
                    var _symbol = _market["symbol"].ToString();
                    if (_symbol == "123456")     // "123456" is a "test symbol/market"
                        continue;

                    var _base_id = _market["baseAsset"].ToString();
                    var _quote_id = _market["quoteAsset"].ToString();
                    var _base_name = _base_id;
                    var _quote_name = _quote_id;
                    var _market_id = _base_name + "/" + _quote_name;

                    var _precision = new MarketPrecision
                    {
                        quantity = _market["baseAssetPrecision"].Value<int>(),
                        price = _market["quotePrecision"].Value<int>(),
                        amount = _market["quotePrecision"].Value<int>()
                    };

                    var _lot = (decimal)(-1.0 * Math.Log10(_precision.quantity));
                    var _active = _market["status"].ToString().ToUpper() == "TRADING";

                    var _taker_fee = 0.075m / 100;
                    var _maker_fee = 0.075m / 100;

                    var _limits = new MarketLimits
                    {
                        quantity = new MarketMinMax
                        {
                            min = (decimal)Math.Pow(10, -_precision.quantity),
                            max = decimal.MaxValue
                        },
                        price = new MarketMinMax
                        {
                            min = (decimal)Math.Pow(10, -_precision.price),
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

                        symbol = _symbol,
                        baseId = _base_id,
                        quoteId = _quote_id,
                        baseName = _base_name,
                        quoteName = _quote_name,

                        lot = _lot,
                        active = _active,

                        takerFee = _taker_fee,
                        makerFee = _maker_fee,

                        precision = _precision,
                        limit = _limits
                    };

                    var _filters = _market["filters"];
                    {
                        var _price_filter = _filters.SingleOrDefault(f => f["filterType"].ToString() == "PRICE_FILTER");
                        if (_price_filter != null)
                        {
                            _entry.precision.price = Numerical.PrecisionFromString(_price_filter["tickSize"].ToString());
                            _entry.limit.price.min = _price_filter["minPrice"].Value<decimal>();
                            _entry.limit.price.max = _price_filter["maxPrice"].Value<decimal>();
                        }

                        var _lot_size = _filters.SingleOrDefault(f => f["filterType"].ToString() == "LOT_SIZE");
                        if (_lot_size != null)
                        {
                            _entry.precision.quantity = Numerical.PrecisionFromString(_lot_size["stepSize"].ToString());
                            _entry.limit.quantity.min = _lot_size["minQty"].Value<decimal>();
                            _entry.limit.quantity.max = _lot_size["maxQty"].Value<decimal>();
                        }

                        var _min_notional = _filters.SingleOrDefault(f => f["filterType"].ToString() == "MIN_NOTIONAL");
                        if (_min_notional != null)
                        {
                            _entry.limit.amount.min = _min_notional["minNotional"].Value<decimal>();
                        }
                    }

                    _result.result.Add(_entry.marketId, _entry);
                }
            }

            return _result;
        }
    }
}