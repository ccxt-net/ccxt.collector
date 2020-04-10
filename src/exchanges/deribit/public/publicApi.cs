using OdinSdk.BaseLib.Coin;
using OdinSdk.BaseLib.Coin.Public;
using OdinSdk.BaseLib.Coin.Types;
using OdinSdk.BaseLib.Configuration;
using OdinSdk.BaseLib.Converter;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CCXT.Collector.Deribit.Public
{
    /// <summary>
    /// exchange's public API implement class
    /// </summary>
    public class PublicApi : OdinSdk.BaseLib.Coin.Public.PublicApi, IPublicApi
    {
        /// <summary>
        ///
        /// </summary>
        public PublicApi(bool is_live = true)
        {
            IsLive = is_live;
        }

        private bool IsLive
        {
            get;
            set;
        }

        /// <summary>
        ///
        /// </summary>
        public override XApiClient publicClient
        {
            get
            {
                if (base.publicClient == null)
                {
                    var _division = (IsLive == false ? "test." : "") + "public";
                    base.publicClient = new DeribitClient(_division);
                }

                return base.publicClient;
            }
        }

        public string GetWsUrl()
        {
            var _division = (IsLive == false ? "test." : "") + "public";

            var _uri = new Uri(publicClient.ExchangeInfo.Urls.api[_division]);
            return _uri.Host;
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

                var _json_value = await publicClient.CallApiGet1Async("/api/v2/public/getinstruments", _params);
#if RAWJSON
                _result.rawJson = _json_value.Content;
#endif
                var _json_result = publicClient.GetResponseMessage(_json_value.Response);
                if (_json_result.success == true)
                {
                    var _markets = publicClient.DeserializeObject<DRResult<DMarketItem>>(_json_value.Content);

                    foreach (var _m in _markets.result)
                    {
                        if (_m.active == false)
                            continue;

                        _m.marketId = _m.symbol;
                        _m.baseId = _m.baseName;
                        _m.quoteId = _m.quoteName;

                        _result.result.Add(_m.marketId, _m);
                    }
                }

                _result.SetResult(_json_result);
            }

            return _result;
        }

        /// <summary>
        /// Fetch array of symbol name and OHLCVs data
        /// </summary>
        /// <param name="symbol">Instrument symbol.</param>
        /// <param name="timeframe">Time interval to bucket by. Available options: [1m,5m,1h,1d].</param>
        /// <param name="limits">Number of results to fetch.</param>
        /// <returns></returns>
        public async ValueTask<OHLCVs> GetOHLCVs(string symbol, string timeframe = "1h", int limits = 12)
        {
            var _result = new OHLCVs();

            var _params = new Dictionary<string, object>();
            {
                _params.Add("symbol", symbol);
                _params.Add("binSize", timeframe);      // Time interval to bucket by. Available options: [1m,5m,1h,1d].
                _params.Add("count", limits);            // Number of results to fetch.
                _params.Add("partial", false);          // If true, will send in-progress (incomplete) bins for the current time period.
                _params.Add("reverse", true);           // If true, will sort results newest first.
            }

            var _response = await publicClient.CallApiGet2Async("/api/v1/trade/bucketed", _params);
            if (_response != null)
            {
#if RAWJSON
            _result.rawJson = _response.Content;
#endif
                if (_response.IsSuccessful == true)
                {
                    var _tickers = publicClient.DeserializeObject<List<BTickerItem>>(_response.Content);

                    _result.result.AddRange(
                         _tickers
                             .Select(x => new OHLCVItem
                             {
                                 timestamp = x.timestamp,
                                 openPrice = x.openPrice,
                                 highPrice = x.highPrice,
                                 lowPrice = x.lowPrice,
                                 closePrice = x.closePrice,
                                 amount = x.quoteVolume,
                                 vwap = x.vwap,
                                 count = x.trades,
                                 volume = x.baseVolume
                             })
                             .OrderByDescending(o => o.timestamp)
                         );

                    _result.SetSuccess();
                }
                else
                {
                    var _message = publicClient.GetResponseMessage(_response);
                    _result.SetFailure(_message.message);
                }
            }

            return _result;
        }

        /// <summary>
        /// Fetch array of recent trades data
        /// </summary>
        /// <param name="symbol">The type of trading base-currency of which information you want to query for.</param>
        /// <param name="limits">maximum number of items (optional): default 25</param>
        /// <returns></returns>
        public async ValueTask<CompleteOrders> GetCompleteOrders(string symbol, int limits = 25)
        {
            var _result = new CompleteOrders();

            var _params = new Dictionary<string, object>();
            {
                var _limits = limits <= 1 ? 1
                            : limits <= 500 ? limits
                            : 500;

                _params.Add("symbol", symbol);
                _params.Add("count", _limits);
                _params.Add("reverse", true);
            }

            var _response = await publicClient.CallApiGet2Async("/api/v1/trade", _params);
            if (_response != null)
            {
#if RAWJSON
            _result.rawJson = _response.Content;
#endif
                if (_response.IsSuccessful == true)
                {
                    var _orders = publicClient.DeserializeObject<List<BCompleteOrderItem>>(_response.Content);

                    foreach (var _o in _orders)
                    {
                        _o.orderType = OrderType.Limit;
                        _o.fillType = FillType.Fill;

                        _o.amount = _o.quantity * _o.price;
                        _result.result.Add(_o);
                    }

                    _result.SetSuccess();
                }
                else
                {
                    var _message = publicClient.GetResponseMessage(_response);
                    _result.SetFailure(_message.message);
                }
            }

            return _result;
        }

        /// <summary>
        /// Fetch pending or registered order details
        /// </summary>
        /// <param name="symbol"></param>
        /// <param name="count">maximum number of items (optional): default 25</param>
        /// <returns></returns>
        public async ValueTask<OrderBooks> GetOrderBooks(string symbol, int count = 25)
        {
            var _result = new OrderBooks();

            var _params = new Dictionary<string, object>();
            {
                _params.Add("symbol", symbol);
                _params.Add("depth", count);
            }

            var _response = await publicClient.CallApiGet2Async("/api/v1/orderBook/L2", _params);
            if (_response != null)
            {
#if RAWJSON
            _result.rawJson = _response.Content;
#endif
                if (_response.IsSuccessful == true)
                {
                    var _orderbooks = publicClient.DeserializeObject<List<BOrderBookItem>>(_response.Content);
                    if (_orderbooks != null)
                    {
                        _result.result.asks = new List<OrderBookItem>();
                        _result.result.bids = new List<OrderBookItem>();

                        foreach (var _o in _orderbooks)
                        {
                            _o.amount = _o.quantity * _o.price;
                            _o.count = 1;

                            if (_o.sideType == SideType.Ask)
                                _result.result.asks.Add(_o);
                            else
                                _result.result.bids.Add(_o);
                        }

                        _result.result.symbol = symbol;
                        _result.result.timestamp = CUnixTime.NowMilli;
                        _result.result.nonce = CUnixTime.Now;

                        _result.SetSuccess();
                    }
                }
                else
                {
                    var _message = publicClient.GetResponseMessage(_response);
                    _result.SetFailure(_message.message);
                }
            }

            return _result;
        }
    }
}