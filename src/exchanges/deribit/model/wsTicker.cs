using System.Collections.Generic;

namespace CCXT.Collector.Deribit.Model
{

    /*
     {
        "jsonrpc": "2.0",
        "method": "subscription",
        "params": {
            "channel": "ticker.BTC-25SEP20-10000-C.100ms",
            "data": {
                "underlying_price": 9163.58,
                "underlying_index": "BTC-25SEP20",
                "timestamp": 1590114717431,
                "stats": {
                    "volume": 20,
                    "price_change": -14.5553,
                    "low": 0.1585,
                    "high": 0.1855
                },
                "state": "open",
                "settlement_price": 0.18,
                "open_interest": 628.1,
                "min_price": 0.1075,
                "max_price": 0.2265,
                "mark_price": 0.16498767,
                "mark_iv": 85.51,
                "last_price": 0.1585,
                "interest_rate": 0,
                "instrument_name": "BTC-25SEP20-10000-C",
                "index_price": 9077.87,
                "greeks": {
                    "vega": 21.4336,
                    "theta": -7.25948,
                    "rho": 11.59813,
                    "gamma": 0.00009,
                    "delta": 0.53097
                },
                "estimated_delivery_price": 9077.87,
                "bid_iv": 84.66,
                "best_bid_price": 0.163,
                "best_bid_amount": 8.9,
                "best_ask_price": 0.167,
                "best_ask_amount": 1,
                "ask_iv": 86.37
            }
        }
     }
     */

    public class DTickerStats
    {
        /// <summary>
        /// 
        /// </summary>
        public decimal volume
        {
            get;
            set;
        }

        /// <summary>
        /// 
        /// </summary>
        public decimal price_change
        {
            get;
            set;
        }

        /// <summary>
        /// 
        /// </summary>
        public decimal low
        {
            get;
            set;
        }

        /// <summary>
        /// 
        /// </summary>
        public decimal high
        {
            get;
            set;
        }
    }

    public class DTickerGreeks
    {
        /// <summary>
        /// 
        /// </summary>
        public decimal vega
        {
            get;
            set;
        }

        /// <summary>
        /// 
        /// </summary>
        public decimal theta
        {
            get;
            set;
        }

        /// <summary>
        /// 
        /// </summary>
        public decimal rho
        {
            get;
            set;
        }

        /// <summary>
        /// 
        /// </summary>
        public decimal gamma
        {
            get;
            set;
        }

        /// <summary>
        /// 
        /// </summary>
        public decimal delta
        {
            get;
            set;
        }
    }

    /// <summary>
    ///
    /// </summary>
    public class DWsTicker
    {
        /// <summary>
        /// 
        /// </summary>
        public decimal underlying_price
        {
            get; set;
        }

        /// <summary>
        /// 
        /// </summary>
        public string underlying_index
        {
            get; set;
        }

        /// <summary>
        /// 
        /// </summary>
        public long timestamp
        {
            get; set;
        }

        /// <summary>
        /// 
        /// </summary>
        public DTickerStats stats
        {
            get;
            set;
        }

        /// <summary>
        /// 
        /// </summary>
        public string state
        {
            get; set;
        }

        /// <summary>
        /// 
        /// </summary>
        public decimal settlement_price
        {
            get; set;
        }

        /// <summary>
        /// 
        /// </summary>
        public decimal open_interest
        {
            get; set;
        }

        /// <summary>
        /// 
        /// </summary>
        public decimal min_price
        {
            get; set;
        }

        /// <summary>
        /// 
        /// </summary>
        public decimal max_price
        {
            get; set;
        }

        /// <summary>
        /// 
        /// </summary>
        public decimal mark_price
        {
            get; set;
        }

        /// <summary>
        /// 
        /// </summary>
        public decimal mark_iv
        {
            get; set;
        }

        /// <summary>
        /// 
        /// </summary>
        public decimal last_price
        {
            get; set;
        }

        /// <summary>
        /// 
        /// </summary>
        public decimal interest_rate
        {
            get; set;
        }

        /// <summary>
        /// 
        /// </summary>
        public string instrument_name
        {
            get; set;
        }

        /// <summary>
        /// 
        /// </summary>
        public decimal index_price
        {
            get; set;
        }

        /// <summary>
        /// 
        /// </summary>
        public DTickerGreeks greeks
        {
            get;
            set;
        }

        /// <summary>
        /// 
        /// </summary>
        public decimal estimated_delivery_price
        {
            get; set;
        }

        /// <summary>
        /// 
        /// </summary>
        public decimal bid_iv
        {
            get; set;
        }

        /// <summary>
        /// 
        /// </summary>
        public decimal best_bid_price
        {
            get; set;
        }

        /// <summary>
        /// 
        /// </summary>
        public decimal best_bid_amount
        {
            get; set;
        }

        /// <summary>
        /// 
        /// </summary>
        public decimal best_ask_price
        {
            get; set;
        }

        /// <summary>
        /// 
        /// </summary>
        public decimal best_ask_amount
        {
            get; set;
        }

        /// <summary>
        /// 
        /// </summary>
        public decimal ask_iv
        {
            get; set;
        }
    }

    public class DWsTickerComparer : IComparer<DWsTicker>
    {
        public int Compare(DWsTicker x, DWsTicker y)
        {
            var _result = 1;

            if (x != null && y != null)
            {
                _result = x.instrument_name.CompareTo(y.instrument_name);
            }
            else
            {
                if (x == null)
                    _result = (y == null) ? 0 : -1;
            }

            return _result;
        }
    }
}