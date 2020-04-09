﻿using Newtonsoft.Json;
using OdinSdk.BaseLib.Coin.Private;

namespace CCXT.Collector.Deribit.Private
{
    /// <summary>
    /// 거래소 회원 지갑 정보
    /// </summary>
    public class BBalanceItem : OdinSdk.BaseLib.Coin.Private.BalanceItem, IBalanceItem
    {
        /// <summary>
        ///
        /// </summary>
        [JsonProperty(PropertyName = "currency")]
        public override string currency
        {
            get;
            set;
        }

        /// <summary>
        ///
        /// </summary>
        [JsonProperty(PropertyName = "availableMargin")]
        public override decimal free
        {
            get;
            set;
        }

        /// <summary>
        ///
        /// </summary>
        [JsonProperty(PropertyName = "marginBalance")]
        public override decimal total
        {
            get;
            set;
        }
    }
}