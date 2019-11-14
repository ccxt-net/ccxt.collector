using CCXT.Collector.Library.Types;
using OdinSdk.BaseLib.Coin;
using OdinSdk.BaseLib.Coin.Public;
using System;
using System.Collections.Generic;
using System.Linq;
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
        /// <returns></returns>
        public override async ValueTask<Markets> FetchMarkets(Dictionary<string, object> args = null)
        {
            var _result = new Markets();

            var _response = await publicClient.CallApiGet2Async("/market/all");
#if DEBUG
            _result.rawJson = _response.Content;
#endif
            if (_response.IsSuccessful == true)
            {
                var _markets = publicClient.DeserializeObject<List<UMarketItem>>(_response.Content);

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

                _result.SetSuccess();
            }
            else
            {
                var _message = publicClient.GetResponseMessage(_response);
                _result.SetFailure(_message.message);
            }

            return _result;
        }

        /// <summary>
        /// Fetch array of recent trades data
        /// </summary>
        /// <param name="base_name">The type of trading base-currency of which information you want to query for.</param>
        /// <param name="quote_name">The type of trading quote-currency of which information you want to query for.</param>
        /// <param name="limits">maximum number of items (optional): default 20</param>
        /// <returns></returns>
        public async ValueTask<SCompleteOrders> GetCompleteOrders(string base_name, string quote_name, int limits = 20)
        {
            var _result = new SCompleteOrders
            {
                symbol = $"{quote_name}-{base_name}"
            };

            var _params = new Dictionary<string, object>();
            {
                _params.Add("market", _result.symbol);
                _params.Add("count", limits);
            }

            var _response = await publicClient.CallApiGet2Async($"/trades/ticks", _params);
#if DEBUG
            _result.rawJson = _response.Content;
#endif
            if (_response.IsSuccessful == true)
            {
                var _trades = publicClient.DeserializeObject<List<UACompleteOrderItem>>(_response.Content);
                {
                    _result.sequentialId = _trades.Max(t => t.sequential_id);
                    _result.result = _trades.Select(t =>
                    {
                        return new SCompleteOrderItem
                        {
                            timestamp = t.timestamp,
                            sideType = t.sideType,
                            price = t.price,
                            quantity = t.quantity
                        };
                    })
                    .ToList<ISCompleteOrderItem>();
                }

                _result.SetSuccess();
            }
            else
            {
                var _message = publicClient.GetResponseMessage(_response);
                _result.SetFailure(_message.message);
            }

            return _result;
        }

        /// <summary>
        /// Fetch pending or registered order details
        /// </summary>
        /// <param name="base_name">The type of trading base-currency of which information you want to query for.</param>
        /// <param name="quote_name">The type of trading quote-currency of which information you want to query for.</param>
        /// <returns></returns>
        public async ValueTask<SOrderBooks> GetOrderBooks(string base_name, string quote_name)
        {
            var _result = new SOrderBooks();

            var _params = new Dictionary<string, object>();
            {
                _params.Add("market", $"{quote_name}-{base_name}");
            }

            var _response = await publicClient.CallApiGet2Async("/orderbook", _params);
#if DEBUG
            _result.rawJson = _response.Content;
#endif
            if (_response.IsSuccessful == true)
            {
                var _orderbooks = publicClient.DeserializeObject<List<UOrderBook>>(_response.Content);
                {
                    var _orderbook = _orderbooks.FirstOrDefault();
                    if (_orderbook != null)
                    {
                        _result.result = _orderbook;
                        _result.SetSuccess();
                    }
                }
            }
            else
            {
                var _message = publicClient.GetResponseMessage(_response);
                _result.SetFailure(_message.message);
            }

            return _result;
        }
    }
}