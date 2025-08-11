using System;
using System.Threading.Tasks;
using CCXT.Collector.Bitget;
using CCXT.Collector.Core.Abstractions;
using CCXT.Collector.Tests.Base;
using Xunit;
using Xunit.Abstractions;

namespace CCXT.Collector.Tests.Exchanges
{
    /// <summary>
    /// Test suite for Bitget exchange WebSocket integration
    /// </summary>
    [Collection("Exchange Tests")]
    [Trait("Category", "Exchange")]
    [Trait("Exchange", "Bitget")]
    public class BitgetTests : WebSocketTestBase
    {
        private readonly ExchangeTestFixture _fixture;

        public BitgetTests(ITestOutputHelper output, ExchangeTestFixture fixture) 
            : base(output, "Bitget")
        {
            _fixture = fixture;
            _testSymbols.Clear();
            _testSymbols.AddRange(_fixture.GetTestSymbols("Bitget"));
        }

        protected override IWebSocketClient CreateClient()
        {
            return new BitgetWebSocketClient();
        }

        protected override async Task<bool> ConnectClientAsync(IWebSocketClient client)
        {
            await client.ConnectAsync();
            return true;
        }

        #region Test Methods

        [Fact]
        [Trait("Type", "Connection")]
        public async Task Bitget_WebSocket_Connection()
        {
            await TestWebSocketConnection();
            _fixture.MarkExchangeTested("Bitget", true);
        }

        [Fact]
        [Trait("Type", "DataStream")]
        public async Task Bitget_Orderbook_Stream()
        {
            await TestOrderbookDataReception();
        }

        [Fact]
        [Trait("Type", "DataStream")]
        public async Task Bitget_Trade_Stream()
        {
            await TestTradeDataReception();
        }

        [Fact]
        [Trait("Type", "DataStream")]
        public async Task Bitget_Ticker_Stream()
        {
            await TestTickerDataReception();
        }

        [Fact]
        [Trait("Type", "MultipleSubscriptions")]
        public async Task Bitget_Multiple_Subscriptions()
        {
            await TestMultipleSubscriptions();
        }

        #endregion
    }
}