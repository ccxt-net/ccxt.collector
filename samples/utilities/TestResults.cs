namespace CCXT.Collector.Samples.Utilities
{
    /// <summary>
    /// Basic test result for exchange connectivity and data reception
    /// </summary>
    public class TestResult
    {
        public string Name { get; set; } = string.Empty;
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public bool OrderbookReceived { get; set; }
        public bool TradesReceived { get; set; }
        public bool TickerReceived { get; set; }
    }

    /// <summary>
    /// Test result for batch subscription functionality
    /// </summary>
    public class BatchTestResult
    {
        public string Name { get; set; } = string.Empty;
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int DataReceived { get; set; }
    }

    /// <summary>
    /// Test result for multi-market data reception
    /// </summary>
    public class MultiMarketTestResult
    {
        public string Name { get; set; } = string.Empty;
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public Dictionary<string, int> MarketData { get; set; } = new Dictionary<string, int>();
    }
}