using System;
using System.Collections.Generic;

namespace CCXT.Collector.Bitget.Public
{
    public class Trade : WResult<List<string[]>>
    {
        public List<TradeData> Trades()
        {
            var _result = new List<TradeData>();

            foreach (var d in this.data)
                _result.Add(new TradeData(d));

            return _result;
        }
    }

    public class TradeData
    {
        public long ts { get; set; }
        public decimal px { get; set; }
        public decimal sz { get; set; }
        public string side { get; set; }

        public TradeData(string[] data)
        {
            ts = Convert.ToInt64(data[0]);
            px = Convert.ToDecimal(data[1]);
            sz = Convert.ToDecimal(data[2]);
            side = data[3];
        }
    }
}