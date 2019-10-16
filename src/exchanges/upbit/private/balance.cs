using OdinSdk.BaseLib.Coin.Private;
using Newtonsoft.Json;

namespace CCXT.Collector.Upbit.Private
{
    /// <summary>
    ///전체 계좌 조회: 내가 보유한 자산 리스트를 보여줍니다.
    /// </summary>
    public class UBalanceItem : OdinSdk.BaseLib.Coin.Private.BalanceItem, IBalanceItem
    {
        /// <summary>
        /// 주문가능 금액/수량
        /// </summary>
        [JsonProperty(PropertyName = "balance")]
        public override decimal free
        {
            get;
            set;
        }

        /// <summary>
        /// 주문 중 묶여있는 금액/수량
        /// </summary>
        [JsonProperty(PropertyName = "locked")]
        public override decimal used
        {
            get;
            set;
        }

        /// <summary>
        /// 매수평균가
        /// </summary>
        public decimal avg_buy_price
        {
            get;
            set;
        }

        /// <summary>
        /// 매수평균가 수정 여부
        /// </summary>
        public bool avg_buy_price_modified
        {
            get;
            set;
        }

        /// <summary>
        /// 평단가 기준 화폐
        /// </summary>
        public string unit_currency
        {
            get;
            set;
        }
    }
}