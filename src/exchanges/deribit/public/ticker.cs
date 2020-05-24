using OdinSdk.BaseLib.Coin.Public;

namespace CCXT.Collector.Deribit.Public
{
    /// <summary>
    ///
    /// </summary>
    public class DTickerItem 
    {
        /// <summary>
        /// 
        /// </summary>
        public string status
        {
            get;
            set;
        }

        /// <summary>
        /// volume of quote currency traded for last 24 hours
        /// </summary>
        public decimal[] volume
        {
            get;
            set;
        }

        /// <summary>
        /// 
        /// </summary>
        public long[] ticks
        {
            get;
            set;
        }

        /// <summary>
        /// opening price 
        /// </summary>
        public decimal[] open
        {
            get;
            set;
        }

        /// <summary>
        /// lowest price 
        /// </summary>
        public decimal[] low
        {
            get;
            set;
        }

        /// <summary>
        /// highest price 
        /// </summary>
        public decimal[] high
        {
            get;
            set;
        }

        /// <summary>
        /// volume weighted average price
        /// </summary>
        public decimal[] cost
        {
            get;
            set;
        }

        /// <summary>
        /// price of last trade (closing price for current period)
        /// </summary>
        public decimal[] close
        {
            get;
            set;
        }
    }
}