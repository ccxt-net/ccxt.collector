using Newtonsoft.Json.Linq;

namespace CCXT.Collector.Library
{
    /// <summary>
    ///
    /// </summary>
    public interface IWsResult
    {
        /// <summary>
        ///
        /// </summary>
        string? table
        {
            get;
            set;
        }

        /// <summary>
        ///
        /// </summary>
        string? action
        {
            get;
            set;
        }
    }

    /// <summary>
    ///
    /// </summary>
    public class WsResult : IWsResult
    {
        /// <summary>
        ///
        /// </summary>
        public string? table
        {
            get;
            set;
        }

        /// <summary>
        ///
        /// </summary>
        public string? action
        {
            get;
            set;
        }
    }

    /// <summary>
    ///
    /// </summary>
    public interface IWsData : IWsResult
    {
        /// <summary>
        ///
        /// </summary>
        JToken data
        {
            get;
            set;
        }
    }

    /// <summary>
    ///
    /// </summary>
    public class WsData : WsResult, IWsData
    {
        /// <summary>
        ///
        /// </summary>
        public JToken data
        {
            get;
            set;
        }
    }

    /// <summary>
    ///
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IWsResult<T> : IWsResult
    {
        /// <summary>
        ///
        /// </summary>
        T data
        {
            get;
            set;
        }
    }

    /// <summary>
    ///
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class WsResult<T> : WsResult, IWsResult
    {
        /// <summary>
        ///
        /// </summary>
        public T data
        {
            get;
            set;
        }
    }

    /// <summary>
    ///
    /// </summary>
    public interface IMxResult
    {
        /// <summary>
        ///
        /// </summary>
        int type
        {
            get;
            set;
        }

        /// <summary>
        ///
        /// </summary>
        string? id
        {
            get;
            set;
        }

        /// <summary>
        ///
        /// </summary>
        string? topic
        {
            get;
            set;
        }

        /// <summary>
        ///
        /// </summary>
        WsData payload
        {
            get;
            set;
        }
    }

    /// <summary>
    ///
    /// </summary>
    public class MxResult : IMxResult
    {
        /// <summary>
        ///
        /// </summary>
        public int type
        {
            get;
            set;
        }

        /// <summary>
        ///
        /// </summary>
        public string? id
        {
            get;
            set;
        }

        /// <summary>
        ///
        /// </summary>
        public string? topic
        {
            get;
            set;
        }

        /// <summary>
        ///
        /// </summary>
        public WsData payload
        {
            get;
            set;
        }
    }
}