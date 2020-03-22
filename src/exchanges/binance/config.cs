using CCXT.Collector.Library;

namespace CCXT.Collector.Binance
{
    /// <summary>
    ///
    /// </summary>
    public class BNConfig : XConfig
    {
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

        private int? __binance_orderbook_counter = null;

        public int BinanceOrderBookCounter
        {
            get
            {
                if (__binance_orderbook_counter == null)
                    __binance_orderbook_counter = this.GetAppInteger("binance.orderbook.snapshot.counter");
                return __binance_orderbook_counter.Value;
            }
        }

        private int? __binance_websocket_retry = null;

        public int BinanceWebSocketRetry
        {
            get
            {
                if (__binance_websocket_retry == null)
                    __binance_websocket_retry = this.GetAppInteger("binance.websocket.retry.waiting.milliseconds");
                return __binance_websocket_retry.Value;
            }
        }

        #endregion Binance
    }
}