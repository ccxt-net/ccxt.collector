using Newtonsoft.Json;

namespace CCXT.Collector.Deribit.Public
{
    /// <summary>
    ///
    /// </summary>
    public class DCurrency
    {
        [JsonProperty(PropertyName = "withdrawal_fee")]
        public decimal withdrawalFee
        {
            get; set;
        }

        [JsonProperty(PropertyName = "min_withdrawal_fee")]
        public decimal minWithdrawalFee
        {
            get; set;
        }

        [JsonProperty(PropertyName = "min_confirmations")]
        public int minConfirmations
        {
            get; set;
        }

        [JsonProperty(PropertyName = "fee_precision")]
        public int feePrecision
        {
            get; set;
        }

        [JsonProperty(PropertyName = "currency_long")]
        public string currencyLong
        {
            get; set;
        }

        [JsonProperty(PropertyName = "currency")]
        public string currency
        {
            get; set;
        }

        [JsonProperty(PropertyName = "coin_type")]
        public string coinType
        {
            get; set;
        }
    }
}