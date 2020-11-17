﻿using CCXT.NET.Shared.Coin.Public;
using CCXT.NET.Shared.Coin.Types;
using CCXT.NET.Shared.Configuration;
using Newtonsoft.Json;
using System;

namespace CCXT.Collector.BitMEX.Public
{
    /// <summary>
    /// recent trade data
    /// </summary>
    public class BCompleteOrderItem : CCXT.NET.Shared.Coin.Public.CompleteOrderItem, ICompleteOrderItem
    {
        /// <summary>
        ///
        /// </summary>
        [JsonProperty(PropertyName = "trdMatchID")]
        public override string transactionId
        {
            get;
            set;
        }

        /// <summary>
        ///
        /// </summary>
        [JsonProperty(PropertyName = "size")]
        public override decimal quantity
        {
            get;
            set;
        }

        /// <summary>
        ///
        /// </summary>
        [JsonProperty(PropertyName = "symbol")]
        public string symbol
        {
            get;
            set;
        }

        /// <summary>
        ///
        /// </summary>
        [JsonProperty(PropertyName = "tickDirection")]
        public string tickDirection
        {
            get;
            set;
        }

        /// <summary>
        ///
        /// </summary>
        [JsonProperty(PropertyName = "grossValue")]
        public decimal grossValue
        {
            get;
            set;
        }

        /// <summary>
        ///
        /// </summary>
        [JsonProperty(PropertyName = "homeNotional")]
        public decimal homeNotional
        {
            get;
            set;
        }

        /// <summary>
        ///
        /// </summary>
        [JsonProperty(PropertyName = "foreignNotional")]
        public decimal foreignNotional
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
        private DateTime timeValue
        {
            set
            {
                timestamp = CUnixTime.ConvertToUnixTimeMilli(value);
            }
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
    }
}