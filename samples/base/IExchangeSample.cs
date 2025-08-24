using System;
using System.Threading.Tasks;

namespace CCXT.Collector.Samples.Base
{
    /// <summary>
    /// Interface for exchange sample implementations
    /// </summary>
    public interface IExchangeSample
    {
        /// <summary>
        /// Exchange name
        /// </summary>
        string ExchangeName { get; }

        /// <summary>
        /// Run the sample demonstration for this exchange
        /// </summary>
        Task SampleRun();
    }
}