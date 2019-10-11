namespace CCXT.Collector.BitMEX.Types
{
    /// <summary>
    ///
    /// </summary>
    public class BTradeItem
    {
        /// <summary>
        /// Symbol
        /// </summary>
        public virtual string symbol
        {
            get;
            set;
        }

        /// <summary>
        /// Aggregate tradeId
        /// </summary>
        public virtual long tradeId
        {
            get;
            set;
        }

        /// <summary>
        /// Price
        /// </summary>
        public virtual decimal price
        {
            get;
            set;
        }

        /// <summary>
        /// Quantity
        /// </summary>
        public virtual decimal quantity
        {
            get;
            set;
        }

        /// <summary>
        /// First tradeId
        /// </summary>
        public virtual long firstTradeId
        {
            get;
            set;
        }

        /// <summary>
        /// Last tradeId
        /// </summary>
        public virtual long lastTradeId
        {
            get;
            set;
        }

        /// <summary>
        /// Timestamp
        /// </summary>
        public virtual long timestamp
        {
            get;
            set;
        }

        /// <summary>
        /// Was the buyer the maker?
        /// </summary>
        public virtual bool isBuyerMaker
        {
            get;
            set;
        }

        /// <summary>
        /// Was the trade the best price match?
        /// </summary>
        public virtual bool isBestMatch
        {
            get;
            set;
        }
    }
}