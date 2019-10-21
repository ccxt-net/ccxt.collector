using Newtonsoft.Json;
using OdinSdk.BaseLib.Coin.Public;
using System.Collections.Generic;

namespace CCXT.Collector.Upbit.Public
{
    /// <summary>
    /// item of orderbook
    /// </summary>
    public class UOrderBookItem
    {
        /// <summary>
        ///
        /// </summary>
        public virtual decimal ask_price
        {
            get;
            set;
        }

        /// <summary>
        ///
        /// </summary>
        public virtual decimal ask_size
        {
            get;
            set;
        }

        /// <summary>
        ///
        /// </summary>
        public virtual decimal bid_price
        {
            get;
            set;
        }

        /// <summary>
        ///
        /// </summary>
        public virtual decimal bid_size
        {
            get;
            set;
        }
    }

    /// <summary>
    ///
    /// </summary>
    public class UOrderBook : CCXT.Collector.Library.Types.SOrderBook
    {
        /// <summary>
        /// 호가 매도 총 잔량
        /// </summary>
        [JsonProperty(PropertyName = "total_ask_size")]
        public override decimal totalAskQuantity
        {
            get;
            set;
        }

        /// <summary>
        /// 호가 매수 총 잔량
        /// </summary>
        [JsonProperty(PropertyName = "total_bid_size")]
        public override decimal totalBidQuantity
        {
            get;
            set;
        }

        /// <summary>
        /// 호가
        /// </summary>
        [JsonProperty(PropertyName = "orderbook_units")]
        private List<UOrderBookItem> orderbooks
        {
            set
            {
                this.asks = new List<OrderBookItem>();
                this.bids = new List<OrderBookItem>();

                foreach (var _o in value)
                {
                    this.asks.Add(new OrderBookItem
                    {
                        quantity = _o.ask_size,
                        price = _o.ask_price,
                        amount = _o.ask_size * _o.ask_price,
                        count = 1
                    });

                    this.bids.Add(new OrderBookItem
                    {
                        quantity = _o.bid_size,
                        price = _o.bid_price,
                        amount = _o.bid_size * _o.bid_price,
                        count = 1
                    });
                }
            }
        }
    }

    //{
    //  "market": "KRW-BTC",
    //  "timestamp": 1529910247984,
    //  "total_ask_size": 8.83621228,
    //  "total_bid_size": 2.43976741,
    //  "orderbook_units": [{
    //      "ask_price": 6956000,
    //      "bid_price": 6954000,
    //      "ask_size": 0.24078656,
    //      "bid_size": 0.00718341
    //  }]
    //}

    /// <summary>
    ///
    /// </summary>
    public class UAOrderBook : UOrderBook
    {
        /// <summary>
        ///
        /// </summary>
        [JsonProperty(PropertyName = "market")]
        public override string symbol
        {
            get;
            set;
        }
    }

    //{
    //    "code": "KRW-BTC",
    //    "timestamp": 1553853571184,
    //    "total_ask_size": 36.90715747,
    //    "total_bid_size": 29.12064978,
    //    "orderbook_units": [{
    //        "ask_price": 4627000.0,
    //        "bid_price": 4626000.0,
    //        "ask_size": 1.40706623,
    //        "bid_size": 3.57203617
    //            }],
    //    "stream_type": "SNAPSHOT"
    //}

    /// <summary>
    ///
    /// </summary>
    public class UWOrderBook : UOrderBook
    {
        /// <summary>
        ///
        /// </summary>
        [JsonProperty(PropertyName = "code")]
        public override string symbol
        {
            get;
            set;
        }

        /// <summary>
        ///
        /// </summary>
        public string stream_type
        {
            get;
            set;
        }
    }
}