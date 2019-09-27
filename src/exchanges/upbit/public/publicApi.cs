using CCXT.Collector.Library;
using CCXT.NET.Coin.Public;
using CCXT.NET.Upbit.Public;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CCXT.Collector.Upbit
{
    public class PublicApi : KRestClient
    {
        public const string PublicUrl = "https://api.upbit.com/v1";
        public const string DunamuUrl = "https://quotation-api-cdn.dunamu.com/v1";

        public async Task<Markets> LoadMarkets()
        {
            var _result = new Markets();

            var _client = CreateJsonClient(PublicUrl);

            var _m_params = new Dictionary<string, object>();
            var _m_request = CreateJsonRequest($"/market/all", _m_params);

            var _m_json_value = await RestExecuteAsync(_client, _m_request);
            if (_m_json_value.IsSuccessful && _m_json_value.Content[0] == '[')
            {
                var _markets = JsonConvert.DeserializeObject<List<UMarketItem>>(_m_json_value.Content);
                foreach (var _market in _markets)
                {
                    var _symbol = _market.symbol;

                    _market.baseId = _symbol.Split('-')[1];
                    _market.quoteId = _symbol.Split('-')[0];

                    _market.baseName = _market.baseId;
                    _market.quoteName = _market.quoteId;

                    _market.marketId = _market.baseName + "/" + _market.quoteName;

                    _market.precision = new MarketPrecision
                    {
                        quantity = 8,
                        price = 8,
                        amount = 8
                    };

                    _market.lot = 1.0m;
                    _market.active = true;

                    _market.takerFee = (_market.quoteId != "KRW" ? 0.25m : 0.05m) / 100;
                    _market.makerFee = (_market.quoteId != "KRW" ? 0.25m : 0.05m) / 100;

                    _market.limit = new MarketLimits
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

            return _result;
        }
    }
}