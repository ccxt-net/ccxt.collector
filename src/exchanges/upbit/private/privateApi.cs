using OdinSdk.BaseLib.Coin;
using OdinSdk.BaseLib.Coin.Private;
using OdinSdk.BaseLib.Coin.Trade;
using OdinSdk.BaseLib.Coin.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CCXT.Collector.Upbit.Private
{
    public class PrivateApi : OdinSdk.BaseLib.Coin.Private.PrivateApi, IPrivateApi
    {
        private readonly string __connect_key;
        private readonly string __secret_key;

        /// <summary>
        ///
        /// </summary>
        public PrivateApi(string connect_key, string secret_key)
        {
            __connect_key = connect_key;
            __secret_key = secret_key;
        }

        /// <summary>
        ///
        /// </summary>
        public override XApiClient privateClient
        {
            get
            {
                if (base.privateClient == null)
                    base.privateClient = new UpbitClient("private", __connect_key, __secret_key);

                return base.privateClient;
            }
        }

        /// <summary>
        /// 전체 계좌 조회: 내가 보유한 자산 리스트를 보여줍니다.
        /// </summary>
        /// <returns></returns>
        public async Task<Balances> GetBalances()
        {
            var _result = new Balances();

            var _response = await privateClient.CallApiGet2Async("/accounts");
#if DEBUG
            _result.rawJson = _response.Content;
#endif
            if (_response.IsSuccessful == true)
            {
                var _balances = privateClient.DeserializeObject<List<UBalanceItem>>(_response.Content);
                {
                    foreach (var _balance in _balances)
                        _balance.total = _balance.free + _balance.used;

                    _result.result = _balances.ToList<IBalanceItem>();
                }
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
        /// 주문 가능 정보: 마켓별 주문 가능 정보를 확인한다.
        /// </summary>
        /// <param name="base_name">코인명</param>
        /// <param name="quote_name">화폐명</param>
        /// <returns></returns>
        public async Task<MyOrdersChance> GetOrdersChance(string base_name, string quote_name)
        {
            var _result = new MyOrdersChance();

            var _params = new Dictionary<string, object>();
            {
                _params.Add("market", $"{quote_name}-{base_name}");
            }

            var _response = await privateClient.CallApiGet2Async("/orders/chance", _params);
#if DEBUG
            _result.rawJson = _response.Content;
#endif
            if (_response.IsSuccessful == true)
            {
                _result.result = privateClient.DeserializeObject<OrdersChance>(_response.Content);
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
        /// 개별 주문 조회: 주문 UUID 를 통해 개별 주문건을 조회한다.
        /// </summary>
        /// <param name="order_id">주문 UUID</param>
        /// <returns></returns>
        public async Task<MyOrder> GetOrder(string order_id)
        {
            var _result = new MyOrder();

            var _params = new Dictionary<string, object>();
            {
                _params.Add("uuid", order_id);
            }

            var _response = await privateClient.CallApiGet2Async("/order", _params);
#if DEBUG
            _result.rawJson = _response.Content;
#endif
            if (_response.IsSuccessful == true)
            {
                var _order = privateClient.DeserializeObject<UMyOrderItem>(_response.Content);
                {
                    _order.amount = _order.price * _order.quantity;
                    _result.result = _order;
                }
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
        /// 주문 리스트 조회: 주문 리스트를 조회한다.
        /// </summary>
        /// <param name="base_name">코인명</param>
        /// <param name="quote_name">화폐명</param>
        /// <returns></returns>
        public async Task<MyOrders> GetOrders(string base_name, string quote_name)
        {
            var _result = new MyOrders();

            var _params = new Dictionary<string, object>();
            {
                _params.Add("market", $"{quote_name}-{base_name}");
                _params.Add("state", "wait");
                _params.Add("page", 1);
                _params.Add("order_by", "asc");
            }

            var _response = await privateClient.CallApiGet2Async("/orders", _params);
#if DEBUG
            _result.rawJson = _response.Content;
#endif
            if (_response.IsSuccessful == true)
            {
                var _orders = privateClient.DeserializeObject<List<UMyOrderItem>>(_response.Content);
                {
                    foreach (var _o in _orders)
                        _o.amount = _o.price * _o.quantity;
                    _result.result = _orders.ToList<IMyOrderItem>();
                }
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
        /// 주문 리스트 조회: 주문 리스트를 조회한다.
        /// </summary>
        /// <param name="base_name">코인명</param>
        /// <param name="quote_name">화폐명</param>
        /// <returns></returns>
        public async Task<MyTrades> GetTrades(string base_name, string quote_name)
        {
            var _result = new MyTrades();

            var _params = new Dictionary<string, object>();
            {
                _params.Add("market", $"{quote_name}-{base_name}");
                _params.Add("state", "done");
                _params.Add("page", 1);
                _params.Add("order_by", "asc");
            }

            var _response = await privateClient.CallApiGet2Async("/orders", _params);
#if DEBUG
            _result.rawJson = _response.Content;
#endif
            if (_response.IsSuccessful == true)
            {
                var _trades = privateClient.DeserializeObject<List<UMyTradeItem>>(_response.Content);
                {
                    foreach (var _t in _trades)
                        _t.amount = _t.price * _t.quantity;
                    _result.result = _trades.ToList<IMyTradeItem>();
                }
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
        /// 전체 주문 리스트 조회: 전체 주문 리스트를 조회한다.
        /// </summary>
        /// <returns></returns>
        public async Task<MyOrders> GetAllOrders()
        {
            var _result = new MyOrders();

            var _params = new Dictionary<string, object>();
            {
                _params.Add("state", "wait");
                _params.Add("page", 1);
                _params.Add("order_by", "asc");
            }

            var _response = await privateClient.CallApiGet2Async("/orders", _params);
#if DEBUG
            _result.rawJson = _response.Content;
#endif
            if (_response.IsSuccessful == true)
            {
                var _orders = privateClient.DeserializeObject<List<UMyOrderItem>>(_response.Content);
                {
                    foreach (var _o in _orders)
                        _o.amount = _o.price * _o.quantity;
                    _result.result = _orders.ToList<IMyOrderItem>();
                }
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
        /// 주문 취소 접수: 주문 UUID를 통해 해당 주문에 대한 취소 접수를 한다.
        /// </summary>
        /// <param name="order_id">주문 UUID</param>
        /// <returns></returns>
        public async Task<MyOrder> CancelOrder(string order_id)
        {
            var _result = new MyOrder();

            var _params = new Dictionary<string, object>();
            {
                _params.Add("uuid", order_id);
            }

            var _response = await privateClient.CallApiDelete2Async("/order", _params);
#if DEBUG
            _result.rawJson = _response.Content;
#endif
            if (_response.IsSuccessful == true)
            {
                var _order = privateClient.DeserializeObject<UPlaceOrderItem>(_response.Content);
                {
                    _order.amount = _order.price * _order.quantity;
                    _result.result = _order;
                }
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
        /// 주문하기: 주문 요청을 한다.
        /// </summary>
        /// <param name="base_name">코인명</param>
        /// <param name="quote_name">화폐명</param>
        /// <param name="quantity">주문 수량</param>
        /// <param name="price">유닛당 주문 가격</param>
        /// <param name="sideType">주문 타입</param>
        /// <returns></returns>
        public async Task<MyOrder> PutOrder(string base_name, string quote_name, decimal quantity, decimal price, SideType sideType)
        {
            var _result = new MyOrder();

            var _params = new Dictionary<string, object>();
            {
                _params.Add("market", $"{quote_name}-{base_name}");
                _params.Add("side", sideType == SideType.Bid ? "bid" : "ask");
                _params.Add("volume", quantity);
                _params.Add("price", price);
                _params.Add("ord_type", "limit");
            }

            var _response = await privateClient.CallApiPost2Async("/orders", _params);
#if DEBUG
            _result.rawJson = _response.Content;
#endif
            if (_response.IsSuccessful == true)
            {
                var _order = privateClient.DeserializeObject<UPlaceOrderItem>(_response.Content);
                {
                    _order.amount = _order.quantity * _order.price;
                    _result.result = _order;
                }
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
        /// 출금 리스트 조회
        /// </summary>
        /// <param name="currency_id">Currency 코드</param>
        /// <param name="limits">You can set the maximum number of transactions you want to get with this parameter</param>
        /// <returns></returns>
        public async Task<Transfers> GetWithdraws(string currency_id, int limits = 20)
        {
            var _result = new Transfers();

            var _params = new Dictionary<string, object>();
            {
                _params.Add("currency", currency_id);
                _params.Add("limit", limits);
            }

            var _response = await privateClient.CallApiGet2Async("/withdraws", _params);
#if DEBUG
            _result.rawJson = _response.Content;
#endif
            if (_response.IsSuccessful == true)
            {
                var _transfers = privateClient.DeserializeObject<List<UTransferItem>>(_response.Content);
                {
                    _result.result = _transfers.ToList<ITransferItem>();
                }
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
        /// 모든 출금 리스트 조회
        /// </summary>
        /// <param name="limits">You can set the maximum number of transactions you want to get with this parameter</param>
        /// <returns></returns>
        public async Task<Transfers> GetAllWithdraws(int limits = 20)
        {
            var _result = new Transfers();

            var _params = new Dictionary<string, object>();
            {
                _params.Add("limit", limits);
            }

            var _response = await privateClient.CallApiGet2Async("/withdraws", _params);
#if DEBUG
            _result.rawJson = _response.Content;
#endif
            if (_response.IsSuccessful == true)
            {
                var _transfers = privateClient.DeserializeObject<List<UTransferItem>>(_response.Content);
                {
                    _result.result = _transfers.ToList<ITransferItem>();
                }
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
        /// 개별 출금 조회: 출금 UUID를 통해 개별 출금 정보를 조회한다.
        /// </summary>
        /// <param name="withdrawId"></param>
        /// <returns></returns>
        public async Task<Transfer> GetWithdraw(string withdrawId)
        {
            var _result = new Transfer();

            var _params = new Dictionary<string, object>();
            {
                _params.Add("uuid", withdrawId);
            }

            var _response = await privateClient.CallApiGet2Async("/withdraw", _params);
#if DEBUG
            _result.rawJson = _response.Content;
#endif
            if (_response.IsSuccessful == true)
            {
                _result.result = privateClient.DeserializeObject<UTransferItem>(_response.Content);
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
        /// 출금 가능 정보: 해당 통화의 가능한 출금 정보를 확인한다.
        /// </summary>
        /// <param name="currency_id">Currency 코드</param>
        /// <returns></returns>
        public async Task<MyWithdrawsChance> GetWithdrawsChance(string currency_id)
        {
            var _result = new MyWithdrawsChance();

            var _params = new Dictionary<string, object>();
            {
                _params.Add("currency", currency_id);
            }

            var _response = await privateClient.CallApiGet2Async("/withdraws/chance", _params);
#if DEBUG
            _result.rawJson = _response.Content;
#endif
            if (_response.IsSuccessful == true)
            {
                _result.result = privateClient.DeserializeObject<WithdrawsChance>(_response.Content);
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
        /// 코인 출금하기: 코인 출금을 요청한다.
        /// </summary>
        /// <param name="currency_id">Currency symbol</param>
        /// <param name="amount">출금 코인 수량</param>
        /// <param name="address">출금 지갑 주소</param>
        /// <param name="tag">Secondary address identifier for coins like XRP,XMR etc.</param>
        /// <returns></returns>
        public async Task<Transfer> WithdrawsCoin(string currency_id, decimal amount, string address, string tag = "")
        {
            var _result = new Transfer();

            var _params = new Dictionary<string, object>();
            {
                _params.Add("currency", currency_id);
                _params.Add("amount", amount);
                _params.Add("address", address);
                if (String.IsNullOrEmpty(tag) == false)
                    _params.Add("secondary_address", tag);
            }

            var _response = await privateClient.CallApiPost2Async("/withdraws/coin", _params);
#if DEBUG
            _result.rawJson = _response.Content;
#endif
            if (_response.IsSuccessful == true)
            {
                var _transfer = privateClient.DeserializeObject<UTransferItem>(_response.Content);
                {
                    _transfer.toAddress = address;
                    _transfer.toTag = tag;
                }

                _result.result = _transfer;
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
        /// 원화 출금하기: 원화 출금을 요청한다.등록된 출금 계좌로 출금된다.
        /// </summary>
        /// <param name="amount"></param>
        /// <returns></returns>
        public async Task<Transfer> WithdrawsKrw(decimal amount)
        {
            var _result = new Transfer();

            var _params = new Dictionary<string, object>();
            {
                _params.Add("amount", amount);
            }

            var _response = await privateClient.CallApiPost2Async("/withdraws/krw", _params);
#if DEBUG
            _result.rawJson = _response.Content;
#endif
            if (_response.IsSuccessful == true)
            {
                _result.result = privateClient.DeserializeObject<UTransferItem>(_response.Content);
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
        /// 입금 리스트 조회
        /// </summary>
        /// <param name="currency_id"></param>
        /// <returns></returns>
        public async Task<Transfers> GetDeposits(string currency_id)
        {
            var _result = new Transfers();

            var _params = new Dictionary<string, object>();
            {
                _params.Add("currency", currency_id);
            }

            var _response = await privateClient.CallApiGet2Async("/deposits", _params);
#if DEBUG
            _result.rawJson = _response.Content;
#endif
            if (_response.IsSuccessful == true)
            {
                var _transfers = privateClient.DeserializeObject<List<UTransferItem>>(_response.Content);
                {
                    _result.result = _transfers.ToList<ITransferItem>();
                }
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
        /// 개별 입금 조회
        /// </summary>
        /// <param name="depositId"></param>
        /// <returns></returns>
        public async Task<Transfer> GetDeposit(string depositId)
        {
            var _result = new Transfer();

            var _params = new Dictionary<string, object>();
            {
                _params.Add("uuid", depositId);
            }

            var _response = await privateClient.CallApiGet2Async("/deposit", _params);
#if DEBUG
            _result.rawJson = _response.Content;
#endif
            if (_response.IsSuccessful == true)
            {
                _result.result = privateClient.DeserializeObject<UTransferItem>(_response.Content);
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
        /// 입금 주소 생성 요청: 입금 주소 생성을 요청한다.
        /// </summary>
        /// <param name="currency_id"></param>
        /// <returns></returns>
        public async Task<Address> DepositsGenerateCoinAddress(string currency_id)
        {
            var _result = new Address();

            var _params = new Dictionary<string, object>();
            {
                _params.Add("currency", currency_id);
            }

            var _response = await privateClient.CallApiPost2Async("/deposits/generate_coin_address", _params);
#if DEBUG
            _result.rawJson = _response.Content;
#endif
            if (_response.IsSuccessful == true)
            {
                var _address = privateClient.DeserializeObject<GenerateAddress>(_response.Content);
                if (_address.success == false)
                {
                    _result.result = _address;
                    _result.SetSuccess();
                }
                else
                {
                    _result.SetFailure(_address.message);
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
        /// 전체 입금 주소 조회: 내가 보유한 자산 리스트를 보여줍니다.
        /// </summary>
        /// <returns></returns>
        public async Task<Addresses> GetDepositsCoinAddresses()
        {
            var _result = new Addresses();

            var _response = await privateClient.CallApiGet2Async("/deposits/coin_addresses");
#if DEBUG
            _result.rawJson = _response.Content;
#endif
            if (_response.IsSuccessful == true)
            {
                var _addresses = privateClient.DeserializeObject<List<UAddress>>(_response.Content);
                {
                    _result.result = _addresses.ToList<IAddressItem>();
                }
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
        /// 개별 입금 주소 조회
        /// </summary>
        /// <param name="currency_id"></param>
        /// <returns></returns>
        public async Task<Address> GetDepositsCoinAddress(string currency_id)
        {
            var _result = new Address();

            var _params = new Dictionary<string, object>();
            {
                _params.Add("currency", currency_id);
            }

            var _response = await privateClient.CallApiGet2Async("/deposits/coin_address", _params);
#if DEBUG
            _result.rawJson = _response.Content;
#endif
            if (_response.IsSuccessful == true)
            {
                _result.result = privateClient.DeserializeObject<UAddress>(_response.Content);
                _result.SetSuccess();
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