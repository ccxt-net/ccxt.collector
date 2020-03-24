using CCXT.Collector.Library;

namespace CCXT.Collector.Gemini
{
    /// <summary>
    ///
    /// </summary>
    public class GMConfig : XConfig
    {
        private static GMConfig _singleton = null;
        public static new GMConfig SNG
        {
            get
            {
                if (_singleton == null)
                    _singleton = new GMConfig();
                return _singleton;
            }
        }

        private int? __websocket_retry = null;

        public int WebSocketRetry
        {
            get
            {
                if (__websocket_retry == null)
                    __websocket_retry = this.GetAppInteger("gemini.websocket.retry.waiting.milliseconds");
                return __websocket_retry.Value;
            }
        }

        private int? __polling_sleep = null;

        public int PollingSleep
        {
            get
            {
                if (__polling_sleep == null)
                    __polling_sleep = this.GetAppInteger("gemini.polling.sleep.milliseconds");
                return __polling_sleep.Value;
            }
        }
    }
}