using Newtonsoft.Json;
using OdinSdk.BaseLib.Coin.Public;
using System.Collections.Generic;

namespace CCXT.Collector.Deribit.Public
{
    /*
    {
        "timestamp":1586715917140
        "stats":
        {
            "volume_usd":84270120
            "volume":12112.91838284
            "price_change":4.6947
            "low":6768.5
            "high":7199
        }
        "state":"open"
        "settlement_price":6835.65
        "open_interest":57362380
        "min_price":7017.27
        "max_price":7230.99
        "mark_price":7124.29
        "last_price":7125
        "instrument_name":"BTC-PERPETUAL"
        "index_price":7125.68
        "funding_8h":-0.00022331
        "estimated_delivery_price":7125.68
        "current_funding":0
        "change_id":18497226614
        "best_bid_price":7124.5
        "best_bid_amount":2180
        "best_ask_price":7125
        "best_ask_amount":56330
        "bids":[ price, qty ]
        "asks":[ price, qty ]
    }
    */

    public class DOrderBookStates
    {
        public decimal volume_usd
        {
            get; set;
        }
        
        public decimal volume
        {
            get; set;
        }
        
        public decimal price_change
        {
            get; set;
        }
        
        public decimal low
        {
            get; set;
        }

        public decimal high
        {
            get; set;
        }
    }

    /// <summary>
    ///
    /// </summary>
    public class DOrderBook : OdinSdk.BaseLib.Coin.Public.OrderBook, IOrderBook
    {
        /// <summary>
        ///
        /// </summary>
        [JsonProperty(PropertyName = "instrument_name")]
        public override string symbol
        {
            get;
            set;
        }

        /// <summary>
        /// 
        /// </summary>
        public override long timestamp
        {
            get; set;
        }

        public DOrderBookStates states
        {
            get; set;
        }

        public string state
        {
            get; set;
        }

        public decimal settlement_price
        {
            get; set;
        }

        public decimal open_interest
        {
            get; set;
        }

        public decimal min_price
        {
            get; set;
        }

        public decimal max_price
        {
            get; set;
        }

        public decimal mark_price
        {
            get; set;
        }

        public decimal last_price
        {
            get; set;
        }

        public decimal index_price
        {
            get; set;
        }

        public decimal funding_8h
        {
            get; set;
        }

        public decimal estimated_delivery_price
        {
            get; set;
        }

        public decimal current_funding
        {
            get; set;
        }

        public long change_id
        {
            get; set;
        }

        public decimal best_bid_price
        {
            get; set;
        }

        public decimal best_bid_amount
        {
            get; set;
        }

        public decimal best_ask_price
        {
            get; set;
        }

        public decimal best_ask_amount
        {
            get; set;
        }

        [JsonProperty(PropertyName = "bids")]
        public decimal[][] bid_array
        {
            set
            {
                this.bids = new List<OrderBookItem>();
                foreach (var _b in value)
                {
                    this.bids.Add(new OrderBookItem
                    {
                        price = _b[0],
                        quantity = _b[1],
                        amount = _b[0] * _b[1],
                        count = 1
                    });
                }
            }
        }

        [JsonProperty(PropertyName = "asks")]
        public decimal[][] ask_array
        {
            set
            {
                this.asks = new List<OrderBookItem>();
                foreach (var _a in value)
                {
                    this.asks.Add(new OrderBookItem
                    {
                        price = _a[0],
                        quantity = _a[1],
                        amount = _a[0] * _a[1],
                        count = 1
                    });
                }
            }
        }
    }
}