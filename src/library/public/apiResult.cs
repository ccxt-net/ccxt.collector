using Newtonsoft.Json;

namespace CCXT.Collector.Library.Public
{
    /// <summary>
    /// api call result class
    /// </summary>
    public class SApiResult
    {
        /// <summary>
        /// api call result class
        /// </summary>
        public SApiResult(bool success = false)
        {
            if (success == true)
                this.SetSuccess();
            else
                this.SetFailure();
        }

        /// <summary>
        /// is success calling
        /// </summary>
        [JsonIgnore]
        public virtual bool success
        {
            get;
            set;
        }

        /// <summary>
        /// error or success message
        /// </summary>
        [JsonIgnore]
        public virtual string message
        {
            get;
            set;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="result"></param>
        /// <returns></returns>
        public void SetResult(SApiResult result)
        {
            this.message = result.message;
            this.success = result.success;
        }

        /// <summary>
        ///
        /// </summary>
        public void SetSuccess(string message = "success", bool success = true)
        {
            this.message = message;
            this.success = success;
        }

        /// <summary>
        ///
        /// </summary>
        public void SetFailure(string message = "failure", bool success = false)
        {
            this.message = message;
            this.success = success;
        }
    }

    public class SApiResult<T> : SApiResult
    {
        /// <summary>
        ///
        /// </summary>
        public string exchange
        {
            get;
            set;
        }

        /// <summary>
        /// S, R
        /// </summary>
        public string stream
        {
            get;
            set;
        }

        /// <summary>
        /// string symbol of the market ('BTCUSD', 'ETHBTC', ...)
        /// </summary>
        public string symbol
        {
            get;
            set;
        }

        /// <summary>
        /// 
        /// </summary>
        public string action
        {
            get;
            set;
        }

        /// <summary>
        ///
        /// </summary>
        public long sequentialId
        {
            get;
            set;
        }

        /// <summary>
        ///
        /// </summary>
        public virtual T result
        {
            get;
            set;
        }
    }
}