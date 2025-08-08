using CCXT.NET.Shared.Configuration;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Runtime.InteropServices;

namespace CCXT.Collector.Library
{
    /// <summary>
    ///
    /// </summary>
    public class XConfig 
    {
        public XConfig()
        {
        }

        public XConfig(IConfiguration configuration)
        {
            __configuration_root = configuration;
        }

        private bool? __is_encrypt_connection_string;

        public bool IsEncryptionConnectionString
        {
            get
            {
                if (__is_encrypt_connection_string == null)
                    __is_encrypt_connection_string = this.GetAppString("encrypt") == "true";

                return __is_encrypt_connection_string.Value;
            }
        }

        private IConfiguration __configuration_root = null;

        public IConfiguration ConfigurationRoot
        {
            get
            {
                if (__configuration_root == null)
                {
                    var _config_builder = new ConfigurationBuilder()
                                            .SetBasePath(Directory.GetCurrentDirectory())
                                            .AddJsonFile($"appsettings.json", true, true)
                                            .AddEnvironmentVariables();

                    __configuration_root = _config_builder.Build();
                }

                return __configuration_root;
            }
        }

        private IConfigurationSection __configuration_section = null;
        public IConfigurationSection ConfigAppSection
        {
            get
            {
                if (__configuration_section == null)
                    __configuration_section = this.ConfigurationRoot.GetSection("appsettings");
                return __configuration_section;
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

        public bool UsePollingOrderboook
        {
            get
            {
                return this.GetAppBoolean("use.polling.orderbook");
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

        /// <summary>
        ///
        /// </summary>
        public bool IsWindows
        {
            get
            {
                return RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="configSection"></param>
        /// <param name="appkey"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public virtual string GetAppString(IConfigurationSection configSection, string appkey, string defaultValue)
        {
            var _result = "";

            if (String.IsNullOrEmpty(appkey) == false)
            {
                if (this.IsWindows == true)
                {
                    _result = configSection[appkey + ".debug"];

                    if (_result == null)
                        _result = configSection[appkey];
                }
                else
                    _result = configSection[appkey];
            }

            if (String.IsNullOrEmpty(_result) == true)
                _result = defaultValue;

            return _result;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="appkey"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public virtual string GetAppString(string appkey, string defaultValue = "")
        {
            return GetAppString(this.ConfigAppSection, appkey, defaultValue);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sectionName"></param>
        /// <param name="appkey"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public virtual string GetAppSection(string sectionName, string appkey, string defaultValue = "")
        {
            var _config_section = this.ConfigAppSection.GetSection(sectionName);
            return GetAppString(_config_section, appkey, defaultValue);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="appkey"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public virtual long GetAppInteger64(string appkey, int defaultValue = 0)
        {
            var _value = GetAppString(appkey);
            return String.IsNullOrEmpty(_value) ? defaultValue : Convert.ToInt64(_value);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="appkey"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public virtual decimal GetAppDecimal(string appkey, decimal defaultValue = 0)
        {
            var _value = GetAppString(appkey);
            return String.IsNullOrEmpty(_value) ? defaultValue : Convert.ToDecimal(_value);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="appkey"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public virtual int GetHexInteger(string appkey, int defaultValue = 0)
        {
            var _value = GetAppString(appkey);
            return String.IsNullOrEmpty(_value) ? defaultValue : Convert.ToInt32(_value, 16);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="appkey"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public virtual int GetAppInteger(string appkey, int defaultValue = 0)
        {
            var _value = GetAppString(appkey);
            return String.IsNullOrEmpty(_value) ? defaultValue : Convert.ToInt32(_value);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="appkey"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public virtual bool GetAppBoolean(string appkey, bool defaultValue = false)
        {
            var _value = GetAppString(appkey);
            return String.IsNullOrEmpty(_value) ? defaultValue : _value.ToLower() == "true";
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="appSection"></param>
        /// <param name="appkey"></param>
        /// <returns></returns>
        public virtual DateTime GetAppDateTime(string appSection, string appkey)
        {
            var _value = GetAppSection(appSection, appkey);
            return String.IsNullOrEmpty(_value) ? CUnixTime.UtcNow : Convert.ToDateTime(_value);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="appSection"></param>
        /// <param name="appkey"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public virtual int GetAppInteger(string appSection, string appkey, int defaultValue = 0)
        {
            var _value = GetAppSection(appSection, appkey);
            return String.IsNullOrEmpty(_value) ? defaultValue : Convert.ToInt32(_value);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="appSection"></param>
        /// <param name="appkey"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public virtual bool GetAppBoolean(string appSection, string appkey, bool defaultValue = false)
        {
            var _value = GetAppSection(appSection, appkey);
            return String.IsNullOrEmpty(_value) ? defaultValue : _value.ToLower() == "true";
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="appSection"></param>
        /// <param name="appkey"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public virtual long GetAppInteger64(string appSection, string appkey, int defaultValue = 0)
        {
            var _value = GetAppSection(appSection, appkey);
            return String.IsNullOrEmpty(_value) ? defaultValue : Convert.ToInt64(_value);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="appSection"></param>
        /// <param name="appkey"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public virtual decimal GetAppDecimal(string appSection, string appkey, decimal defaultValue = 0)
        {
            var _value = GetAppSection(appSection, appkey);
            return String.IsNullOrEmpty(_value) ? defaultValue : Convert.ToDecimal(_value);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="appSection"></param>
        /// <param name="appkey"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public virtual int GetHexInteger(string appSection, string appkey, int defaultValue = 0)
        {
            var _value = GetAppSection(appSection, appkey);
            return String.IsNullOrEmpty(_value) ? defaultValue : Convert.ToInt32(_value, 16);
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

        private string __name_start_exchange = null;

        public string StartExchangeName
        {
            get
            {
                if (__name_start_exchange == null)
                    __name_start_exchange = this.GetAppString("auto.start.exchange.name");
                return __name_start_exchange;
            }
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