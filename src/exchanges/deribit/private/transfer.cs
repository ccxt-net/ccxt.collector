using Newtonsoft.Json;
using CCXT.NET.Shared.Coin.Private;
using System.Collections.Generic;

namespace CCXT.Collector.Deribit.Private
{
    /*
     {
        "count": 2,
        "data": [
            {
                "amount": 0.2,
                "created_timestamp": 1550579457727,
                "currency": "BTC",
                "direction": "payment",
                "id": 2,
                "other_side": "2MzyQc5Tkik61kJbEpJV5D5H9VfWHZK9Sgy",
                "state": "prepared",
                "type": "user",
                "updated_timestamp": 1550579457727
            },
            {
                "amount": 0.3,
                "created_timestamp": 1550579255800,
                "currency": "BTC",
                "direction": "payment",
                "id": 1,
                "other_side": "new_user_1_1",
                "state": "confirmed",
                "type": "subaccount",
                "updated_timestamp": 1550579255800
            }
        ]
    }
    */

    /// <summary>
    ///
    /// </summary>
    public class DTransfer
    {
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
        public List<DTransferItem> data
        {
            get;
            set;
        }
    }

    /// <summary>
    ///
    /// </summary>
    public class DTransferItem : CCXT.NET.Shared.Coin.Private.TransferItem, ITransferItem
    {
        /// <summary>
        /// 
        /// </summary>
        public override decimal amount
        {
            get;
            set;
        }

        /// <summary>
        ///
        /// </summary>
        [JsonProperty(PropertyName = "created_timestamp")]
        public override long timestamp
        {
            get;
            set;
        }

        /// <summary>
        ///
        /// </summary>
        public override string currency
        {
            get;
            set;
        }

        /// <summary>
        ///
        /// </summary>
        public string direction
        {
            get;
            set;
        }

        /// <summary>
        ///
        /// </summary>
        public int id
        {
            get;
            set;
        }

        /// <summary>
        ///
        /// </summary>
        [JsonProperty(PropertyName = "other_side")]
        public override string toAddress
        {
            get;
            set;
        }

        /// <summary>
        ///
        /// </summary>
        public string state
        {
            get;
            set;
        }

        /// <summary>
        ///
        /// </summary>
        public string type
        {
            get;
            set;
        }

        /// <summary>
        ///
        /// </summary>
        public long updated_timestamp
        {
            get;
            set;
        }
    }
}