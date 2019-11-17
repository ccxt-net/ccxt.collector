using Newtonsoft.Json;
using OdinSdk.BaseLib.Coin;
using OdinSdk.BaseLib.Coin.Types;
using OdinSdk.BaseLib.Configuration;
using System.Collections.Generic;

namespace CCXT.Collector.Library.Types
{
    /// <summary>
    ///
    /// </summary>
    public interface ISMyOrderItem
    {
        /// <summary>
        ///
        /// </summary>
        string orderId
        {
            get;
            set;
        }

        /// <summary>
        ///
        /// </summary>
        string symbol
        {
            get;
            set;
        }

        /// <summary>
        ///
        /// </summary>
        SideType sideType
        {
            get;
            set;
        }

        /// <summary>
        ///
        /// </summary>
        OrderType orderType
        {
            get;
            set;
        }

        /// <summary>
        ///
        /// </summary>
        MakerType makerType
        {
            get;
            set;
        }

        /// <summary>
        ///
        /// </summary>
        OrderStatus orderStatus
        {
            get;
            set;
        }

        /// <summary>
        ///
        /// </summary>
        long timestamp
        {
            get;
            set;
        }

        /// <summary>
        /// ISO 8601 datetime string with milliseconds
        /// </summary>
        string datetime
        {
            get;
        }

        /// <summary>
        ///
        /// </summary>
        decimal quantity
        {
            get;
            set;
        }

        /// <summary>
        ///
        /// </summary>
        decimal price
        {
            get;
            set;
        }

        /// <summary>
        ///
        /// </summary>
        decimal amount
        {
            get;
            set;
        }

        /// <summary>
        /// executedQty
        /// </summary>
        decimal filled
        {
            get;
            set;
        }

        /// <summary>
        ///
        /// </summary>
        decimal remaining
        {
            get;
            set;
        }

        /// <summary>
        ///
        /// </summary>
        decimal cost
        {
            get;
            set;
        }

        /// <summary>
        ///
        /// </summary>
        decimal fee
        {
            get;
            set;
        }

        /// <summary>
        ///
        /// </summary>
        int count
        {
            get;
            set;
        }
    }

    /// <summary>
    ///
    /// </summary>
    public class SMyOrderItem : ISMyOrderItem
    {
        /// <summary>
        ///
        /// </summary>
        public virtual string orderId
        {
            get;
            set;
        }

        /// <summary>
        ///
        /// </summary>
        public virtual string symbol
        {
            get;
            set;
        }

        /// <summary>
        ///
        /// </summary>
        public virtual SideType sideType
        {
            get;
            set;
        }

        /// <summary>
        ///
        /// </summary>
        public virtual OrderType orderType
        {
            get;
            set;
        }

        /// <summary>
        ///
        /// </summary>
        public virtual MakerType makerType
        {
            get;
            set;
        }

        /// <summary>
        ///
        /// </summary>
        public virtual OrderStatus orderStatus
        {
            get;
            set;
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
        public virtual long timestamp
        {
            get;
            set;
        }

        /// <summary>
        /// ISO 8601 datetime string with milliseconds
        /// </summary>
        public virtual string datetime
        {
            get
            {
                return CUnixTime.ConvertToUtcTimeMilli(timestamp).ToString("o");
            }
        }

        /// <summary>
        ///
        /// </summary>
        public virtual decimal quantity
        {
            get;
            set;
        }

        /// <summary>
        ///
        /// </summary>
        public virtual decimal price
        {
            get;
            set;
        }

        /// <summary>
        ///
        /// </summary>
        public virtual decimal amount
        {
            get;
            set;
        }

        /// <summary>
        ///
        /// </summary>
        public virtual decimal filled
        {
            get;
            set;
        }

        /// <summary>
        ///
        /// </summary>
        public virtual decimal remaining
        {
            get;
            set;
        }

        /// <summary>
        ///
        /// </summary>
        public virtual decimal cost
        {
            get;
            set;
        }

        /// <summary>
        ///
        /// </summary>
        public virtual decimal fee
        {
            get;
            set;
        }

        /// <summary>
        ///
        /// </summary>
        public virtual int count
        {
            get;
            set;
        }
    }

    /// <summary>
    /// a market order list
    /// </summary>
    public interface ISMyOrders : IApiResult<List<ISMyOrderItem>>
    {
        /// <summary>
        ///
        /// </summary>
        string exchange
        {
            get;
            set;
        }

        /// <summary>
        /// S, R
        /// </summary>
        string stream
        {
            get;
            set;
        }

        /// <summary>
        ///
        /// </summary>
        string symbol
        {
            get;
            set;
        }

        /// <summary>
        ///
        /// </summary>
        string action
        {
            get;
            set;
        }

        /// <summary>
        ///
        /// </summary>
        long sequentialId
        {
            get;
            set;
        }
#if DEBUG
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
    /// a market order list
    /// </summary>
    public class SMyOrders : ApiResult<List<ISMyOrderItem>>, ISMyOrders
    {
        /// <summary>
        /// is success calling
        /// </summary>
        [JsonIgnore]
        public override bool success
        {
            get;
            set;
        }

        /// <summary>
        /// error or success message
        /// </summary>
        [JsonIgnore]
        public override string message
        {
            get;
            set;
        }

        /// <summary>
        /// status, error code
        /// </summary>
        [JsonIgnore]
        public override int statusCode
        {
            get;
            set;
        }

        /// <summary>
        ///
        /// </summary>
        [JsonIgnore]
        public override ErrorCode errorCode
        {
            get;
            set;
        }

        /// <summary>
        /// check implemented
        /// </summary>
        [JsonIgnore]
        public override bool supported
        {
            get;
            set;
        }

        /// <summary>
        ///
        /// </summary>
        public virtual string exchange
        {
            get;
            set;
        }

        /// <summary>
        /// S, R
        /// </summary>
        public virtual string stream
        {
            get;
            set;
        }

        /// <summary>
        ///
        /// </summary>
        public virtual string symbol
        {
            get;
            set;
        }

        /// <summary>
        ///
        /// </summary>
        public virtual string action
        {
            get;
            set;
        }

        /// <summary>
        ///
        /// </summary>
        public virtual long sequentialId
        {
            get;
            set;
        }
#if DEBUG
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