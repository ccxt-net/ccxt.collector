using OdinSdk.BaseLib.Coin;
using OdinSdk.BaseLib.Coin.Private;
using OdinSdk.BaseLib.Coin.Types;
using OdinSdk.BaseLib.Configuration;
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
        /// <param name="quote_name">화폐명</param>
        /// <returns></returns>
        public async Task<Address> DepositAddress(string quote_name)
        {
            var _result = new Address();

            var _params = new Dictionary<string, object>();
            {
                _params.Add("currency", quote_name);
            }

            var _response = await privateClient.CallApiGet2Async("/api/v1/user/depositAddress", _params);
#if DEBUG
            _result.rawJson = _response.Content;
#endif
            if (_response.IsSuccessful == true)
            {
                _result.result.currency = quote_name;
                _result.result.address = _response.Content;
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
        /// <param name="quote_name">화폐명</param>
        /// <param name="address">coin address for send</param>
        /// <param name="tag">Secondary address identifier for coins like XRP,XMR etc.</param>
        /// <param name="quantity">amount of coin</param>
        /// <returns></returns>
        public async Task<Transfer> RequestWithdrawal(string quote_name, string address, string tag, decimal quantity)
        {
            var _result = new Transfer();

            var _params = new Dictionary<string, object>();
            {
                _params.Add("currency", quote_name);
                _params.Add("amount", quantity);
                _params.Add("address", address);
            }

            var _response = await privateClient.CallApiPost2Async("/api/v1/user/requestWithdrawal", _params);
#if DEBUG
            _result.rawJson = _response.Content;
#endif
            if (_response.IsSuccessful == true)
            {
                var _json_data = privateClient.DeserializeObject<BTransferItem>(_response.Content);
                if (String.IsNullOrEmpty(_json_data.transferId) == false)
                {
                    var _withdraw = new BTransferItem
                    {
                        transferId = _json_data.transferId,
                        transactionId = _json_data.transactionId,
                        timestamp = _json_data.timestamp,

                        transactionType = TransactionType.Withdraw,

                        currency = _json_data.currency,
                        toAddress = _json_data.toAddress,
                        toTag = tag,

                        amount = _json_data.amount,
                        fee = _json_data.fee,

                        confirmations = 0,
                        isCompleted = true
                    };

                    _withdraw.account = _json_data.account;
                    _withdraw.transferType = _json_data.transferType;
                    _withdraw.text = _json_data.text;
                    _withdraw.transactTime = _json_data.transactTime;

                    _result.result = _withdraw;
                }
                else
                {
                    _result.SetFailure();
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
        /// <param name="quote_name">화폐명</param>
        /// <param name="timeframe">time frame interval (optional): default "1d"</param>
        /// <param name="since">return committed data since given time (milli-seconds) (optional): default 0</param>
        /// <param name="limits">You can set the maximum number of transactions you want to get with this parameter</param>
        /// <returns></returns>
        public async Task<Transfers> FetchTransfers(string quote_name, string timeframe = "1d", long since = 0, int limits = 20)
        {
            var _result = new Transfers();

            var _timestamp = privateClient.ExchangeInfo.GetTimestamp(timeframe);
            var _timeframe = privateClient.ExchangeInfo.GetTimeframe(timeframe);

            var _params = new Dictionary<string, object>();
            {
                _params.Add("currency", quote_name);
            }

            var _response = await privateClient.CallApiGet2Async("/api/v1/user/walletHistory", _params);
#if DEBUG
            _result.rawJson = _response.Content;
#endif
            if (_response.IsSuccessful == true)
            {
                var _json_data = privateClient.DeserializeObject<List<BTransferItem>>(_response.Content);
                {
                    var _transfers = _json_data
                                            .Where(t => t.timestamp >= since)
                                            .OrderByDescending(t => t.timestamp)
                                            .Take(limits);

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

                        _t.walletBalance = (_t.walletBalance ?? 0m) * 0.00000001m;
                        _t.marginBalance = (_t.marginBalance ?? 0m) * 0.00000001m;
                        _t.amount = _t.amount * 0.00000001m;
                        _t.fee = _t.fee * 0.00000001m;

                        if (_t.timestamp == 0)
                            _t.timestamp = CUnixTime.NowMilli;

                        //_t.transferId = _t.timestamp.ToString();              // transferId 있음
                        _t.transactionId = (_t.timestamp * 1000).ToString();    // transactionId 없음

                        _result.result.Add(_t);
                    }
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
        /// <param name="base_name">코인명</param>
        /// <returns></returns>
        public async Task<Balance> UserMargin(string base_name)
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
                _balance.used = _balance.total - _balance.free;

                _result.result = _balance;
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
        /// <param name="args">Add additional attributes for each exchange</param>
        /// <returns></returns>
        public async Task<Balances> UserMargins()
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
                
                foreach (var _balance in _balances)
                    _balance.used = _balance.total - _balance.free;

                _result.result = _balances.ToList<IBalanceItem>();
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
        public async Task<BUserInfo> UserInfo()
        {
            var _result = new BUserInfo();

            var _response = await privateClient.CallApiGet2Async("/api/v1/user");
#if DEBUG
            _result.rawJson = _response.Content;
#endif
            if (_response.IsSuccessful == true)
            {
                var _user_info = privateClient.DeserializeObject<BUserInfoItem>(_response.Content);
                _result.result = _user_info;
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