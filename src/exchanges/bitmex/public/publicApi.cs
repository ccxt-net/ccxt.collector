using OdinSdk.BaseLib.Coin;
using OdinSdk.BaseLib.Coin.Public;
using OdinSdk.BaseLib.Coin.Types;
using OdinSdk.BaseLib.Configuration;
using OdinSdk.BaseLib.Converter;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CCXT.Collector.BitMEX.Public
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
                    base.publicClient = new BitmexClient(_division);
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
        public override async ValueTask<Markets> FetchMarkets(Dictionary<string, object>? args = null)
        {
            var _result = new Markets();

            publicClient.ExchangeInfo.ApiCallWait(TradeType.Public);
            {
                var _params = publicClient.MergeParamsAndArgs(args);

                var _json_value = await publicClient.CallApiGet1Async("/api/v1/instrument/active", _params);
#if RAWJSON
                _result.rawJson = _json_value.Content;
#endif
                var _json_result = publicClient.GetResponseMessage(_json_value.Response);
                if (_json_result.success == true)
                {
                    var _markets = publicClient.DeserializeObject<List<BMarketItem>>(_json_value.Content);
                    foreach (var _m in _markets)
                    {
                        _m.active = _m.state != "Unlisted";
                        if (_m.active == false || _m.symbol == null)
                            continue;

                        var _base_id = _m.underlying;
                        var _quote_id = _m.quoteCurrency;

                        var _base_name = publicClient.ExchangeInfo.GetCommonCurrencyName(_base_id);
                        var _quote_name = publicClient.ExchangeInfo.GetCommonCurrencyName(_quote_id);

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

                        _m.limits = new MarketLimits
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