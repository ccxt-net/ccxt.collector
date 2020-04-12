using Newtonsoft.Json;
using OdinSdk.BaseLib.Coin.Public;
using System;

namespace CCXT.Collector.Deribit.Public
{
    /*
    {
        "instrument_name":"BTC-26JUN20-8000-C"
        "base_currency":"BTC"
        "quote_currency":"USD"
        "is_active":true
        "tick_size":0.0005
        "min_trade_amount":0.1
        "kind":"option"
        "contract_size":1
        "strike":8000
        "expiration_timestamp":1593158400000
        "creation_timestamp":1571905490000
        "option_type":"call"
        "settlement_period":"month"
        "taker_commission":0.0004
        "maker_commission":0.0004
    }
    */

    /// <summary>
    ///
    /// </summary>
    public class DMarketItem : OdinSdk.BaseLib.Coin.Public.MarketItem, IMarketItem
    {
        /// <summary>
        /// Unique instrument identifier
        /// </summary>
        /// <value>Unique instrument identifier</value>
        [JsonProperty(PropertyName = "instrument_name")]
        public override string marketId
        {
            get; set;
        }

        /// <summary>
        /// The underlying currency being traded.
        /// </summary>
        /// <value>The underlying currency being traded.</value>
        [JsonProperty(PropertyName = "base_currency")]
        public override string baseId
        {
            get; set;
        }

        /// <summary>
        /// The currency in which the instrument prices are quoted.
        /// </summary>
        /// <value>The currency in which the instrument prices are quoted.</value>
        [JsonProperty(PropertyName = "quote_currency")]
        public override string quoteId
        {
            get; set;
        }

        /// <summary>
        /// 
        /// </summary>
        [JsonProperty(PropertyName = "taker_commission")]
        public decimal taker
        {
            get; set;
        }

        /// <summary>
        /// 
        /// </summary>
        [JsonProperty(PropertyName = "maker_commission")]
        public decimal maker
        {
            get; set;
        }

        /// <summary>
        /// Instrument kind, "future" or "option"
        /// </summary>
        /// <value>Instrument kind, "future" or "option"</value>
        [JsonProperty(PropertyName = "kind")]
        public string type
        {
            get; set;
        }

        /// <summary>
        /// Indicates if the instrument can currently be traded.
        /// </summary>
        /// <value>Indicates if the instrument can currently be traded.</value>
        [JsonProperty(PropertyName = "is_active")]
        public override bool active
        {
            get; set;
        }

        /// <summary>
        /// Minimum amount for trading. For perpetual and futures - in USD units, for options it is amount of corresponding cryptocurrency contracts, e.g., BTC or ETH.
        /// </summary>
        /// <value>Minimum amount for trading. For perpetual and futures - in USD units, for options it is amount of corresponding cryptocurrency contracts, e.g., BTC or ETH.</value>
        [JsonProperty(PropertyName = "min_trade_amount")]
        public decimal minTradeAmount
        {
            get; set;
        }

        /// <summary>
        /// specifies minimal price change and, as follows, the number of decimal places for instrument prices
        /// </summary>
        /// <value>specifies minimal price change and, as follows, the number of decimal places for instrument prices</value>
        [JsonProperty(PropertyName = "tick_size")]
        public decimal tickSize
        {
            get; set;
        }

        /// <summary>
        /// Contract size for instrument
        /// </summary>
        /// <value>Contract size for instrument</value>
        [JsonProperty(PropertyName = "contract_size")]
        public decimal contractSize
        {
            get; set;
        }

        /// <summary>
        /// The strike value. (only for options)
        /// </summary>
        /// <value>The strike value. (only for options)</value>
        [JsonProperty(PropertyName = "strike")]
        public decimal strike
        {
            get; set;
        }

        /// <summary>
        /// The time when the instrument was first created (milliseconds)
        /// </summary>
        /// <value>The time when the instrument was first created (milliseconds)</value>
        [JsonProperty(PropertyName = "creation_timestamp")]
        public long CreationTimestamp
        {
            get; set;
        }

        /// <summary>
        /// The time when the instrument will expire (milliseconds)
        /// </summary>
        /// <value>The time when the instrument will expire (milliseconds)</value>
        [JsonProperty(PropertyName = "expiration_timestamp")]
        public long ExpirationTimestamp
        {
            get; set;
        }

        /// <summary>
        /// The option type (only for options)
        /// </summary>
        /// <value>The option type (only for options)</value>
        [JsonProperty(PropertyName = "option_type")]
        public string optionType
        {
            get; set;
        }

        /// <summary>
        /// The settlement period.
        /// </summary>
        /// <value>The settlement period.</value>
        [JsonProperty(PropertyName = "settlement_period")]
        public string settlementPeriod
        {
            get; set;
        }
   }
}