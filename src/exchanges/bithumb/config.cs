using CCXT.Collector.Library;
using Microsoft.Extensions.Configuration;

namespace CCXT.Collector.Bithumb
{
    /// <summary>
    ///
    /// </summary>
    public class BTConfig : XConfig
    {
        public const string DealerName = "bithumb";

        public BTConfig(IConfiguration configuration)
              : base(configuration)
        {
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