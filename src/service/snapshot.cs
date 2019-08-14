using CCXT.Collector.Binance.Orderbook;
using CCXT.Collector.Bitmex.Orderbook;
using CCXT.Collector.Library.Types;
using CCXT.Collector.Upbit.Orderbook;
using CCXT.NET.Coin.Public;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CCXT.Collector.Library.Service
{
    public class SnapshotQ : FactoryQ
    {
        private static string __last_exchange = "";
        private static string __last_symbol = "";

        private static CancellationTokenSource __ss_token_source;

        public static CancellationTokenSource SSTokenSource
        {
            get
            {
                if (__ss_token_source == null)
                    __ss_token_source = new CancellationTokenSource();

                return __ss_token_source;
            }
            set
            {
                __ss_token_source = value;
            }
        }

        private static SynchronizedCollection<Task> __snapshot_tasks;

        /// <summary>
        ///
        /// </summary>
        public static SynchronizedCollection<Task> SnapshotTasks
        {
            get
            {
                if (__snapshot_tasks == null)
                    __snapshot_tasks = new SynchronizedCollection<Task>();

                return __snapshot_tasks;
            }
            set
            {
                __snapshot_tasks = value;
            }
        }

        private static bool __change_symbol_flag = false;

        private async Task CancelSnapshot()
        {
            SSTokenSource.Cancel();
            await Task.WhenAll(SnapshotTasks);
            SnapshotTasks.Clear();
            SSTokenSource = null;
        }

        private async Task StartNewSymbol(string exchange, string baseIds)
        {
            __change_symbol_flag = true;

            if (String.IsNullOrEmpty(__last_symbol) == false)
                LoggerQ.WriteO($"snapshot stopped: symbol => {__last_symbol}", __last_exchange);

            await CancelSnapshot();

            var _symbols = baseIds.Split(';');
            if (exchange == UPLogger.exchange_name)
            {
                if (KConfig.UpbitUsePollingBookticker == false)
                {
                    foreach (var _s in _symbols)
                    {
                        if (String.IsNullOrEmpty(_s) == true)
                            continue;

                        SnapshotTasks.Add((new Upbit.Orderbook.WebSocket()).Start(SSTokenSource, _s));
                        SnapshotTasks.Add((new Upbit.Orderbook.Polling()).OStart(SSTokenSource, _s));
                    }
                }
                else
                    SnapshotTasks.Add((new Upbit.Orderbook.Polling()).BStart(SSTokenSource, _symbols));

                SnapshotTasks.Add((new Upbit.Orderbook.Processing()).Start(SSTokenSource));
            }
            else if (exchange == BNLogger.exchange_name)
            {
                if (KConfig.BinanceUsePollingBookticker == false)
                {
                    foreach (var _s in _symbols)
                    {
                        if (String.IsNullOrEmpty(_s) == true)
                            continue;

                        SnapshotTasks.Add((new Binance.Orderbook.WebSocket()).Start(SSTokenSource, _s));
                        SnapshotTasks.Add((new Binance.Orderbook.Polling()).OStart(SSTokenSource, _s));
                    }
                }
                else
                    SnapshotTasks.Add((new Binance.Orderbook.Polling()).BStart(SSTokenSource, _symbols));

                SnapshotTasks.Add((new Binance.Orderbook.Processing()).Start(SSTokenSource));
            }
            else if (exchange == BMLogger.exchange_name)
            {
                if (KConfig.BitmexUsePollingBookticker == false)
                {
                    foreach (var _s in _symbols)
                    {
                        if (String.IsNullOrEmpty(_s) == true)
                            continue;

                        SnapshotTasks.Add((new Binance.Orderbook.WebSocket()).Start(SSTokenSource, _s));
                        SnapshotTasks.Add((new Binance.Orderbook.Polling()).OStart(SSTokenSource, _s));
                    }
                }
                else
                    SnapshotTasks.Add((new Binance.Orderbook.Polling()).BStart(SSTokenSource, _symbols));

                SnapshotTasks.Add((new Binance.Orderbook.Processing()).Start(SSTokenSource));
            }

            LoggerQ.WriteO($"snapshot restart: symbol => {baseIds}", exchange);

            __last_exchange = exchange;
            __last_symbol = baseIds;

            __change_symbol_flag = false;
        }

        private async Task StartNewMarket(string baseIds, string quoteIds)
        {
            __change_symbol_flag = true;

            if (String.IsNullOrEmpty(__last_symbol) == false)
                LoggerQ.WriteO($"qsnapshot stopped: symbol => {__last_symbol}", __last_exchange);

            await CancelSnapshot();

            var _base_ids = baseIds.Split(';');
            var _quote_ids = quoteIds.Split(';');

            var _usymbols = new List<IMarketItem>();
            var _bsymbols = new List<IMarketItem>();
            {
                var _umarkets = await (new Upbit.PublicApi()).LoadMarkets();
                var _bmarkets = await (new Binance.PublicApi()).LoadMarkets();

                foreach (var _base_id in _base_ids)
                {
                    foreach (var _quote_id in _quote_ids)
                    {
                        var _market_id = _base_id + "/" + _quote_id;

                        var _umarket = _umarkets.result.Values.Where(m => m.marketId == _market_id).SingleOrDefault();
                        if (_umarket != null)
                            _usymbols.Add(_umarket as IMarketItem);

                        var _bmarket = _bmarkets.result.Values.Where(m => m.marketId == _market_id).SingleOrDefault();
                        if (_bmarket != null)
                            _bsymbols.Add(_bmarket as IMarketItem);
                    }
                }
            }

            SnapshotTasks.Add((new Upbit.Orderbook.Polling()).AStart(SSTokenSource, _usymbols));
            SnapshotTasks.Add((new Binance.Orderbook.Polling()).AStart(SSTokenSource, _bsymbols));

            foreach (var _base_id in _base_ids)
                SnapshotTasks.Add((new AProcessing()).Start(SSTokenSource, _base_id));

            LoggerQ.WriteO($"qsnapshot restart: symbol => {baseIds}", "market");

            __last_exchange = "market";
            __last_symbol = baseIds;

            __change_symbol_flag = false;
        }

        public SnapshotQ(
             string host_name = null, string ip_address = null, string virtual_host = null,
             string user_name = null, string password = null
         )
         : base(host_name, ip_address, virtual_host, user_name, password, SnapshotQName)
        {
        }

        public async Task Start(CancellationTokenSource tokenSource, int sleep_seconds = 1)
        {
            LoggerQ.WriteO($"snapshot service start...", FactoryQ.RootQName);

            if (KConfig.UsePollingArbitrage == false)
            {
                if (KConfig.UseAutoStart == true)
                    await StartNewSymbol(KConfig.StartExchangeName, KConfig.StartSymbolNames);
            }
            else
            {
                await StartNewMarket(KConfig.ArbitrageBaseNames, KConfig.ArbitrageQuoteNames);
            }

            var _processing = Task.Run(async () =>
            {
                using (var _connection = CFactory.CreateConnection())
                {
                    using (var _channel = _connection.CreateModel())
                    {
                        _channel.QueueDeclare(queue: QueueName, durable: true, exclusive: false, autoDelete: false, arguments: null);
                        _channel.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);

                        var _consumer = new EventingBasicConsumer(_channel);

                        _consumer.Received += async (object model, BasicDeliverEventArgs ea) =>
                        {
                            var _payload = ea.Body;
                            var _message = Encoding.UTF8.GetString(_payload);

                            try
                            {
                                var _selector = JsonConvert.DeserializeObject<QSelector>(_message);
                                if (__last_exchange == _selector.exchange && __last_symbol == _selector.symbol)
                                {
                                    foreach (var _s in _selector.symbol.Split(';'))
                                    {
                                        var _q_message = new QMessage
                                        {
                                            command = "SS",
                                            json = JsonConvert.SerializeObject(new QSelector
                                            {
                                                exchange = _selector.exchange,
                                                symbol = _s
                                            })
                                        };

                                        if (_selector.exchange == BNLogger.exchange_name)
                                        {
                                            if (KConfig.BinanceUsePollingBookticker == false)
                                                Binance.Orderbook.Processing.SendReceiveQ(_q_message);
                                        }
                                        else if (_selector.exchange == BMLogger.exchange_name)
                                        {
                                            if (KConfig.BitmexUsePollingBookticker == false)
                                                Bitmex.Orderbook.Processing.SendReceiveQ(_q_message);
                                        }
                                        else if (_selector.exchange == UPLogger.exchange_name)
                                        {
                                            if (KConfig.UpbitUsePollingBookticker == false)
                                                Upbit.Orderbook.Processing.SendReceiveQ(_q_message);
                                        }
                                    }
                                }
                                else
                                {
                                    await StartNewSymbol(_selector.exchange, _selector.symbol);
                                }
                            }
                            catch (Exception ex)
                            {
                                LoggerQ.WriteX(ex.ToString());
                            }
                            finally
                            {
                                _channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
                            }

                            await Task.Delay(0);
                        };

                        _channel.BasicConsume(queue: QueueName, autoAck: false, consumer: _consumer);

                        while (tokenSource != null)
                        {
                            var _cancelled = tokenSource.Token.WaitHandle.WaitOne(0);
                            if (_cancelled == true)
                                break;

                            if (_channel.IsClosed == true)
                            {
                                await CancelSnapshot();

                                tokenSource.Cancel();
                                break;
                            }

                            if (__change_symbol_flag == false)
                            {
                                if (SSTokenSource.IsCancellationRequested == true)
                                {
                                    tokenSource.Cancel();
                                    break;
                                }
                            }

                            await Task.Delay(sleep_seconds * 1000);
                        }
                    }
                }
            },
            tokenSource.Token
            );

            await Task.WhenAll(_processing);

            LoggerQ.WriteO($"snapshot service stopped...", FactoryQ.RootQName);
        }
    }
}