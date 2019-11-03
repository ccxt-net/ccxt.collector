using Newtonsoft.Json;
using OdinSdk.BaseLib.Coin.Public;
using System.Collections.Generic;

namespace CCXT.Collector.BitMEX.Public
{
    /// <summary>
    ///
    /// </summary>
    public class BOrderBookItem : OdinSdk.BaseLib.Coin.Public.OrderBookItem, IOrderBookItem
    {
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
        public long id
        {
            get;
            set;
        }

        /// <summary>
        ///
        /// </summary>
        public string side
        {
            get;
            set;
        }

        /// <summary>
        ///
        /// </summary>
        [JsonProperty(PropertyName = "size")]
        public override decimal quantity
        {
            get;
            set;
        }

        /// <summary>
        ///
        /// </summary>
        public override decimal price
        {
            get;
            set;
        }
    }

    /// <summary>
    ///
    /// </summary>
    public class BOrderBookData
    {
        /// <summary>
        /// Symbol
        /// </summary>
        public virtual string symbol
        {
            get;
            set;
        }

        /// <summary>
        /// First update ID in event
        /// </summary>
        public virtual long firstId
        {
            get;
            set;
        }

        /// <summary>
        ///
        /// </summary>
        public virtual long lastId
        {
            get;
            set;
        }

        /// <summary>
        ///
        /// </summary>
        public virtual List<decimal[]> asks
        {
            get;
            set;
        }

        /// <summary>
        ///
        /// </summary>
        public virtual List<decimal[]> bids
        {
            get;
            set;
        }
    }

    //{
    //    "lastUpdateId": 482182248,
    //    "bids": [
    //        ["5005.00000000", "0.09685300"],
    //        ["5004.27000000", "0.00629700"],
    //        ["5001.81000000", "0.50000000"]
    //    ],
    //    "asks": [
    //        ["5005.27000000", "0.61975200"],
    //        ["5005.28000000", "0.00002800"],
    //        ["5009.19000000", "0.96814300"]
    //    ]
    //}

    /// <summary>
    ///
    /// </summary>
    public class BAOrderBook
    {
        /// <summary>
        ///
        /// </summary>
        public string stream
        {
            get;
            set;
        }

        /// <summary>
        ///
        /// </summary>
        public BAOrderBookData data
        {
            get;
            set;
        }
    }

    /// <summary>
    ///
    /// </summary>
    public class BAOrderBookData : BOrderBookData
    {
        /// <summary>
        ///
        /// </summary>
        [JsonProperty(PropertyName = "lastUpdateId")]
        public override long lastId
        {
            get;
            set;
        }
    }

    //{
    //    "stream": "btcusdt@depth",
    //    "data": {
    //        "e": "depthUpdate",
    //        "E": 1554366183368,
    //        "s": "BTCUSDT",
    //        "U": 482159138,
    //        "u": 482159151,
    //        "b": [
    //            ["5022.59000000", "2.19595400"],
    //            ["5021.43000000", "0.00000000"],
    //            ["5021.42000000", "0.50000000"],
    //            ["5021.39000000", "0.00000000"],
    //            ["5021.04000000", "0.03981900"],
    //            ["5019.56000000", "0.50000000"],
    //            ["5010.26000000", "0.00000000"]
    //        ],
    //        "a": [
    //            ["5022.71000000", "0.20744000"],
    //            ["5022.80000000", "0.06535500"],
    //            ["5024.72000000", "0.00000000"],
    //            ["5025.07000000", "0.20000000"],
    //            ["5025.93000000", "0.00000000"],
    //            ["5055.89000000", "4.40000000"]
    //        ]
    //    }
    //}

    /// <summary>
    ///
    /// </summary>
    public class BWOrderBook
    {
        /// <summary>
        ///
        /// </summary>
        public string stream
        {
            get;
            set;
        }

        /// <summary>
        ///
        /// </summary>
        public BWOrderBookData data
        {
            get;
            set;
        }
    }

    /// <summary>
    ///
    /// </summary>
    public class BWOrderBookData : BOrderBookData
    {
        /// <summary>
        ///
        /// </summary>
        [JsonProperty(PropertyName = "e")]
        public string eventType
        {
            get;
            set;
        }

        /// <summary>
        /// Event time
        /// </summary>
        [JsonProperty(PropertyName = "E")]
        public long eventTime
        {
            get;
            set;
        }

        /// <summary>
        /// Symbol
        /// </summary>
        [JsonProperty(PropertyName = "s")]
        public override string symbol
        {
            get;
            set;
        }

        /// <summary>
        /// First update ID in event
        /// </summary>
        [JsonProperty(PropertyName = "U")]
        public override long firstId
        {
            get;
            set;
        }

        /// <summary>
        /// Final update ID in event
        /// </summary>
        [JsonProperty(PropertyName = "u")]
        public override long lastId
        {
            get;
            set;
        }

        /// <summary>
        ///
        /// </summary>
        [JsonProperty(PropertyName = "a")]
        public override List<decimal[]> asks
        {
            get;
            set;
        }

        /// <summary>
        ///
        /// </summary>
        [JsonProperty(PropertyName = "b")]
        public override List<decimal[]> bids
        {
            get;
            set;
        }
    }
}