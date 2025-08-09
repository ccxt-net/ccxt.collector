using Microsoft.Extensions.Configuration;
using RabbitMQ.Client;
using System;

namespace CCXT.Collector.Library
{
    /// <summary>
    ///
    /// </summary>
    public class FactoryX
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

        private static XConfig __cconfig { get; } = new XConfig();

        /// <summary>
        ///
        /// </summary>
        /// <param name="host_name">호스트 명칭</param>
        /// <param name="ip_address">
        /// The connection abstracts the socket connection, and takes care of protocol version negotiation and authentication and so on for us.
        /// Here we connect to a broker on the local machine - hence the localhost.
        /// If we wanted to connect to a broker on a different machine we'd simply specify its name or IP address here.
        /// </param>
        /// <param name="virtual_host">virtual host to access during this connection</param>
        /// <param name="user_name">user name</param>
        /// <param name="password">password</param>
        /// <param name="queue_name">
        /// A queue is the name for a mailbox. It lives inside RabbitMQ.
        /// Although messages flow through RabbitMQ and your applications, they can be stored only inside a queue.
        /// A queue is not bound by any limits, it can store as many messages as you like - it's essentially an infinite buffer.
        /// Many producers can send messages that go to one queue - many consumers can try to receive data from one queue.
        /// A queue will be drawn like this, with its name above it:
        /// </param>
        /// <param name="section_name">default Queue Name</param>
        public FactoryX
            (
                string host_name = null, string ip_address = null, string virtual_host = null,
                string user_name = null, string password = null, string queue_name = null,
                string section_name = null
            )
        {
            __section_name = section_name ?? "default-queue";

            __host_name = host_name ?? DefaultQ["hostName"];
            __ip_address = ip_address ?? DefaultQ["ipAddress"];
            __virtual_host = virtual_host ?? DefaultQ["virtualHost"];

            __user_name = user_name ?? DefaultQ["userName"];
            __password = password ?? DefaultQ["password"];

            __queue_name = queue_name ?? "";
        }

        /// <summary>
        /// default queue 명칭 입니다.
        /// </summary>
        public string __section_name;

        private IConfigurationSection __defaultQ = null;

        /// <summary>
        ///
        /// </summary>
        public IConfigurationSection DefaultQ
        {
            get
            {
                if (__defaultQ == null)
                    __defaultQ = __cconfig.ConfigurationRoot.GetSection(__section_name);

                return __defaultQ;
            }
        }

        private string __host_name;

        /// <summary>
        ///
        /// </summary>
        public string HostName
        {
            get
            {
                return __host_name;
            }
            set
            {
                __host_name = value;
            }
        }

        private string __ip_address;

        /// <summary>
        ///
        /// </summary>
        public string IpAddress
        {
            get
            {
                return __ip_address;
            }
            set
            {
                __ip_address = value;
            }
        }

        private string __virtual_host;

        /// <summary>
        ///
        /// </summary>
        public string VirtualHost
        {
            get
            {
                return __virtual_host;
            }
            set
            {
                __virtual_host = value;
            }
        }

        private string __user_name;

        /// <summary>
        ///
        /// </summary>
        public string UserName
        {
            get
            {
                return __user_name;
            }
            set
            {
                __user_name = value;
            }
        }

        private string __password;

        /// <summary>
        ///
        /// </summary>
        public string Password
        {
            get
            {
                return __password;
            }
            set
            {
                __password = value;
            }
        }

        private string __queue_name;

        /// <summary>
        ///
        /// </summary>
        public string QueueName
        {
            get
            {
                return __queue_name;
            }
            set
            {
                __queue_name = value;
            }
        }

        private ConnectionFactory __factory;

        /// <summary>
        ///
        /// </summary>
        public ConnectionFactory CFactory
        {
            get
            {
                if (__factory == null)
                {
                    __factory = new ConnectionFactory
                    {
                        HostName = __ip_address,
                        VirtualHost = __virtual_host,
                        UserName = __user_name,
                        Password = __password
                    };
                }

                return __factory;
            }
        }
    }
}