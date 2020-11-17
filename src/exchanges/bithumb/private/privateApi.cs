using CCXT.NET.Shared.Coin;
using CCXT.NET.Shared.Coin.Private;

namespace CCXT.Collector.Bithumb.Private
{
    public class PrivateApi : CCXT.NET.Shared.Coin.Private.PrivateApi, IPrivateApi
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
                    base.privateClient = new BithumbClient("private", __connect_key, __secret_key);

                return base.privateClient;
            }
        }
    }
}