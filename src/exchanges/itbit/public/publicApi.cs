using CCXT.NET.Shared.Coin;
using CCXT.NET.Shared.Coin.Public;

namespace CCXT.Collector.ItBit.Public
{
    public class PublicApi : CCXT.NET.Shared.Coin.Public.PublicApi, IPublicApi
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