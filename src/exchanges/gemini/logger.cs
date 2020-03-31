using CCXT.Collector.Service;

namespace CCXT.Collector.Gemini
{
    /// <summary>
    /// Gemini
    /// </summary>
    public class GMLogger : CCLogger
    {
        public GMLogger() : base(GMConfig.DealerName)
        {
        }

        private static GMLogger _single_instance = null;

        public static GMLogger SNG
        {
            get
            {
                if (_single_instance == null)
                    _single_instance = new GMLogger();
                return _single_instance;
            }
        }
    }
}