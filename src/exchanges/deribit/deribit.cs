using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OdinSdk.BaseLib.Coin;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace CCXT.Collector.Deribit
{
    /// <summary>
    ///
    /// </summary>
    public sealed class DeribitClient : OdinSdk.BaseLib.Coin.XApiClient, IXApiClient
    {
        /// <summary>
        ///
        /// </summary>
        public override string DealerName { get; set; } = DRConfig.DealerName;

        /// <summary>
        ///
        /// </summary>
        /// <param name="division">exchange's division for communication</param>
        public DeribitClient(string division)
            : base(division)
        {
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="division">exchange's division for communication</param>
        /// <param name="connect_key">exchange's api key for connect</param>
        /// <param name="secret_key">exchange's secret key for signature</param>
        public DeribitClient(string division, string connect_key, string secret_key)
            : base(division, connect_key, secret_key, authentication: true)
        {
        }

        /// <summary>
        /// information of exchange for trading
        /// </summary>
        public override ExchangeInfo ExchangeInfo
        {
            get
            {
                if (base.ExchangeInfo == null)
                {
                    base.ExchangeInfo = new ExchangeInfo(this.DealerName)
                    {
                        Countries = new List<string>
                        {
                            "NL" // Netherlands
                        },
                        Urls = new ExchangeUrls
                        {
                            logo = "https://user-images.githubusercontent.com/1294454/41933112-9e2dd65a-798b-11e8-8440-5bab2959fcb8.jpg",
                            api = new Dictionary<string, string>
                            {
                                { "public", "https://www.deribit.com" },
                                { "private", "https://www.deribit.com" },
                                { "trade", "https://www.deribit.com" },
                                { "wss", "wss://www.deribit.com" },
                                { "test.public", "https://test.deribit.com" },
                                { "test.private", "https://test.deribit.com" },
                                { "test.trade", "https://test.deribit.com" },
                                { "test.wss", "wss://test.deribit.com" }
                            },
                            www = "https://www.deribit.com",
                            doc = new List<string>
                            {
                                "https://docs.deribit.com/v2",
                                "https://github.com/deribit"
                            },
                            fees = new List<string>
                            {
                                "https://www.deribit.com/pages/information/fees"
                            }
                        },
                        AmountMultiplier = new Dictionary<string, decimal>
                        {
                        },
                        RequiredCredentials = new RequiredCredentials
                        {
                            apikey = true,
                            secret = true,
                            uid = false,
                            login = false,
                            password = false,
                            twofa = false
                        },
                        LimitRate = new ExchangeLimitRate
                        {
                            useTotal = true,
                            total = new ExchangeLimitCalled { rate = 500 }
                        },
                        Fees = new MarketFees
                        {
                            trading = new MarketFee
                            {
                                tierBased = false,          // true for tier-based/progressive
                                percentage = false,         // fixed commission

                                maker = 0.0004m,
                                taker = 0.0004m
                            }
                        },
                        Timeframes = new Dictionary<string, string>
                        {
                            {"1m", "1"},
                            {"3m", "3"},
                            {"5m", "5"},
                            {"10m", "10"},
                            {"15m", "15"},
                            {"30m", "30"},
                            {"1h", "60"},
                            {"2h", "120"},
                            {"3h", "180"},
                            {"6h", "360"},
                            {"12h", "720"},
                            {"1d", "1D"}                        
                        }
                    };
                }

                return base.ExchangeInfo;
            }
        }

        private HMACSHA256 __encryptor = null;

        /// <summary>
        ///
        /// </summary>
        public HMACSHA256 Encryptor
        {
            get
            {
                if (__encryptor == null)
                    __encryptor = new HMACSHA256(Encoding.UTF8.GetBytes(SecretKey));

                return __encryptor;
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="endpoint">api link address of a function</param>
        /// <param name="args">Add additional attributes for each exchange</param>
        /// <returns></returns>
        public override async ValueTask<IRestRequest> CreatePostRequestAsync(string endpoint, Dictionary<string, object> args = null)
        {
            var _request = await base.CreatePostRequestAsync(endpoint);

            if (IsAuthentication == true)
            {
                var _basic_auth = "Basic " + Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"{ConnectKey}:{SecretKey}"));
                _request.AddHeader("Authorization", _basic_auth);
            }

            return await Task.FromResult(_request);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="args">Add additional attributes for each exchange</param>
        /// <returns></returns>
        public override async ValueTask<IRestRequest> CreatePutRequestAsync(string endpoint, Dictionary<string, object> args = null)
        {
            var _request = await base.CreatePutRequestAsync(endpoint);

            if (IsAuthentication == true)
            {
                var _basic_auth = "Basic " + Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"{ConnectKey}:{SecretKey}"));
                _request.AddHeader("Authorization", _basic_auth);
            }

            return await Task.FromResult(_request);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="endpoint">api link address of a function</param>
        /// <param name="args">Add additional attributes for each exchange</param>
        /// <returns></returns>
        public override async ValueTask<IRestRequest> CreateGetRequestAsync(string endpoint, Dictionary<string, object> args = null)
        {
            var _request = await base.CreateGetRequestAsync(endpoint, args);

            if (IsAuthentication == true)
            {
                var _basic_auth = "Basic " + Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"{ConnectKey}:{SecretKey}"));
                _request.AddHeader("Authorization", _basic_auth);
            }

            return await Task.FromResult(_request);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="endpoint">api link address of a function</param>
        /// <param name="args">Add additional attributes for each exchange</param>
        /// <returns></returns>
        public override async ValueTask<IRestRequest> CreateDeleteRequestAsync(string endpoint, Dictionary<string, object> args = null)
        {
            var _request = await base.CreateDeleteRequestAsync(endpoint);

            if (IsAuthentication == true)
            {
                var _basic_auth = "Basic " + Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"{ConnectKey}:{SecretKey}"));
                _request.AddHeader("Authorization", _basic_auth);
            }

            return await Task.FromResult(_request);
        }

        /// <summary>
        ///
        /// </summary>
        /// <returns></returns>
        public async ValueTask<string> CreateSignature(Method verb, string endpoint, string nonce, string json_body = "")
        {
            return await Task.FromResult
                    (
                        this.ConvertHexString
                        (
                            Encryptor.ComputeHash
                            (
                                Encoding.UTF8.GetBytes($"{verb}{endpoint}{nonce}{json_body}")
                            )
                        )
                        .ToLower()
                    );
        }

        /// <summary>
        ///
        /// </summary>
        public new Dictionary<string, ErrorCode> ErrorMessages = new Dictionary<string, ErrorCode>
        {
            { "Not Found", ErrorCode.OrderNotFound },
            { "Invalid API Key.", ErrorCode.AuthenticationError },
            { "Access Denied", ErrorCode.PermissionDenied }
        };

        /// <summary>
        ///
        /// </summary>
        /// <param name="response">response value arrive from exchange's server</param>
        /// <returns></returns>
        public override BoolResult GetResponseMessage(IRestResponse response = null)
        {
            var _result = new BoolResult();

            if (response != null)
            {
                if (response.IsSuccessful == false) // (int) StatusCode >= 200 && (int) StatusCode <= 299 && ResponseStatus == ResponseStatus.Completed;
                {
                    if ((int)response.StatusCode != 429)
                    {
                        if (String.IsNullOrEmpty(response.Content) == false && response.Content[0] == '{')
                        {
                            var _json_result = this.DeserializeObject<JToken>(response.Content);

                            var _json_error = _json_result.SelectToken("error");
                            if (_json_error != null)
                            {
                                var _json_message = _json_error.SelectToken("message");
                                if (_json_message != null)
                                {
                                    var _error_code = ErrorCode.ExchangeError;

                                    var _error_msg = _json_message.Value<string>();
                                    if (String.IsNullOrEmpty(_error_msg) == false)
                                    {
                                        if (ErrorMessages.ContainsKey(_error_msg) == true)
                                            _error_code = ErrorMessages[_error_msg];
                                    }
                                    else
                                    {
                                        _error_msg = response.Content;
                                    }

                                    _result.SetFailure(_error_msg, _error_code);
                                }
                            }
                        }
                    }
                    else
                    {
                        _result.SetFailure(
                                response.ErrorMessage ?? response.StatusDescription,
                                ErrorCode.DDoSProtection,
                                (int)response.StatusCode,
                                false
                            );
                    }
                }

                if (_result.success == true && response.IsSuccessful == false)
                {
                    _result.SetFailure(
                            response.ErrorMessage ?? response.StatusDescription,
                            ErrorCode.ResponseRestError,
                            (int)response.StatusCode,
                            false
                        );
                }
            }

            return _result;
        }
    }
}