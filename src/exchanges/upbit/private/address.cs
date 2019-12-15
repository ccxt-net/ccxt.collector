using Newtonsoft.Json;
using OdinSdk.BaseLib.Coin.Private;

namespace CCXT.Collector.Upbit.Private
{
    /// <summary>
    /// 입금 주소
    /// </summary>
    public class UAddress : OdinSdk.BaseLib.Coin.Private.AddressItem, IAddressItem
    {
        /// <summary>
        /// 화폐를 의미하는 영문 대문자 코드
        /// </summary>
        public override string? currency
        {
            get;
            set;
        }

        /// <summary>
        /// 입금 주소
        /// </summary>
        [JsonProperty(PropertyName = "deposit_address")]
        public override string? address
        {
            get;
            set;
        }

        /// <summary>
        /// 2차 입금 주소
        /// </summary>
        [JsonProperty(PropertyName = "secondary_address")]
        public override string? tag
        {
            get;
            set;
        }
    }

    /// <summary>
    ///
    /// </summary>
    public class GenerateAddress : UAddress
    {
        /// <summary>
        /// 요청 성공 여부
        /// </summary>
        public override bool success
        {
            get;
            set;
        }

        /// <summary>
        /// 요청 결과에 대한 메세지
        /// </summary>
        public string? message
        {
            get;
            set;
        }
    }
}