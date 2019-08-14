using CCXT.Collector.Library.Service;

namespace CCXT.Collector.KebHana.Collector
{
    /// <summary>
    /// kebhana
    /// </summary>
    public class KELogger
    {
        public const string exchange_name = "kebhana";

        /// <summary>
        ///
        /// </summary>
        /// <param name="message"></param>
        public static void WriteQ(string message)
        {
            LoggerQ.WriteQ(message, exchange_name);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="message"></param>
        public static void WriteO(string message)
        {
            LoggerQ.WriteO(message, exchange_name);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="message"></param>
        public static void WriteX(string message)
        {
            LoggerQ.WriteX(message, exchange_name);
        }
    }
}