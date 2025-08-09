using System;
using System.Collections.Generic;

namespace CCXT.Collector.Bitget.Public
{
    public class Candle : WResult<List<string[]>>
    {
        public List<CandleData> Candles()
        {
            var _result = new List<CandleData>();

            foreach (var d in this.data)
                _result.Add(new CandleData(d));

            return _result;
        }
    }

    public class CandleData
    {
        public long ts { get; set; }
        public decimal o { get; set; }
        public decimal h { get; set; }
        public decimal l { get; set; }
        public decimal c { get; set; }
        public decimal v { get; set; }

        public CandleData(string[] data)
        {
            ts = Convert.ToInt64(data[0]);
            o = Convert.ToDecimal(data[1]);
            h = Convert.ToDecimal(data[2]);
            l = Convert.ToDecimal(data[3]);
            c = Convert.ToDecimal(data[4]);
            v = Convert.ToDecimal(data[5]);
        }
    }
}