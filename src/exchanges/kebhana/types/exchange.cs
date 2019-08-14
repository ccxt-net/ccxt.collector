using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace CCXT.Collector.KebHana.Types
{
    /// <summary>
    /// 
    /// </summary>
    public class KebExchange
    {
        /// <summary>
        /// 날짜
        /// </summary>
        [JsonProperty(propertyName: "날짜")]
        public DateTime timestamp
        {
            get;
            set;
        }

        /// <summary>
        /// 
        /// </summary>
        public long sequential_id
        {
            get;
            set;
        }

        /// <summary>
        /// 리스트
        /// </summary>
        [JsonProperty(propertyName: "리스트")]
        public List<KebExchangeItem> data
        {
            get;
            set;
        }

    }

    /// <summary>
    /// 
    /// </summary>
    public class KebExchangeItem
    {
        /// <summary>
        /// KRWUSD
        /// </summary>
        public string code
        {
            get;
            set;
        }
        
        /// <summary>
        /// 통화명 (미국 USD)
        /// </summary>
        [JsonProperty("통화명")]
        public string name
        {
            get;
            set;
        }

        /// <summary>
        /// 미국
        /// </summary>
        public string country
        {
            get;
            set;
        }

        /// <summary>
        /// 현찰사실때
        /// </summary>
        [JsonProperty(propertyName: "현찰사실때")]
        public decimal cashBuyingPrice
        {
            get;
            set;
        }


        /// <summary>
        /// 현찰파실떄
        /// </summary>
        [JsonProperty(propertyName: "현찰파실떄")]
        public decimal cashSellingPrice
        {
            get;
            set;
        }


        /// <summary>
        /// 송금_전신환보내실떄
        /// </summary>
        [JsonProperty(propertyName: "송금_전신환보내실떄")]
        public decimal ttSellingPrice
        {
            get;
            set;
        }


        /// <summary>
        /// 송금_전신환받으실떄
        /// </summary>
        [JsonProperty(propertyName: "송금_전신환받으실떄")]
        public decimal ttBuyingPrice
        {
            get;
            set;
        }


        /// <summary>
        /// 매매기준율
        /// </summary>
        [JsonProperty(propertyName: "매매기준율")]
        public decimal basePrice
        {
            get;
            set;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="통화명"></param>
        /// <param name="현찰사실때"></param>
        /// <param name="현찰파실때"></param>
        /// <param name="송금_전신환보내실때"></param>
        /// <param name="송금_전신환받으실때"></param>
        /// <param name="매매기준율"></param>
        [JsonConstructor]
        public KebExchangeItem(string 통화명, string 현찰사실때, string 현찰파실때, string 송금_전신환보내실때, string 송금_전신환받으실때, string 매매기준율)
        {
            this.name = 통화명;
            this.code = "KRW" + this.name.Split(' ')[1];
            this.country = this.name.Split(' ')[0];

            this.cashBuyingPrice = decimal.Parse(현찰사실때);
            this.cashSellingPrice = decimal.Parse(현찰파실때);
            this.ttSellingPrice = decimal.Parse(송금_전신환보내실때);
            this.ttBuyingPrice = decimal.Parse(송금_전신환받으실때);

            this.basePrice = decimal.Parse(매매기준율);
        }
    }
}