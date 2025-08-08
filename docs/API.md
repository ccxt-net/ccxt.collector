# CCXT.Collector API Documentation

## Table of Contents

- [WebSocket Client Interface](#websocket-client-interface)
- [Exchange-Specific Clients](#exchange-specific-clients)
- [Data Models](#data-models)
- [Callback Events](#callback-events)
- [Technical Indicators](#technical-indicators)
- [Error Handling](#error-handling)

## WebSocket Client Interface

### IWebSocketClient

The core interface that all exchange WebSocket clients implement.

```csharp
public interface IWebSocketClient : IDisposable
{
    string ExchangeName { get; }
    bool IsConnected { get; }
    
    Task<bool> ConnectAsync();
    Task DisconnectAsync();
    
    Task<bool> SubscribeOrderbookAsync(string symbol);
    Task<bool> SubscribeTradesAsync(string symbol);
    Task<bool> SubscribeTickerAsync(string symbol);
    Task<bool> UnsubscribeAsync(string channel, string symbol);
    
    // Callback Events
    event Action<SOrderBooks> OnOrderbookReceived;
    event Action<SCompleteOrders> OnTradeReceived;
    event Action<STicker> OnTickerReceived;
    event Action OnConnected;
    event Action OnDisconnected;
    event Action<string> OnError;
}
```

## Exchange-Specific Clients

### BinanceWebSocketClient

WebSocket client for Binance exchange.

```csharp
var client = new BinanceWebSocketClient();

// Connect
await client.ConnectAsync();

// Subscribe to channels
await client.SubscribeOrderbookAsync("BTC/USDT");
await client.SubscribeTradesAsync("BTC/USDT");
await client.SubscribeTickerAsync("BTC/USDT");

// Register callbacks
client.OnOrderbookReceived += (orderbook) => { /* handle orderbook */ };
client.OnTradeReceived += (trade) => { /* handle trade */ };
client.OnTickerReceived += (ticker) => { /* handle ticker */ };
```

**Features:**
- WebSocket URL: `wss://stream.binance.com:9443/ws`
- Supports USDT, BUSD, USDC pairs
- Automatic ping every 3 minutes
- Incremental orderbook updates

### UpbitWebSocketClient

WebSocket client for Upbit exchange (Korean market).

```csharp
var client = new UpbitWebSocketClient();

// Connect
await client.ConnectAsync();

// Subscribe to KRW markets
await client.SubscribeOrderbookAsync("BTC/KRW");
await client.SubscribeTradesAsync("ETH/KRW");
await client.SubscribeTickerAsync("XRP/KRW");

// Also supports USDT pairs
await client.SubscribeTickerAsync("BTC/USDT");
```

**Features:**
- WebSocket URL: `wss://api.upbit.com/websocket/v1`
- Supports KRW and USDT markets
- Korean won (₩) formatting
- Market code conversion (BTC/KRW ↔ KRW-BTC)

### BithumbWebSocketClient

WebSocket client for Bithumb exchange.

```csharp
var client = new BithumbWebSocketClient();

// Connect
await client.ConnectAsync();

// Subscribe to payment coins
await client.SubscribeTickerAsync("XRP/KRW");
await client.SubscribeTickerAsync("ADA/KRW");
await client.SubscribeTickerAsync("DOGE/KRW");
```

**Features:**
- WebSocket URL: `wss://pubwss.bithumb.com/pub/ws`
- Focus on KRW markets
- Payment coin support
- 30-level orderbook depth

## Data Models

### SOrderBooks

Unified orderbook data structure.

```csharp
public class SOrderBooks
{
    public string exchange { get; set; }
    public string symbol { get; set; }
    public long timestamp { get; set; }
    public string sequentialId { get; set; }
    public SOrderBook result { get; set; }
}

public class SOrderBook
{
    public long timestamp { get; set; }
    public List<SOrderBookItem> asks { get; set; }
    public List<SOrderBookItem> bids { get; set; }
}

public class SOrderBookItem
{
    public string action { get; set; }    // "I" (Insert), "U" (Update), "D" (Delete)
    public decimal quantity { get; set; }
    public decimal price { get; set; }
    public decimal amount { get; set; }    // quantity * price
}
```

### SCompleteOrders

Unified trade data structure.

```csharp
public class SCompleteOrders
{
    public string exchange { get; set; }
    public string symbol { get; set; }
    public long timestamp { get; set; }
    public SCompleteOrder result { get; set; }
}

public class SCompleteOrder
{
    public string orderId { get; set; }
    public long timestamp { get; set; }
    public SideType sideType { get; set; }  // Bid or Ask
    public OrderType orderType { get; set; } // Limit, Market
    public decimal price { get; set; }
    public decimal quantity { get; set; }
    public decimal amount { get; set; }
}
```

### STicker

Unified ticker data structure.

```csharp
public class STicker
{
    public string exchange { get; set; }
    public string symbol { get; set; }
    public long timestamp { get; set; }
    public STickerItem result { get; set; }
}

public class STickerItem
{
    public long timestamp { get; set; }
    public decimal openPrice { get; set; }
    public decimal highPrice { get; set; }
    public decimal lowPrice { get; set; }
    public decimal closePrice { get; set; }
    public decimal volume { get; set; }
    public decimal quoteVolume { get; set; }
    public decimal bidPrice { get; set; }
    public decimal bidQuantity { get; set; }
    public decimal askPrice { get; set; }
    public decimal askQuantity { get; set; }
    public decimal change { get; set; }
    public decimal percentage { get; set; }
}
```

## Callback Events

### Connection Events

```csharp
// Connection established
client.OnConnected += () => 
{
    Console.WriteLine("Connected successfully");
};

// Connection lost
client.OnDisconnected += () => 
{
    Console.WriteLine("Disconnected - will auto-reconnect");
};

// Error occurred
client.OnError += (error) => 
{
    Console.WriteLine($"Error: {error}");
};
```

### Data Events

```csharp
// Orderbook updates
client.OnOrderbookReceived += (orderbook) =>
{
    var bestBid = orderbook.result.bids.FirstOrDefault();
    var bestAsk = orderbook.result.asks.FirstOrDefault();
    
    if (bestBid != null && bestAsk != null)
    {
        var spread = bestAsk.price - bestBid.price;
        Console.WriteLine($"Spread: {spread}");
    }
};

// Trade data
client.OnTradeReceived += (trade) =>
{
    var side = trade.result.sideType == SideType.Bid ? "BUY" : "SELL";
    Console.WriteLine($"{side} {trade.result.quantity} @ {trade.result.price}");
};

// Ticker updates
client.OnTickerReceived += (ticker) =>
{
    Console.WriteLine($"Price: {ticker.result.closePrice} ({ticker.result.percentage:+0.00;-0.00}%)");
};
```

## Technical Indicators

### Using Indicators with WebSocket Data

```csharp
// Create indicator calculators
var rsi = new RSI(14);
var macd = new MACD(12, 26, 9);
var bb = new BollingerBand(20, 2);

// Process ticker data through indicators
client.OnTickerReceived += (ticker) =>
{
    var ohlcv = new Ohlc
    {
        timestamp = ticker.timestamp,
        open = ticker.result.openPrice,
        high = ticker.result.highPrice,
        low = ticker.result.lowPrice,
        close = ticker.result.closePrice,
        volume = ticker.result.volume
    };
    
    // Calculate indicators
    var rsiValue = rsi.Calculate(ohlcv);
    var macdResult = macd.Calculate(ohlcv);
    var bbResult = bb.Calculate(ohlcv);
    
    Console.WriteLine($"RSI: {rsiValue}");
    Console.WriteLine($"MACD: {macdResult.MACD}, Signal: {macdResult.Signal}");
    Console.WriteLine($"BB: Upper={bbResult.Upper}, Middle={bbResult.Middle}, Lower={bbResult.Lower}");
};
```

### Available Indicators

**Trend Indicators:**
- SMA, EMA, WMA, DEMA, ZLEMA
- MACD
- SAR (Parabolic SAR)

**Momentum Indicators:**
- RSI, CMO
- Momentum, ROC, TRIX

**Volatility Indicators:**
- Bollinger Bands
- ATR, Envelope, DPO

**Volume Indicators:**
- OBV, ADL, CMF, PVT, VROC

**Market Strength:**
- ADX, Aroon, CCI, WPR

**Advanced:**
- Ichimoku Cloud

## Error Handling

### Connection Errors

```csharp
client.OnError += (error) =>
{
    if (error.Contains("Connection failed"))
    {
        // Connection issue - will auto-reconnect
        Console.WriteLine("Connection lost, reconnecting...");
    }
    else if (error.Contains("Subscribe"))
    {
        // Subscription error
        Console.WriteLine($"Subscription failed: {error}");
    }
    else if (error.Contains("Parse"))
    {
        // Data parsing error
        Console.WriteLine($"Data parsing error: {error}");
    }
};
```

### Automatic Reconnection

All WebSocket clients implement automatic reconnection with exponential backoff:

1. First reconnect attempt: 5 seconds
2. Second attempt: 10 seconds
3. Third attempt: 15 seconds
4. Fourth attempt: 20 seconds
5. Fifth attempt: 25 seconds
6. After 5 failed attempts, stops reconnecting

### Manual Reconnection

```csharp
// Check connection status
if (!client.IsConnected)
{
    // Manually reconnect
    var success = await client.ConnectAsync();
    
    if (success)
    {
        // Resubscribe to channels
        await client.SubscribeOrderbookAsync("BTC/USDT");
    }
}
```

## Advanced Usage

### Multi-Exchange Arbitrage

```csharp
// Track prices across exchanges
var prices = new ConcurrentDictionary<string, decimal>();

// Binance
binanceClient.OnTickerReceived += (ticker) =>
{
    prices[$"Binance_{ticker.symbol}"] = ticker.result.closePrice;
    CheckArbitrage();
};

// Upbit
upbitClient.OnTickerReceived += (ticker) =>
{
    prices[$"Upbit_{ticker.symbol}"] = ticker.result.closePrice;
    CheckArbitrage();
};

void CheckArbitrage()
{
    // Compare prices across exchanges
    var binancePrice = prices.GetValueOrDefault("Binance_BTC/USDT");
    var upbitPrice = prices.GetValueOrDefault("Upbit_BTC/USDT");
    
    if (binancePrice > 0 && upbitPrice > 0)
    {
        var diff = Math.Abs(binancePrice - upbitPrice);
        var diffPercent = (diff / Math.Min(binancePrice, upbitPrice)) * 100;
        
        if (diffPercent > 0.5m) // 0.5% threshold
        {
            Console.WriteLine($"Arbitrage opportunity: {diffPercent:F2}%");
        }
    }
}
```

### Performance Optimization

```csharp
// Use batch subscriptions for better performance
var symbols = new[] { "BTC/USDT", "ETH/USDT", "BNB/USDT" };

foreach (var symbol in symbols)
{
    await client.SubscribeTickerAsync(symbol);
}

// Process data in separate thread
client.OnOrderbookReceived += (orderbook) =>
{
    Task.Run(() => ProcessOrderbook(orderbook));
};

// Use concurrent collections for thread safety
var orderbookCache = new ConcurrentDictionary<string, SOrderBooks>();
```

## Rate Limits

Each exchange has different rate limits:

- **Binance**: 5000 weight per minute
- **Upbit**: 10 requests per second
- **Bithumb**: 20 requests per second

WebSocket connections typically don't have strict rate limits, but subscription limits may apply:

- **Binance**: Up to 200 subscriptions per connection
- **Upbit**: Up to 5 subscriptions per connection
- **Bithumb**: No hard limit, but performance may degrade with too many subscriptions