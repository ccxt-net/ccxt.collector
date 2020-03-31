using CCXT.Collector.Service;

namespace CCXT.Collector.Binance
{
    /// <summary>
    /// binance
    /// </summary>
    public class BNLogger : CCLogger
    {
        public BNLogger() : base(BNConfig.DealerName)
        {
        }

        private static BNLogger _single_instance = null;

        public static BNLogger SNG
        {
            get
            {
                if (_single_instance == null)
                    _single_instance = new BNLogger();
                return _single_instance;
            }
        }
    }
}