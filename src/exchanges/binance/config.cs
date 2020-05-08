using CCXT.Collector.Library;
using Microsoft.Extensions.Configuration;

namespace CCXT.Collector.Binance
{
    /// <summary>
    ///
    /// </summary>
    public class BNConfig : XConfig
    {
        public const string DealerName = "binance";

        public BNConfig(IConfiguration configuration)
            : base(configuration)
        {
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