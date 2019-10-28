using OdinSdk.BaseLib.Coin;
using OdinSdk.BaseLib.Configuration;
using System.Collections.Generic;

namespace CCXT.Collector.Library.Types
{
    /// <summary>
    ///
    /// </summary>
    public class STickerItem //: OdinSdk.BaseLib.Coin.Public.TickerItem, ITickerItem
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
    public class STickers : ApiResult<List<STickerItem>>
    {
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
        /// sequential id
        /// </summary>
        public virtual long sequential_id
        {
            get;
            set;
        }

        ///// <summary>
        ///// data
        ///// </summary>
        //public virtual List<STickerItem> data
        //{
        //    get;
        //    set;
        //}
#if DEBUG
        /// <summary>
        ///
        /// </summary>
        public virtual string rawJson
        {
            get;
            set;
        }
#endif
    }
}