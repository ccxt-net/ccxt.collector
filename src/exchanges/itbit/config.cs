using CCXT.Collector.Library;

namespace CCXT.Collector.ItBit
{
    /// <summary>
    ///
    /// </summary>
    public class IBConfig : XConfig
    {
        public const string DealerName = "itbit";

        private static IBConfig _singleton = null;
        public static new IBConfig SNG
        {
            get
            {
                if (_singleton == null)
                    _singleton = new IBConfig();
                return _singleton;
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
    }
}