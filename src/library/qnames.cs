using System;

namespace CCXT.Collector.Library
{
    /// <summary>
    ///
    /// </summary>
    public partial class FactoryQ
    {
        private static string __root_qname;

        public static string RootQName
        {
            get
            {
                if (String.IsNullOrEmpty(__root_qname) == true)
                    __root_qname = "ccxt";
                return __root_qname;
            }
            set
            {
                __root_qname = value;
            }
        }

        public static string SnapshotQName
        {
            get
            {
                return RootQName + "_snapshot_queue";
            }
        }

        public static string LoggerQName
        {
            get
            {
                return RootQName + "_logger_exchange";
            }
        }

        public static string OrderbookQName
        {
            get
            {
                return RootQName + "_orderbook_exchange";
            }
        }

        public static string BooktickerQName
        {
            get
            {
                return RootQName + "_bookticker_exchange";
            }
        }
    }
}