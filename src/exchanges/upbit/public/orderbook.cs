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
    public class UOrderBook : OdinSdk.BaseLib.Coin.Public.OrderBook, IOrderBook
    {
        /// <summary>
        ///
        /// </summary>
        public virtual string type
        {
            get;
            set;
        }

        /// <summary>
        /// 마켓 코드
        /// </summary>
        [JsonProperty(PropertyName = "market")]
        public override string symbol
        {
            get;
            set;
        }

        /// <summary>
        /// 호가 매도 총 잔량
        /// </summary>
        [JsonProperty(PropertyName = "total_ask_size")]
        public decimal totalAskQuantity
        {
            get;
            set;
        }

        /// <summary>
        /// 호가 매수 총 잔량
        /// </summary>
        [JsonProperty(PropertyName = "total_bid_size")]
        public decimal totalBidQuantity
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
                this.asks = new List<IOrderBookItem>();
                this.bids = new List<IOrderBookItem>();

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
}