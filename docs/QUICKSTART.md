# Quick Start Guide

Get started with CCXT.Collector in 5 minutes!

## Prerequisites

- .NET 8.0 or 9.0 SDK installed
- Visual Studio 2022 or VS Code (optional)
- Basic knowledge of C# and async/await

## Installation

### 1. Create a new project

```bash
dotnet new console -n CryptoTracker
cd CryptoTracker
```

### 2. Add CCXT.Collector package

```bash
dotnet add package CCXT.Collector
```

Or add to your `.csproj`:

```xml
<PackageReference Include="CCXT.Collector" Version="1.5.2" />
```

## Basic Usage

### Simple Price Tracker

Create a simple program to track Bitcoin prices:

```csharp
using CCXT.Collector.Binance;
using System;
using System.Threading.Tasks;

class Program
{
    static async Task Main()
    {
        // Create Binance WebSocket client
        var client = new BinanceWebSocketClient();
        
        // Subscribe to connection events
        client.OnConnected += () => Console.WriteLine("✅ Connected to Binance!");
        client.OnError += (error) => Console.WriteLine($"❌ Error: {error}");
        
        // Subscribe to ticker updates
        client.OnTickerReceived += (ticker) =>
        {
            Console.WriteLine($"BTC Price: ${ticker.result.closePrice:F2}");
            Console.WriteLine($"24h Change: {ticker.result.percentage:+0.00;-0.00}%");
            Console.WriteLine($"24h Volume: ${ticker.result.quoteVolume:N0}");
            Console.WriteLine("---");
        };
        
        // Connect and subscribe
        await client.ConnectAsync();
        await client.SubscribeTickerAsync("BTC/USDT");
        
        // Keep running
        Console.WriteLine("Press any key to stop...");
        Console.ReadKey();
        
        // Cleanup
        await client.DisconnectAsync();
    }
}
```

### Run the program

```bash
dotnet run
```

## Exchange-Specific Examples

### Binance (Global Exchange)

```csharp
var client = new BinanceWebSocketClient();

// Subscribe to multiple data types
await client.ConnectAsync();
await client.SubscribeOrderbookAsync("BTC/USDT");
await client.SubscribeTradesAsync("BTC/USDT");
await client.SubscribeTickerAsync("BTC/USDT");

// Handle orderbook updates
client.OnOrderbookReceived += (orderbook) =>
{
    var bestBid = orderbook.result.bids[0];
    var bestAsk = orderbook.result.asks[0];
    var spread = bestAsk.price - bestBid.price;
    
    Console.WriteLine($"Best Bid: ${bestBid.price:F2}");
    Console.WriteLine($"Best Ask: ${bestAsk.price:F2}");
    Console.WriteLine($"Spread: ${spread:F2}");
};
```

### Upbit (Korean Exchange - KRW)

```csharp
var client = new UpbitWebSocketClient();

await client.ConnectAsync();
await client.SubscribeTickerAsync("BTC/KRW");
await client.SubscribeTickerAsync("ETH/KRW");

// Handle Korean Won prices
client.OnTickerReceived += (ticker) =>
{
    if (ticker.symbol.EndsWith("/KRW"))
    {
        Console.WriteLine($"{ticker.symbol}: ₩{ticker.result.closePrice:N0}");
        Console.WriteLine($"거래량: {ticker.result.volume:F2}");
    }
};
```

### Bithumb (Payment Coins)

```csharp
var client = new BithumbWebSocketClient();

// Track payment coins
var paymentCoins = new[] { "XRP/KRW", "ADA/KRW", "DOGE/KRW" };

await client.ConnectAsync();

foreach (var coin in paymentCoins)
{
    await client.SubscribeTickerAsync(coin);
}

client.OnTickerReceived += (ticker) =>
{
    Console.WriteLine($"{ticker.symbol}: ₩{ticker.result.closePrice:N0}");
    Console.WriteLine($"Change: {ticker.result.percentage:+0.00;-0.00}%");
};
```

## Common Patterns

### 1. Multi-Exchange Price Monitoring

```csharp
// Create clients for each exchange
var binance = new BinanceWebSocketClient();
var upbit = new UpbitWebSocketClient();

// Track prices
var prices = new Dictionary<string, decimal>();

binance.OnTickerReceived += (t) => 
{
    prices["Binance"] = t.result.closePrice;
    DisplayPrices();
};

upbit.OnTickerReceived += (t) => 
{
    prices["Upbit"] = t.result.closePrice;
    DisplayPrices();
};

// Connect both
await Task.WhenAll(
    binance.ConnectAsync(),
    upbit.ConnectAsync()
);

// Subscribe to same asset on different exchanges
await binance.SubscribeTickerAsync("BTC/USDT");
await upbit.SubscribeTickerAsync("BTC/USDT");
```

### 2. Trade Flow Analysis

```csharp
decimal buyVolume = 0;
decimal sellVolume = 0;

client.OnTradeReceived += (trade) =>
{
    if (trade.result.sideType == SideType.Bid)
        buyVolume += trade.result.amount;
    else
        sellVolume += trade.result.amount;
    
    var netFlow = buyVolume - sellVolume;
    var sentiment = netFlow > 0 ? "Bullish 🟢" : "Bearish 🔴";
    
    Console.WriteLine($"Net Flow: ${netFlow:N2} {sentiment}");
};
```

### 3. Orderbook Spread Monitoring

```csharp
client.OnOrderbookReceived += (orderbook) =>
{
    if (orderbook.result.bids.Any() && orderbook.result.asks.Any())
    {
        var bestBid = orderbook.result.bids[0].price;
        var bestAsk = orderbook.result.asks[0].price;
        var spread = bestAsk - bestBid;
        var spreadPercent = (spread / bestBid) * 100;
        
        if (spreadPercent < 0.1m) // Tight spread
        {
            Console.WriteLine($"⚡ Tight spread detected: {spreadPercent:F3}%");
        }
    }
};
```

### 4. Technical Indicator Integration

```csharp
using CCXT.Collector.Indicator;

// Create indicators
var rsi = new RSI(14);
var sma20 = new SMA(20);
var sma50 = new SMA(50);

// Track price history
var priceHistory = new List<decimal>();

client.OnTickerReceived += (ticker) =>
{
    priceHistory.Add(ticker.result.closePrice);
    
    if (priceHistory.Count >= 50)
    {
        var currentRSI = rsi.Calculate(priceHistory.TakeLast(14));
        var sma20Value = sma20.Calculate(priceHistory.TakeLast(20));
        var sma50Value = sma50.Calculate(priceHistory.TakeLast(50));
        
        // Trading signals
        if (currentRSI < 30)
            Console.WriteLine("🟢 Oversold - Potential Buy Signal");
        else if (currentRSI > 70)
            Console.WriteLine("🔴 Overbought - Potential Sell Signal");
            
        if (sma20Value > sma50Value)
            Console.WriteLine("📈 Golden Cross - Bullish");
    }
};
```

## Error Handling

### Connection Management

```csharp
client.OnError += (error) =>
{
    Console.WriteLine($"Error: {error}");
    
    // Log to file
    File.AppendAllText("errors.log", $"{DateTime.Now}: {error}\n");
};

client.OnDisconnected += () =>
{
    Console.WriteLine("Disconnected - will auto-reconnect");
};

client.OnConnected += () =>
{
    Console.WriteLine("Reconnected successfully");
};
```

### Graceful Shutdown

```csharp
// Handle Ctrl+C
Console.CancelKeyPress += async (sender, e) =>
{
    e.Cancel = true;
    Console.WriteLine("Shutting down...");
    
    await client.DisconnectAsync();
    Environment.Exit(0);
};
```

## Best Practices

### 1. Resource Management

Always dispose WebSocket clients properly:

```csharp
using var client = new BinanceWebSocketClient();
// ... use client
// Automatically disposed
```

Or manually:

```csharp
try
{
    await client.ConnectAsync();
    // ... use client
}
finally
{
    await client.DisconnectAsync();
    client.Dispose();
}
```

### 2. Subscription Limits

Be aware of exchange limits:
- **Binance**: Up to 200 subscriptions per connection
- **Upbit**: Maximum 5 subscriptions per connection
- **Bithumb**: No hard limit but performance considerations

### 3. Data Processing

Process data asynchronously to avoid blocking:

```csharp
client.OnOrderbookReceived += (orderbook) =>
{
    // Don't block the WebSocket thread
    Task.Run(() => ProcessOrderbookAsync(orderbook));
};

async Task ProcessOrderbookAsync(SOrderBooks orderbook)
{
    // Heavy processing here
    await SaveToDatabase(orderbook);
    await CalculateIndicators(orderbook);
}
```

### 4. Logging

Implement comprehensive logging:

```csharp
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/crypto-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

client.OnError += (error) => Log.Error("WebSocket error: {Error}", error);
client.OnConnected += () => Log.Information("Connected to {Exchange}", client.ExchangeName);
```

## Next Steps

1. **Explore Advanced Features**
   - Read the [API Documentation](API.md)
   - Check out [Sample Projects](../samples/)

2. **Build Your Application**
   - Trading bot
   - Market analyzer
   - Arbitrage detector
   - Portfolio tracker

3. **Contribute**
   - Report issues on GitHub
   - Submit pull requests
   - Share your projects

## Troubleshooting

### Connection Issues

```csharp
if (!client.IsConnected)
{
    Console.WriteLine("Not connected. Retrying...");
    await Task.Delay(5000);
    await client.ConnectAsync();
}
```

### Missing Data

```csharp
// Check if you're subscribed
client.OnConnected += async () =>
{
    // Resubscribe after connection
    await client.SubscribeTickerAsync("BTC/USDT");
};
```

### Performance Issues

```csharp
// Use concurrent collections for thread safety
var cache = new ConcurrentDictionary<string, decimal>();

// Limit subscription count
var symbols = new[] { "BTC/USDT", "ETH/USDT" }; // Don't subscribe to too many
```

## Support

- GitHub Issues: [Report bugs](https://github.com/ccxt-net/ccxt.collector/issues)
- Documentation: [Full docs](https://github.com/ccxt-net/ccxt.collector/wiki)
- Examples: [Sample code](../samples/)