﻿using Newtonsoft.Json;
using OdinSdk.BaseLib.Coin.Trade;
using OdinSdk.BaseLib.Coin.Types;
using OdinSdk.BaseLib.Configuration;
using System;

namespace CCXT.Collector.BitMEX.Private
{
    /// <summary>
    ///
    /// </summary>
    public class BMyOrderItem : OdinSdk.BaseLib.Coin.Trade.MyOrderItem, IMyOrderItem
    {
        /// <summary>
        ///
        /// </summary>
        [JsonProperty(PropertyName = "symbol")]
        public override string symbol
        {
            get;
            set;
        }

        /// <summary>
        ///
        /// </summary>
        [JsonProperty(PropertyName = "orderID")]
        public override string orderId
        {
            get;
            set;
        }

        /// <summary>
        ///
        /// </summary>
        public string account
        {
            get;
            set;
        }

        /// <summary>
        ///
        /// </summary>
        public string currency
        {
            get;
            set;
        }

        /// <summary>
        ///
        /// </summary>
        public string settlCurrency
        {
            get;
            set;
        }

        /// <summary>
        ///
        /// </summary>
        [JsonProperty(PropertyName = "clOrdID")]
        public string clientOrderId
        {
            get;
            set;
        }

        /// <summary>
        ///
        /// </summary>
        [JsonProperty(PropertyName = "price")]
        public decimal? orderPrice
        {
            set
            {
                this.price = value.HasValue ? value.Value : 0;
            }
        }

        /// <summary>
        ///
        /// </summary>
        [JsonProperty(PropertyName = "orderQty")]
        private decimal? orderQty
        {
            set
            {
                this.quantity = value.HasValue ? value.Value : 0;
            }
        }

        /// <summary>
        ///
        /// </summary>
        [JsonProperty(PropertyName = "cumQty")]
        public decimal? cumQty
        {
            set
            {
                this.filled = value.HasValue ? value.Value : 0;
            }
        }

        /// <summary>
        ///
        /// </summary>
        [JsonProperty(PropertyName = "leavesQty")]
        public decimal? leavesQty
        {
            set
            {
                this.remaining = value.HasValue ? value.Value : 0;
            }
        }

        /// <summary>
        ///
        /// </summary>
        [JsonProperty(PropertyName = "avgPx")]
        public decimal? avgPx
        {
            get;
            set;
        }

        /// <summary>
        ///
        /// </summary>
        [JsonProperty(PropertyName = "timeInForce")]
        public string timeInForce
        {
            get;
            set;
        }

        /// <summary>
        ///
        /// </summary>
        [JsonProperty(PropertyName = "origin_timestamp")]
        public override long timestamp
        {
            get;
            set;
        }

        /// <summary>
        ///
        /// </summary>
        [JsonProperty(PropertyName = "timestamp")]
        public DateTime timeValue
        {
            set
            {
                timestamp = CUnixTime.ConvertToUnixTimeMilli(value);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public bool workingIndicator
        {
            get;
            set;
        }

        /// <summary>
        /// 
        /// </summary>
        public string execInst
        {
            get;
            set;
        }

        /// <summary>
        /// 
        /// </summary>
        public string text
        {
            get;
            set;
        }

        /// <summary>
        ///
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
        [JsonProperty(PropertyName = "ordStatus")]
        private string statusValue
        {
            set
            {
                orderStatus = OrderStatusConverter.FromString(value);
            }
        }

        /// <summary>
        ///
        /// </summary>
        [JsonProperty(PropertyName = "ordType")]
        private string orderValue
        {
            set
            {
                orderType = OrderTypeConverter.FromString(value);
            }
        }
    }

    /// <summary>
    ///
    /// </summary>
    public class BCancelAllOrders
    {
        /// <summary>
        ///
        /// </summary>
        public string result
        {
            get;
            set;
        }
    }

    /// <summary>
    ///
    /// </summary>
    public class BBulkOrderItem
    {
        /// <summary>
        ///
        /// </summary>
        public string orderID
        {
            get;
            set;
        }

        /// <summary>
        ///
        /// </summary>
        public string symbol
        {
            get;
            set;
        }

        /// <summary>
        ///
        /// </summary>
        public string side
        {
            get;
            set;
        }

        /// <summary>
        ///
        /// </summary>
        public string execInst
        {
            get;
            set;
        }

        /// <summary>
        ///
        /// </summary>
        public string ordType
        {
            get;
            set;
        }

        /// <summary>
        ///
        /// </summary>
        public decimal orderQty
        {
            get;
            set;
        }

        /// <summary>
        ///
        /// </summary>
        public decimal price
        {
            get;
            set;
        }
    }
}