using CCXT.NET.Shared.Serialize;
using RestSharp;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace CCXT.Collector.Library
{
    public class KRestClient : CCXT.NET.Shared.Coin.XApiClient
    {
        private const string __content_type = "application/json";
        private const string __user_agent = "odinsoft - ccxt.collector / 1.0.2019.08";

        /// <summary>
        /// 
        /// </summary>
        public KRestClient() : base("")
        {
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="division"></param>
        public KRestClient(string division) : base(division)
        {
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="baseurl"></param>
        /// <returns></returns>
        public override RestClient CreateJsonClient(string baseurl)
        {
            var _options = new RestClientOptions
            {
                BaseUrl = new System.Uri(baseurl),
                Timeout = 5 * 1000,
                //ReadWriteTimeout = 32 * 1000,
                UserAgent = __user_agent,
                Encoding = Encoding.GetEncoding(65001)
            };

            var _client = new RestClient(_options);

            //_client.RemoveHandler(__content_type);
            //_client.AddHandler(__content_type, () => new RestSharpJsonNetDeserializer());

            return _client;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="baseurl"></param>
        /// <returns></returns>
        public RestClient CreateJsonBytesClient(string baseurl)
        {
            var _options = new RestClientOptions
            {
                BaseUrl =new System.Uri(baseurl),
                Timeout = 10 * 1000,
                UserAgent = __user_agent
            };

            var _client = new RestClient(_options);

            //_client.RemoveHandler(__content_type);
            //_client.AddHandler(__content_type, () => new RestSharpJsonNetDeserializer());

            return _client;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="request"></param>
        /// <param name="max_retry"></param>
        /// <param name="delay_milliseconds"></param>
        /// <returns></returns>
        public async ValueTask<RestResponse> RestExecuteAsync(RestClient client, RestRequest request, int max_retry = 3, int delay_milliseconds = 1000)
        {
            var _result = (RestResponse)null;

            for (var _retry_count = 0; _retry_count < max_retry; _retry_count++)
            {
                //var _tcs = new TaskCompletionSource<RestResponse>();
                //{
                    //var _handle = client.ExecuteAsync(request, response =>
                    //{
                    //    _tcs.SetResult(response);
                    //});

                    //_result = await _tcs.Task;
                //}

                _result = await client.ExecuteAsync(request);

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
        public async ValueTask<byte[]> RestExecuteBytesAsync(RestClient client, RestRequest request, int max_retry = 3, int delay_milliseconds = 1000)
        {
            var _result = (byte[])null;

            for (var _retry_count = 0; _retry_count < max_retry; _retry_count++)
            {
                //var _tcs = new TaskCompletionSource<byte[]>();
                //{
                //    var _handle = client.ExecuteAsync(request, response =>
                //    {
                //        _tcs.SetResult(response.RawBytes);
                //    });

                //    _result = await _tcs.Task;
                //}

                _result = (await client.ExecuteAsync(request)).RawBytes;
                if (_result != null)
                    break;

                await Task.Delay(delay_milliseconds);
            }

            return _result;
        }
    }
}