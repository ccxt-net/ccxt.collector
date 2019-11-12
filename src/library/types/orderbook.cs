using Newtonsoft.Json;
using OdinSdk.BaseLib.Coin;
using OdinSdk.BaseLib.Configuration;
using System.Collections.Generic;

namespace CCXT.Collector.Library.Types
{
    /// <summary>
    /// item of orderbook
    /// </summary>
    public class SOrderBookItem
    {
        /// <summary>
        /// I,U,D
        /// </summary>
        public string action
        {
            get;
            set;
        }

        /// <summary>
        /// quantity
        /// </summary>
        public virtual decimal quantity
        {
            get;
            set;
        }

        /// <summary>
        /// price
        /// </summary>
        public virtual decimal price
        {
            get;
            set;
        }

        /// <summary>
        /// amount (quantity * price)
        /// </summary>
        public virtual decimal amount
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
    ///
    /// </summary>
    public interface ISOrderBook
    {
        /// <summary>
        /// 호가 매도 총 잔량
        /// </summary>
        decimal askSumQty
        {
            get;
            set;
        }

        /// <summary>
        /// 호가 매수 총 잔량
        /// </summary>
        decimal bidSumQty
        {
            get;
            set;
        }

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
        /// buy array
        /// </summary>
        List<SOrderBookItem> bids
        {
            get;
            set;
        }

        /// <summary>
        /// sell array
        /// </summary>
        List<SOrderBookItem> asks
        {
            get;
            set;
        }
    }

    /// <summary>
    ///
    /// </summary>
    public class SOrderBook : ISOrderBook
    {
        /// <summary>
        /// 
        /// </summary>
        public SOrderBook()
        {
            this.asks = new List<SOrderBookItem>();
            this.bids = new List<SOrderBookItem>();
        }

        /// <summary>
        /// 호가 매도 총 잔량
        /// </summary>
        public virtual decimal askSumQty
        {
            get;
            set;
        }

        /// <summary>
        /// 호가 매수 총 잔량
        /// </summary>
        public virtual decimal bidSumQty
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
        /// buy array
        /// </summary>
        public virtual List<SOrderBookItem> bids
        {
            get;
            set;
        }

        /// <summary>
        /// sell array
        /// </summary>
        public virtual List<SOrderBookItem> asks
        {
            get;
            set;
        }
    }

    /// <summary>
    ///
    /// </summary>
    public interface ISOrderBooks : IApiResult<ISOrderBook>
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
        /// string symbol of the market ('BTCUSD', 'ETHBTC', ...)
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
    public class SOrderBooks : ApiResult<ISOrderBook>, ISOrderBooks
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
        /// string symbol of the market ('BTCUSD', 'ETHBTC', ...)
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