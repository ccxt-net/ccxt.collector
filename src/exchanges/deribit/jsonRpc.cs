namespace CCXT.Collector.Deribit
{

    public class JsonRpc
    {
        public string jsonrpc
        {
            get; set;
        }

        public string method
        {
            get; set;
        }
    }

    public class JsonRpcRequest : JsonRpc
    {
        public int id
        {
            get; set;
        }

        public object @params
        {
            get; set;
        }
    }

    public class JsonRpcData<T>
    {
        /// <summary>
        ///
        /// </summary>
        public string channel
        {
            get;
            set;
        }

        public T data
        {
            get; set;
        }
    }

    public class JsonRpcResponse<T> : JsonRpc
    {
        /// <summary>
        ///
        /// </summary>
        public JsonRpcData<T> @params
        {
            get;
            set;
        }
    }
}