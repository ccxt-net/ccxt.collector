using OdinSdk.BaseLib.Coin;
using OdinSdk.BaseLib.Coin.Private;
using OdinSdk.BaseLib.Coin.Types;
using System.Collections.Generic;
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
        public async Task<ApiResult<List<Accounts>>> GetAccounts()
        {
            var _result = new ApiResult<List<Accounts>>();

            var _response = await privateClient.CallApiGet2Async("/accounts");
            if (_response.IsSuccessful == true)
            {
                _result.result = privateClient.DeserializeObject<List<Accounts>>(_response.Content);
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
        /// <param name="baseId">코인명</param>
        /// <param name="quoteId">화폐명</param>
        /// <returns></returns>
        public async Task<ApiResult<OrdersChance>> GetOrdersChance(string baseId, string quoteId)
        {
            var _result = new ApiResult<OrdersChance>();

            var _params = new Dictionary<string, object>();
            {
                _params.Add("market", $"{quoteId}-{baseId}");
            }

            var _response = await privateClient.CallApiGet2Async("/orders/chance", _params);
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
        /// <param name="orderId">주문 UUID</param>
        /// <returns></returns>
        public async Task<ApiResult<UMyOrderItem>> GetOrder(string orderId)
        {
            var _result = new ApiResult<UMyOrderItem>();

            var _params = new Dictionary<string, object>();
            {
                _params.Add("uuid", orderId);
            }

            var _response = await privateClient.CallApiGet2Async("/order", _params);
            if (_response.IsSuccessful == true)
            {
                _result.result = privateClient.DeserializeObject<UMyOrderItem>(_response.Content);
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
        /// <param name="baseId">코인명</param>
        /// <param name="quoteId">화폐명</param>
        /// <returns></returns>
        public async Task<ApiResult<List<UMyOrderItem>>> GetOrders(string baseId, string quoteId)
        {
            var _result = new ApiResult<List<UMyOrderItem>>();

            var _params = new Dictionary<string, object>();
            {
                _params.Add("market", $"{quoteId}-{baseId}");
                _params.Add("state", "wait");
                _params.Add("page", 1);
                _params.Add("order_by", "asc");
            }

            var _response = await privateClient.CallApiGet2Async("/orders", _params);
            if (_response.IsSuccessful == true)
            {
                _result.result = privateClient.DeserializeObject<List<UMyOrderItem>>(_response.Content);
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
        public async Task<ApiResult<List<UMyOrderItem>>> GetAllOrders()
        {
            var _result = new ApiResult<List<UMyOrderItem>>();

            var _params = new Dictionary<string, object>();
            {
                _params.Add("state", "wait");
                _params.Add("page", 1);
                _params.Add("order_by", "asc");
            }

            var _response = await privateClient.CallApiGet2Async("/orders", _params);
            if (_response.IsSuccessful == true)
            {
                _result.result = privateClient.DeserializeObject<List<UMyOrderItem>>(_response.Content);
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
        /// <param name="orderId">주문 UUID</param>
        /// <returns></returns>
        public async Task<ApiResult<UMyOrderItem>> DeleteOrder(string orderId)
        {
            var _result = new ApiResult<UMyOrderItem>();

            var _params = new Dictionary<string, object>();
            {
                _params.Add("uuid", orderId);
            }

            var _response = await privateClient.CallApiDelete2Async("/order", _params);
            if (_response.IsSuccessful == true)
            {
                _result.result = privateClient.DeserializeObject<UMyOrderItem>(_response.Content);
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
        /// <param name="baseId">코인명</param>
        /// <param name="quoteId">화폐명</param>
        /// <param name="quantity">주문 수량</param>
        /// <param name="price">유닛당 주문 가격</param>
        /// <param name="sideType">주문 타입</param>
        /// <returns></returns>
        public async Task<ApiResult<UMyOrderItem>> PutOrder(string baseId, string quoteId, decimal quantity, decimal price, SideType sideType)
        {
            var _result = new ApiResult<UMyOrderItem>();

            var _params = new Dictionary<string, object>();
            {
                _params.Add("market", $"{quoteId}-{baseId}");
                _params.Add("side", sideType == SideType.Bid ? "bid" : "ask");
                _params.Add("volume", quantity);
                _params.Add("price", price);
                _params.Add("ord_type", "limit");
            }

            var _response = await privateClient.CallApiPost2Async("/orders", _params);
            if (_response.IsSuccessful == true)
            {
                _result.result = privateClient.DeserializeObject<UMyOrderItem>(_response.Content);
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
        /// <param name="currencyId">Currency 코드</param>
        /// <returns></returns>
        public async Task<ApiResult<List<UTransferItem>>> GetWithdraws(string currencyId)
        {
            var _result = new ApiResult<List<UTransferItem>>();

            var _params = new Dictionary<string, object>();
            {
                _params.Add("currency", currencyId);
            }

            var _response = await privateClient.CallApiGet2Async("/withdraws", _params);
            if (_response.IsSuccessful == true)
            {
                _result.result = privateClient.DeserializeObject<List<UTransferItem>>(_response.Content);
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
        public async Task<ApiResult<UTransferItem>> GetWithdraw(string withdrawId)
        {
            var _result = new ApiResult<UTransferItem>();

            var _params = new Dictionary<string, object>();
            {
                _params.Add("uuid", withdrawId);
            }

            var _response = await privateClient.CallApiGet2Async("/withdraw", _params);
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
        /// <param name="currencyId">Currency 코드</param>
        /// <returns></returns>
        public async Task<ApiResult<WithdrawsChance>> GetWithdrawsChance(string currencyId)
        {
            var _result = new ApiResult<WithdrawsChance>();

            var _params = new Dictionary<string, object>();
            {
                _params.Add("currency", currencyId);
            }

            var _response = await privateClient.CallApiGet2Async("/withdraws/chance", _params);
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
        /// <param name="currencyId">Currency symbol</param>
        /// <param name="amount">출금 코인 수량</param>
        /// <param name="address">출금 지갑 주소</param>
        /// <returns></returns>
        public async Task<ApiResult<UTransferItem>> WithdrawsCoin(string currencyId, decimal amount, string address)
        {
            var _result = new ApiResult<UTransferItem>();

            var _params = new Dictionary<string, object>();
            {
                _params.Add("currency", currencyId);
                _params.Add("amount", amount);
                _params.Add("address", address);
            }

            var _response = await privateClient.CallApiPost2Async("/withdraws/coin", _params);
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
        /// 원화 출금하기: 원화 출금을 요청한다.등록된 출금 계좌로 출금된다.
        /// </summary>
        /// <param name="amount"></param>
        /// <returns></returns>
        public async Task<ApiResult<UTransferItem>> WithdrawsKrw(decimal amount)
        {
            var _result = new ApiResult<UTransferItem>();

            var _params = new Dictionary<string, object>();
            {
                _params.Add("amount", amount);
            }

            var _response = await privateClient.CallApiPost2Async("/withdraws/krw", _params);
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
        /// <param name="currencyId"></param>
        /// <returns></returns>
        public async Task<ApiResult<List<UTransferItem>>> GetDeposits(string currencyId)
        {
            var _result = new ApiResult<List<UTransferItem>>();

            var _params = new Dictionary<string, object>();
            {
                _params.Add("currency", currencyId);
            }

            var _response = await privateClient.CallApiGet2Async("/deposits", _params);
            if (_response.IsSuccessful == true)
            {
                _result.result = privateClient.DeserializeObject<List<UTransferItem>>(_response.Content);
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
        public async Task<ApiResult<UTransferItem>> GetDeposit(string depositId)
        {
            var _result = new ApiResult<UTransferItem>();

            var _params = new Dictionary<string, object>();
            {
                _params.Add("uuid", depositId);
            }

            var _response = await privateClient.CallApiGet2Async("/deposit", _params);
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
        /// <param name="currencyId"></param>
        /// <returns></returns>
        public async Task<ApiResult<Address>> DepositsGenerateCoinAddress(string currencyId)
        {
            var _result = new ApiResult<Address>();

            var _params = new Dictionary<string, object>();
            {
                _params.Add("currency", currencyId);
            }

            var _response = await privateClient.CallApiPost2Async("/deposits/generate_coin_address", _params);
            if (_response.IsSuccessful == true)
            {
                var _generate = privateClient.DeserializeObject<GenerateAddress>(_response.Content);
                if (_generate.success == false)
                {
                    _result.result = _generate;
                    _result.SetSuccess();
                }
                else
                {
                    _result.SetFailure(_generate.message);
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
        public async Task<ApiResult<List<Address>>> GetDepositsCoinAddresses()
        {
            var _result = new ApiResult<List<Address>>();

            var _response = await privateClient.CallApiGet2Async("/deposits/coin_addresses");
            if (_response.IsSuccessful == true)
            {
                _result.result = privateClient.DeserializeObject<List<Address>>(_response.Content);
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
        /// <param name="currencyId"></param>
        /// <returns></returns>
        public async Task<ApiResult<Address>> GetDepositsCoinAddress(string currencyId)
        {
            var _result = new ApiResult<Address>();

            var _params = new Dictionary<string, object>();
            {
                _params.Add("currency", currencyId);
            }

            var _response = await privateClient.CallApiGet2Async("/deposits/coin_address", _params);
            if (_response.IsSuccessful == true)
            {
                _result.result = privateClient.DeserializeObject<Address>(_response.Content);
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