using System.Collections.Generic;

namespace CCXT.Collector.Crypto
{
    public class WsTickerHeader
    {
        public long id { get; set; }
        public int code { get; set; }
        public string method { get; set; }
    }

    public class WsTicker
    {
        public decimal h { get; set; }
        public decimal l { get; set; }
        public decimal a { get; set; }
        public decimal c { get; set; }
        public decimal b { get; set; }
        public decimal bs { get; set; }
        public decimal k { get; set; }
        public decimal ks { get; set; }
        public string i { get; set; }
        public decimal v { get; set; }
        public decimal vv { get; set; }
        public decimal oi { get; set; }
        public long t { get; set; }
    }

    public class WsTickerResult
    {
        public string channel { get; set; }
        public string instrument_name { get; set; }
        public string subscription { get; set; }
        public int id { get; set; }
        public List<WsTicker> data { get; set; }
    }

    public class WsTickers : WsTickerHeader
    {
        public WsTickerResult result { get; set; }
    }
}