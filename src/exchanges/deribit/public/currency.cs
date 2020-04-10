namespace CCXT.Collector.Deribit.Public
{
    /// <summary>
    ///
    /// </summary>
    public class DCurrency
    {
        public decimal txFee
        {
            get; set;
        }

        public int minConfirmation
        {
            get; set;
        }

        public bool isActive
        {
            get; set;
        }

        public string currencyLong
        {
            get; set;
        }

        public string currency
        {
            get; set;
        }

        public string coinType
        {
            get; set;
        }

        public string baseaddress
        {
            get; set;
        }
    }
}