using CCXT.Collector.Service;

namespace CCXT.Collector.KebHana.Public
{
    /// <summary>
    /// kebhana
    /// </summary>
    public class KELogger : CCLogger
    {
        public KELogger() : base("kebhana")
        {
        }

        private static KELogger _single_instance = null;

        public static KELogger SNG
        {
            get
            {
                if (_single_instance == null)
                    _single_instance = new KELogger();
                return _single_instance;
            }
        }
    }
}