using Newtonsoft.Json;
using OdinSdk.BaseLib.Coin;

namespace CCXT.Collector.Upbit.Private
{
    /// <summary>
    /// 매수/매도 시 제약사항
    /// </summary>
    public class OrderConstraint
    {
        /// <summary>
        /// 화폐를 의미하는 영문 대문자 코드
        /// </summary>
        public string? currency
        {
            get;
            set;
        }

        /// <summary>
        /// 주문금액 단위
        /// </summary>
        public decimal price_unit
        {
            get;
            set;
        }

        /// <summary>
        /// 최소 매도/매수 금액
        /// </summary>
        public decimal min_total
        {
            get;
            set;
        }
    }

    /// <summary>
    /// 마켓에 대한 정보
    /// </summary>
    public class OrderMarketInfo
    {
        /// <summary>
        /// 마켓의 유일 키
        /// </summary>
        public string? id
        {
            get;
            set;
        }

        /// <summary>
        /// 마켓 이름
        /// </summary>
        public string? name
        {
            get;
            set;
        }

        /// <summary>
        /// 지원 주문 방식
        /// </summary>
        public string[] order_types
        {
            get;
            set;
        }

        /// <summary>
        /// 지원 주문 종류
        /// </summary>
        public string[] order_sides
        {
            get;
            set;
        }

        /// <summary>
        /// 매수 시 제약사항
        /// </summary>
        public OrderConstraint bid
        {
            get;
            set;
        }

        /// <summary>
        /// 매도 시 제약사항
        /// </summary>
        public OrderConstraint ask
        {
            get;
            set;
        }

        /// <summary>
        /// 최대 매도/매수 금액
        /// </summary>
        public decimal max_total
        {
            get;
            set;
        }

        /// <summary>
        /// 마켓 운영 상태
        /// </summary>
        public string? state
        {
            get;
            set;
        }
    }

    /// <summary>
    ///
    /// </summary>
    public interface IOrdersChance
    {
        /// <summary>
        /// 매수 수수료 비율
        /// </summary>
        decimal bid_fee
        {
            get;
            set;
        }

        /// <summary>
        /// 매도 수수료 비율
        /// </summary>
        decimal ask_fee
        {
            get;
            set;
        }

        /// <summary>
        /// 마켓에 대한 정보
        /// </summary>
        OrderMarketInfo market
        {
            get;
            set;
        }

        /// <summary>
        /// 매수 시 사용하는 화폐의 계좌 상태
        /// </summary>
        UBalanceItem bid_account
        {
            get;
            set;
        }

        /// <summary>
        /// 매도 시 사용하는 화폐의 계좌 상태
        /// </summary>
        UBalanceItem ask_account
        {
            get;
            set;
        }
    }

    /// <summary>
    ///
    /// </summary>
    public class OrdersChance : IOrdersChance
    {
        /// <summary>
        /// 매수 수수료 비율
        /// </summary>
        public decimal bid_fee
        {
            get;
            set;
        }

        /// <summary>
        /// 매도 수수료 비율
        /// </summary>
        public decimal ask_fee
        {
            get;
            set;
        }

        /// <summary>
        /// 마켓에 대한 정보
        /// </summary>
        public OrderMarketInfo market
        {
            get;
            set;
        }

        /// <summary>
        /// 매수 시 사용하는 화폐의 계좌 상태
        /// </summary>
        public UBalanceItem bid_account
        {
            get;
            set;
        }

        /// <summary>
        /// 매도 시 사용하는 화폐의 계좌 상태
        /// </summary>
        public UBalanceItem ask_account
        {
            get;
            set;
        }
    }

    /// <summary>
    ///
    /// </summary>
    public interface IMyOrdersChance : IApiResult<IOrdersChance>
    {
#if RAWJSON

        /// <summary>
        ///
        /// </summary>
        string? rawJson
        {
            get;
            set;
        }

#endif
    }

    /// <summary>
    /// 
    /// </summary>
    public class MyOrdersChance : ApiResult<IOrdersChance>, IMyOrdersChance
    {
        /// <summary>
        ///
        /// </summary>
        public MyOrdersChance()
        {
            this.result = new OrdersChance();
        }

#if RAWJSON
        /// <summary>
        ///
        /// </summary>
        [JsonIgnore]
        public virtual string? rawJson
        {
            get;
            set;
        }
#endif
    }
}