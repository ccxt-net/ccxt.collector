using System;
using System.Threading.Tasks;
using CCXT.Collector.Coinone;
using CCXT.Collector.Core.Abstractions;
using CCXT.Collector.Tests.Base;
using Xunit;
using Xunit.Abstractions;

namespace CCXT.Collector.Tests.Exchanges
{
    /// <summary>
    /// Test suite for Coinone exchange WebSocket integration
    /// </summary>
    [Collection("Exchange Tests")]
    [Trait("Category", "Exchange")]
    [Trait("Exchange", "Coinone")]
    [Trait("Region", "Korea")]
    public class CoinoneTests : WebSocketTestBase
    {
        private readonly ExchangeTestFixture _fixture;

        public CoinoneTests(ITestOutputHelper output, ExchangeTestFixture fixture) 
            : base(output, "Coinone")
        {
            _fixture = fixture;
            _testSymbols.Clear();
            _testSymbols.AddRange(_fixture.GetTestSymbols("Coinone"));
        }

        protected override IWebSocketClient CreateClient()
        {
            return new CoinoneWebSocketClient();
        }

        protected override async Task<bool> ConnectClientAsync(IWebSocketClient client)
        {
            await client.ConnectAsync();
            return true;
        }

        #region Test Methods

        [Fact]
        [Trait("Type", "Connection")]
        public async Task Coinone_WebSocket_Connection()
        {
            await TestWebSocketConnection();
            _fixture.MarkExchangeTested("Coinone", true);
        }

        [Fact]
        [Trait("Type", "DataStream")]
        public async Task Coinone_Orderbook_Stream()
        {
            await TestOrderbookDataReception();
        }

        [Fact]
        [Trait("Type", "DataStream")]
        public async Task Coinone_Trade_Stream()
        {
            await TestTradeDataReception();
        }

        [Fact]
        [Trait("Type", "DataStream")]
        public async Task Coinone_Ticker_Stream()
        {
            await TestTickerDataReception();
        }

        [Fact]
        [Trait("Type", "MultipleSubscriptions")]
        public async Task Coinone_Multiple_Subscriptions()
        {
            await TestMultipleSubscriptions();
        }

        #endregion
    }
}