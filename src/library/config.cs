using CCXT.NET.Configuration;
using Microsoft.Extensions.Configuration;
using System.IO;

namespace CCXT.Collector.Library
{
    /// <summary>
    ///
    /// </summary>
    public class KConfig
    {
        public static CConfig CConfig = new CConfig();

        public static string CollectorVersion
        {
            get
            {
                return CConfig.GetAppString("collector.version");
            }
        }

        public static bool BinanceUsePollingBookticker
        {
            get
            {
                return CConfig.GetAppBoolean("binance.use.polling.bookticker");
            }
        }

        private static long? __binance_polling_prev = null;

        public static long PollingPrevTime
        {
            get
            {
                if (__binance_polling_prev == null)
                    __binance_polling_prev = CConfig.GetAppInteger64("polling.bookticker.prev.millisconds");
                return __binance_polling_prev.Value;
            }
        }

        private static long? __binance_polling_term = null;

        public static long PollingTermTime
        {
            get
            {
                if (__binance_polling_term == null)
                    __binance_polling_term = CConfig.GetAppInteger64("polling.bookticker.term.millisconds");
                return __binance_polling_term.Value;
            }
        }

        public static bool UseAutoStart
        {
            get
            {
                return CConfig.GetAppBoolean("use.auto.start");
            }
        }

        private static string __name_start_exchange = null;

        public static string StartExchangeName
        {
            get
            {
                if (__name_start_exchange == null)
                    __name_start_exchange = CConfig.GetAppString("auto.start.exchange.name");
                return __name_start_exchange;
            }
        }

        private static string __name_start_symbol_names = null;

        public static string StartSymbolNames
        {
            get
            {
                if (__name_start_symbol_names == null)
                    __name_start_symbol_names = CConfig.GetAppString("auto.start.symbol.names");
                return __name_start_symbol_names;
            }
        }

        private static int? __binance_orderbook_counter = null;

        public static int BinanceOrderBookCounter
        {
            get
            {
                if (__binance_orderbook_counter == null)
                    __binance_orderbook_counter = CConfig.GetAppInteger("binance.orderbook.snapshot.counter");
                return __binance_orderbook_counter.Value;
            }
        }

        private static int? __binance_websocket_retry = null;

        public static int BinanceWebSocketRetry
        {
            get
            {
                if (__binance_websocket_retry == null)
                    __binance_websocket_retry = CConfig.GetAppInteger("websocket.retry.waiting.milliseconds");
                return __binance_websocket_retry.Value;
            }
        }

        private static int? __upbit_websocket_retry = null;

        public static int UpbitWebSocketRetry
        {
            get
            {
                if (__upbit_websocket_retry == null)
                    __upbit_websocket_retry = CConfig.GetAppInteger("websocket.retry.waiting.milliseconds");
                return __upbit_websocket_retry.Value;
            }
        }

        public static bool UpbitUsePollingBookticker
        {
            get
            {
                return CConfig.GetAppBoolean("upbit.use.polling.bookticker");
            }
        }

        private static int? __upbit_polling_sleep = null;

        public static int UpbitPollingSleep
        {
            get
            {
                if (__upbit_polling_sleep == null)
                    __upbit_polling_sleep = CConfig.GetAppInteger("upbit.polling.sleep.milliseconds");
                return __upbit_polling_sleep.Value;
            }
        }

        private static IConfigurationBuilder __config_builder = null;
        private static IConfigurationRoot __config_root = null;

        public static void SetConfigRoot()
        {
            __config_builder = new ConfigurationBuilder()
                            .SetBasePath(Directory.GetCurrentDirectory())
                            .AddJsonFile($"appsettings.json", true, true)
                            .AddEnvironmentVariables();

            __config_root = __config_builder.Build();

            CConfig.SetConfigRoot(__config_root);
        }
    }
}