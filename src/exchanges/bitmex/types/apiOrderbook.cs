using Newtonsoft.Json;

namespace CCXT.Collector.BitMEX.Types
{
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
}