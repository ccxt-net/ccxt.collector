﻿using CCXT.NET.Shared.Coin.Trade;
using CCXT.NET.Shared.Coin.Types;
using CCXT.NET.Shared.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace CCXT.Collector.Upbit.Private
{
    /// <summary>
    /// 개별 주문 조회: 주문 UUID 를 통해 개별 주문건을 조회한다.
    /// </summary>
    public class UMyOrderItem : CCXT.NET.Shared.Coin.Trade.MyOrderItem, IMyOrderItem
    {
        /// <summary>
        /// 주문의 고유 아이디
        /// </summary>
        [JsonProperty(PropertyName = "uuid")]
        public override string orderId
        {
            get;
            set;
        }

        /// <summary>
        /// 주문 종류
        /// </summary>
        [JsonProperty(PropertyName = "side")]
        private string sideValue
        {
            set
            {
                sideType = SideTypeConverter.FromString(value);
            }
        }

        /// <summary>
        /// 주문 방식
        /// </summary>
        [JsonProperty(PropertyName = "ord_type")]
        private string orderValue
        {
            set
            {
                orderType = OrderTypeConverter.FromString(value);
            }
        }

        /// <summary>
        /// 주문 당시 화폐 가격
        /// </summary>
        public override decimal price
        {
            get;
            set;
        }

        /// <summary>
        /// 주문 상태
        /// </summary>
        [JsonProperty(PropertyName = "state")]
        private string statusValue
        {
            set
            {
                orderStatus = OrderStatusConverter.FromString(value);
            }
        }

        /// <summary>
        /// 마켓의 유일키
        /// </summary>
        [JsonProperty(PropertyName = "market")]
        public override string symbol
        {
            get;
            set;
        }

        /// <summary>
        /// 주문 생성 시간
        /// </summary>
        [JsonProperty(PropertyName = "created_at")]
        private DateTime timeValue
        {
            set
            {
                timestamp = CUnixTime.ConvertToUnixTimeMilli(value);
            }
        }

        /// <summary>
        /// 사용자가 입력한 주문 양
        /// </summary>
        [JsonProperty(PropertyName = "volume")]
        public override decimal quantity
        {
            get;
            set;
        }

        /// <summary>
        /// 체결 후 남은 주문 양
        /// </summary>
        public decimal remaining_volume
        {
            get;
            set;
        }

        /// <summary>
        /// 수수료로 예약된 비용
        /// </summary>
        [JsonProperty(PropertyName = "reserved_fee")]
        public override decimal fee
        {
            get;
            set;
        }

        /// <summary>
        /// 남은 수수료
        /// </summary>
        public decimal remaining_fee
        {
            get;
            set;
        }

        /// <summary>
        /// 사용된 수수료
        /// </summary>
        public decimal paid_fee
        {
            get;
            set;
        }

        /// <summary>
        /// 거래에 사용중인 비용
        /// </summary>
        [JsonProperty(PropertyName = "locked")]
        public decimal used
        {
            get;
            set;
        }

        /// <summary>
        /// 체결된 양
        /// </summary>
        [JsonProperty(PropertyName = "executed_volume")]
        public override decimal filled
        {
            get;
            set;
        }

        /// <summary>
        /// 해당 주문에 걸린 체결 수
        /// </summary>
        [JsonProperty(PropertyName = "trades_count")]
        public override int count
        {
            get;
            set;
        }

        /// <summary>
        ///
        /// </summary>
        public List<UMyOrderTrade> trades
        {
            get;
            set;
        }
    }

    /// <summary>
    ///
    /// </summary>
    public class UMyOrderTrade
    {
        /// <summary>
        /// 마켓의 유일키
        /// </summary>
        [JsonProperty(PropertyName = "market")]
        public string symbol
        {
            get;
            set;
        }

        /// <summary>
        /// 체결의 고유 아이디
        /// </summary>
        [JsonProperty(PropertyName = "uuid")]
        public string tradeId
        {
            get;
            set;
        }

        /// <summary>
        /// 체결 가격
        /// </summary>
        public decimal price
        {
            get;
            set;
        }

        /// <summary>
        /// 체결 양
        /// </summary>
        [JsonProperty(PropertyName = "volume")]
        public decimal quantity
        {
            get;
            set;
        }

        /// <summary>
        /// 체결된 총 가격
        /// </summary>
        [JsonProperty(PropertyName = "funds")]
        public decimal amount
        {
            get;
            set;
        }

        /// <summary>
        /// 주문 종류
        /// </summary>
        [JsonProperty(PropertyName = "side")]
        private string sideValue
        {
            set
            {
                sideType = SideTypeConverter.FromString(value);
            }
        }

        /// <summary>
        ///
        /// </summary>
        public SideType sideType
        {
            get;
            set;
        }
    }
}