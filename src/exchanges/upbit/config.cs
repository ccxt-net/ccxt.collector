using CCXT.Collector.Library;

namespace CCXT.Collector.Upbit
{
    /// <summary>
    ///
    /// </summary>
    public class UPConfig : XConfig
    {
        public const string DealerName = "Upbit";

        private static UPConfig _singleton = null;
        public static new UPConfig SNG
        {
            get
            {
                if (_singleton == null)
                    _singleton = new UPConfig();
                return _singleton;
            }
        }

        #region Upbit

        private int? __websocket_retry = null;

        public int WebSocketRetry
        {
            get
            {
                if (__websocket_retry == null)
                    __websocket_retry = this.GetAppInteger("upbit.websocket.retry.waiting.milliseconds");
                return __websocket_retry.Value;
            }
        }

        private int? __polling_sleep = null;

        public int PollingSleep
        {
            get
            {
                if (__polling_sleep == null)
                    __polling_sleep = this.GetAppInteger("upbit.polling.sleep.milliseconds");
                return __polling_sleep.Value;
            }
        }

        #endregion Upbit
    }
}