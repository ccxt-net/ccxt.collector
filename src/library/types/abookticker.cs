using System.Collections.Concurrent;

namespace CCXT.Collector.Library.Types
{
    /// <summary>
    /// 
    /// </summary>
    public class ABookTickerItem : SBookTickerItem
    {
        /// <summary>
        /// sequential id
        /// </summary>
        public virtual long sequential_id
        {
            get;
            set;
        }

        /// <summary>
        /// 
        /// </summary>
        public virtual decimal sellPrice
        {
            get;
            set;
        }

        /// <summary>
        /// 
        /// </summary>
        public virtual decimal buyPrice
        {
            get;
            set;
        }

        /// <summary>
        /// 
        /// </summary>
        public virtual decimal quantity
        {
            get;
            set;
        }

        /// <summary>
        /// 
        /// </summary>
        public virtual decimal price
        {
            get;
            set;
        }

        /// <summary>
        /// 
        /// </summary>
        public virtual decimal amount
        {
            get;
            set;
        }

        /// <summary>
        /// 
        /// </summary>
        public virtual decimal last_askQty
        {
            get;
            set;
        }

        /// <summary>
        /// 
        /// </summary>
        public virtual decimal last_bidQty
        {
            get;
            set;
        }

        /// <summary>
        /// 
        /// </summary>
        public virtual decimal profit
        {
            get;
            set;
        }

        /// <summary>
        /// 
        /// </summary>
        public virtual decimal exchangeRate
        {
            get;
            set;
        }

        /// <summary>
        /// 
        /// </summary>
        public virtual ABookExchange market
        {
            get;
            set;
        }
    }

    public class ABookExchange
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
        /// 
        /// </summary>
        public virtual string symbol
        {
            get;
            set;
        }

        /// <summary>
        /// 
        /// </summary>
        public virtual string market_id
        {
            get;
            set;
        }

        /// <summary>
        /// BTC,ETH,XRP
        /// </summary>
        public virtual string base_id
        {
            get;
            set;
        }

        /// <summary>
        /// USDT,KRW
        /// </summary>
        public virtual string quote_id
        {
            get;
            set;
        }

        /// <summary>
        /// 
        /// </summary>
        public virtual decimal taker_fee
        {
            get;
            set;
        }

        /// <summary>
        /// 
        /// </summary>
        public virtual decimal maker_fee
        {
            get;
            set;
        }

        /// <summary>
        /// 
        /// </summary>
        public virtual long buy_count
        {
            get;
            set;
        }

        /// <summary>
        /// 
        /// </summary>
        public virtual long sell_count
        {
            get;
            set;
        }

        /// <summary>
        /// BTC,ETH,XRP
        /// </summary>
        public virtual decimal quantity
        {
            get;
            set;
        }

        /// <summary>
        /// 
        /// </summary>
        public virtual AQuoteItem quote
        {
            get;
            set;
        }

        /// <summary>
        /// 
        /// </summary>
        public virtual ConcurrentBag<AQuoteItem> quotes
        {
            get;
            set;
        }
    }

    public class AQuoteItem
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
        /// USDT,KRW
        /// </summary>
        public virtual string quote_id
        {
            get;
            set;
        }

        /// <summary>
        /// 
        /// </summary>
        public virtual decimal total_amt
        {
            get;
            set;
        }

        /// <summary>
        /// 
        /// </summary>
        public virtual decimal invest_amt
        {
            get;
            set;
        }

        /// <summary>
        /// 
        /// </summary>
        public virtual decimal income
        {
            get;
            set;
        }
    }
}