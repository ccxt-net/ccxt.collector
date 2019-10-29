using CCXT.Collector.KebHana.Public;
using CCXT.Collector.KebHana.Types;
using CCXT.Collector.Library;
using Newtonsoft.Json;
using OdinSdk.BaseLib.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CCXT.Collector.KebHana
{
    // var exView = {
    //"날짜": "2019년 07월 22일 14:49",
    //"리스트":
    //[
    //{
    //"통화명": "미국 USD",
    //"현찰사실때":"1198.51",
    //"현찰파실때":"1157.29",
    //"송금_전신환보내실때":"1189.40",
    //"송금_전신환받으실때":"1166.40",
    //"매매기준율":"1177.90"
    //},
    //{
    //"통화명": "일본 JPY 100",
    //"현찰사실때":"1109.88",
    //"현찰파실때":"1071.72",
    //"송금_전신환보내실때":"1101.48",
    //"송금_전신환받으실때":"1080.12",
    //"매매기준율":"1090.80"
    //}
    //]

    public class KPolling : KRestClient
    {
        private const string __kebhana_api_rul = "http://fx.kebhana.com/";
        //private static SemaphoreSlim __semaphore = new SemaphoreSlim(1, 1);

        private SynchronizedCollection<Task> __polling_tasks;

        /// <summary>
        ///
        /// </summary>
        public SynchronizedCollection<Task> PollingTasks
        {
            get
            {
                if (__polling_tasks == null)
                    __polling_tasks = new SynchronizedCollection<Task>();

                return __polling_tasks;
            }
            set
            {
                __polling_tasks = value;
            }
        }

        /// <summary>
        ///
        /// </summary>
        public static KebExchange LastExchange
        {
            get;
            set;
        }

        public static KebExchangeItem GetExchange(string symbol)
        {
            var _result = (KebExchangeItem)null;

            if (LastExchange != null)
                _result = LastExchange.data.Where(e => e.code == symbol).FirstOrDefault();

            return _result;
        }

        private static Encoding HanaEncoding = Encoding.GetEncoding("euc-kr");

        public async Task Start(CancellationTokenSource tokenSource)
        {
            KELogger.WriteO($"polling service start...");

            PollingTasks.Add(Task.Run(async () =>
            {
                var _client = CreateJsonBytesClient(__kebhana_api_rul);

                var _k_params = new Dictionary<string, object>();
                var _k_request = CreateJsonRequest($"/FER1101M.web", _k_params);
                var _last_limit_milli_secs = 0L;

                while (true)
                {
                    //await __semaphore.WaitAsync(tokenSource.Token);

                    try
                    {
                        await Task.Delay(0);

                        var _waiting_milli_secs = (CUnixTime.NowMilli - 0) / (60 * 1000);
                        if (_waiting_milli_secs == _last_limit_milli_secs)
                        {
                            var _waiting = tokenSource.Token.WaitHandle.WaitOne(0);
                            if (_waiting == true)
                                break;

                            await Task.Delay(10);
                        }
                        else
                        {
                            _last_limit_milli_secs = _waiting_milli_secs;

                            // orderbook
                            var _k_json_value = await RestExecuteBytesAsync(_client, _k_request);

                            var _json_view = HanaEncoding.GetString(_k_json_value);
                            _json_view = _json_view.Substring(_json_view.IndexOf('{'));

                            var _k_json_data = JsonConvert.DeserializeObject<KebExchange>(_json_view);
                            _k_json_data.sequentialId = _last_limit_milli_secs;

                            LastExchange = _k_json_data;
                        }
                    }
                    catch (TaskCanceledException)
                    {
                    }
                    catch (Exception ex)
                    {
                        KELogger.WriteX(ex.ToString());
                    }
                    //finally
                    {
                        //__semaphore.Release();

                        if (tokenSource.IsCancellationRequested == true)
                            break;
                    }

                    var _cancelled = tokenSource.Token.WaitHandle.WaitOne(0);
                    if (_cancelled == true)
                        break;
                }
            },
            tokenSource.Token
            ));

            await Task.WhenAll(PollingTasks);

            KELogger.WriteO($"polling service stopped..");
        }
    }
}