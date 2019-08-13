using CCXT.NET.Serialize;
using RestSharp;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace CCXT.Collector.Library
{
    public class KRestClient
    {
        private const string __content_type = "application/json";
        private const string __user_agent = "odinsoft - ccxt.collector / 1.0.2019.08";

        /// <summary>
        ///
        /// </summary>
        /// <param name="baseurl"></param>
        /// <returns></returns>
        public IRestClient CreateJsonClient(string baseurl)
        {
            var _client = new RestClient(baseurl)
            {
                Timeout = 5 * 1000,
                ReadWriteTimeout = 32 * 1000,
                UserAgent = __user_agent,
                Encoding = Encoding.GetEncoding(65001)
            };

            _client.RemoveHandler(__content_type);
            _client.AddHandler(__content_type, () => new RestSharpJsonNetDeserializer());

            return _client;
        }


        /// <summary>
        ///
        /// </summary>
        /// <param name="baseurl"></param>
        /// <returns></returns>
        public IRestClient CreateJsonBytesClient(string baseurl)
        {
            var _client = new RestClient(baseurl)
            {
                Timeout = 10 * 1000,
                UserAgent = __user_agent
            };

            _client.RemoveHandler(__content_type);
            _client.AddHandler(__content_type, () => new RestSharpJsonNetDeserializer());

            return _client;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="endpoint">api link address of a function</param>
        /// <param name="args">Add additional attributes for each exchange</param>
        /// <returns></returns>
        public IRestRequest CreateJsonRequest(string endpoint, Dictionary<string, object> args)
        {
            var _request = new RestRequest(endpoint, Method.GET)
            {
                RequestFormat = DataFormat.Json,
                JsonSerializer = new RestSharpJsonNetSerializer()
            };

            foreach (var _arg in args)
                _request.AddParameter(_arg.Key, _arg.Value);

            return _request;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="request"></param>
        /// <param name="max_retry"></param>
        /// <param name="delay_milliseconds"></param>
        /// <returns></returns>
        public async Task<IRestResponse> RestExecuteAsync(IRestClient client, IRestRequest request, int max_retry = 3, int delay_milliseconds = 1000)
        {
            var _result = (IRestResponse)null;

            for (var _retry_count = 0; _retry_count < max_retry; _retry_count++)
            {
                var _tcs = new TaskCompletionSource<IRestResponse>();
                {
                    var _handle = client.ExecuteAsync(request, response =>
                    {
                        _tcs.SetResult(response);
                    });

                    _result = await _tcs.Task;
                }

                if (_result.ResponseStatus != ResponseStatus.TimedOut && _result.StatusCode != HttpStatusCode.RequestTimeout)
                    break;

                await Task.Delay(delay_milliseconds);
            }

            return _result;
        }


        /// <summary>
        ///
        /// </summary>
        /// <param name="request"></param>
        /// <param name="max_retry"></param>
        /// <param name="delay_milliseconds"></param>
        /// <returns></returns>
        public async Task<byte[]> RestExecuteBytesAsync(IRestClient client, IRestRequest request, int max_retry = 3, int delay_milliseconds = 1000)
        {
            var _result = (byte[])null;

            for (var _retry_count = 0; _retry_count < max_retry; _retry_count++)
            {
                var _tcs = new TaskCompletionSource<byte[]>();
                {
                    var _handle = client.ExecuteAsync(request, response =>
                    {
                        _tcs.SetResult(response.RawBytes);
                    });

                    _result = await _tcs.Task;
                }

                if (_result != null)
                    break;

                await Task.Delay(delay_milliseconds);
            }

            return _result;
        }
    }
}