using CCXT.Collector.Library;

namespace CCXT.Collector.Binance
{
    /// <summary>
    ///
    /// </summary>
    public class BNConfig : XConfig
    {
        public const string DealerName = "binance";

        private static BNConfig _singleton = null;
        public static new BNConfig SNG
        {
            get
            {
                if (_singleton == null)
                    _singleton = new BNConfig();
                return _singleton;
            }
        }

        #region Binance

        public string[] StartSymbolNames
        {
            get
            {
                return this.GetAppSection(DealerName, "auto.start.symbol.names").Split(';');
            }
        }

        private int? __orderbook_counter = null;

        public int OrderBookCounter
        {
            get
            {
                if (__orderbook_counter == null)
                    __orderbook_counter = this.GetAppInteger(DealerName, "orderbook.snapshot.counter");
                return __orderbook_counter.Value;
            }
        }

        private int? __websocket_retry = null;

        public int WebSocketRetry
        {
            get
            {
                if (__websocket_retry == null)
                    __websocket_retry = this.GetAppInteger(DealerName, "websocket.retry.waiting.milliseconds");
                return __websocket_retry.Value;
            }
        }

        #endregion Binance
    }
}