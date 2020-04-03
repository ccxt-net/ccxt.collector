using CCXT.Collector.Library;

namespace CCXT.Collector.Binance
{
    /// <summary>
    ///
    /// </summary>
    public class BNConfig : XConfig
    {
        public const string DealerName = "Binance";

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

        public string[] BinanceStartSymbolNames
        {
            get
            {
                return this.GetAppStringSectionName(DealerName, "auto.start.symbol.names").Split(';');
            }
        }

        private int? __binance_orderbook_counter = null;

        public int BinanceOrderBookCounter
        {
            get
            {
                if (__binance_orderbook_counter == null)
                    __binance_orderbook_counter = this.GetAppInteger(DealerName, "orderbook.snapshot.counter");
                return __binance_orderbook_counter.Value;
            }
        }

        private int? __binance_websocket_retry = null;

        public int BinanceWebSocketRetry
        {
            get
            {
                if (__binance_websocket_retry == null)
                    __binance_websocket_retry = this.GetAppInteger(DealerName, "websocket.retry.waiting.milliseconds");
                return __binance_websocket_retry.Value;
            }
        }

        #endregion Binance
    }
}