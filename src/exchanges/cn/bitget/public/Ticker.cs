using System;
using System.Collections.Generic;

namespace CCXT.Collector.Bitget.Public
{
    public class Ticker : WResult<List<TickerData>>
    {
    }

    public class TickerData
    {
        public string instId { get; set; }
        public decimal last { get; set; }
        public decimal open24h { get; set; }
        public decimal high24h { get; set; }
        public decimal low24h { get; set; }
        public decimal bestBid { get; set; }
        public decimal bestAsk { get; set; }
        public decimal baseVolume { get; set; }
        public decimal quoteVolume { get; set; }
        public long ts { get; set; }
        public int labeId { get; set; }
        public decimal openUtc { get; set; }
        public decimal chgUTC { get; set; }
        public decimal bidSz { get; set; }
        public decimal askSz { get; set; }
    }
}