namespace CCXT.Collector.Deribit.Model
{
    /*
        timestamp, instrument_name, 
        settlement_price, open_interest, mark_price, mark_iv, last_price, index_price, 
        underlying_price, underlying_index, 
        vega, theta, rho, gamma, delta, 
        best_bid_price, best_bid_amount, best_ask_price, best_ask_amount
    */

    public class DExportCsv
    {
        /// <summary>
        /// 
        /// </summary>
        public long timestamp
        {
            get; set;
        }

        /// <summary>
        /// 
        /// </summary>
        public string instrument_name
        {
            get; set;
        }


        /// <summary>
        /// 
        /// </summary>
        public decimal settlement_price
        {
            get; set;
        }

        /// <summary>
        /// 
        /// </summary>
        public decimal open_interest
        {
            get; set;
        }

        /// <summary>
        /// 
        /// </summary>
        public decimal mark_price
        {
            get; set;
        }

        /// <summary>
        /// 
        /// </summary>
        public decimal mark_iv
        {
            get; set;
        }

        /// <summary>
        /// 
        /// </summary>
        public decimal last_price
        {
            get; set;
        }

        /// <summary>
        /// 
        /// </summary>
        public decimal index_price
        {
            get; set;
        }
        /// <summary>
        /// 
        /// </summary>
        public decimal underlying_price
        {
            get; set;
        }

        /// <summary>
        /// 
        /// </summary>
        public string underlying_index
        {
            get; set;
        }

        /// <summary>
        /// 
        /// </summary>
        public decimal vega
        {
            get;
            set;
        }

        /// <summary>
        /// 
        /// </summary>
        public decimal theta
        {
            get;
            set;
        }

        /// <summary>
        /// 
        /// </summary>
        public decimal rho
        {
            get;
            set;
        }

        /// <summary>
        /// 
        /// </summary>
        public decimal gamma
        {
            get;
            set;
        }

        /// <summary>
        /// 
        /// </summary>
        public decimal delta
        {
            get;
            set;
        }

        /// <summary>
        /// 
        /// </summary>
        public decimal best_bid_price
        {
            get; set;
        }

        /// <summary>
        /// 
        /// </summary>
        public decimal best_bid_amount
        {
            get; set;
        }

        /// <summary>
        /// 
        /// </summary>
        public decimal best_ask_price
        {
            get; set;
        }

        /// <summary>
        /// 
        /// </summary>
        public decimal best_ask_amount
        {
            get; set;
        }
    }
}