using CCXT.Collector.Core.Abstractions;
using CCXT.Collector.Library;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace CCXT.Collector.Service
{
    /// <summary>
    /// Ticker item with comprehensive market data
    /// </summary>
    public class STickerItem
    {
        /// <summary>
        /// Unix timestamp in milliseconds
        /// </summary>
        public long timestamp { get; set; }

        /// <summary>
        /// Opening price of the period
        /// </summary>
        public decimal openPrice { get; set; }

        /// <summary>
        /// Highest price of the period
        /// </summary>
        public decimal highPrice { get; set; }

        /// <summary>
        /// Lowest price of the period
        /// </summary>
        public decimal lowPrice { get; set; }

        /// <summary>
        /// Closing/Last price
        /// </summary>
        public decimal closePrice { get; set; }

        /// <summary>
        /// Volume in base currency
        /// </summary>
        public decimal volume { get; set; }

        /// <summary>
        /// Volume in quote currency
        /// </summary>
        public decimal quoteVolume { get; set; }

        /// <summary>
        /// Best bid price
        /// </summary>
        public virtual decimal bidPrice { get; set; }

        /// <summary>
        /// Best bid quantity/size
        /// </summary>
        public virtual decimal bidQuantity { get; set; }

        /// <summary>
        /// Best ask price
        /// </summary>
        public virtual decimal askPrice { get; set; }

        /// <summary>
        /// Best ask quantity/size  
        /// </summary>
        public virtual decimal askQuantity { get; set; }

        /// <summary>
        /// Volume weighted average price
        /// </summary>
        public decimal vwap { get; set; }

        /// <summary>
        /// Number of trades in the period
        /// </summary>
        public long count { get; set; }

        /// <summary>
        /// Previous closing price
        /// </summary>
        public decimal prevClosePrice { get; set; }

        /// <summary>
        /// Price change amount
        /// </summary>
        public decimal change { get; set; }

        /// <summary>
        /// Price change percentage
        /// </summary>
        public decimal percentage { get; set; }

        // Legacy properties for backward compatibility
        [Obsolete("Use bidQuantity instead")]
        public virtual decimal bidSize 
        { 
            get => bidQuantity; 
            set => bidQuantity = value; 
        }

        [Obsolete("Use askQuantity instead")]
        public virtual decimal askSize 
        { 
            get => askQuantity; 
            set => askQuantity = value; 
        }
    }

    /// <summary>
    /// Single ticker data structure
    /// </summary>
    public class STicker
    {
        /// <summary>
        /// Exchange name
        /// </summary>
        public string exchange { get; set; }

        /// <summary>
        /// Trading symbol/pair
        /// </summary>
        public string symbol { get; set; }

        /// <summary>
        /// Unix timestamp in milliseconds
        /// </summary>
        public long timestamp { get; set; }

        /// <summary>
        /// Ticker data
        /// </summary>
        public STickerItem result { get; set; }
    }

    /// <summary>
    ///
    /// </summary>
    public class STickers : SApiResult<List<STickerItem>>
    {
        /// <summary>
        /// 64-bit Unix Timestamp in milliseconds since Epoch 1 Jan 1970
        /// </summary>
        public new long timestamp
        {
            get;
            set;
        }

        /// <summary>
        /// 
        /// </summary>
        public decimal totalAskSize
        {
            get;
            set;
        }

        /// <summary>
        /// 
        /// </summary>
        public decimal totalBidSize
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
    public delegate void TickerEventHandler(object sender, CCEventArgs e);


    /// <summary>
    /// 
    /// </summary>
    public class CCTicker
    {
        public static event TickerEventHandler TickerEvent;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="exchange"></param>
        /// <param name="jsonMessage"></param>
        public void Write(object sender, string exchange, string jsonMessage)
        {
            if (TickerEvent != null)
            {
                TickerEvent(sender, new CCEventArgs
                {
                    exchange = exchange,
                    message = jsonMessage
                });
            }
        }
    }
}