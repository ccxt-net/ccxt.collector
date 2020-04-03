using CCXT.Collector.Library;

namespace CCXT.Collector.BitMEX
{
    /// <summary>
    ///
    /// </summary>
    public class BMConfig : XConfig
    {
        public const string DealerName = "bitmex";

        private static BMConfig _singleton = null;
        public static new BMConfig SNG
        {
            get
            {
                if (_singleton == null)
                    _singleton = new BMConfig();
                return _singleton;
            }
        }

        #region BitMEX

        public string[] BitMexStartSymbolNames
        {
            get
            {
                return this.GetAppSection(DealerName, "auto.start.symbol.names").Split(';');
            }
        }

        public bool BitMexUseLiveServer
        {
            get
            {
                return this.GetAppBoolean(DealerName, "use.live.server");
            }
        }

        public bool BitMexUseMyOrderStream
        {
            get
            {
                return this.GetAppBoolean(DealerName, "use.myorder.stream");
            }
        }

        public bool BitMexUsePollingOrderboook
        {
            get
            {
                return this.GetAppBoolean(DealerName, "use.polling.orderbook");
            }
        }

        public string BitMexConnectKey
        {
            get
            {
                return this.GetAppSection(DealerName, "private.connect.key");
            }
        }

        public string BitMexSecretKey
        {
            get
            {
                return this.GetAppSection(DealerName, "private.secret.key");
            }
        }

        public string BitMexUserName
        {
            get
            {
                return this.GetAppSection(DealerName, "private.user.name");
            }
        }

        private int? __bitmex_orderbook_counter = null;
        public int BitmexOrderBookCounter
        {
            get
            {
                if (__bitmex_orderbook_counter == null)
                    __bitmex_orderbook_counter = this.GetAppInteger(DealerName, "orderbook.snapshot.counter");
                return __bitmex_orderbook_counter.Value;
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

        #endregion BitMEX
    }
}