using CCXT.Collector.Service;

namespace CCXT.Collector.Deribit
{
    /// <summary>
    /// deribit
    /// </summary>
    public class DRLogger : CCLogger
    {
        public DRLogger() : base(DRConfig.DealerName)
        {
        }

        private static DRLogger _single_instance = null;

        public static DRLogger SNG
        {
            get
            {
                if (_single_instance == null)
                    _single_instance = new DRLogger();
                return _single_instance;
            }
        }
    }
}