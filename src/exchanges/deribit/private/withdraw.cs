using Newtonsoft.Json;
using CCXT.NET.Shared.Coin.Private;

namespace CCXT.Collector.Deribit.Private
{
    /*
     {
        "address": "2NBqqD5GRJ8wHy1PYyCXTe9ke5226FhavBz",
        "amount": 0.4,
        "confirmed_timestamp": null,
        "created_timestamp": 1550574558607,
        "currency": "BTC",
        "fee": 0.0001,
        "id": 4,
        "priority": 1,
        "state": "unconfirmed",
        "transaction_id": null,
        "updated_timestamp": 1550574558607
    }
    */

    /// <summary>
    ///
    /// </summary>
    public class DWithdraw : CCXT.NET.Shared.Coin.Private.TransferItem, ITransferItem
    {
        /// <summary>
        ///
        /// </summary>
        [JsonProperty(PropertyName = "address")]
        public override string toAddress
        {
            get;
            set;
        }

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
        public override decimal fee
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
        public int priority
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
        [JsonProperty(PropertyName = "transaction_id")]
        public override string transactionId
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