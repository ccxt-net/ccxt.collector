using System;
using System.Threading.Tasks;
using CCXT.Collector.Bittrex;
using CCXT.Collector.Core.Abstractions;
using CCXT.Collector.Tests.Base;
using Xunit;
using Xunit.Abstractions;

namespace CCXT.Collector.Tests.Exchanges
{
    /// <summary>
    /// Test suite for Bittrex exchange WebSocket integration
    /// Note: Bittrex closed on December 4, 2023
    /// </summary>
    [Collection("Exchange Tests")]
    [Trait("Category", "Exchange")]
    [Trait("Exchange", "Bittrex")]
    [Trait("Status", "Closed")]
    public class BittrexTests : WebSocketTestBase
    {
        private readonly ExchangeTestFixture _fixture;

        public BittrexTests(ITestOutputHelper output, ExchangeTestFixture fixture) 
            : base(output, "Bittrex")
        {
            _fixture = fixture;
            _testSymbols.Clear();
            _testSymbols.AddRange(_fixture.GetTestSymbols("Bittrex"));
        }

        protected override IWebSocketClient CreateClient()
        {
            return new BittrexWebSocketClient();
        }

        protected override async Task<bool> ConnectClientAsync(IWebSocketClient client)
        {
            // Bittrex is closed, so we expect connection to fail
            try
            {
                await client.ConnectAsync();
                return false; // Should not reach here
            }
            catch (Exception ex)
            {
                _output.WriteLine($"Expected failure for closed exchange: {ex.Message}");
                return false;
            }
        }

        #region Test Methods

        [Fact(Skip = "Bittrex closed on December 4, 2023")]
        [Trait("Type", "Connection")]
        public async Task Bittrex_WebSocket_Connection_Should_Fail()
        {
            _output.WriteLine("\n[Bittrex] Testing Closed Exchange Handling");
            _output.WriteLine("----------------------------------------");
            
            using var client = CreateClient();
            
            // Attempting to connect should throw an exception or return error
            await Assert.ThrowsAnyAsync<Exception>(async () =>
            {
                await client.ConnectAsync();
            });
            
            _output.WriteLine("✅ Correctly blocked connection to closed exchange");
            _fixture.MarkExchangeTested("Bittrex", true);
        }

        #endregion
    }
}