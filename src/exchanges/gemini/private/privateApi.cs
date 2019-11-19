using OdinSdk.BaseLib.Coin;
using OdinSdk.BaseLib.Coin.Private;

namespace CCXT.Collector.Gemini.Private
{
    public class PrivateApi : OdinSdk.BaseLib.Coin.Private.PrivateApi, IPrivateApi
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
                    base.privateClient = new GeminiClient("private", __connect_key, __secret_key);

                return base.privateClient;
            }
        }
    }
}