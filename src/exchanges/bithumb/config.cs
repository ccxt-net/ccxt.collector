using CCXT.Collector.Library;

namespace CCXT.Collector.Bithumb
{
    /// <summary>
    ///
    /// </summary>
    public class BTConfig : XConfig
    {
        public const string DealerName = "Bithumb";

        private static BTConfig _singleton = null;
        public static new BTConfig SNG
        {
            get
            {
                if (_singleton == null)
                    _singleton = new BTConfig();
                return _singleton;
            }
        }
        
        private int? __websocket_retry = null;
        public int WebSocketRetry
        {
            get
            {
                if (__websocket_retry == null)
                    __websocket_retry = this.GetAppInteger("bithumb.websocket.retry.waiting.milliseconds");
                return __websocket_retry.Value;
            }
        }

        private int? __polling_sleep = null;

        public int PollingSleep
        {
            get
            {
                if (__polling_sleep == null)
                    __polling_sleep = this.GetAppInteger("bithumb.polling.sleep.milliseconds");
                return __polling_sleep.Value;
            }
        }
    }
}