using CCXT.Collector.Service;

namespace CCXT.Collector.ItBit
{
    /// <summary>
    /// itbit
    /// </summary>
    public class IBLogger : CCLogger
    {
        public IBLogger() : base(IBConfig.DealerName)
        {
        }

        private static IBLogger _single_instance = null;

        public static IBLogger SNG
        {
            get
            {
                if (_single_instance == null)
                    _single_instance = new IBLogger();
                return _single_instance;
            }
        }
    }
}