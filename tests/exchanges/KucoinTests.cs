using System;
using System.Threading.Tasks;
using CCXT.Collector.Kucoin;
using CCXT.Collector.Core.Abstractions;
using CCXT.Collector.Tests.Base;
using Xunit;
using Xunit.Abstractions;

namespace CCXT.Collector.Tests.Exchanges
{
    /// <summary>
    /// Test suite for Kucoin exchange WebSocket integration
    /// </summary>
    [Collection("Exchange Tests")]
    [Trait("Category", "Exchange")]
    [Trait("Exchange", "Kucoin")]
    [Trait("Status", "Complete")]
    public class KucoinTests : WebSocketTestBase
    {
        private readonly ExchangeTestFixture _fixture;

        public KucoinTests(ITestOutputHelper output, ExchangeTestFixture fixture) 
            : base(output, "Kucoin")
        {
            _fixture = fixture;
            _testSymbols.Clear();
            _testSymbols.AddRange(_fixture.GetTestSymbols("Kucoin"));
        }

        protected override IWebSocketClient CreateClient()
        {
            return new KucoinWebSocketClient();
        }

        protected override async Task<bool> ConnectClientAsync(IWebSocketClient client)
        {
            await client.ConnectAsync();
            return true;
        }

        #region Test Methods

        [Fact]
        [Trait("Type", "Connection")]
        public async Task Kucoin_WebSocket_Connection()
        {
            await TestWebSocketConnection();
            _fixture.MarkExchangeTested("Kucoin", true);
        }

        [Fact]
        [Trait("Type", "DataStream")]
        public async Task Kucoin_Orderbook_Stream()
        {
            await TestOrderbookDataReception();
        }

        [Fact]
        [Trait("Type", "DataStream")]
        public async Task Kucoin_Trade_Stream()
        {
            await TestTradeDataReception();
        }

        [Fact]
        [Trait("Type", "DataStream")]
        public async Task Kucoin_Ticker_Stream()
        {
            await TestTickerDataReception();
        }

        [Fact]
        [Trait("Type", "MultipleSubscriptions")]
        public async Task Kucoin_Multiple_Subscriptions()
        {
            await TestMultipleSubscriptions();
        }

        #endregion
    }
}