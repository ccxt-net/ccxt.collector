using CCXT.Collector.Library;
using System.Collections.Generic;

namespace CCXT.Collector.Service
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
        public decimal quantity
        {
            get;
            set;
        }

        /// <summary>
        /// price
        /// </summary>
        public decimal price
        {
            get;
            set;
        }

        /// <summary>
        /// amount (quantity * price)
        /// </summary>
        public decimal amount
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

        /// <summary>
        ///
        /// </summary>
        public long id
        {
            get;
            set;
        }
    }

    /// <summary>
    ///
    /// </summary>
    public class SOrderBook
    {
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
        public long timestamp
        {
            get;
            set;
        }

        /// <summary>
        /// buy array
        /// </summary>
        public List<SOrderBookItem> bids
        {
            get;
            set;
        }

        /// <summary>
        /// sell array
        /// </summary>
        public List<SOrderBookItem> asks
        {
            get;
            set;
        }
    }

    /// <summary>
    ///
    /// </summary>
    public class SOrderBooks : SApiResult<SOrderBook>
    {
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
    public delegate void OrderBookEventHandler(object sender, CCEventArgs e);


    /// <summary>
    /// 
    /// </summary>
    public class CCOrderBook
    {
        public static event OrderBookEventHandler OrderBookEvent;

        /// <summary>
        ///
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="exchange"></param>
        /// <param name="jsonMessage"></param>
        public void Write(object sender, string exchange, string jsonMessage)
        {
            if (OrderBookEvent != null)
            {
                OrderBookEvent(sender, new CCEventArgs
                {
                    exchange = exchange,
                    message = jsonMessage
                });
            }
        }
    }
}