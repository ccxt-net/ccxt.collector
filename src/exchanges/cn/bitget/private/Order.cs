using System.Collections.Generic;

namespace CCXT.Collector.Bitget.Private
{
    public class Order : WResult<List<OrderData>>
    {
    }

    public class OrderData
    {
        public string instId { get; set; }
        public string ordId { get; set; }
        public string clOrdId { get; set; }
        public decimal px { get; set; }
        public decimal sz { get; set; }
        public decimal notional { get; set; }
        public string ordType { get; set; }
        public string force { get; set; }
        public string side { get; set; }
        public decimal fillPx { get; set; }
        public string tradeId { get; set; }
        public decimal fillSz { get; set; }
        public long fillTime { get; set; }
        public decimal fillFee { get; set; }
        public string fillFeeCcy { get; set; }
        public string execType { get; set; }
        public decimal accFillSz { get; set; }
        public decimal avgPx { get; set; }
        public string status { get; set; }
        public long cTime { get; set; }
        public long uTime { get; set; }
        public List<OrderFee> orderFee { get; set; }
    }

    public class OrderFee
    {
        public string feeCcy { get; set; }
        public decimal fee { get; set; }
    }
}