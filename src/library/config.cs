﻿using OdinSdk.BaseLib.Configuration;
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


        private static bool? __use_polling_ticker = null;

        public static bool UsePollingTicker
        {
            get
            {
                if (__use_polling_ticker == null)
                    __use_polling_ticker = CConfig.GetAppBoolean("use.polling.ticker");
                return __use_polling_ticker.Value;
            }
        }

        private static bool? __use_publish_trade = null;

        public static bool UsePublishTrade
        {
            get
            {
                if (__use_publish_trade == null)
                    __use_publish_trade = CConfig.GetAppBoolean("use.publish.trade");
                return __use_publish_trade.Value;
            }
        }

        private static int? __snapshot_skip_counter = null;

        public static int SnapshotSkipCounter
        {
            get
            {
                if (__snapshot_skip_counter == null)
                    __snapshot_skip_counter = CConfig.GetAppInteger("snapshot.skip.counter");
                return __snapshot_skip_counter.Value;
            }
        }

        #region Binance

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

        #endregion Binance

        #region BitMEX

        private static int? __bitmex_orderbook_counter = null;


        public static int BitmexOrderBookCounter
        {
            get
            {
                if (__bitmex_orderbook_counter == null)
                    __bitmex_orderbook_counter = CConfig.GetAppInteger("bitmex.orderbook.snapshot.counter");
                return __bitmex_orderbook_counter.Value;
            }
        }

        private static int? __bitmex_websocket_retry = null;

        public static int BitmexWebSocketRetry
        {
            get
            {
                if (__bitmex_websocket_retry == null)
                    __bitmex_websocket_retry = CConfig.GetAppInteger("websocket.retry.waiting.milliseconds");
                return __bitmex_websocket_retry.Value;
            }
        }

        #endregion BitMEX

        #region Upbit

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

        #endregion Upbit

        #region Common

        public static string CollectorVersion
        {
            get
            {
                return CConfig.GetAppString("collector.version");
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

        private static long? __polling_prev_time = null;

        public static long PollingPrevTime
        {
            get
            {
                if (__polling_prev_time == null)
                    __polling_prev_time = CConfig.GetAppInteger64("polling.ticker.prev.millisconds");
                return __polling_prev_time.Value;
            }
        }

        private static long? __polling_term_time = null;

        public static long PollingTermTime
        {
            get
            {
                if (__polling_term_time == null)
                    __polling_term_time = CConfig.GetAppInteger64("polling.ticker.term.millisconds");
                return __polling_term_time.Value;
            }
        }

        #endregion Common

        #region Arbitrage

        public static bool UsePollingArbitrage
        {
            get
            {
                return CConfig.GetAppBoolean("use.polling.arbitrage");
            }
        }

        public static string ArbitrageBaseNames
        {
            get
            {
                return CConfig.GetAppString("arbitrage.base.names");
            }
        }

        public static string ArbitrageQuoteNames
        {
            get
            {
                return CConfig.GetAppString("arbitrage.quote.names");
            }
        }

        #endregion Arbitrage

        #region Creator

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

        #endregion Creator
    }
}