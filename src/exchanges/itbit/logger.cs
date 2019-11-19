using CCXT.Collector.Service;

namespace CCXT.Collector.ItBit
{
    /// <summary>
    /// itbit
    /// </summary>
    public class IBLogger
    {
        public const string exchange_name = "itbit";

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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        public static void WriteC(string message)
        {
            LoggerQ.WriteC(message, exchange_name);
        }
    }
}