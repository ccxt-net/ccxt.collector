using CCXT.NET.Shared.Coin;
using CCXT.NET.Shared.Coin.Public;

namespace CCXT.Collector.Gemini.Public
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
                    base.publicClient = new GeminiClient("public");

                return base.publicClient;
            }
        }
    }
}