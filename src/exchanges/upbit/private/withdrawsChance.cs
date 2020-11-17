using Newtonsoft.Json;
using CCXT.NET.Shared.Coin;

namespace CCXT.Collector.Upbit.Private
{
    /// <summary>
    /// 사용자의 보안등급 정보
    /// </summary>
    public class WithdrawMemberLevel
    {
        /// <summary>
        /// 사용자의 보안등급
        /// </summary>
        public int security_level
        {
            get;
            set;
        }

        /// <summary>
        /// 사용자의 수수료등급
        /// </summary>
        public int fee_level
        {
            get;
            set;
        }

        /// <summary>
        /// 사용자의 이메일 인증 여부
        /// </summary>
        public bool email_verified
        {
            get;
            set;
        }

        /// <summary>
        /// 사용자의 실명 인증 여부
        /// </summary>
        public bool identity_auth_verified
        {
            get;
            set;
        }

        /// <summary>
        /// 사용자의 계좌 인증 여부
        /// </summary>
        public bool bank_account_verified
        {
            get;
            set;
        }

        /// <summary>
        /// 사용자의 카카오페이 인증 여부
        /// </summary>
        public bool kakao_pay_auth_verified
        {
            get;
            set;
        }

        /// <summary>
        /// 사용자의 계정 보호 상태
        /// </summary>
        public bool locked
        {
            get;
            set;
        }

        /// <summary>
        /// 사용자의 출금 보호 상태
        /// </summary>
        public bool wallet_locked
        {
            get;
            set;
        }
    }

    /// <summary>
    /// 화폐 정보
    /// </summary>
    public class WithdrawCurrency
    {
        /// <summary>
        /// 화폐를 의미하는 영문 대문자 코드
        /// </summary>
        public string code
        {
            get;
            set;
        }

        /// <summary>
        /// 해당 화폐의 출금 수수료
        /// </summary>
        public decimal withdraw_fee
        {
            get;
            set;
        }

        /// <summary>
        /// 화폐의 코인 여부
        /// </summary>
        public bool is_coin
        {
            get;
            set;
        }

        /// <summary>
        /// 해당 화폐의 지갑 상태
        /// </summary>
        public string wallet_state
        {
            get;
            set;
        }

        /// <summary>
        /// 해당 화폐가 지원하는 입출금 정보
        /// </summary>
        public string[] wallet_support
        {
            get;
            set;
        }
    }

    /// <summary>
    /// 사용자의 계좌 정보
    /// </summary>
    public class WithdrawAccount
    {
        /// <summary>
        /// 화폐를 의미하는 영문 대문자 코드
        /// </summary>
        public string currency
        {
            get;
            set;
        }

        /// <summary>
        /// 주문가능 금액/수량
        /// </summary>
        public decimal balance
        {
            get;
            set;
        }

        /// <summary>
        /// 주문 중 묶여있는 금액/수량
        /// </summary>
        public decimal locked
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

    /// <summary>
    /// 출금 제약 정보
    /// </summary>
    public class WithdrawLimit
    {
        /// <summary>
        /// 화폐를 의미하는 영문 대문자 코드
        /// </summary>
        public string currency
        {
            get;
            set;
        }

        /// <summary>
        /// 출금 최소 금액/수량
        /// </summary>
        public decimal minimum
        {
            get;
            set;
        }

        /// <summary>
        /// 1회 출금 한도
        /// </summary>
        public decimal onetime
        {
            get;
            set;
        }

        /// <summary>
        /// 1일 출금 한도
        /// </summary>
        public decimal daily
        {
            get;
            set;
        }

        /// <summary>
        /// 1일 잔여 출금 한도
        /// </summary>
        public decimal remaining_daily
        {
            get;
            set;
        }

        /// <summary>
        /// 통합 1일 잔여 출금 한도
        /// </summary>
        public decimal remaining_daily_krw
        {
            get;
            set;
        }

        /// <summary>
        /// 출금 금액/ 수량 소수점 자리 수
        /// </summary>
        public int @fixed
        {
            get;
            set;
        }

        /// <summary>
        /// 출금 지원 여부
        /// </summary>
        public bool can_withdraw
        {
            get;
            set;
        }
    }

    /// <summary>
    /// 출금 가능 정보: 해당 통화의 가능한 출금 정보를 확인한다.
    /// </summary>
    public interface IWithdrawsChance
    {
        /// <summary>
        /// 사용자의 보안등급 정보
        /// </summary>
        WithdrawMemberLevel member_level
        {
            get;
            set;
        }

        /// <summary>
        /// 화폐 정보
        /// </summary>
        WithdrawCurrency currency
        {
            get;
            set;
        }

        /// <summary>
        /// 사용자의 계좌 정보
        /// </summary>
        WithdrawAccount account
        {
            get;
            set;
        }

        /// <summary>
        /// 출금 제약 정보
        /// </summary>
        WithdrawLimit withdraw_limit
        {
            get;
            set;
        }
    }

    /// <summary>
    /// 출금 가능 정보: 해당 통화의 가능한 출금 정보를 확인한다.
    /// </summary>
    public class WithdrawsChance : IWithdrawsChance
    {
        /// <summary>
        /// 사용자의 보안등급 정보
        /// </summary>
        public WithdrawMemberLevel member_level
        {
            get;
            set;
        }

        /// <summary>
        /// 화폐 정보
        /// </summary>
        public WithdrawCurrency currency
        {
            get;
            set;
        }

        /// <summary>
        /// 사용자의 계좌 정보
        /// </summary>
        public WithdrawAccount account
        {
            get;
            set;
        }

        /// <summary>
        /// 출금 제약 정보
        /// </summary>
        public WithdrawLimit withdraw_limit
        {
            get;
            set;
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public interface IMyWithdrawsChance : IApiResult<IWithdrawsChance>
    {
#if RAWJSON
        /// <summary>
        ///
        /// </summary>
        string rawJson
        {
            get;
            set;
        }
#endif
    }

    /// <summary>
    /// 
    /// </summary>
    public class MyWithdrawsChance : ApiResult<IWithdrawsChance>, IMyWithdrawsChance
    {
        /// <summary>
        ///
        /// </summary>
        public MyWithdrawsChance()
        {
            this.result = new WithdrawsChance();
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