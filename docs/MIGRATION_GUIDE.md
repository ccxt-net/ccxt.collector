# Migration Guide - CCXT.Collector v2.0

## Overview

This guide helps you migrate from CCXT.Collector v1.x to v2.0, which introduces a complete WebSocket-based architecture supporting 132 cryptocurrency exchanges.

## Major Changes in v2.0

### 1. Architecture Transformation
- **From**: REST API polling
- **To**: WebSocket real-time streaming
- **Impact**: Complete change in data reception model

### 2. Exchange Support Expansion
- **From**: 7 exchanges
- **To**: 132 exchanges across 22 countries/regions

### 3. Folder Structure Reorganization
- **From**: Flat structure with mixed namespaces
- **To**: Organized by namespace and country code

## Breaking Changes

### Namespace Changes

| Old Namespace | New Namespace | Description |
|--------------|---------------|-------------|
| `CCXT.Collector.Data` | `CCXT.Collector.Service` | Data models and structures |
| `CCXT.Collector.Models` | `CCXT.Collector.Library` | Core library components |
| `CCXT.Collector.{Exchange}` | `CCXT.Collector.{Exchange}` | Exchange implementations (unchanged) |

### Folder Structure Changes

```
Old Structure:
src/
├── data/          → src/service/
├── models/        → src/library/
├── services/      → src/library/ & src/service/
├── exchanges/     → src/exchanges/{country_code}/{exchange}/
└── indicator/     → src/indicator/ (unchanged)

New Structure:
src/
├── library/       # Core components (CCXT.Collector.Library)
├── service/       # Data models (CCXT.Collector.Service)
├── indicator/     # Technical indicators (CCXT.Collector.Indicator)
└── exchanges/     # Exchange implementations
    ├── kr/        # South Korea
    │   ├── upbit/
    │   └── bithumb/
    ├── us/        # United States
    │   ├── coinbase/
    │   └── kraken/
    └── ...        # 20 more regions
```

### API Changes

#### Client Initialization

**v1.x (REST-based):**
```csharp
var client = new BinanceClient("public");
client.Connect();
```

**v2.0 (WebSocket-based):**
```csharp
var client = new BinanceWebSocketClient();
await client.ConnectAsync();
```

#### Data Reception

**v1.x (Polling):**
```csharp
// Polling loop
while (running)
{
    var orderbook = client.GetOrderbook("BTC/USDT");
    ProcessOrderbook(orderbook);
    Thread.Sleep(1000);
}
```

**v2.0 (Callback-based):**
```csharp
// Register callback once
client.OnOrderbookReceived += (orderbook) =>
{
    ProcessOrderbook(orderbook);
};

// Subscribe to symbol
await client.SubscribeOrderbookAsync("BTC/USDT");
```

#### Data Models

**v1.x:**
```csharp
public class Orderbook
{
    public List<OrderbookItem> Bids { get; set; }
    public List<OrderbookItem> Asks { get; set; }
}
```

**v2.0:**
```csharp
public class SOrderBooks
{
    public string exchange { get; set; }
    public string symbol { get; set; }
    public long timestamp { get; set; }
    public SOrderBook result { get; set; }
}
```

## Step-by-Step Migration

### Step 1: Update Package Reference

```xml
<!-- Old -->
<PackageReference Include="CCXT.Collector" Version="1.5.2" />

<!-- New -->
<PackageReference Include="CCXT.Collector" Version="2.0.0" />
```

### Step 2: Update Namespace Imports

```csharp
// Old
using CCXT.Collector.Data;
using CCXT.Collector.Models;

// New
using CCXT.Collector.Service;
using CCXT.Collector.Library;
```

### Step 3: Replace Client Initialization

```csharp
// Old
var binanceClient = new BinanceClient("public");
var upbitClient = new UpbitClient("public");

// New
var binanceClient = new BinanceWebSocketClient();
var upbitClient = new UpbitWebSocketClient();
```

### Step 4: Convert Polling to Callbacks

```csharp
// Old - Polling approach
public async Task CollectData()
{
    while (_running)
    {
        try
        {
            var orderbook = _client.GetOrderbook("BTC/USDT");
            var trades = _client.GetTrades("BTC/USDT");
            
            ProcessOrderbook(orderbook);
            ProcessTrades(trades);
            
            await Task.Delay(1000);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }
}

// New - Callback approach
public async Task CollectData()
{
    // Register callbacks
    _client.OnOrderbookReceived += ProcessOrderbook;
    _client.OnTradeReceived += ProcessTrade;
    _client.OnError += (error) => Console.WriteLine($"Error: {error}");
    
    // Connect and subscribe
    await _client.ConnectAsync();
    await _client.SubscribeOrderbookAsync("BTC/USDT");
    await _client.SubscribeTradesAsync("BTC/USDT");
}

private void ProcessOrderbook(SOrderBooks orderbook)
{
    // Process orderbook data
}

private void ProcessTrade(SCompleteOrders trade)
{
    // Process trade data
}
```

### Step 5: Update Data Processing

```csharp
// Old
void ProcessOrderbook(Orderbook orderbook)
{
    var bestBid = orderbook.Bids.FirstOrDefault();
    var bestAsk = orderbook.Asks.FirstOrDefault();
    
    Console.WriteLine($"Bid: {bestBid?.Price} @ {bestBid?.Quantity}");
    Console.WriteLine($"Ask: {bestAsk?.Price} @ {bestAsk?.Quantity}");
}

// New
void ProcessOrderbook(SOrderBooks orderbook)
{
    var bestBid = orderbook.result.bids.FirstOrDefault();
    var bestAsk = orderbook.result.asks.FirstOrDefault();
    
    Console.WriteLine($"[{orderbook.exchange}] {orderbook.symbol}");
    Console.WriteLine($"Bid: {bestBid?.price} @ {bestBid?.quantity}");
    Console.WriteLine($"Ask: {bestAsk?.price} @ {bestAsk?.quantity}");
}
```

### Step 6: Handle Connection Management

```csharp
// New - Connection management
public class ExchangeManager
{
    private readonly BinanceWebSocketClient _client;
    
    public ExchangeManager()
    {
        _client = new BinanceWebSocketClient();
        
        // Handle connection events
        _client.OnConnected += () => Console.WriteLine("Connected");
        _client.OnDisconnected += () => Console.WriteLine("Disconnected");
        _client.OnError += (error) => HandleError(error);
    }
    
    public async Task Start()
    {
        await _client.ConnectAsync();
        
        // Subscribe to multiple symbols
        var symbols = new[] { "BTC/USDT", "ETH/USDT", "BNB/USDT" };
        foreach (var symbol in symbols)
        {
            await _client.SubscribeOrderbookAsync(symbol);
            await Task.Delay(100); // Rate limiting
        }
    }
    
    public async Task Stop()
    {
        await _client.DisconnectAsync();
    }
    
    private void HandleError(string error)
    {
        Console.WriteLine($"Error: {error}");
        // Implement error handling logic
    }
}
```

## Common Migration Scenarios

### Scenario 1: Multi-Exchange Data Collection

**v1.x:**
```csharp
public class MultiExchangeCollector
{
    private readonly List<BaseClient> _clients = new();
    
    public void Start()
    {
        _clients.Add(new BinanceClient("public"));
        _clients.Add(new UpbitClient("public"));
        
        foreach (var client in _clients)
        {
            Task.Run(() => PollExchange(client));
        }
    }
    
    private async Task PollExchange(BaseClient client)
    {
        while (true)
        {
            var data = client.GetOrderbook("BTC/USDT");
            ProcessData(client.Name, data);
            await Task.Delay(1000);
        }
    }
}
```

**v2.0:**
```csharp
public class MultiExchangeCollector
{
    private readonly List<IWebSocketClient> _clients = new();
    
    public async Task Start()
    {
        var binance = new BinanceWebSocketClient();
        var upbit = new UpbitWebSocketClient();
        
        // Unified callback handling
        binance.OnOrderbookReceived += ProcessOrderbook;
        upbit.OnOrderbookReceived += ProcessOrderbook;
        
        _clients.Add(binance);
        _clients.Add(upbit);
        
        // Connect all exchanges
        await Task.WhenAll(_clients.Select(c => c.ConnectAsync()));
        
        // Subscribe to markets
        await binance.SubscribeOrderbookAsync("BTC/USDT");
        await upbit.SubscribeOrderbookAsync("BTC/KRW");
    }
    
    private void ProcessOrderbook(SOrderBooks orderbook)
    {
        // Unified processing for all exchanges
        Console.WriteLine($"[{orderbook.exchange}] {orderbook.symbol}: " +
                         $"Bid={orderbook.result.bids[0].price}, " +
                         $"Ask={orderbook.result.asks[0].price}");
    }
}
```

### Scenario 2: Technical Indicator Integration

**v1.x:**
```csharp
public class TechnicalAnalysis
{
    private readonly RSI _rsi = new RSI(14);
    
    public void Analyze()
    {
        while (true)
        {
            var candles = _client.GetOHLCV("BTC/USDT", "1h");
            foreach (var candle in candles)
            {
                var value = _rsi.Calculate(candle);
                Console.WriteLine($"RSI: {value}");
            }
            Thread.Sleep(60000);
        }
    }
}
```

**v2.0:**
```csharp
public class TechnicalAnalysis
{
    private readonly RSI _rsi = new RSI(14);
    private readonly BinanceWebSocketClient _client;
    
    public async Task Start()
    {
        _client = new BinanceWebSocketClient();
        
        // Real-time indicator updates
        _client.OnCandleReceived += (candle) =>
        {
            var value = _rsi.Calculate(candle.result);
            Console.WriteLine($"RSI: {value.Value}");
            
            if (value.Value < 30)
                Console.WriteLine("Oversold signal!");
            else if (value.Value > 70)
                Console.WriteLine("Overbought signal!");
        };
        
        await _client.ConnectAsync();
        await _client.SubscribeCandlesAsync("BTC/USDT", "1h");
    }
}
```

### Scenario 3: Error Handling and Reconnection

**v2.0 (New capability):**
```csharp
public class ResilientConnection
{
    private readonly BinanceWebSocketClient _client;
    private readonly List<string> _subscribedSymbols = new();
    
    public ResilientConnection()
    {
        _client = new BinanceWebSocketClient();
        
        _client.OnDisconnected += async () =>
        {
            Console.WriteLine("Connection lost, attempting reconnection...");
            await Task.Delay(5000);
            await Reconnect();
        };
        
        _client.OnError += (error) =>
        {
            Console.WriteLine($"Error: {error}");
            // Log error or send alert
        };
    }
    
    public async Task Subscribe(string symbol)
    {
        _subscribedSymbols.Add(symbol);
        await _client.SubscribeOrderbookAsync(symbol);
    }
    
    private async Task Reconnect()
    {
        try
        {
            await _client.ConnectAsync();
            
            // Resubscribe to all symbols
            foreach (var symbol in _subscribedSymbols)
            {
                await _client.SubscribeOrderbookAsync(symbol);
                await Task.Delay(100);
            }
            
            Console.WriteLine("Reconnection successful");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Reconnection failed: {ex.Message}");
            await Task.Delay(10000);
            await Reconnect(); // Retry
        }
    }
}
```

## Testing Your Migration

### 1. Connection Testing
```csharp
[Test]
public async Task TestWebSocketConnection()
{
    var client = new BinanceWebSocketClient();
    var connected = false;
    
    client.OnConnected += () => connected = true;
    
    await client.ConnectAsync();
    await Task.Delay(1000);
    
    Assert.IsTrue(connected);
    Assert.IsTrue(client.IsConnected);
}
```

### 2. Data Reception Testing
```csharp
[Test]
public async Task TestDataReception()
{
    var client = new BinanceWebSocketClient();
    SOrderBooks receivedOrderbook = null;
    
    client.OnOrderbookReceived += (ob) => receivedOrderbook = ob;
    
    await client.ConnectAsync();
    await client.SubscribeOrderbookAsync("BTC/USDT");
    await Task.Delay(2000);
    
    Assert.IsNotNull(receivedOrderbook);
    Assert.AreEqual("Binance", receivedOrderbook.exchange);
    Assert.AreEqual("BTC/USDT", receivedOrderbook.symbol);
}
```

### 3. Performance Testing
```csharp
[Test]
public async Task TestLatency()
{
    var client = new BinanceWebSocketClient();
    var latencies = new List<long>();
    
    client.OnOrderbookReceived += (ob) =>
    {
        var latency = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - ob.timestamp;
        latencies.Add(latency);
    };
    
    await client.ConnectAsync();
    await client.SubscribeOrderbookAsync("BTC/USDT");
    await Task.Delay(10000); // Collect data for 10 seconds
    
    var avgLatency = latencies.Average();
    Console.WriteLine($"Average latency: {avgLatency}ms");
    
    Assert.Less(avgLatency, 100); // Should be under 100ms
}
```

## Troubleshooting

### Issue: "Type or namespace 'Data' does not exist"
**Solution**: Update namespace from `CCXT.Collector.Data` to `CCXT.Collector.Service`

### Issue: "GetOrderbook method not found"
**Solution**: Replace polling methods with subscription and callbacks

### Issue: "Cannot convert Orderbook to SOrderBooks"
**Solution**: Update data model types and property names

### Issue: Connection drops frequently
**Solution**: The library handles automatic reconnection, but ensure your network allows WebSocket connections

### Issue: Rate limiting errors
**Solution**: Add delays between subscriptions:
```csharp
foreach (var symbol in symbols)
{
    await client.SubscribeOrderbookAsync(symbol);
    await Task.Delay(100); // Prevent rate limiting
}
```

## Performance Comparison

| Metric | v1.x (REST) | v2.0 (WebSocket) | Improvement |
|--------|-------------|------------------|-------------|
| Latency | 500-1000ms | 10-50ms | 10-100x faster |
| CPU Usage | High (polling) | Low (event-driven) | 70% reduction |
| Network Traffic | High (repeated requests) | Low (streaming) | 90% reduction |
| Scalability | Limited | High | 10x more symbols |
| Real-time Updates | No | Yes | Real-time |

## Support Resources

- **GitHub Issues**: https://github.com/ccxt-net/ccxt.collector/issues
- **Documentation**: https://github.com/ccxt-net/ccxt.collector/wiki
- **Email Support**: support@ccxt.net
- **Discord Community**: https://discord.gg/ccxt

## Conclusion

The migration to v2.0 brings significant improvements in performance, scalability, and real-time capabilities. While the migration requires code changes, the benefits include:

- Real-time data streaming with minimal latency
- Support for 132 exchanges worldwide
- Unified data models across all exchanges
- Automatic connection management
- Better resource utilization

Follow this guide step by step, test thoroughly, and reach out for support if needed.