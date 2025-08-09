using System.Collections.Generic;

namespace CCXT.Collector.Service
{
    /// <summary>
    /// Orders container for batch processing (주문 목록 컨테이너)
    /// </summary>
    public class SOrder
    {
        /// <summary>
        /// Exchange name
        /// </summary>
        public string exchange { get; set; }

        /// <summary>
        /// Timestamp when data was received
        /// </summary>
        public long timestamp { get; set; }

        /// <summary>
        /// List of orders
        /// </summary>
        public List<SOrderItem> orders { get; set; }

        public SOrder()
        {
            orders = new List<SOrderItem>();
        }
    }

    /// <summary>
    /// Positions container for batch processing (포지션 목록 컨테이너)
    /// </summary>
    public class SPosition
    {
        /// <summary>
        /// Exchange name
        /// </summary>
        public string exchange { get; set; }

        /// <summary>
        /// Timestamp when data was received
        /// </summary>
        public long timestamp { get; set; }

        /// <summary>
        /// List of positions
        /// </summary>
        public List<SPositionItem> positions { get; set; }

        public SPosition()
        {
            positions = new List<SPositionItem>();
        }
    }
}