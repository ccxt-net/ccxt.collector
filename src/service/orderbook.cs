using RabbitMQ.Client;
using System;
using System.Collections.Concurrent;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CCXT.Collector.Library.Service
{
    public class OrderbookQ : FactoryQ
    {
        public OrderbookQ(
             string host_name = null, string ip_address = null, string virtual_host = null,
             string user_name = null, string password = null,
             string queue_name = OrderbookQName
         )
         : base(host_name, ip_address, virtual_host, user_name, password, queue_name)
        {
        }

        private static ConcurrentQueue<string> __order_book_queue = null;

        /// <summary>
        ///
        /// </summary>
        private static ConcurrentQueue<string> QOrderbook
        {
            get
            {
                if (__order_book_queue == null)
                    __order_book_queue = new ConcurrentQueue<string>();

                return __order_book_queue;
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="jsonMessage"></param>
        public static void Write(string jsonMessage)
        {
            QOrderbook.Enqueue(jsonMessage);
        }

        public async Task Start(CancellationTokenSource tokenSource)
        {
            LoggerQ.WriteO($"orderbook service start...", FactoryQ.RootQName);

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

                                var _json_message = (string)null;
                                if (QOrderbook.TryDequeue(out _json_message) == false)
                                {
                                    var _cancelled = tokenSource.Token.WaitHandle.WaitOne(0);
                                    if (_cancelled == true)
                                        break;

                                    await Task.Delay(10);
                                    continue;
                                }

                                var _body = Encoding.UTF8.GetBytes(_json_message);
                                _channel.BasicPublish(exchange: QueueName, routingKey: "", basicProperties: null, body: _body);
#if DEBUG
                                LoggerQ.WriteO(_json_message.Substring(0, _json_message.Length < 256 ? _json_message.Length : 256));
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

            LoggerQ.WriteO($"orderbook service stopped...", FactoryQ.RootQName);
        }
    }
}