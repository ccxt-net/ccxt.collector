namespace CCXT.Collector.Bithumb
{
    public class WsOrderbookItem
    {
        /// <summary>
        /// coin symbol
        /// </summary>
        public string symbol
        {
            get;
            set;
        }

        /// <summary>
        /// 
        /// </summary>
        public string orderType
        {
            get;
            set;
        }

        /// <summary>
        /// 
        /// </summary>
        public decimal price
        {
            get;
            set;
        }

        /// <summary>
        /// 
        /// </summary>
        public decimal quantity
        {
            get;
            set;
        }

        /// <summary>
        /// 
        /// </summary>
        public int total
        {
            get;
            set;
        }
    }
}