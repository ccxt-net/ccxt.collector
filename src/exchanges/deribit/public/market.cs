using Newtonsoft.Json;
using OdinSdk.BaseLib.Coin.Public;
using System;

namespace CCXT.Collector.Deribit.Public
{
    /// <summary>
    ///
    /// </summary>
    public class DMarketItem : OdinSdk.BaseLib.Coin.Public.MarketItem, IMarketItem
    {
        /// <summary>
        /// Unique instrument identifier
        /// </summary>
        /// <value>Unique instrument identifier</value>
        [JsonProperty(PropertyName = "instrumentName")]
        public override string symbol
        {
            get; set;
        }

        /// <summary>
        /// The underlying currency being traded.
        /// </summary>
        /// <value>The underlying currency being traded.</value>
        [JsonProperty(PropertyName = "baseCurrency")]
        public override string baseName
        {
            get; set;
        }

        /// <summary>
        /// The currency in which the instrument prices are quoted.
        /// </summary>
        /// <value>The currency in which the instrument prices are quoted.</value>
        [JsonProperty(PropertyName = "currency")]
        public override string quoteName
        {
            get; set;
        }

        /// <summary>
        /// Indicates if the instrument can currently be traded.
        /// </summary>
        /// <value>Indicates if the instrument can currently be traded.</value>
        [JsonProperty(PropertyName = "isActive")]
        public override bool active
        {
            get; set;
        }

        /// <summary>
        /// specifies minimal price change and, as follows, the number of decimal places for instrument prices
        /// </summary>
        /// <value>specifies minimal price change and, as follows, the number of decimal places for instrument prices</value>
        [JsonProperty(PropertyName = "tickSize")]
        public decimal tickSize
        {
            get; set;
        }

        /// <summary>
        /// Contract size for instrument
        /// </summary>
        /// <value>Contract size for instrument</value>
        [JsonProperty(PropertyName = "minTradeSize")]
        public decimal minTradeSize
        {
            get; set;
        }

        /// <summary>
        /// Minimum amount for trading. For perpetual and futures - in USD units, for options it is amount of corresponding cryptocurrency contracts, e.g., BTC or ETH.
        /// </summary>
        /// <value>Minimum amount for trading. For perpetual and futures - in USD units, for options it is amount of corresponding cryptocurrency contracts, e.g., BTC or ETH.</value>
        [JsonProperty(PropertyName = "minTradeAmount")]
        public decimal minTradeAmount
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
        [JsonProperty(PropertyName = "created")]
        public DateTime created
        {
            get; set;
        }

        /// <summary>
        /// The time when the instrument will expire (milliseconds)
        /// </summary>
        /// <value>The time when the instrument will expire (milliseconds)</value>
        [JsonProperty(PropertyName = "expiration")]
        public DateTime expiration
        {
            get; set;
        }

        /// <summary>
        /// The settlement period.
        /// </summary>
        /// <value>The settlement period.</value>
        [JsonProperty(PropertyName = "settlement")]
        public string settlement
        {
            get; set;
        }

        /// <summary>
        /// The option type (only for options)
        /// </summary>
        /// <value>The option type (only for options)</value>
        [JsonProperty(PropertyName = "optionType")]
        public string optionType
        {
            get; set;
        }

        /// <summary>
        /// Instrument kind, "future" or "option"
        /// </summary>
        /// <value>Instrument kind, "future" or "option"</value>
        [JsonProperty(PropertyName = "kind")]
        public string kind
        {
            get; set;
        }

        public int pricePrecision
        {
            get; set;
        }
    }
}