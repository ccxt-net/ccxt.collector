using System;
using System.Collections.Generic;

namespace CCXT.Collector.Upbit.Public
{
    /// <summary>
    ///
    /// </summary>
    public class UExchange
    {
        /// <summary>
        ///
        /// </summary>
        public long sequentialId
        {
            get;
            set;
        }

        /// <summary>
        ///
        /// </summary>
        public List<UExchangeItem> data
        {
            get;
            set;
        }
    }

    /// <summary>
    ///
    /// </summary>
    public class UExchangeItem
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
        /// USD
        /// </summary>
        public string currencyCode
        {
            get;
            set;
        }

        /// <summary>
        /// 달러
        /// </summary>
        public string currencyName
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
        /// 미국(KRW/USD)
        /// </summary>
        public string name
        {
            get;
            set;
        }

        /// <summary>
        ///
        /// </summary>
        public string date
        {
            get;
            set;
        }

        /// <summary>
        ///
        /// </summary>
        public string time
        {
            get;
            set;
        }

        /// <summary>
        ///
        /// </summary>
        public int recurrenceCount
        {
            get;
            set;
        }

        /// <summary>
        /// 매매기준율
        /// </summary>
        public decimal basePrice
        {
            get;
            set;
        }

        /// <summary>
        ///
        /// </summary>
        public decimal openingPrice
        {
            get;
            set;
        }

        /// <summary>
        ///
        /// </summary>
        public decimal highPrice
        {
            get;
            set;
        }

        /// <summary>
        ///
        /// </summary>
        public decimal lowPrice
        {
            get;
            set;
        }

        /// <summary>
        ///
        /// </summary>
        public string change
        {
            get;
            set;
        }

        /// <summary>
        ///
        /// </summary>
        public decimal changePrice
        {
            get;
            set;
        }

        /// <summary>
        /// 현찰사실때
        /// </summary>
        public decimal cashBuyingPrice
        {
            get;
            set;
        }

        /// <summary>
        /// 현찰파실떄
        /// </summary>
        public decimal cashSellingPrice
        {
            get;
            set;
        }

        /// <summary>
        /// 송금_전신환받으실떄
        /// </summary>
        public decimal ttBuyingPrice
        {
            get;
            set;
        }

        /// <summary>
        /// 송금_전신환보내실떄
        /// </summary>
        public decimal ttSellingPrice
        {
            get;
            set;
        }

        /// <summary>
        ///
        /// </summary>
        public decimal tcBuyingPrice
        {
            get;
            set;
        }

        /// <summary>
        ///
        /// </summary>
        public decimal fcSellingPrice
        {
            get;
            set;
        }

        /// <summary>
        ///
        /// </summary>
        public decimal exchangeCommission
        {
            get;
            set;
        }

        /// <summary>
        ///
        /// </summary>
        public decimal usDollarRate
        {
            get;
            set;
        }

        /// <summary>
        ///
        /// </summary>
        public decimal high52wPrice
        {
            get;
            set;
        }

        /// <summary>
        ///
        /// </summary>
        public string high52wDate
        {
            get;
            set;
        }

        /// <summary>
        ///
        /// </summary>
        public decimal low52wPrice
        {
            get;
            set;
        }

        /// <summary>
        ///
        /// </summary>
        public string low52wDate
        {
            get;
            set;
        }

        /// <summary>
        ///
        /// </summary>
        public decimal currencyUnit
        {
            get;
            set;
        }

        /// <summary>
        ///
        /// </summary>
        public string provider
        {
            get;
            set;
        }

        /// <summary>
        ///
        /// </summary>
        public long timestamp
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
        public DateTime createdAt
        {
            get;
            set;
        }

        /// <summary>
        ///
        /// </summary>
        public DateTime modifiedAt
        {
            get;
            set;
        }

        /// <summary>
        ///
        /// </summary>
        public decimal changeRate
        {
            get;
            set;
        }

        /// <summary>
        ///
        /// </summary>
        public decimal signedChangePrice
        {
            get;
            set;
        }

        /// <summary>
        ///
        /// </summary>
        public decimal signedChangeRate
        {
            get;
            set;
        }
    }
}