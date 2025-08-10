using System.Text.Json;
using System.Text.Json.Serialization;

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
        string table
        {
            get;
            set;
        }

        /// <summary>
        ///
        /// </summary>
        string action
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
        public string table
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
    }

    /// <summary>
    ///
    /// </summary>
    public interface IWsData : IWsResult
    {
        /// <summary>
        /// Dynamic data as JsonElement
        /// </summary>
        JsonElement data
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
        /// Dynamic data as JsonElement
        /// </summary>
        public JsonElement data
        {
            get;
            set;
        }

        /// <summary>
        /// Helper method to get value from dynamic data
        /// </summary>
        public T GetValue<T>(string path)
        {
            var pathParts = path.Split('.');
            var current = data;

            foreach (var part in pathParts)
            {
                if (current.ValueKind == JsonValueKind.Object)
                {
                    if (!current.TryGetProperty(part, out current))
                    {
                        return default(T);
                    }
                }
                else if (current.ValueKind == JsonValueKind.Array && int.TryParse(part, out var index))
                {
                    if (index >= 0 && index < current.GetArrayLength())
                    {
                        int i = 0;
                        foreach (var item in current.EnumerateArray())
                        {
                            if (i == index)
                            {
                                current = item;
                                break;
                            }
                            i++;
                        }
                    }
                    else
                    {
                        return default(T);
                    }
                }
                else
                {
                    return default(T);
                }
            }

            return current.Deserialize<T>();
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
    public class WsResult<T> : WsResult, IWsResult<T>
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
        string id
        {
            get;
            set;
        }

        /// <summary>
        ///
        /// </summary>
        string topic
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
        public string id
        {
            get;
            set;
        }

        /// <summary>
        ///
        /// </summary>
        public string topic
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