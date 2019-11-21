using Newtonsoft.Json;
using OdinSdk.BaseLib.Coin.Types;
using System.Collections.Generic;

namespace CCXT.Collector.Library.Public
{
    /// <summary>
    /// 
    /// </summary>
    public class SCompleteOrderItem
    {
        /// <summary>
        ///
        /// </summary>
        public virtual long timestamp
        {
            get;
            set;
        }

        public virtual decimal quantity
        {
            get;
            set;
        }

        public virtual decimal price
        {
            get;
            set;
        }

        /// <summary>
        /// 
        /// </summary>
        public string side
        {
            get
            {
                return sideType.ToString();
            }
        }

        /// <summary>
        /// sell or buy
        /// </summary>
        [JsonIgnore]
        public SideType sideType
        {
            get;
            set;
        }
    }

    /// <summary>
    ///
    /// </summary>
    public class SCompleteOrders : SApiResult<List<SCompleteOrderItem>>
    {
    }
}