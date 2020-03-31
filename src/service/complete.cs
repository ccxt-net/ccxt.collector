using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using OdinSdk.BaseLib.Coin;
using OdinSdk.BaseLib.Coin.Types;
using OdinSdk.BaseLib.Configuration;
using System.Collections.Generic;

namespace CCXT.Collector.Service
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
                return CUnixTime.ConvertToUtcTimeMilli(timestamp).ToString("o");
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
#if RAWJSON
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
        public string exchange
        {
            get;
            set;
        }

        /// <summary>
        /// S, R
        /// </summary>
        public string stream
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
        public string action
        {
            get;
            set;
        }

        /// <summary>
        ///
        /// </summary>
        public long sequentialId
        {
            get;
            set;
        }
#if RAWJSON
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