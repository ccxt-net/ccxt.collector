using CCXT.Collector.Library;

namespace CCXT.Collector.Deribit
{
    /// <summary>
    ///
    /// </summary>
    public class DRConfig : XConfig
    {
        public const string DealerName = "deribit";

        private static DRConfig _singleton = null;
        public static new DRConfig SNG
        {
            get
            {
                if (_singleton == null)
                    _singleton = new DRConfig();
                return _singleton;
            }
        }

        #region Deribit

        public string[] StartSymbolNames
        {
            get
            {
                return this.GetAppSection(DealerName, "auto.start.symbol.names").Split(';');
            }
        }

        public bool UseLiveServer
        {
            get
            {
                return this.GetAppBoolean(DealerName, "use.live.server");
            }
        }

        public bool UseMyOrderStream
        {
            get
            {
                return this.GetAppBoolean(DealerName, "use.myorder.stream");
            }
        }

        public bool UsePollingOrderboook
        {
            get
            {
                return this.GetAppBoolean(DealerName, "use.polling.orderbook");
            }
        }

        public string ConnectKey
        {
            get
            {
                return this.GetAppSection(DealerName, "private.connect.key");
            }
        }

        public string SecretKey
        {
            get
            {
                return this.GetAppSection(DealerName, "private.secret.key");
            }
        }

        public string LoginName
        {
            get
            {
                return this.GetAppSection(DealerName, "private.user.name");
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

        private int? __polling_sleep = null;

        public int PollingSleep
        {
            get
            {
                if (__polling_sleep == null)
                    __polling_sleep = this.GetAppInteger(DealerName, "polling.sleep.milliseconds");
                return __polling_sleep.Value;
            }
        }

        #endregion Deribit
    }
}