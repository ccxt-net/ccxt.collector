namespace CCXT.Collector.Bitget
{
    public class WResult<T>
    {
        public string action { get; set; }
        public InstArg arg { get; set; }
        public T data { get; set; }
    }

    public class InstArg
    {
        public string instType { get; set; }
        public string channel { get; set; }
        public string instId { get; set; }
    }
}