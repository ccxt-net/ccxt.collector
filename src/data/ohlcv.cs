using CCXT.Collector.Library;
using System.Collections.Generic;

namespace CCXT.Collector.Service
{
    /// <summary>
    ///
    /// </summary>
    public class SOhlcvItem
    {
        /// <summary>
        /// string symbol of the market ('BTCUSD', 'ETHBTC', ...)
        /// </summary>
        public string symbol
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
        /// highest price for last 24H
        /// </summary>
        public decimal highPrice
        {
            get;
            set;
        }

        /// <summary>
        /// lowest price for last 24H
        /// </summary>
        public decimal lowPrice
        {
            get;
            set;
        }

        /// <summary>
        /// opening price before 24H
        /// </summary>
        public decimal openPrice
        {
            get;
            set;
        }

        /// <summary>
        /// price of last trade (closing price for current period)
        /// </summary>
        public decimal closePrice
        {
            get;
            set;
        }

        /// <summary>
        ///
        /// </summary>
        public long trades
        {
            get;
            set;
        }

        /// <summary>
        /// volume weighted average price
        /// </summary>
        public decimal vwap
        {
            get;
            set;
        }

        /// <summary>
        ///
        /// </summary>
        public decimal lastSize
        {
            get;
            set;
        }

        /// <summary>
        ///
        /// </summary>
        public decimal turnOver
        {
            get;
            set;
        }

        /// <summary>
        /// volume of base currency traded for last 24 hours
        /// </summary>
        public decimal baseVolume
        {
            get;
            set;
        }

        /// <summary>
        /// volume of quote currency traded for last 24 hours
        /// </summary>
        public decimal quoteVolume
        {
            get;
            set;
        }
    }

    /// <summary>
    ///
    /// </summary>
    public class SOhlcvs : SApiResult<List<SOhlcvItem>>
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
}