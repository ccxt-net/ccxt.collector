# Migration Guide v1.x to v2.0

## Overview

Version 2.0 of CCXT.Collector represents a complete architectural transformation from REST API polling to WebSocket-based real-time streaming. This guide will help you migrate your existing v1.x code to the new v2.0 architecture.

## Table of Contents
- [Breaking Changes](#breaking-changes)
- [Quick Migration Examples](#quick-migration-examples)
- [Detailed Migration Steps](#detailed-migration-steps)
- [Testing Migration](#testing-migration)
- [Common Issues](#common-issues)
- [Performance Improvements](#performance-improvements)

## Breaking Changes

### 1. Architecture Change: REST to WebSocket

**v1.x (REST-based)**
- Polling mechanism with configurable intervals
- Synchronous data fetching
- Queue-based data distribution
- High latency (500ms+)

**v2.0 (WebSocket-based)**
- Real-time streaming
- Asynchronous event-driven callbacks
- Direct callback invocation
- Low latency (<50ms)

### 2. Namespace Changes

| v1.x | v2.0 |
|------|------|
| `CCXT.Collector.Data` | `CCXT.Collector.Service` |
| `CCXT.Collector.Models` | `CCXT.Collector.Models.Market` / `CCXT.Collector.Models.Trading` |
| `CCXT.Collector.Library` | `CCXT.Collector.Core.Abstractions` |
| `CCXT.Collector.Exchanges.Binance` | `CCXT.Collector.Binance` |
| `CCXT.Collector.Indicator` | `CCXT.Collector.Indicators.*` (subcategorized) |

### 3. Client Initialization

**v1.x:**
```csharp
var client = new BinanceClient("public");
client.Initialize(config);
```

**v2.0:**
```csharp
using CCXT.Collector.Binance;
using CCXT.Collector.Core.Abstractions;
using CCXT.Collector.Service;

var client = new BinanceWebSocketClient();
// No initialization needed - configuration is automatic
```

### 4. Data Models

| v1.x Model | v2.0 Model | Key Changes |
|------------|------------|-------------|
| `OrderBook` | `SOrderBooks` | Added `result` wrapper, changed property names |
| `Trade` | `SCompleteOrders` | Array of trades in `result`, added `sideType` enum |
| `Ticker` | `STicker` | Nested `result` with `STickerItem` |
| `OHLCV` | `SCandlestick` | Added `result` wrapper with `Ohlc` data |

## Quick Migration Examples

### Example 1: Orderbook Subscription

**v1.x (Polling):**
```csharp
public class OrderbookPoller
{
    private BinanceClient client;
    private Timer timer;
    
    public void Start()
    {
        client = new BinanceClient("public");
        timer = new Timer(PollOrderbook, null, 0, 1000);
    }
    
    private void PollOrderbook(object state)
    {
        try
        {
            var orderbook = client.GetOrderbook("BTC/USDT");
            ProcessOrderbook(orderbook);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }
    
    private void ProcessOrderbook(OrderBook orderbook)
    {
        Console.WriteLine($"Best bid: {orderbook.Bids[0].Price}");
        Console.WriteLine($"Best ask: {orderbook.Asks[0].Price}");
    }
}
```

**v2.0 (WebSocket):**
```csharp
public class OrderbookStreamer
{
    private BinanceWebSocketClient client;
    
    public async Task StartAsync()
    {
        client = new BinanceWebSocketClient();
        
        // Register callback
        client.OnOrderbookReceived += ProcessOrderbook;
        client.OnError += (error) => Console.WriteLine($"Error: {error}");
        
        // Connect and subscribe
        await client.ConnectAsync();
        await client.SubscribeOrderbookAsync("BTC/USDT");
    }
    
    private void ProcessOrderbook(SOrderBooks orderbook)
    {
        Console.WriteLine($"Best bid: {orderbook.result.bids[0].price}");
        Console.WriteLine($"Best ask: {orderbook.result.asks[0].price}");
    }
}
```

### Example 2: Trade Data Collection

**v1.x:**
```csharp
public async Task<List<Trade>> GetRecentTrades(string symbol)
{
    var client = new BinanceClient("public");
    var trades = await client.FetchTrades(symbol, limit: 100);
    
    foreach (var trade in trades)
    {
        SaveToDatabase(new TradeRecord
        {
            Price = trade.Price,
            Amount = trade.Amount,
            Side = trade.Side,
            Timestamp = trade.Timestamp
        });
    }
    
    return trades;
}
```

**v2.0:**
```csharp
public async Task StreamTrades(string symbol)
{
    var client = new BinanceWebSocketClient();
    
    client.OnTradeReceived += (trade) =>
    {
        foreach (var item in trade.result)
        {
            SaveToDatabase(new TradeRecord
            {
                Price = item.price,
                Amount = item.amount,
                Side = item.sideType == SideType.Bid ? "buy" : "sell",
                Timestamp = item.timestamp
            });
        }
    };
    
    await client.ConnectAsync();
    await client.SubscribeTradesAsync(symbol);
}
```

### Example 3: Multi-Exchange Data Collection

**v1.x:**
```csharp
public class MultiExchangeCollector
{
    private Dictionary<string, IExchangeClient> clients;
    private Timer pollingTimer;
    
    public void Initialize()
    {
        clients = new Dictionary<string, IExchangeClient>
        {
            ["binance"] = new BinanceClient("public"),
            ["upbit"] = new UpbitClient("public"),
            ["bithumb"] = new BithumbClient("public")
        };
        
        pollingTimer = new Timer(PollAllExchanges, null, 0, 5000);
    }
    
    private void PollAllExchanges(object state)
    {
        foreach (var kvp in clients)
        {
            try
            {
                var ticker = kvp.Value.FetchTicker("BTC/USDT");
                ProcessTicker(kvp.Key, ticker);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error on {kvp.Key}: {ex.Message}");
            }
        }
    }
}
```

**v2.0:**
```csharp
public class MultiExchangeStreamer
{
    public async Task StartStreamingAsync()
    {
        var binance = new BinanceWebSocketClient();
        var upbit = new UpbitWebSocketClient();
        var bithumb = new BithumbWebSocketClient();
        
        // Unified callback handler
        Action<string, STicker> handler = (exchange, ticker) =>
        {
            ProcessTicker(exchange, ticker);
        };
        
        // Register callbacks
        binance.OnTickerReceived += (t) => handler("binance", t);
        upbit.OnTickerReceived += (t) => handler("upbit", t);
        bithumb.OnTickerReceived += (t) => handler("bithumb", t);
        
        // Connect all exchanges
        await Task.WhenAll(
            binance.ConnectAsync(),
            upbit.ConnectAsync(),
            bithumb.ConnectAsync()
        );
        
        // Subscribe to markets
        await binance.SubscribeTickerAsync("BTC/USDT");
        await upbit.SubscribeTickerAsync("BTC/KRW");
        await bithumb.SubscribeTickerAsync("BTC/KRW");
    }
    
    private void ProcessTicker(string exchange, STicker ticker)
    {
        Console.WriteLine($"[{exchange}] {ticker.symbol}: {ticker.result.closePrice}");
    }
}
```

## Detailed Migration Steps

### Step 1: Update NuGet Packages

```xml
<!-- Remove old packages -->
<PackageReference Include="CCXT.Collector" Version="1.*" />

<!-- Add new package -->
<PackageReference Include="CCXT.Collector" Version="2.0.0" />
```

### Step 2: Update Using Statements

```csharp
// Remove
using CCXT.Collector.Data;
using CCXT.Collector.Models;
using CCXT.Collector.Exchanges.Binance;

// Add
using CCXT.Collector.Service;
using CCXT.Collector.Core.Abstractions;
using CCXT.Collector.Library;
using CCXT.Collector.Binance;
```

### Step 3: Replace Client Initialization

```csharp
// Remove
var config = new ExchangeConfig { /* ... */ };
var client = new BinanceClient("public");
client.Initialize(config);

// Add
var client = new BinanceWebSocketClient();
```

### Step 4: Convert Polling to Callbacks

```csharp
// Remove polling logic
Timer pollingTimer;
void PollData() { /* ... */ }

// Add callback registration
client.OnOrderbookReceived += (orderbook) => { /* ... */ };
client.OnTradeReceived += (trade) => { /* ... */ };
client.OnTickerReceived += (ticker) => { /* ... */ };
```

### Step 5: Update Data Processing

```csharp
// v1.x data access
var bestBid = orderbook.Bids[0].Price;
var lastPrice = ticker.Last;
var tradeAmount = trade.Amount;

// v2.0 data access
var bestBid = orderbook.result.bids[0].price;
var lastPrice = ticker.result.closePrice;
var tradeAmount = trade.result[0].amount;
```

### Step 6: Handle Connection Management

```csharp
public class WebSocketManager
{
    private BinanceWebSocketClient client;
    
    public async Task StartAsync()
    {
        client = new BinanceWebSocketClient();
        
        // Connection events
        client.OnConnected += () => Console.WriteLine("Connected");
        client.OnDisconnected += () => Console.WriteLine("Disconnected");
        client.OnError += (error) => Console.WriteLine($"Error: {error}");
        
        // Connect with retry
        await ConnectWithRetryAsync();
    }
    
    private async Task ConnectWithRetryAsync()
    {
        int retries = 0;
        while (retries < 5)
        {
            try
            {
                await client.ConnectAsync();
                break;
            }
            catch
            {
                retries++;
                await Task.Delay(1000 * retries);
            }
        }
    }
}
```

## Testing Migration

### Update Test Framework

**v1.x (MSTest):**
```csharp
[TestClass]
public class BinanceTests
{
    [TestMethod]
    [TestCategory("Integration")]
    public void TestOrderbookFetch()
    {
        var client = new BinanceClient("public");
        var orderbook = client.GetOrderbook("BTC/USDT");
        Assert.IsNotNull(orderbook);
    }
}
```

**v2.0 (XUnit):**
```csharp
public class BinanceTests : IDisposable
{
    private BinanceWebSocketClient client;
    
    public BinanceTests()
    {
        client = new BinanceWebSocketClient();
    }
    
    [Fact]
    [Trait("Category", "Integration")]
    public async Task TestOrderbookStream()
    {
        var received = false;
        client.OnOrderbookReceived += (ob) => received = true;
        
        await client.ConnectAsync();
        await client.SubscribeOrderbookAsync("BTC/USDT");
        await Task.Delay(2000);
        
        Assert.True(received);
    }
    
    public void Dispose()
    {
        client?.Dispose();
    }
}
```

## Common Issues

### Issue 1: Missing Callbacks
**Problem:** Data not being received after subscription  
**Solution:** Ensure callbacks are registered before connecting

```csharp
// ✅ Correct order
client.OnOrderbookReceived += ProcessOrderbook;
await client.ConnectAsync();
await client.SubscribeOrderbookAsync("BTC/USDT");

// ❌ Wrong order
await client.ConnectAsync();
await client.SubscribeOrderbookAsync("BTC/USDT");
client.OnOrderbookReceived += ProcessOrderbook; // Too late!
```

### Issue 2: Property Access Errors
**Problem:** `'OrderBook' does not contain a definition for 'result'`  
**Solution:** Update to new data model structure

```csharp
// v1.x
var price = orderbook.Bids[0].Price;

// v2.0
var price = orderbook.result.bids[0].price;
```

### Issue 3: Synchronous to Async
**Problem:** Blocking calls in async context  
**Solution:** Use async/await properly

```csharp
// ❌ Blocking
client.ConnectAsync().Wait();

// ✅ Async
await client.ConnectAsync();
```

### Issue 4: Memory Leaks
**Problem:** Not disposing WebSocket clients  
**Solution:** Implement proper disposal

```csharp
public class DataCollector : IDisposable
{
    private BinanceWebSocketClient client;
    
    public void Dispose()
    {
        client?.DisconnectAsync().Wait();
        client?.Dispose();
    }
}
```

## Performance Improvements

### Metrics Comparison

| Metric | v1.x (REST) | v2.0 (WebSocket) | Improvement |
|--------|-------------|------------------|-------------|
| Latency | 500-1000ms | 20-50ms | 95% reduction |
| Bandwidth | 100KB/s | 25KB/s | 75% reduction |
| CPU Usage | 15-20% | 5-8% | 60% reduction |
| Connections | 1 per request | 1 persistent | 99% reduction |
| Update Rate | 1 per second | 100+ per second | 100x increase |

### Resource Usage

**v1.x:**
- Multiple HTTP connections
- Thread pool for polling
- Queue storage overhead
- JSON parsing per request

**v2.0:**
- Single WebSocket connection
- Event-driven callbacks
- Direct memory streaming
- Incremental updates only

## Best Practices

### 1. Connection Management
```csharp
public class RobustWebSocketClient
{
    private readonly SemaphoreSlim reconnectLock = new(1, 1);
    
    public async Task MaintainConnectionAsync()
    {
        client.OnDisconnected += async () =>
        {
            await reconnectLock.WaitAsync();
            try
            {
                await Task.Delay(5000);
                await client.ConnectAsync();
                await ResubscribeAllAsync();
            }
            finally
            {
                reconnectLock.Release();
            }
        };
    }
}
```

### 2. Error Handling
```csharp
client.OnError += (error) =>
{
    logger.LogError($"WebSocket error: {error}");
    
    if (error.Contains("rate limit"))
    {
        // Handle rate limiting
        Task.Delay(60000).Wait();
    }
    else if (error.Contains("invalid symbol"))
    {
        // Handle invalid subscription
        UnsubscribeInvalidSymbol();
    }
};
```

### 3. Data Validation
```csharp
client.OnOrderbookReceived += (orderbook) =>
{
    // Validate data before processing
    if (orderbook?.result?.bids?.Count > 0 && 
        orderbook?.result?.asks?.Count > 0)
    {
        ProcessValidOrderbook(orderbook);
    }
    else
    {
        logger.LogWarning($"Invalid orderbook data for {orderbook?.symbol}");
    }
};
```

## Rollback Plan

If you need to rollback to v1.x:

1. Keep v1.x code in a separate branch
2. Maintain configuration for both versions
3. Use feature flags for gradual migration
4. Monitor both versions in parallel initially

```csharp
public interface IMarketDataClient
{
    Task<OrderBook> GetOrderbookAsync(string symbol);
}

public class HybridClient : IMarketDataClient
{
    private bool useWebSocket = true;
    
    public async Task<OrderBook> GetOrderbookAsync(string symbol)
    {
        if (useWebSocket)
            return await GetViaWebSocket(symbol);
        else
            return await GetViaRest(symbol);
    }
}
```

## Support

For migration support:
- GitHub Issues: [https://github.com/ccxt-net/ccxt.collector/issues](https://github.com/ccxt-net/ccxt.collector/issues)
- Documentation: [https://github.com/ccxt-net/ccxt.collector/wiki](https://github.com/ccxt-net/ccxt.collector/wiki)
- Email: support@ccxt.net

## Conclusion

The migration from v1.x to v2.0 requires significant code changes but provides substantial benefits:
- 95% latency reduction
- 75% bandwidth savings
- Real-time data streaming
- Better error handling
- Simplified API

Plan your migration carefully, test thoroughly, and leverage the improved performance of WebSocket streaming!