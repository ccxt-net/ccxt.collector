using CCXT.Collector.Service;

namespace CCXT.Collector.BitMEX
{
    /// <summary>
    /// bitmex
    /// </summary>
    public class BMLogger : CCLogger
    {
        public BMLogger() : base(BMConfig.DealerName)
        {
        }

        private static BMLogger _single_instance = null;

        public static BMLogger SNG
        {
            get
            {
                if (_single_instance == null)
                    _single_instance = new BMLogger();
                return _single_instance;
            }
        }
    }
}