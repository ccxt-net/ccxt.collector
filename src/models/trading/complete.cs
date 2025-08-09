using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.Collections.Generic;
using CCXT.Collector.Core.Abstractions;
using CCXT.Collector.Library;

namespace CCXT.Collector.Service
{
    /// <summary>
    /// Maker type enum
    /// </summary>
    public enum MakerType
    {
        Maker,
        Taker,
        Unknown
    }


    /// <summary>
    /// Error code enum
    /// </summary>
    public enum ErrorCode
    {
        Success = 0,
        InvalidRequest = 400,
        Unauthorized = 401,
        Forbidden = 403,
        NotFound = 404,
        ServerError = 500,
        Unknown = -1
    }

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
        bool workingIndicator
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
        decimal avgPx
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
        public string orderId
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
        [JsonConverter(typeof(StringEnumConverter))]
        public SideType sideType
        {
            get;
            set;
        }

        /// <summary>
        ///
        /// </summary>
        [JsonConverter(typeof(StringEnumConverter))]
        public OrderType orderType
        {
            get;
            set;
        }

        /// <summary>
        ///
        /// </summary>
        [JsonConverter(typeof(StringEnumConverter))]
        public MakerType makerType
        {
            get;
            set;
        }

        /// <summary>
        ///
        /// </summary>
        [JsonConverter(typeof(StringEnumConverter))]
        public OrderStatus orderStatus
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
        public long timestamp
        {
            get;
            set;
        }

        /// <summary>
        /// ISO 8601 datetime string with milliseconds
        /// </summary>
        public string datetime
        {
            get
            {
                var epoch = new System.DateTime(1970, 1, 1, 0, 0, 0, System.DateTimeKind.Utc);
                return epoch.AddMilliseconds(timestamp).ToString("o");
            }
        }

        /// <summary>
        ///
        /// </summary>
        public decimal quantity
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

        /// <summary>
        ///
        /// </summary>
        public decimal amount
        {
            get;
            set;
        }

        /// <summary>
        ///
        /// </summary>
        public decimal filled
        {
            get;
            set;
        }

        /// <summary>
        ///
        /// </summary>
        public decimal remaining
        {
            get;
            set;
        }

        /// <summary>
        ///
        /// </summary>
        public decimal avgPx
        {
            get;
            set;
        }

        /// <summary>
        ///
        /// </summary>
        public decimal cost
        {
            get;
            set;
        }

        /// <summary>
        ///
        /// </summary>
        public decimal fee
        {
            get;
            set;
        }

        /// <summary>
        ///
        /// </summary>
        public int count
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Interface for API result
    /// </summary>
    public interface ISApiResult<T>
    {
        bool success { get; set; }
        string message { get; set; }
        int statusCode { get; set; }
        ErrorCode errorCode { get; set; }
        bool supported { get; set; }
        T result { get; set; }
    }

    /// <summary>
    /// a market order list
    /// </summary>
    public interface ISMyOrders : ISApiResult<List<ISMyOrderItem>>
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
    public class SMyOrders : SApiResult<List<ISMyOrderItem>>, ISMyOrders
    {
        /// <summary>
        /// status, error code
        /// </summary>
        [JsonIgnore]
        public int statusCode
        {
            get;
            set;
        }

        /// <summary>
        ///
        /// </summary>
        [JsonIgnore]
        public ErrorCode errorCode
        {
            get;
            set;
        }

        /// <summary>
        /// check implemented
        /// </summary>
        [JsonIgnore]
        public bool supported
        {
            get;
            set;
        }

        /// <summary>
        ///
        /// </summary>
        public new string exchange
        {
            get;
            set;
        }

        /// <summary>
        /// S, R
        /// </summary>
        public new string stream
        {
            get;
            set;
        }

        /// <summary>
        ///
        /// </summary>
        public new string symbol
        {
            get;
            set;
        }

        /// <summary>
        ///
        /// </summary>
        public new string action
        {
            get;
            set;
        }

        /// <summary>
        ///
        /// </summary>
        public new long sequentialId
        {
            get;
            set;
        }
#if DEBUG
        /// <summary>
        ///
        /// </summary>
        [JsonIgnore]
        public string rawJson
        {
            get;
            set;
        }
#endif
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    public delegate void CompleteEventHandler(object sender, CCEventArgs e);


    /// <summary>
    /// 
    /// </summary>
    public class CCComplete
    {
        public static event CompleteEventHandler CompleteEvent;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="exchange"></param>
        /// <param name="jsonMessage"></param>
        public void Write(object sender, string exchange, string jsonMessage)
        {
            if (CompleteEvent != null)
            {
                CompleteEvent(sender, new CCEventArgs
                {
                    exchange = exchange,
                    message = jsonMessage
                });
            }
        }
    }
}