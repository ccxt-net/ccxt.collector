using CCXT.Collector.Library;

namespace CCXT.Collector.BitMEX
{
    /// <summary>
    ///
    /// </summary>
    public class BMConfig : XConfig
    {
        public const string DealerName = "BitMEX";

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

        public bool BitMexUseLiveServer
        {
            get
            {
                return this.GetAppBoolean("bitmex.use.live.server");
            }
        }

        public bool BitMexUseMyOrderStream
        {
            get
            {
                return this.GetAppBoolean("bitmex.use.myorder.stream");
            }
        }

        public bool BitMexUsePollingOrderboook
        {
            get
            {
                return this.GetAppBoolean("bitmex.use.polling.orderbook");
            }
        }

        public string BitMexConnectKey
        {
            get
            {
                return this.GetAppString("bitmex.private.connect.key");
            }
        }

        public string BitMexSecretKey
        {
            get
            {
                return this.GetAppString("bitmex.private.secret.key");
            }
        }

        public string BitMexUserName
        {
            get
            {
                return this.GetAppString("bitmex.private.user.name");
            }
        }

        private int? __bitmex_orderbook_counter = null;
        public int BitmexOrderBookCounter
        {
            get
            {
                if (__bitmex_orderbook_counter == null)
                    __bitmex_orderbook_counter = this.GetAppInteger("bitmex.orderbook.snapshot.counter");
                return __bitmex_orderbook_counter.Value;
            }
        }

        private int? __websocket_retry = null;
        public int WebSocketRetry
        {
            get
            {
                if (__websocket_retry == null)
                    __websocket_retry = this.GetAppInteger("bitmex.websocket.retry.waiting.milliseconds");
                return __websocket_retry.Value;
            }
        }

        private int? __polling_sleep = null;

        public int PollingSleep
        {
            get
            {
                if (__polling_sleep == null)
                    __polling_sleep = this.GetAppInteger("bitmex.polling.sleep.milliseconds");
                return __polling_sleep.Value;
            }
        }

        #endregion BitMEX
    }
}