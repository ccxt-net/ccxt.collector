using OdinSdk.BaseLib.Coin;
using OdinSdk.BaseLib.Coin.Public;

namespace CCXT.Collector.ItBit.Public
{
    public class PublicApi : OdinSdk.BaseLib.Coin.Public.PublicApi, IPublicApi
    {
        /// <summary>
        ///
        /// </summary>
        public PublicApi()
        {
        }

        /// <summary>
        ///
        /// </summary>
        public override XApiClient publicClient
        {
            get
            {
                if (base.publicClient == null)
                    base.publicClient = new ItBitClient("public");

                return base.publicClient;
            }
        }
    }
}