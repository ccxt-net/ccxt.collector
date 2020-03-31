using CCXT.Collector.Service;

namespace CCXT.Collector.Bithumb
{
    /// <summary>
    /// Bithumb
    /// </summary>
    public class BTLogger : CCLogger
    {
        public BTLogger() : base(BTConfig.DealerName)
        {
        }

        private static BTLogger _single_instance = null;

        public static BTLogger SNG
        {
            get
            {
                if (_single_instance == null)
                    _single_instance = new BTLogger();
                return _single_instance;
            }
        }
    }
}