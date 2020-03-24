using OdinSdk.BaseLib.Configuration;
using System.Collections.Generic;

namespace CCXT.Collector.Library
{
    /// <summary>
    ///
    /// </summary>
    public class XConfig : CConfig
    {
        private static XConfig _singleton = null;
        public static XConfig SNG
        {
            get
            {
                if (_singleton == null)
                    _singleton = new XConfig();
                return _singleton;
            }
        }

        private bool? __use_polling_ticker = null;

        public bool UsePollingTicker
        {
            get
            {
                if (__use_polling_ticker == null)
                    __use_polling_ticker = this.GetAppBoolean("use.polling.ticker");
                return __use_polling_ticker.Value;
            }
        }

        private bool? __use_publish_trade = null;

        public bool UsePublishTrade
        {
            get
            {
                if (__use_publish_trade == null)
                    __use_publish_trade = this.GetAppBoolean("use.publish.trade");
                return __use_publish_trade.Value;
            }
        }

        private int? __snapshot_skip_counter = null;

        public int SnapshotSkipCounter
        {
            get
            {
                if (__snapshot_skip_counter == null)
                    __snapshot_skip_counter = this.GetAppInteger("snapshot.skip.counter");
                return __snapshot_skip_counter.Value;
            }
        }

        #region Common

        public string CollectorVersion
        {
            get
            {
                return this.GetAppString("collector.version");
            }
        }

        public bool UseAutoStart
        {
            get
            {
                return this.GetAppBoolean("collector.auto.start");
            }
        }

        private string? __name_start_exchange = null;

        public string StartExchangeNames
        {
            get
            {
                if (__name_start_exchange == null)
                    __name_start_exchange = this.GetAppString("auto.start.exchange.names");
                return __name_start_exchange;
            }
        }

        private Dictionary<string, string> __name_start_symbol_names = null;

        public string GetStartSymbolNames(string exchange)
        {
            var _result = "";

            if (__name_start_symbol_names == null)
                __name_start_symbol_names = new Dictionary<string, string>();

            if (__name_start_symbol_names.ContainsKey(exchange) == false)
            {
                _result = this.GetAppString($"{exchange}.auto.start.symbol.names");
                __name_start_symbol_names.Add(exchange, _result);
            }
            else
            {
                _result = __name_start_symbol_names[exchange];
            }

            return _result;
        }

        private long? __polling_prev_time = null;

        public long PollingPrevTime
        {
            get
            {
                if (__polling_prev_time == null)
                    __polling_prev_time = this.GetAppInteger64("polling.ticker.prev.millisconds");
                return __polling_prev_time.Value;
            }
        }

        private long? __polling_term_time = null;

        public long PollingTermTime
        {
            get
            {
                if (__polling_term_time == null)
                    __polling_term_time = this.GetAppInteger64("polling.ticker.term.millisconds");
                return __polling_term_time.Value;
            }
        }

        #endregion Common

        #region Arbitrage

        public bool UsePollingArbitrage
        {
            get
            {
                return this.GetAppBoolean("use.polling.arbitrage");
            }
        }

        public string ArbitrageBaseNames
        {
            get
            {
                return this.GetAppString("arbitrage.base.names");
            }
        }

        public string ArbitrageQuoteNames
        {
            get
            {
                return this.GetAppString("arbitrage.quote.names");
            }
        }

        #endregion Arbitrage
    }
}