using Newtonsoft.Json;
using OdinSdk.BaseLib.Coin;
using OdinSdk.BaseLib.Coin.Types;
using System.Collections.Generic;

namespace CCXT.Collector.Library.Types
{
    /// <summary>
    /// 
    /// </summary>
    public interface ISCompleteOrderItem
    {
        ///// <summary>
        ///// 
        ///// </summary>
        //string symbol
        //{
        //    get;
        //    set;
        //}

        /// <summary>
        ///
        /// </summary>
        long timestamp
        {
            get;
            set;
        }

        decimal quantity
        {
            get;
            set;
        }

        decimal price
        {
            get;
            set;
        }

        /// <summary>
        /// sell or buy
        /// </summary>
        string side
        {
            get;
        }

        /// <summary>
        /// sell or buy
        /// </summary>
        SideType sideType
        {
            get;
            set;
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public class SCompleteOrderItem : ISCompleteOrderItem
    {
        ///// <summary>
        ///// 
        ///// </summary>
        //public virtual string symbol
        //{
        //    get;
        //    set;
        //}

        /// <summary>
        ///
        /// </summary>
        public virtual long timestamp
        {
            get;
            set;
        }

        public virtual decimal quantity
        {
            get;
            set;
        }

        public virtual decimal price
        {
            get;
            set;
        }

        /// <summary>
        /// 
        /// </summary>
        public string side
        {
            get
            {
                return sideType.ToString();
            }
        }

        /// <summary>
        /// sell or buy
        /// </summary>
        [JsonIgnore]
        public SideType sideType
        {
            get;
            set;
        }
    }

    /// <summary>
    ///
    /// </summary>
    public interface ISCompleteOrders : IApiResult<List<ISCompleteOrderItem>>
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
    ///
    /// </summary>
    public class SCompleteOrders : ApiResult<List<ISCompleteOrderItem>>, ISCompleteOrders
    {
        /// <summary>
        /// 
        /// </summary>
        public SCompleteOrders()
        {
        }

        /// <summary>
        /// 
        /// </summary>
        public SCompleteOrders(string base_name, string quote_name)
        {
            this.symbol = $"{quote_name}-{base_name}";
        }

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