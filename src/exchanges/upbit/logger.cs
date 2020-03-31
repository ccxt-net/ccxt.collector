using CCXT.Collector.Service;

namespace CCXT.Collector.Upbit
{
    /// <summary>
    /// upbit
    /// </summary>
    public class UPLogger : CCLogger
    {
        public UPLogger() : base(UPConfig.DealerName)
        {
        }

        private static UPLogger _single_instance = null;

        public static UPLogger SNG
        {
            get
            {
                if (_single_instance == null)
                    _single_instance = new UPLogger();
                return _single_instance;
            }
        }
    }
}