using System.Collections.Generic;

namespace CCXT.Collector.Bitget.Private
{
    public class Account : WResult<List<AccountData>>
    {
    }

    public class AccountData
    {
        public string coinId { get; set; }
        public string coinName { get; set; }
        public decimal available { get; set; }
    }
}