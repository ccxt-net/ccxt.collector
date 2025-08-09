using System;
using System.Collections.Generic;

namespace CCXT.Collector.Bitget.Public
{
    public class Depth : WResult<List<DepthData>>
    {
    }

    public class DepthData
    {
        public List<decimal[]> asks { get; set; }
        public List<decimal[]> bids { get; set; }
        public long ts { get; set; }
    }
}