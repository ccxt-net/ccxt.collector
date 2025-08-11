using System;
using System.Threading.Tasks;
using CCXT.Collector.Bybit;
using CCXT.Collector.Core.Abstractions;
using CCXT.Collector.Tests.Base;
using Xunit;
using Xunit.Abstractions;

namespace CCXT.Collector.Tests.Exchanges
{
    /// <summary>
    /// Test suite for Bybit exchange WebSocket integration
    /// </summary>
    [Collection("Exchange Tests")]
    [Trait("Category", "Exchange")]
    [Trait("Exchange", "Bybit")]
    public class BybitTests : WebSocketTestBase
    {
        private readonly ExchangeTestFixture _fixture;

        public BybitTests(ITestOutputHelper output, ExchangeTestFixture fixture) 
            : base(output, "Bybit")
        {
            _fixture = fixture;
            _testSymbols.Clear();
            _testSymbols.AddRange(_fixture.GetTestSymbols("Bybit"));
        }

        protected override IWebSocketClient CreateClient()
        {
            return new BybitWebSocketClient();
        }

        protected override async Task<bool> ConnectClientAsync(IWebSocketClient client)
        {
            await client.ConnectAsync();
            return true;
        }

        #region Test Methods

        [Fact]
        [Trait("Type", "Connection")]
        public async Task Bybit_WebSocket_Connection()
        {
            await TestWebSocketConnection();
            _fixture.MarkExchangeTested("Bybit", true);
        }

        [Fact]
        [Trait("Type", "DataStream")]
        public async Task Bybit_Orderbook_Stream()
        {
            await TestOrderbookDataReception();
        }

        [Fact]
        [Trait("Type", "DataStream")]
        public async Task Bybit_Trade_Stream()
        {
            await TestTradeDataReception();
        }

        [Fact]
        [Trait("Type", "DataStream")]
        public async Task Bybit_Ticker_Stream()
        {
            await TestTickerDataReception();
        }

        [Fact]
        [Trait("Type", "MultipleSubscriptions")]
        public async Task Bybit_Multiple_Subscriptions()
        {
            await TestMultipleSubscriptions();
        }

        #endregion
    }
}