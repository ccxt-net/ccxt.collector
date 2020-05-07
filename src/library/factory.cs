using Microsoft.Extensions.Configuration;
using OdinSdk.BaseLib.Queue;
using System;

namespace CCXT.Collector.Library
{
    /// <summary>
    ///
    /// </summary>
    public class FactoryX : FactoryQ
    {
        private static string __root_qname;

        public static string RootQName
        {
            get
            {
                if (String.IsNullOrEmpty(__root_qname) == true)
                    __root_qname = "ccxt";
                return __root_qname;
            }
            set
            {
                __root_qname = value;
            }
        }

        public static string SnapshotQName
        {
            get
            {
                return RootQName + "_snapshot_queue";
            }
        }

        public static string LoggerQName
        {
            get
            {
                return RootQName + "_logger_exchange";
            }
        }

        public static string OrderbookQName
        {
            get
            {
                return RootQName + "_orderbook_exchange";
            }
        }

        public static string TradingQName
        {
            get
            {
                return RootQName + "_trading_exchange";
            }
        }

        public static string CompleteQName
        {
            get
            {
                return RootQName + "_complete_exchange";
            }
        }

        public static string TickerQName
        {
            get
            {
                return RootQName + "_ticker_exchange";
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="host_name"></param>
        /// <param name="ip_address"></param>
        /// <param name="virtual_host"></param>
        /// <param name="user_name"></param>
        /// <param name="password"></param>
        /// <param name="queue_name"></param>
        public FactoryX(
            string host_name = null, string ip_address = null, string virtual_host = null,
            string user_name = null, string password = null, string queue_name = null
            ) 
            : base(host_name, ip_address, virtual_host, user_name, password, queue_name)
        {
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="configuration"></param>
        /// <param name="queue_name"></param>
        public FactoryX(IConfiguration configuration, string queue_name)
        {
            var _section = configuration.GetSection("default-queue");

            this.HostName = _section["hostName"];
            this.IpAddress = _section["ipAddress"];
            this.VirtualHost = _section["virtualHost"];
            this.UserName = _section["userName"];
            this.Password = _section["password"];
            this.QueueName = queue_name;
        }
    }
}