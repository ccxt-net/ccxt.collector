using CCXT.Collector.Service;

namespace CCXT.Collector.Deribit
{
    /// <summary>
    /// deribit
    /// </summary>
    public class BMLogger : CCLogger
    {
        public BMLogger() : base(DRConfig.DealerName)
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