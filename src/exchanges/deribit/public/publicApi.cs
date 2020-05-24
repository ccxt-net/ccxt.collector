using CCXT.Collector.Deribit.Model;
using OdinSdk.BaseLib.Coin;
using OdinSdk.BaseLib.Coin.Public;
using OdinSdk.BaseLib.Coin.Types;
using OdinSdk.BaseLib.Configuration;
using System;
using System.Collections.Generic;
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

            var _json_currencies = await publicClient.CallApiGet1Async("/api/v2/public/get_currencies");
            var _currencies = publicClient.DeserializeObject<DRResultList<DCurrency>>(_json_currencies.Content);

            foreach(var _c in _currencies.result)
            {
                var _params = publicClient.MergeParamsAndArgs(args);
                {
                    _params.Add("currency", _c.currency);
                }

                var _json_value = await publicClient.CallApiGet1Async("/api/v2/public/get_instruments", _params);

                var _json_result = publicClient.GetResponseMessage(_json_value.Response);
                if (_json_result.success == true)
                {
                    var _markets = publicClient.DeserializeObject<DRResultList<DMarketItem>>(_json_value.Content);

                    foreach (var _m in _markets.result)
                    {
                        if (_m.active == false)
                            continue;

                        _m.symbol = _m.marketId;
                        _m.precision = new MarketPrecision
                        {
                            amount = _m.minTradeAmount,
                            price = _m.tickSize
                        };
                        _m.limits = new MarketLimits
                        {
                            quantity = new MarketMinMax
                            {
                                min = _m.minTradeAmount,
                                max = Decimal.MaxValue
                            },
                            price = new MarketMinMax
                            {
                                min = _m.tickSize,
                                max = Decimal.MaxValue
                            },
                            amount = new MarketMinMax
                            {
                                min = Decimal.MinValue,
                                max = Decimal.MaxValue
                            }
                        };
                        _m.withdrawEnabled = false;

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
        public async ValueTask<OHLCVs> GetOHLCVs(string symbol, string timeframe = "1h", int limits = 24)
        {
            var _result = new OHLCVs();

            var _params = new Dictionary<string, object>();
            {
                var _resolution = publicClient.ExchangeInfo.GetTimeframe(timeframe);
                var _duration = publicClient.ExchangeInfo.GetTimestamp(timeframe);

                var _end_timestamp = (CUnixTime.NowMilli / _duration) * _duration;
                var _start_timestamp = _end_timestamp - (limits - 1) * _duration * 1000;

                _params.Add("instrument_name", symbol);
                _params.Add("resolution", _resolution);      
                _params.Add("start_timestamp", _start_timestamp);
                _params.Add("end_timestamp", _end_timestamp);
            }

            var _response = await publicClient.CallApiGet2Async("/api/v2/public/get_tradingview_chart_data", _params);
            if (_response != null)
            {
#if RAWJSON
                _result.rawJson = _response.Content;
#endif
                if (_response.IsSuccessful == true)
                {
                    var _tickers = publicClient.DeserializeObject<DRResults<DTickerItem>>(_response.Content);
                    for (var i = 0; i < _tickers.result.ticks.Length; i++)
                    {
                        _result.result.Add(
                               new OHLCVItem
                               {
                                   timestamp = _tickers.result.ticks[i],
                                   openPrice = _tickers.result.open[i],
                                   highPrice = _tickers.result.high[i],
                                   lowPrice = _tickers.result.low[i],
                                   closePrice = _tickers.result.close[i],
                                   amount = _tickers.result.cost[i],
                                   vwap = 0,
                                   count = 0,
                                   volume = _tickers.result.volume[i]
                               }
                           );
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
        /// Fetch array of recent trades data
        /// </summary>
        /// <param name="symbol">The type of trading base-currency of which information you want to query for.</param>
        /// <param name="limits">maximum number of items (optional): default 25</param>
        /// <returns></returns>
        public async ValueTask<CompleteOrders> GetCompleteOrders(string symbol, int limits = 100)
        {
            var _result = new CompleteOrders();

            var _params = new Dictionary<string, object>();
            {
                _params.Add("instrument_name", symbol);
                _params.Add("count", limits);
                _params.Add("include_old", "true");
                _params.Add("sorting", "desc");
            }

            var _response = await publicClient.CallApiGet2Async("/api/v2/public/get_last_trades_by_instrument", _params);
            if (_response != null)
            {
#if RAWJSON
                _result.rawJson = _response.Content;
#endif
                if (_response.IsSuccessful == true)
                {
                    var _orders = publicClient.DeserializeObject<DRResults<DCompleteOrders>>(_response.Content);

                    foreach (var _o in _orders.result.trades)
                    {
                        _o.orderType = OrderType.Limit;
                        _o.fillType = FillType.Fill;
                        _o.makerType = MakerType.Taker;

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
                _params.Add("instrument_name", symbol);
                _params.Add("depth", count);
            }

            var _response = await publicClient.CallApiGet2Async("/api/v2/public/get_order_book", _params);
            if (_response != null)
            {
#if RAWJSON
                _result.rawJson = _response.Content;
#endif
                if (_response.IsSuccessful == true)
                {
                    var _orderbooks = publicClient.DeserializeObject<DRResults<DOrderBook>>(_response.Content);
                    if (_orderbooks != null)
                    {
                        _result.result.asks = _orderbooks.result.asks;
                        _result.result.bids = _orderbooks.result.bids;

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