using CCXT.Collector.Library;
using RabbitMQ.Client;
using System;
using System.Collections.Concurrent;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CCXT.Collector.Service
{
    public class TradingQ : FactoryX
    {
        public TradingQ(
             string? host_name = null, string? ip_address = null, string? virtual_host = null,
             string? user_name = null, string password = null
         )
         : base(host_name, ip_address, virtual_host, user_name, password, TradingQName)
        {
        }

        private static ConcurrentQueue<string> __trading_queue = null;

        /// <summary>
        ///
        /// </summary>
        private static ConcurrentQueue<string> QTrading
        {
            get
            {
                if (__trading_queue == null)
                    __trading_queue = new ConcurrentQueue<string>();

                return __trading_queue;
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="jsonMessage"></param>
        public static void Write(string jsonMessage)
        {
            QTrading.Enqueue(jsonMessage);
        }

        public async Task Start(CancellationTokenSource tokenSource)
        {
            LoggerQ.WriteO($"trading service start...", FactoryX.RootQName);

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

                                var _json_message = (string?)null;
                                if (QTrading.TryDequeue(out _json_message) == false)
                                {
                                    var _cancelled = tokenSource.Token.WaitHandle.WaitOne(0);
                                    if (_cancelled == true)
                                        break;

                                    await Task.Delay(10);
                                    continue;
                                }

#if !DEBUG
                                var _body = Encoding.UTF8.GetBytes(_json_message);
                                _channel.BasicPublish(exchange: QueueName, routingKey: "", basicProperties: null, body: _body);
#else
                                LoggerQ.WriteO(_json_message);
#endif
                                if (_channel.IsClosed == true)
                                {
                                    tokenSource.Cancel();
                                    break;
                                }

                                if (tokenSource.IsCancellationRequested == true)
                                    break;
                            }
                            catch (Exception ex)
                            {
                                LoggerQ.WriteX(ex.ToString());
                            }
                        }
                    }
                }
            },
            tokenSource.Token
            );

            await Task.WhenAll(_processing);

            LoggerQ.WriteO($"trading service stopped...", FactoryX.RootQName);
        }
    }
}