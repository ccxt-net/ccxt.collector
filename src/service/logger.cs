using CCXT.Collector.Library;
using OdinSdk.BaseLib.Configuration;
using OdinSdk.BaseLib.Extension;
using RabbitMQ.Client;
using System;
using System.Collections.Concurrent;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CCXT.Collector.Service
{
    public class LoggerQ : FactoryX
    {
        internal class PLogger
        {
            /// <summary>
            ///
            /// </summary>
            public string command
            {
                get;
                set;
            }

            /// <summary>
            ///
            /// </summary>
            public string exchange
            {
                get;
                set;
            }

            /// <summary>
            ///
            /// </summary>
            public string message
            {
                get;
                set;
            }
        }

        public LoggerQ()
            : base(queue_name: LoggerQName)
        {
        }

        private static ConcurrentQueue<PLogger> __log_queue = null;

        /// <summary>
        ///
        /// </summary>
        private static ConcurrentQueue<PLogger> LogQ
        {
            get
            {
                if (__log_queue == null)
                    __log_queue = new ConcurrentQueue<PLogger>();

                return __log_queue;
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="message"></param>
        public static void WriteQ(string message, string exchange = "")
        {
            LogQ.Enqueue(new PLogger
            {
                command = "WQ",
                exchange = exchange,
                message = message
            });
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="message"></param>
        public static void WriteO(string message, string exchange = "")
        {
            LogQ.Enqueue(new PLogger
            {
                command = "WO",
                exchange = exchange,
                message = message
            });
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="message"></param>
        public static void WriteX(string message, string exchange = "")
        {
            LogQ.Enqueue(new PLogger
            {
                command = "WX",
                exchange = exchange,
                message = message
            });
        }

        public async Task Start(CancellationTokenSource tokenSource)
        {
            Console.Out.WriteLine($"{CUnixTime.UtcNow.ToLogDateTimeString()} {FactoryX.RootQName} logger service start...");

            var _processing = Task.Run(async () =>
            {
                using (var _connection = CFactory.CreateConnection())
                {
                    using (var _channel = _connection.CreateModel())
                    {
                        _channel.ExchangeDeclare(exchange: QueueName, type: "fanout");

                        while (true)
                        {
                            try
                            {
                                await Task.Delay(0);

                                var _packet = (PLogger)null;
                                if (LogQ.TryDequeue(out _packet) == false)
                                {
                                    var _cancelled = tokenSource.Token.WaitHandle.WaitOne(0);
                                    if (_cancelled == true)
                                        break;

                                    await Task.Delay(10);
                                    continue;
                                }

                                var _message = $"{CUnixTime.UtcNow.ToLogDateTimeString()} {_packet.exchange} {_packet.message}";

                                if (_packet.command == "WQ")
                                {
#if !DEBUG
                                    var _body = Encoding.UTF8.GetBytes(_message);
                                    _channel.BasicPublish(exchange: QueueName, routingKey: "", basicProperties: null, body: _body);
#else
                                    Console.Out.WriteLine(_message);
#endif
                                }
                                else if (_packet.command == "WO")
                                {
                                    Console.Out.WriteLine(_message);
                                }
                                else if (_packet.command == "WX")
                                {
                                    Console.Error.WriteLine(_message);
                                }

                                if (_channel.IsClosed == true)
                                {
                                    tokenSource.Cancel();
                                    break;
                                }
                            }
                            catch (Exception ex)
                            {
                                Console.Error.WriteLine(ex.ToString());
                            }
                        }
                    }
                }
            },
            tokenSource.Token
            );

            await Task.WhenAll(_processing);

            Console.Out.WriteLine($"{CUnixTime.UtcNow.ToLogDateTimeString()} {FactoryX.RootQName} logger service stopped...");
        }
    }
}