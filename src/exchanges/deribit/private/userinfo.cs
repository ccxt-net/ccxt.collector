using OdinSdk.BaseLib.Coin;

namespace CCXT.Collector.Deribit.Private
{
    /*
     {
        "available_funds": 75.20590014,
        "available_withdrawal_funds": 75.20590014,
        "balance": 78.28121225,
        "creation_timestamp": 1581505544344,
        "currency": "BTC",
        "delta_total": -10.1566,
        "email": "user_AAA@email.com",
        "equity": 78.44878563,
        "futures_pl": 2.2229243,
        "futures_session_rpl": 0,
        "futures_session_upl": -0.04632662,
        "id": 3,
        "initial_margin": 3.24288549,
        "interuser_transfers_enabled": false,
        "limits": {
            "matching_engine": 200,
            "matching_engine_burst": 200,
            "non_matching_engine": 200,
            "non_matching_engine_burst": 300
        },
        "maintenance_margin": 2.4945273,
        "margin_balance": 78.23488563,
        "options_delta": 5.107,
        "options_gamma": 0.0117,
        "options_pl": -0.7061,
        "options_session_rpl": 0,
        "options_session_upl": -0.2346,
        "options_theta": -562.1021,
        "options_value": 0.2139,
        "options_vega": 53.409,
        "portfolio_margining_enabled": true,
        "projected_initial_margin": 4.508373444269301,
        "projected_maintenance_margin": 3.467979572514847,
        "referrer_id": null,
        "session_funding": 0,
        "session_rpl": 0,
        "session_upl": -0.28092662,
        "system_name": "user_1",
        "tfa_enabled": false,
        "total_pl": 1.5168243,
        "type": "main",
        "username": "user_1"
    }
    */

    public class DUserInfoLimits
    {
        public decimal matching_engine
        {
            get;
            set;
        }

        public decimal matching_engine_burst
        {
            get;
            set;
        }

        public decimal non_matching_engine
        {
            get;
            set;
        }

        public decimal non_matching_engine_burst
        {
            get;
            set;
        }
    }

    /// <summary>
    ///
    /// </summary>
    public class DUserInfoItem
    {
        public decimal available_funds
        {
            get;
            set;
        }

        public decimal available_withdrawal_funds
        {
            get;
            set;
        }

        public decimal balance
        {
            get;
            set;
        }

        public long creation_timestamp
        {
            get;
            set;
        }

        public string currency
        {
            get;
            set;
        }

        public decimal delta_total
        {
            get;
            set;
        }

        public string email
        {
            get;
            set;
        }

        public decimal equity
        {
            get;
            set;
        }

        public decimal futures_pl
        {
            get;
            set;
        }

        public decimal futures_session_rpl
        {
            get;
            set;
        }

        public decimal futures_session_upl
        {
            get;
            set;
        }

        public int id
        {
            get;
            set;
        }

        public decimal initial_margin
        {
            get;
            set;
        }

        public bool interuser_transfers_enabled
        {
            get;
            set;
        }


        public DUserInfoLimits limits
        {
            get;
            set;
        }

        public decimal maintenance_margin
        {
            get;
            set;
        }

        public decimal margin_balance
        {
            get;
            set;
        }

        public decimal options_delta
        {
            get;
            set;
        }

        public decimal options_gamma
        {
            get;
            set;
        }

        public decimal options_pl
        {
            get;
            set;
        }

        public decimal options_session_rpl
        {
            get;
            set;
        }

        public decimal options_session_upl
        {
            get;
            set;
        }

        public decimal options_theta
        {
            get;
            set;
        }

        public decimal options_value
        {
            get;
            set;
        }

        public decimal options_vega
        {
            get;
            set;
        }

        public bool portfolio_margining_enabled
        {
            get;
            set;
        }

        public decimal projected_initial_margin
        {
            get;
            set;
        }

        public decimal projected_maintenance_margin
        {
            get;
            set;
        }

        public string referrer_id
        {
            get;
            set;
        }

        public decimal session_funding
        {
            get;
            set;
        }

        public decimal session_rpl
        {
            get;
            set;
        }

        public decimal session_upl
        {
            get;
            set;
        }

        public string system_name
        {
            get;
            set;
        }

        public bool tfa_enabled
        {
            get;
            set;
        }

        public decimal total_pl
        {
            get;
            set;
        }

        public string type
        {
            get;
            set;
        }

        public string username
        {
            get;
            set;
        }
    }

    /// <summary>
    ///
    /// </summary>
    public class BUserInfo : ApiResult<DUserInfoItem>
    {
        /// <summary>
        ///
        /// </summary>
        public BUserInfo()
        {
            this.result = new DUserInfoItem();
        }

#if RAWJSON

        /// <summary>
        ///
        /// </summary>
        [JsonIgnore]
        public virtual string rawJson
        {
            get;
            set;
        }

#endif
    }
}