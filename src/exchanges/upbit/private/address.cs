namespace CCXT.Collector.Upbit.Private
{
    /// <summary>
    /// 입금 주소
    /// </summary>
    public class Address
    {
        /// <summary>
        /// 화폐를 의미하는 영문 대문자 코드
        /// </summary>
        public string currency
        {
            get;
            set;
        }

        /// <summary>
        /// 입금 주소
        /// </summary>
        public string deposit_address
        {
            get;
            set;
        }

        /// <summary>
        /// 2차 입금 주소
        /// </summary>
        public string secondary_address
        {
            get;
            set;
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public class GenerateAddress : Address
    {
        /// <summary>
        /// 요청 성공 여부
        /// </summary>
        public bool success
        {
            get;
            set;
        }

        /// <summary>
        /// 요청 결과에 대한 메세지
        /// </summary>
        public string message
        {
            get;
            set;
        }
    }
}