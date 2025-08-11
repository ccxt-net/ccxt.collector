using System;
using System.Threading.Tasks;
using CCXT.Collector.Okx;
using CCXT.Collector.Core.Abstractions;
using CCXT.Collector.Tests.Base;
using Xunit;
using Xunit.Abstractions;

namespace CCXT.Collector.Tests.Exchanges
{
    /// <summary>
    /// Test suite for OKX exchange WebSocket integration
    /// </summary>
    [Collection("Exchange Tests")]
    [Trait("Category", "Exchange")]
    [Trait("Exchange", "OKX")]
    public class OkxTests : WebSocketTestBase
    {
        private readonly ExchangeTestFixture _fixture;

        public OkxTests(ITestOutputHelper output, ExchangeTestFixture fixture) 
            : base(output, "OKX")
        {
            _fixture = fixture;
            _testSymbols.Clear();
            _testSymbols.AddRange(_fixture.GetTestSymbols("OKX"));
        }

        protected override IWebSocketClient CreateClient()
        {
            return new OkxWebSocketClient();
        }

        protected override async Task<bool> ConnectClientAsync(IWebSocketClient client)
        {
            await client.ConnectAsync();
            return true;
        }

        #region Test Methods

        [Fact]
        [Trait("Type", "Connection")]
        public async Task Okx_WebSocket_Connection()
        {
            await TestWebSocketConnection();
            _fixture.MarkExchangeTested("OKX", true);
        }

        [Fact]
        [Trait("Type", "DataStream")]
        public async Task Okx_Orderbook_Stream()
        {
            await TestOrderbookDataReception();
        }

        [Fact]
        [Trait("Type", "DataStream")]
        public async Task Okx_Trade_Stream()
        {
            await TestTradeDataReception();
        }

        [Fact]
        [Trait("Type", "DataStream")]
        public async Task Okx_Ticker_Stream()
        {
            await TestTickerDataReception();
        }

        [Fact]
        [Trait("Type", "MultipleSubscriptions")]
        public async Task Okx_Multiple_Subscriptions()
        {
            await TestMultipleSubscriptions();
        }

        #endregion
    }
}