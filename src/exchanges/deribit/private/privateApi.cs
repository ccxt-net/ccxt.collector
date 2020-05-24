using CCXT.Collector.Deribit.Model;
using OdinSdk.BaseLib.Coin;
using OdinSdk.BaseLib.Coin.Private;
using OdinSdk.BaseLib.Coin.Trade;
using OdinSdk.BaseLib.Coin.Types;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CCXT.Collector.Deribit.Private
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
                    base.privateClient = new DeribitClient(_division, __connect_key, __secret_key);
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

            var _response = await privateClient.CallApiGet2Async("/api/v2/private/get_current_deposit_address", _params);
            if (_response != null)
            {
#if RAWJSON
                _result.rawJson = _response.Content;
#endif
                if (_response.IsSuccessful == true)
                {
                    var _address = privateClient.DeserializeObject<DRResults<DAddress>>(_response.Content);

                    _result.result.currency = _address.result.currency;
                    _result.result.address = _address.result.address;

                    _result.SetSuccess();
                }
                else
                {
                    var _message = privateClient.GetResponseMessage(_response);
                    _result.SetFailure(_message.message);
                }
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

            var _response = await privateClient.CallApiPost2Async("/api/v2/private/withdraw", _params);
            if (_response != null)
            {
#if RAWJSON
            _result.rawJson = _response.Content;
#endif
                if (_response.IsSuccessful == true)
                {
                    var _withdraw = privateClient.DeserializeObject<DRResults<DWithdraw>>(_response.Content);
                    {
                        _result.result.transactionType = TransactionType.Withdraw;
                        _result.result.toAddress = _withdraw.result.toAddress;
                        _result.result.amount = _withdraw.result.amount;
                        _result.result.currency = _withdraw.result.currency;
                        _result.result.fee = _withdraw.result.fee;
                        _result.result.transactionId = _withdraw.result.transactionId;
                        _result.result.timestamp = _withdraw.result.timestamp;

                        _result.result.isCompleted = _withdraw.result.state == "completed";
                        _result.result.confirmations = 0;

                        _result.SetSuccess();
                    }
                }
                else
                {
                    var _message = privateClient.GetResponseMessage(_response);
                    _result.SetFailure(_message.message);
                }
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
                _params.Add("offset", start);
            }

            var _response = await privateClient.CallApiGet2Async("/api/v2/private/get_transfers", _params);
            if (_response != null)
            {
#if RAWJSON
                _result.rawJson = _response.Content;
#endif
                if (_response.IsSuccessful == true)
                {
                    var _transfer = privateClient.DeserializeObject<DRResults<DTransfer>>(_response.Content);
                    if (_transfer.result.count > 0)
                    {
                        foreach (var _t in _transfer.result.data)
                        {
                            _result.result.Add(new TransferItem
                            {
                                amount = _t.amount,
                                confirmations = 0,
                                currency = _t.currency,
                                isCompleted = _t.state == "confirmed",
                                timestamp = _t.timestamp,
                                toAddress = _t.toAddress,
                                transactionType = TransactionType.Transfer
                            });
                        }

                        _result.SetSuccess();
                    }
                }
                else
                {
                    var _message = privateClient.GetResponseMessage(_response);
                    _result.SetFailure(_message.message);
                }
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

            var _response = await privateClient.CallApiGet2Async("/api/v2/private/user");
            if (_response != null)
            {
#if RAWJSON
            _result.rawJson = _response.Content;
#endif
                if (_response.IsSuccessful == true)
                {
                    var _user_info = privateClient.DeserializeObject<DRResults<DUserInfoItem>>(_response.Content);
                    if (_user_info != null)
                    {
                        _result.result = _user_info.result;
                        _result.SetSuccess();
                    }
                }
                else
                {
                    var _message = privateClient.GetResponseMessage(_response);
                    _result.SetFailure(_message.message);
                }
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

            await Task.Delay(0);

            return _result;
        }

    }
}