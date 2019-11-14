using Newtonsoft.Json;
using OdinSdk.BaseLib.Coin;
using OdinSdk.BaseLib.Configuration;
using System.Collections.Generic;

namespace CCXT.Collector.Library.Types
{
    /// <summary>
    ///
    /// </summary>
    public interface ISTickerItem
    {
        ///// <summary>
        ///// string symbol of the market ('BTCUSD', 'ETHBTC', ...)
        ///// </summary>
        //string symbol
        //{
        //    get;
        //    set;
        //}

        /// <summary>
        /// 64-bit Unix Timestamp in milliseconds since Epoch 1 Jan 1970
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
        /// current best bid (buy) price
        /// </summary>
        decimal bidPrice
        {
            get;
            set;
        }

        /// <summary>
        /// current best bid (buy) amount (may be missing or undefined)
        /// </summary>
        decimal bidQuantity
        {
            get;
            set;
        }

        /// <summary>
        /// current best ask (sell) price
        /// </summary>
        decimal askPrice
        {
            get;
            set;
        }

        /// <summary>
        /// current best ask (sell) amount (may be missing or undefined)
        /// </summary>
        decimal askQuantity
        {
            get;
            set;
        }
    }

    /// <summary>
    ///
    /// </summary>
    public class STickerItem : ISTickerItem
    {
        /// <summary>
        /// string symbol of the market ('BTCUSD', 'ETHBTC', ...)
        /// </summary>
        public virtual string symbol
        {
            get;
            set;
        }

        /// <summary>
        /// 64-bit Unix Timestamp in milliseconds since Epoch 1 Jan 1970
        /// </summary>
        public virtual long timestamp
        {
            get;
            set;
        }

        /// <summary>
        /// ISO 8601 datetime string with milliseconds
        /// </summary>
        [JsonIgnore]
        public virtual string datetime
        {
            get
            {
                return CUnixTime.ConvertToUtcTimeMilli(timestamp).ToString("o");
            }
        }

        /// <summary>
        /// current best bid (buy) price
        /// </summary>
        public virtual decimal bidPrice
        {
            get;
            set;
        }

        /// <summary>
        /// current best bid (buy) amount (may be missing or undefined)
        /// </summary>
        public virtual decimal bidQuantity
        {
            get;
            set;
        }

        /// <summary>
        /// current best ask (sell) price
        /// </summary>
        public virtual decimal askPrice
        {
            get;
            set;
        }

        /// <summary>
        /// current best ask (sell) amount (may be missing or undefined)
        /// </summary>
        public virtual decimal askQuantity
        {
            get;
            set;
        }
    }

    /// <summary>
    ///
    /// </summary>
    public interface ISTickers : IApiResult<List<ISTickerItem>>
    {
        /// <summary>
        /// exchange
        /// </summary>
        string exchange
        {
            get;
            set;
        }

        /// <summary>
        /// stream
        /// </summary>
        string stream
        {
            get;
            set;
        }

        /// <summary>
        /// symbol
        /// </summary>
        string symbol
        {
            get;
            set;
        }

        /// <summary>
        /// sequential id
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
    public class STickers : ApiResult<List<ISTickerItem>>, ISTickers
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
        /// exchange
        /// </summary>
        public virtual string exchange
        {
            get;
            set;
        }

        /// <summary>
        /// stream
        /// </summary>
        public virtual string stream
        {
            get;
            set;
        }

        /// <summary>
        /// symbol
        /// </summary>
        public virtual string symbol
        {
            get;
            set;
        }

        /// <summary>
        /// action
        /// </summary>
        public virtual string action
        {
            get;
            set;
        }

        /// <summary>
        /// sequential id
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