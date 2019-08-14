using CCXT.Collector.Library;
using CCXT.NET.BitMEX.Public;
using CCXT.NET.Coin.Public;
using CCXT.NET.Converter;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CCXT.Collector.Bitmex
{
    public class PublicApi : KRestClient
    {
        public const string PublicUrl = "https://www.bitmex.com";

        public async Task<Markets> LoadMarkets()
        {
            var _result = new Markets();

            var _client = CreateJsonClient(PublicUrl);

            var _m_params = new Dictionary<string, object>();
            var _m_request = CreateJsonRequest($"/api/v1/instrument/active", _m_params);

            var _json_value = await RestExecuteAsync(_client, _m_request);
            if (_json_value.IsSuccessful && _json_value.Content[0] == '{')
            {
                var _markets = JsonConvert.DeserializeObject<List<BMarketItem>>(_json_value.Content);
                foreach (var _m in _markets)
                {
                    _m.active = _m.state != "Unlisted";
                    if (_m.active == false)
                        continue;

                    var _base_id = _m.underlying;
                    var _quote_id = _m.quoteCurrency;

                    var _base_name = _base_id;
                    var _quote_name = _quote_id;

                    var _market_id = _base_name + "/" + _quote_name;

                    var _order_base = _base_name;
                    var _order_quote = _quote_name;

                    var _base_quote = _base_id + _quote_id;
                    if (_m.symbol == _base_quote)
                    {
                        _m.swap = true;
                        _m.type = "swap";
                    }
                    else
                    {
                        var _symbols = _m.symbol.Split('_');
                        if (_symbols.Length > 1)
                        {
                            _market_id = _symbols[0] + "/" + _symbols[1];

                            _order_base = _symbols[0];
                            _order_quote = _symbols[1];
                        }
                        else
                        {
                            _market_id = _m.symbol.Substring(0, 3) + "/" + _m.symbol.Substring(3);

                            _order_base = _m.symbol.Substring(0, 3);
                            _order_quote = _m.symbol.Substring(3);
                        }

                        if (_m.symbol.IndexOf("B_") >= 0)
                        {
                            _m.prediction = true;
                            _m.type = "prediction";
                        }
                        else
                        {
                            _m.future = true;
                            _m.type = "future";
                        }
                    }

                    _m.marketId = _market_id;

                    _m.baseId = (_base_name != "BTC") ? _base_id : _m.settlCurrency;
                    _m.quoteId = (_quote_name != "BTC") ? _quote_id : _m.settlCurrency;

                    _m.orderBase = _order_base;
                    _m.orderQuote = _order_quote;

                    _m.baseName = _base_name;
                    _m.quoteName = _quote_name;

                    _m.lot = _m.lotSize;

                    _m.precision = new MarketPrecision()
                    {
                        quantity = Numerical.PrecisionFromString(Numerical.TruncateToString(_m.lotSize, 16)),
                        price = Numerical.PrecisionFromString(Numerical.TruncateToString(_m.tickSize, 16)),
                        amount = Numerical.PrecisionFromString(Numerical.TruncateToString(_m.tickSize, 16))
                    };

                    var _lot_size = _m.lotSize;
                    var _max_order_qty = _m.maxOrderQty;
                    var _tick_size = _m.tickSize;
                    var _max_price = _m.maxPrice;

                    _m.limit = new MarketLimits
                    {
                        quantity = new MarketMinMax
                        {
                            min = _lot_size,
                            max = _max_order_qty
                        },
                        price = new MarketMinMax
                        {
                            min = _tick_size,
                            max = _max_price
                        },
                        amount = new MarketMinMax
                        {
                            min = _lot_size * _tick_size,
                            max = _max_order_qty * _max_price
                        }
                    };

                    if (_m.initMargin != 0)
                        _m.maxLeverage = (int)(1 / _m.initMargin);

                    _result.result.Add(_m.marketId, _m);
                }
            }

            return _result;
        }
    }
}