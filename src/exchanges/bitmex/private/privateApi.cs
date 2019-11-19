using OdinSdk.BaseLib.Coin;
using OdinSdk.BaseLib.Coin.Private;
using OdinSdk.BaseLib.Coin.Trade;
using OdinSdk.BaseLib.Coin.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CCXT.Collector.BitMEX.Private
{
    public class PrivateApi : OdinSdk.BaseLib.Coin.Private.PrivateApi, IPrivateApi
    {
        private readonly string __connect_key;
        private readonly string __secret_key;

        /// <summary>
        ///
        /// </summary>
        public PrivateApi(string connect_key, string secret_key, bool is_live = true)
        {
            __connect_key = connect_key;
            __secret_key = secret_key;

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
        public override XApiClient privateClient
        {
            get
            {
                if (base.privateClient == null)
                {
                    var _division = (IsLive == false ? "test." : "") + "private";
                    base.privateClient = new BitmexClient(_division, __connect_key, __secret_key);
                }

                return base.privateClient;
            }
        }

        /// <summary>
        /// Get a deposit address.
        /// </summary>
        /// <param name="currency"></param>        
        /// <returns></returns>
        public async ValueTask<Address> GetDepositAddress(string currency)
        {
            var _result = new Address();

            var _params = new Dictionary<string, object>();
            {
                _params.Add("currency", currency);
            }

            var _response = await privateClient.CallApiGet2Async("/api/v1/user/depositAddress", _params);
#if DEBUG
            _result.rawJson = _response.Content;
#endif
            if (_response.IsSuccessful == true)
            {
                _result.result.currency = currency;
                _result.result.address = _response.Content;
                _result.SetSuccess();
            }
            else
            {
                var _message = privateClient.GetResponseMessage(_response);
                _result.SetFailure(_message.message);
            }

            return _result;
        }

        /// <summary>
        /// Request a withdrawal to an external wallet.
        /// </summary>
        /// <param name="currency"></param>
        /// <param name="address">coin address for send</param>
        /// <param name="quantity">amount of coin</param>
        /// <returns></returns>
        public async ValueTask<Transfer> RequestWithdrawal(string currency, string address, decimal quantity)
        {
            var _result = new Transfer();

            var _params = new Dictionary<string, object>();
            {
                _params.Add("currency", currency);
                _params.Add("address", address);
                _params.Add("amount", quantity);
            }

            var _response = await privateClient.CallApiPost2Async("/api/v1/user/requestWithdrawal", _params);
#if DEBUG
            _result.rawJson = _response.Content;
#endif
            if (_response.IsSuccessful == true)
            {
                var _withdraw = privateClient.DeserializeObject<BTransferItem>(_response.Content);
                if (_withdraw != null && String.IsNullOrEmpty(_withdraw.transferId) == false)
                {
                    _withdraw.transactionType = TransactionType.Withdraw;
                    _withdraw.confirmations = 0;
                    _withdraw.isCompleted = true;

                    _result.result = _withdraw;
                    _result.SetSuccess();
                }
            }
            else
            {
                var _message = privateClient.GetResponseMessage(_response);
                _result.SetFailure(_message.message);
            }

            return _result;
        }

        /// <summary>
        /// Get a history of all of your wallet transactions (deposits, withdrawals, PNL).
        /// </summary>
        /// <param name="currency"></param>
        /// <param name="count">Number of results to fetch.</param>
        /// <param name="start">Starting point for results.</param>
        /// <returns></returns>
        public async ValueTask<Transfers> GetWalletHistory(string currency, long count, long start = 0)
        {
            var _result = new Transfers();

            var _params = new Dictionary<string, object>();
            {
                _params.Add("currency", currency);
                if (count > 0)
                    _params.Add("count", count);
                _params.Add("start", start);
            }

            var _response = await privateClient.CallApiGet2Async("/api/v1/user/walletHistory", _params);
#if DEBUG
            _result.rawJson = _response.Content;
#endif
            if (_response.IsSuccessful == true)
            {
                var _transfers = privateClient.DeserializeObject<List<BTransferItem>>(_response.Content);
                if (_transfers != null)
                {
                    foreach (var _t in _transfers)
                    {
                        _t.toAddress = String.IsNullOrEmpty(_t.toAddress) == false ? _t.toAddress : "undefined";

                        if (_t.transactionType == TransactionType.Deposit)
                        {
                            _t.fromAddress = _t.toAddress;
                            _t.fromTag = _t.toTag;

                            _t.toAddress = "";
                            _t.toTag = "";
                        }

                        _t.transferType = TransferTypeConverter.FromString(_t.transactStatus);
                        _t.isCompleted = (_t.transferType == TransferType.Done);

                        _t.transactionId = (_t.timestamp * 1000).ToString();
                    }

                    _result.result = _transfers.ToList<ITransferItem>();
                    _result.SetSuccess();
                }
            }
            else
            {
                var _message = privateClient.GetResponseMessage(_response);
                _result.SetFailure(_message.message);
            }

            return _result;
        }

        /// <summary>
        /// Get your account's margin status.
        /// </summary>
        /// <param name="base_name">The type of trading base-currency of which information you want to query for.</param>
        /// <returns></returns>
        public async ValueTask<Balance> GetUserMargin(string base_name)
        {
            var _result = new Balance();

            var _params = new Dictionary<string, object>();
            {
                _params.Add("currency", base_name);
            }

            var _response = await privateClient.CallApiGet2Async("/api/v1/user/margin", _params);
#if DEBUG
            _result.rawJson = _response.Content;
#endif
            if (_response.IsSuccessful == true)
            {
                var _balance = privateClient.DeserializeObject<BBalanceItem>(_response.Content);
                if (_balance != null)
                {
                    _balance.used = _balance.total - _balance.free;

                    _result.result = _balance;
                    _result.SetSuccess();
                }
            }
            else
            {
                var _message = privateClient.GetResponseMessage(_response);
                _result.SetFailure(_message.message);
            }

            return _result;
        }

        /// <summary>
        /// Get your account's margin status.
        /// </summary>
        /// <returns></returns>
        public async ValueTask<Balances> GetUserMargins()
        {
            var _result = new Balances();

            var _params = new Dictionary<string, object>();
            {
                _params.Add("currency", "all");
            }

            var _response = await privateClient.CallApiGet2Async("/api/v1/user/margin", _params);
#if DEBUG
            _result.rawJson = _response.Content;
#endif
            if (_response.IsSuccessful == true)
            {
                var _balances = privateClient.DeserializeObject<List<BBalanceItem>>(_response.Content);
                if (_balances != null)
                {
                    foreach (var _balance in _balances)
                        _balance.used = _balance.total - _balance.free;

                    _result.result = _balances.ToList<IBalanceItem>();
                    _result.SetSuccess();
                }
            }
            else
            {
                var _message = privateClient.GetResponseMessage(_response);
                _result.SetFailure(_message.message);
            }

            return _result;
        }

        /// <summary>
        /// Get your account's information.
        /// </summary>
        /// <returns></returns>
        public async ValueTask<BUserInfo> GetUserInfo()
        {
            var _result = new BUserInfo();

            var _response = await privateClient.CallApiGet2Async("/api/v1/user");
#if DEBUG
            _result.rawJson = _response.Content;
#endif
            if (_response.IsSuccessful == true)
            {
                var _user_info = privateClient.DeserializeObject<BUserInfoItem>(_response.Content);
                if (_user_info != null)
                {
                    _result.result = _user_info;
                    _result.SetSuccess();
                }
            }
            else
            {
                var _message = privateClient.GetResponseMessage(_response);
                _result.SetFailure(_message.message);
            }

            return _result;
        }

        /// <summary>
        /// To get open orders on a symbol.
        /// </summary>
        /// <param name="symbol">Instrument symbol. Send a bare series (e.g. XBT) to get data for the nearest expiring contract in that series.</param>        
        /// <param name="count">Number of results to fetch.</param>
        /// <param name="start">Starting point for results.</param>
        /// <returns></returns>
        public async ValueTask<MyOrders> GetOrders(string symbol, long count = 0, long start = 0)
        {
            var _result = new MyOrders();

            var _params = new Dictionary<string, object>();
            {
                _params.Add("symbol", symbol);
                if (count > 0)
                    _params.Add("count", count);
                _params.Add("start", start);
                _params.Add("reverse", true);
                _params.Add("filter", new CArgument
                {
                    isJson = true,
                    value = new Dictionary<string, object>
                        {
                            { "open", true }
                        }
                });
            }

            var _response = await privateClient.CallApiGet2Async("/api/v1/order", _params);
#if DEBUG
            _result.rawJson = _response.Content;
#endif
            if (_response.IsSuccessful == true)
            {
                var _orders = privateClient.DeserializeObject<List<BMyOrderItem>>(_response.Content);
                if (_orders != null)
                {
                    foreach (var _o in _orders)
                    {
                        _o.makerType = MakerType.Maker;

                        _o.amount = _o.price * _o.quantity;
                        _o.filled = Math.Max(_o.quantity - _o.remaining, 0);
                        _o.cost = _o.price * _o.filled;
                    }

                    _result.result = _orders.ToList<IMyOrderItem>();
                    _result.SetSuccess();
                }
            }
            else
            {
                var _message = privateClient.GetResponseMessage(_response);
                _result.SetFailure(_message.message);
            }

            return _result;
        }

        /// <summary>
        /// Get all open orders on a symbol. Careful when accessing this with no symbol.
        /// </summary>
        /// <param name="count">Number of results to fetch.</param>
        /// <param name="start">Starting point for results.</param>
        /// <returns></returns>
        public async ValueTask<MyOrders> GetAllOrders(long count, long start = 0)
        {
            var _result = new MyOrders();

            var _params = new Dictionary<string, object>();
            {
                if (count > 0)
                    _params.Add("count", count);
                _params.Add("start", start);
                _params.Add("reverse", true);
                _params.Add("filter", new CArgument
                {
                    isJson = true,
                    value = new Dictionary<string, object>
                        {
                            { "open", true }
                        }
                });
            }

            var _response = await privateClient.CallApiGet2Async("/api/v1/order", _params);
#if DEBUG
            _result.rawJson = _response.Content;
#endif
            if (_response.IsSuccessful == true)
            {
                var _orders = privateClient.DeserializeObject<List<BMyOrderItem>>(_response.Content);
                if (_orders != null)
                {
                    foreach (var _o in _orders.Where(o => OrderStatusConverter.IsAlive(o.orderStatus) == true))
                    {
                        _o.makerType = MakerType.Maker;

                        _o.amount = _o.price * _o.quantity;
                        _o.filled = Math.Max(_o.quantity - _o.remaining, 0);
                        _o.cost = _o.price * _o.filled;
                    }

                    _result.result = _orders.ToList<IMyOrderItem>();
                    _result.SetSuccess();
                }
            }
            else
            {
                var _message = privateClient.GetResponseMessage(_response);
                _result.SetFailure(_message.message);
            }

            return _result;
        }

        /// <summary>
        /// To get open positions on a symbol.
        /// </summary>
        /// <param name="symbol">The contract for this position.</param>
        /// <param name="count">Number of results to fetch.</param>
        /// <returns></returns>
        public async ValueTask<MyPositions> GetPositions(string symbol, long count)
        {
            var _result = new MyPositions();

            var _params = new Dictionary<string, object>();
            {
                if (count > 0)
                    _params.Add("count", count);
                _params.Add("filter", new CArgument
                {
                    isJson = true,
                    value = new Dictionary<string, object>
                        {
                            { "symbol", symbol }
                        }
                });
            }

            var _response = await privateClient.CallApiGet2Async("/api/v1/position", _params);
#if DEBUG
            _result.rawJson = _response.Content;
#endif
            if (_response.IsSuccessful == true)
            {
                var _positions = privateClient.DeserializeObject<List<BMyPositionItem>>(_response.Content);
                if (_positions != null)
                {
                    foreach (var _p in _positions)
                    {
                        _p.orderType = OrderType.Position;

                        _p.orderStatus = _p.isOpen ? OrderStatus.Open : OrderStatus.Closed;
                        _p.sideType = _p.quantity > 0 ? SideType.Bid : _p.quantity < 0 ? SideType.Ask : SideType.Unknown;

                        _p.quantity = Math.Abs(_p.quantity);
                        _p.amount = _p.price * _p.quantity;
                    }

                    _result.result = _positions.ToList<IMyPositionItem>();
                    _result.SetSuccess();
                }
            }
            else
            {
                var _message = privateClient.GetResponseMessage(_response);
                _result.SetFailure(_message.message);
            }

            return _result;
        }

        /// <summary>
        /// Get open positions
        /// </summary>
        /// <returns></returns>
        public async ValueTask<MyPositions> GetAllPositions()
        {
            var _result = new MyPositions();

            var _response = await privateClient.CallApiGet2Async("/api/v1/position");
#if DEBUG
            _result.rawJson = _response.Content;
#endif
            if (_response.IsSuccessful == true)
            {
                var _positions = privateClient.DeserializeObject<List<BMyPositionItem>>(_response.Content);
                if (_positions != null)
                {
                    foreach (var _p in _positions)
                    {
                        _p.orderType = OrderType.Position;

                        _p.orderStatus = _p.isOpen ? OrderStatus.Open : OrderStatus.Closed;
                        _p.sideType = _p.quantity > 0 ? SideType.Bid : _p.quantity < 0 ? SideType.Ask : SideType.Unknown;

                        _p.quantity = Math.Abs(_p.quantity);
                        _p.amount = _p.price * _p.quantity;
                    }

                    _result.result = _positions.ToList<IMyPositionItem>();
                    _result.SetSuccess();
                }
            }
            else
            {
                var _message = privateClient.GetResponseMessage(_response);
                _result.SetFailure(_message.message);
            }

            return _result;
        }

        /// <summary>
        /// Get all balance-affecting executions. This includes each trade, insurance charge, and settlement.
        /// </summary>
        /// <param name="symbol">Instrument symbol. Send a bare series (e.g. XBT) to get data for the nearest expiring contract in that series.</param>
        /// <param name="count">maximum number of items</param>
        /// <returns></returns>
        public async ValueTask<MyTrades> GetTrades(string symbol, int count)
        {
            var _result = new MyTrades();

            var _params = new Dictionary<string, object>();
            {
                _params.Add("symbol", symbol);
                if (count > 0)
                    _params.Add("count", count);
                _params.Add("reverse", true);
            }

            var _response = await privateClient.CallApiGet2Async("/api/v1/execution/tradeHistory", _params);
#if DEBUG
            _result.rawJson = _response.Content;
#endif
            if (_response.IsSuccessful == true)
            {
                var _trades = privateClient.DeserializeObject<List<BMyTradeItem>>(_response.Content);
                if (_trades != null)
                {
                    foreach (var _t in _trades)
                        _t.amount = _t.price * _t.quantity;

                    _result.result = _trades.ToList<IMyTradeItem>();
                    _result.SetSuccess();
                }
            }
            else
            {
                var _message = privateClient.GetResponseMessage(_response);
                _result.SetFailure(_message.message);
            }

            return _result;
        }

        /// <summary>
        /// Create a new limit order.
        /// </summary>
        /// <param name="symbol">Instrument symbol. e.g. 'XBTUSD'.</param>
        /// <param name="quantity">amount of coin</param>
        /// <param name="price">price of coin</param>
        /// <param name="sideType">type of buy(bid) or sell(ask)</param>
        /// <param name="execInst">Optional execution instructions.</param>
        /// <returns></returns>
        public async ValueTask<MyOrder> CreateLimitOrder(string symbol, decimal quantity, decimal price, SideType sideType, string execInst = "")
        {
            var _result = new MyOrder();

            var _params = new Dictionary<string, object>();
            {
                _params.Add("symbol", symbol);
                _params.Add("side", sideType == SideType.Bid ? "Buy" : "Sell");
                _params.Add("ordType", "Limit");
                _params.Add("orderQty", quantity);
                _params.Add("price", price);
                if (String.IsNullOrEmpty(execInst) == false)
                    _params.Add("execInst", execInst);
            }

            var _response = await privateClient.CallApiPost2Async("/api/v1/order", _params);
#if DEBUG
            _result.rawJson = _response.Content;
#endif
            if (_response.IsSuccessful == true)
            {
                var _order = privateClient.DeserializeObject<BPlaceOrderItem>(_response.Content);
                if (_order != null)
                {
                    _order.orderType = OrderType.Limit;

                    _order.remaining = Math.Max(_order.quantity - _order.filled, 0);
                    _order.cost = _order.price * _order.filled;

                    _result.result = _order;
                    _result.SetSuccess();
                }
            }
            else
            {
                var _message = privateClient.GetResponseMessage(_response);
                _result.SetFailure(_message.message);
            }

            return _result;
        }

        /// <summary>
        /// Create a new market order.
        /// </summary>
        /// <param name="symbol">Instrument symbol. e.g. 'XBTUSD'.</param>
        /// <param name="quantity">amount of coin</param>
        /// <param name="sideType">type of buy(bid) or sell(ask)</param>
        /// <param name="execInst">Optional execution instructions.</param>
        /// <returns></returns>
        public async ValueTask<MyOrder> CreateMarketOrder(string symbol, decimal quantity, SideType sideType, string execInst = "")
        {
            var _result = new MyOrder();

            var _params = new Dictionary<string, object>();
            {
                _params.Add("symbol", symbol);
                _params.Add("side", sideType == SideType.Bid ? "Buy" : "Sell");
                _params.Add("ordType", "Market");
                _params.Add("orderQty", quantity);
                if (String.IsNullOrEmpty(execInst) == false)
                    _params.Add("execInst", execInst);
            }

            var _response = await privateClient.CallApiPost2Async("/api/v1/order", _params);
#if DEBUG
            _result.rawJson = _response.Content;
#endif
            if (_response.IsSuccessful == true)
            {
                var _order = privateClient.DeserializeObject<BPlaceOrderItem>(_response.Content);
                if (_order != null)
                {
                    _order.orderType = OrderType.Market;

                    _result.result = _order;
                    _result.SetSuccess();
                }
            }
            else
            {
                var _message = privateClient.GetResponseMessage(_response);
                _result.SetFailure(_message.message);
            }

            return _result;
        }

        /// <summary>
        /// Create multiple new orders for the same symbol.
        /// </summary>
        /// <param name="orders"></param>
        /// <returns></returns>
        public async ValueTask<MyOrders> CreateBulkOrder(List<BBulkOrderItem> orders)
        {
            var _result = new MyOrders();

            var _params = new Dictionary<string, object>();
            {
                _params.Add("orders", orders);
            }

            var _response = await privateClient.CallApiPost2Async("/api/v1/order/bulk", _params);
#if DEBUG
            _result.rawJson = _response.Content;
#endif
            if (_response.IsSuccessful == true)
            {
                var _orders = privateClient.DeserializeObject<List<BMyOrderItem>>(_response.Content);
                if (_orders != null)
                {
                    _orders.ForEach(o => o.amount = o.quantity * o.price);

                    _result.result = _orders.ToList<IMyOrderItem>();
                    _result.SetSuccess();
                }
            }
            else
            {
                var _message = privateClient.GetResponseMessage(_response);
                _result.SetFailure(_message.message);
            }

            return _result;
        }

        /// <summary>
        /// Close a position
        /// </summary>
        /// <param name="symbol">Instrument symbol. e.g. 'XBTUSD'.</param>
        /// <param name="orderType">The type of order is limit, market or position</param>
        /// <param name="price">price of coin</param>
        /// <returns></returns>
        public async ValueTask<MyOrder> ClosePosition(string symbol, OrderType orderType, decimal price = 0.0m)
        {
            var _result = new MyOrder();

            var _params = new Dictionary<string, object>();
            {
                _params.Add("symbol", symbol);
                _params.Add("execInst", "Close");

                if (orderType == OrderType.Limit)
                    _params.Add("price", price);
            }

            var _response = await privateClient.CallApiPost2Async("/api/v1/order", _params);
#if DEBUG
            _result.rawJson = _response.Content;
#endif
            if (_response.IsSuccessful == true)
            {
                var _order = privateClient.DeserializeObject<BMyOrderItem>(_response.Content);
                if (_order != null)
                {
                    _result.result = _order;
                    _result.SetSuccess();
                }
            }
            else
            {
                var _message = privateClient.GetResponseMessage(_response);
                _result.SetFailure(_message.message);
            }

            return _result;
        }

        /// <summary>
        /// Amend the quantity or price of an open order.
        /// </summary>
        /// <param name="order_id">Order number registered for sale or purchase</param>
        /// <param name="quantity">amount of coin</param>
        /// <param name="price">price of coin</param>
        /// <param name="execInst">Optional execution instructions.</param>
        /// <returns></returns>
        public async ValueTask<MyOrder> UpdateOrder(string order_id, decimal quantity, decimal price, string execInst = "")
        {
            var _result = new MyOrder();

            var _params = new Dictionary<string, object>();
            {
                _params.Add("orderID", order_id);
                _params.Add("orderQty", quantity);
                _params.Add("price", price);
                if (String.IsNullOrEmpty(execInst) == false)
                    _params.Add("execInst", execInst);
            }

            var _response = await privateClient.CallApiPut2Async("/api/v1/order", _params);
#if DEBUG
            _result.rawJson = _response.Content;
#endif
            if (_response.IsSuccessful == true)
            {
                var _order = privateClient.DeserializeObject<BPlaceOrderItem>(_response.Content);
                if (_order != null)
                {
                    _result.result = _order;
                    _result.SetSuccess();
                }
            }
            else
            {
                var _message = privateClient.GetResponseMessage(_response);
                _result.SetFailure(_message.message);
            }

            return _result;
        }

        /// <summary>
        /// Amend multiple orders for the same symbol.
        /// </summary>
        /// <param name="order_id">Order number registered for sale or purchase</param>
        /// <param name="quantity">amount of coin</param>
        /// <param name="price">price of coin</param>
        /// <returns></returns>
        public async ValueTask<MyOrders> UpdateBulkOrder(List<BBulkOrderItem> orders)
        {
            var _result = new MyOrders();

            var _params = new Dictionary<string, object>();
            {
                _params.Add("orders", orders);
            }

            var _response = await privateClient.CallApiPut2Async("/api/v1/order/bulk", _params);
#if DEBUG
            _result.rawJson = _response.Content;
#endif
            if (_response.IsSuccessful == true)
            {
                var _orders = privateClient.DeserializeObject<List<BPlaceOrderItem>>(_response.Content);
                if (_orders != null)
                {
                    _result.result = _orders.ToList<IMyOrderItem>();
                    _result.SetSuccess();
                }
            }
            else
            {
                var _message = privateClient.GetResponseMessage(_response);
                _result.SetFailure(_message.message);
            }

            return _result;
        }

        /// <summary>
        /// Cancel an order.
        /// </summary>
        /// <param name="order_id">Order number registered for sale or purchase</param>
        /// <returns></returns>
        public async ValueTask<MyOrder> CancelOrder(string order_id)
        {
            var _result = new MyOrder();

            var _params = new Dictionary<string, object>();
            {
                _params.Add("orderID", order_id);
            }

            var _response = await privateClient.CallApiDelete2Async("/api/v1/order", _params);
#if DEBUG
            _result.rawJson = _response.Content;
#endif
            if (_response.IsSuccessful == true)
            {
                var _orders = privateClient.DeserializeObject<List<BPlaceOrderItem>>(_response.Content);
                if (_orders != null)
                {
                    _result.result = _orders.FirstOrDefault();
                    _result.SetSuccess();
                }
            }
            else
            {
                var _message = privateClient.GetResponseMessage(_response);
                _result.SetFailure(_message.message);
            }

            return _result;
        }

        /// <summary>
        /// Cancel orders. Send multiple order IDs to cancel in bulk.
        /// </summary>
        /// <param name="order_ids"></param>
        /// <returns></returns>
        public async ValueTask<MyOrders> CancelOrders(string[] order_ids)
        {
            var _result = new MyOrders();

            var _params = new Dictionary<string, object>();
            {
                _params.Add("orderID", order_ids);
            }

            var _response = await privateClient.CallApiDelete2Async("/api/v1/order", _params);
#if DEBUG
            _result.rawJson = _response.Content;
#endif
            if (_response.IsSuccessful == true)
            {
                var _orders = privateClient.DeserializeObject<List<BPlaceOrderItem>>(_response.Content);
                if (_orders != null)
                {
                    _result.result.AddRange(_orders);
                    _result.SetSuccess();
                }
            }
            else
            {
                var _message = privateClient.GetResponseMessage(_response);
                _result.SetFailure(_message.message);
            }

            return _result;
        }

        /// <summary>
        /// Cancels all of your orders.
        /// </summary>
        /// <returns></returns>
        public async ValueTask<MyOrders> CancelAllOrders()
        {
            var _result = new MyOrders();

            var _response = await privateClient.CallApiDelete2Async("/api/v1/order/all");
#if DEBUG
            _result.rawJson = _response.Content;
#endif
            if (_response.IsSuccessful == true)
            {
                var _orders = privateClient.DeserializeObject<List<BPlaceOrderItem>>(_response.Content);
                if (_orders != null)
                {
                    _result.result.AddRange(_orders);
                    _result.SetSuccess();
                }
            }
            else
            {
                var _message = privateClient.GetResponseMessage(_response);
                _result.SetFailure(_message.message);
            }

            return _result;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="symbol">Symbol of position to adjust.</param>
        /// <param name="leverage">Leverage value. Send a number between 0.01 and 100 to enable isolated margin with a fixed leverage. Send 0 to enable cross margin.</param>
        /// <returns></returns>
        public async ValueTask<MyPosition> ChooseLeverage(string symbol, decimal leverage)
        {
            var _result = new MyPosition();

            var _params = new Dictionary<string, object>();
            {
                _params.Add("symbol", symbol);
                _params.Add("leverage", leverage);
            }

            var _response = await privateClient.CallApiPost2Async("/api/v1/position/leverage", _params);
#if DEBUG
            _result.rawJson = _response.Content;
#endif
            if (_response.IsSuccessful == true)
            {
                var _position = privateClient.DeserializeObject<BMyPositionItem>(_response.Content);
                if (_position != null)
                {
                    _result.result = _position;
                    _result.SetSuccess();
                }
            }
            else
            {
                var _message = privateClient.GetResponseMessage(_response);
                _result.SetFailure(_message.message);
            }

            return _result;
        }
    }
}