using CCXT.Collector.Binance.Public;
using CCXT.Collector.Library;
using CCXT.Collector.Service;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace CCXT.Collector.Binance
{
    public partial class Processing
    {
        private static ConcurrentQueue<QMessage> __recv_queue = null;

        /// <summary>
        ///
        /// </summary>
        private static ConcurrentQueue<QMessage> ReceiveQ
        {
            get
            {
                if (__recv_queue == null)
                    __recv_queue = new ConcurrentQueue<QMessage>();

                return __recv_queue;
            }
        }

        private readonly BNConfig __bnconfig;

        public Processing(IConfiguration configuration)
        {
            __bnconfig = new BNConfig(configuration);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="message"></param>
        public static void SendReceiveQ(QMessage message)
        {
            ReceiveQ.Enqueue(message);
        }

        public async ValueTask Start(CancellationToken cancelToken)
        {
            BNLogger.SNG.WriteO(this, $"processing service start...");

            var _processing = Task.Run(async () =>
            {
                while (true)
                {
                    try
                    {
                        await Task.Delay(0);

                        var _message = (QMessage)null;
                        if (ReceiveQ.TryDequeue(out _message) == false)
                        {
                            var _cancelled = cancelToken.WaitHandle.WaitOne(0);
                            if (_cancelled == true)
                                break;

                            await Task.Delay(10);
                            continue;
                        }

                        //var _json_data = JsonConvert.DeserializeObject<QSelector>(_message.json);
                        if (_message.command == "WS")
                        {
                            if (_message.stream == "trade")
                            {
                                var _trade = JsonConvert.DeserializeObject<BWTrade>(_message.payload ?? "");
                                await mergeTradeItem(_trade.data);
                            }
                            //else if (_message.stream == "orderbook")
                            //{
                            //    var _orderbook = JsonConvert.DeserializeObject<BWOrderBook>(_message.json);
                            //    await mergeOrderbook(_orderbook);
                            //}
                        }
                        else if (_message.command == "AP")
                        {
                            if (_message.stream == "trade")
                            {
                                var _trades = JsonConvert.DeserializeObject<BATrade>(_message.payload ?? "");
                                await mergeTradeItems(_trades);
                            }
                            else if (_message.stream == "orderbook")
                            {
                                var _orderbook = JsonConvert.DeserializeObject<BAOrderBook>(_message.payload ?? "");
                                await mergeOrderbook(_orderbook);
                            }
                            else if (_message.stream == "ticker")
                            {
                                var _ticker = JsonConvert.DeserializeObject<STickers>(_message.payload ?? "");
                                await publishTicker(_ticker);
                            }
                        }
                        else if (_message.command == "SS")
                        {
                            await snapshotOrderbook(_message.exchange, _message.symbol);
                        }
#if DEBUG
                        else
                            BNLogger.SNG.WriteO(this, _message.payload);
#endif
                        if (cancelToken.IsCancellationRequested == true)
                            break;
                    }
                    catch (TaskCanceledException)
                    {
                    }
                    catch (Exception ex)
                    {
                        BNLogger.SNG.WriteX(this, ex.ToString());
                    }
                }
            },
            cancelToken
            );

            await Task.WhenAll(_processing);

            BNLogger.SNG.WriteO(this, $"processing service stop...");
        }
    }
}