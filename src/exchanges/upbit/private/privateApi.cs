using CCXT.NET.Coin;
using CCXT.NET.Coin.Private;
using CCXT.NET.Upbit;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CCXT.Collector.Upbit.Private
{
    public class PrivateApi : CCXT.NET.Coin.Private.PrivateApi, IPrivateApi
    {
        private readonly string __connect_key;
        private readonly string __secret_key;

        /// <summary>
        ///
        /// </summary>
        public PrivateApi(string connect_key, string secret_key)
        {
            __connect_key = connect_key;
            __secret_key = secret_key;
        }

        /// <summary>
        ///
        /// </summary>
        public override XApiClient privateClient
        {
            get
            {
                if (base.privateClient == null)
                    base.privateClient = new UpbitClient("private", __connect_key, __secret_key);

                return base.privateClient;
            }
        }

        /// <summary>
        /// 전체 계좌 조회: 내가 보유한 자산 리스트를 보여줍니다.
        /// </summary>
        /// <returns></returns>
        public async Task<List<Accounts>> Accounts()
        {
            var _result = new List<Accounts>();

            var _json_value = await privateClient.CallApiGet1Async("/accounts");

            var _json_result = privateClient.GetResponseMessage(_json_value.Response);
            if (_json_result.success == true)
                _result = privateClient.DeserializeObject<List<Accounts>>(_json_value.Content);

            return _result;
        }
    }
}