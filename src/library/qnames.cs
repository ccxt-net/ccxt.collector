namespace CCXT.Collector.Library
{
    /// <summary>
    ///
    /// </summary>
    public partial class FactoryQ
    {
        public static string RootQName
        {
            get;
            set;
        }

        public static string SnapshotQName = RootQName + "_snapshot_queue";
        public static string LoggerQName = RootQName + "_logger_exchange";
        public static string OrderbookQName = RootQName + "_orderbook_exchange";
        public static string BooktickerQName = RootQName + "_bookticker_exchange";
    }
}