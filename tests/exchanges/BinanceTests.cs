using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CCXT.Collector.Binance;
using CCXT.Collector.Core.Abstractions;
using CCXT.Collector.Tests.Base;
using Xunit;
using Xunit.Abstractions;

namespace CCXT.Collector.Tests.Exchanges
{
    /// <summary>
    /// Test suite for Binance exchange WebSocket integration
    /// </summary>
    [Collection("Exchange Tests")]
    [Trait("Category", "Exchange")]
    [Trait("Exchange", "Binance")]
    public class BinanceTests : WebSocketTestBase
    {
        private readonly ExchangeTestFixture _fixture;

        public BinanceTests(ITestOutputHelper output, ExchangeTestFixture fixture) 
            : base(output, "Binance")
        {
            _fixture = fixture;
            _testSymbols.Clear();
            _testSymbols.AddRange(_fixture.GetTestSymbols("Binance"));
        }

        protected override IWebSocketClient CreateClient()
        {
            return new BinanceWebSocketClient();
        }

        protected override async Task<bool> ConnectClientAsync(IWebSocketClient client)
        {
            await client.ConnectAsync();
            return true;
        }

        #region Test Methods

        [Fact]
        [Trait("Type", "Connection")]
        public async Task Binance_WebSocket_Connection()
        {
            await TestWebSocketConnection();
            _fixture.MarkExchangeTested("Binance", true);
        }

        [Fact]
        [Trait("Type", "DataStream")]
        public async Task Binance_Orderbook_Stream()
        {
            await TestOrderbookDataReception();
        }

        [Fact]
        [Trait("Type", "DataStream")]
        public async Task Binance_Trade_Stream()
        {
            await TestTradeDataReception();
        }

        [Fact]
        [Trait("Type", "DataStream")]
        public async Task Binance_Ticker_Stream()
        {
            await TestTickerDataReception();
        }

        [Fact]
        [Trait("Type", "MultipleSubscriptions")]
        public async Task Binance_Multiple_Subscriptions()
        {
            await TestMultipleSubscriptions();
        }

        [Fact]
        [Trait("Type", "ComprehensiveTest")]
        public async Task Binance_Comprehensive_MultiMarket_MultiChannel()
        {
            await TestComprehensiveSubscriptions();
        }

        #endregion
        
        #region Binance Specific Configuration
        
        protected override List<string> GetComprehensiveTestSymbols()
        {
            // Binance specific popular trading pairs
            return new List<string> 
            { 
                "BTC/USDT", "ETH/USDT", "BNB/USDT", 
                "XRP/USDT", "SOL/USDT", "DOGE/USDT",
                "ADA/USDT", "AVAX/USDT", "DOT/USDT",
                "MATIC/USDT", "LINK/USDT", "UNI/USDT"
            };
        }
        
        #endregion
    }
}