# CCXT.Collector Developer Guide

## Table of Contents

### Part 1: Architecture & System Design
1. [System Overview](#system-overview)
2. [Project Structure](#project-structure)
3. [Core Components](#core-components)
4. [WebSocket Architecture](#websocket-architecture)
5. [Data Flow](#data-flow)
6. [Technical Indicators](#technical-indicators)
7. [Exchange Integration](#exchange-integration)
8. [Performance Considerations](#performance-considerations)

### Part 2: API Reference
9. [WebSocket Client Interface](#websocket-client-interface)
10. [Exchange-Specific Clients](#exchange-specific-clients)
11. [Data Models](#data-models)
12. [Technical Indicators API](#technical-indicators-api)
13. [Configuration](#configuration)
14. [Error Handling](#error-handling)

### Part 3: Contributing
15. [Getting Started](#getting-started)
16. [Development Process](#development-process)
17. [Coding Standards](#coding-standards)
18. [Testing](#testing)
19. [Pull Request Process](#pull-request-process)

---

# Part 1: Architecture & System Design

## System Overview

CCXT.Collector is a comprehensive .NET library designed for real-time cryptocurrency market data collection and analysis. The system is built on a WebSocket-first architecture with callback-based event handling, providing low-latency data streaming from 132+ cryptocurrency exchanges worldwide.

### Key Design Principles

- **Unified Interface**: All exchanges expose the same data models and callback interfaces
- **Real-time First**: WebSocket connections prioritized over REST API polling
- **Event-Driven**: Callback-based architecture for asynchronous data handling
- **Modular Design**: Clear separation between library, service, indicator, and exchange layers
- **Resilient Connections**: Automatic reconnection with exponential backoff
- **Performance Optimized**: Direct JSON to standard model conversion without intermediate objects

## Project Structure

```
src/
├── core/                     # Core framework components
│   ├── abstractions/        # Interfaces and base classes
│   │   ├── IWebSocketClient.cs  # WebSocket interface definition
│   │   └── WebSocketClientBase.cs # Base WebSocket implementation
│   ├── configuration/       # Configuration management
│   │   ├── config.cs       # Configuration classes
│   │   └── settings.cs     # Application settings
│   └── infrastructure/      # Infrastructure components
│       ├── factory.cs      # Factory pattern implementations
│       ├── logger.cs       # Logging infrastructure
│       └── selector.cs     # Selector utilities
│
├── models/                  # Data models and structures
│   ├── market/             # Market data models
│   │   ├── orderbook.cs   # Order book data structures
│   │   ├── ticker.cs      # Ticker data structures
│   │   ├── ohlcv.cs       # OHLCV candle data
│   │   └── candle.cs      # Candlestick data
│   ├── trading/            # Trading related models
│   │   ├── account.cs     # Account/balance structures
│   │   ├── trading.cs     # Trading data structures
│   │   └── complete.cs    # Complete order structures
│   └── websocket/          # WebSocket specific models
│       ├── apiResult.cs   # API result models
│       ├── wsResult.cs    # WebSocket result models
│       └── message.cs     # Message structures
│
├── indicators/             # Technical indicators
│   ├── base/              # Base indicator classes
│   │   └── IndicatorCalculatorBase.cs
│   ├── trend/             # Trend indicators
│   │   ├── SMA.cs, EMA.cs, WMA.cs
│   │   ├── MACD.cs, SAR.cs
│   │   └── DEMA.cs, ZLEMA.cs
│   ├── momentum/          # Momentum indicators
│   │   ├── RSI.cs, ROC.cs
│   │   ├── CMO.cs, Momentum.cs
│   │   └── TRIX.cs
│   ├── volatility/        # Volatility indicators
│   │   ├── BollingerBand.cs
│   │   ├── ATR.cs, Envelope.cs
│   │   └── DPO.cs
│   ├── volume/            # Volume indicators
│   │   ├── OBV.cs, ADL.cs
│   │   ├── CMF.cs, PVT.cs
│   │   └── VROC.cs, Volume.cs
│   ├── marketstrength/    # Market strength indicators
│   │   ├── ADX.cs, Aroon.cs
│   │   └── CCI.cs, WPR.cs
│   ├── advanced/          # Advanced indicators
│   │   └── Ichimoku.cs
│   └── series/            # Indicator series data
│       └── Various serie classes
│
├── utilities/              # Utility classes
│   ├── extension.cs       # Extension methods
│   ├── Statistics.cs      # Statistical calculations
│   ├── Ohlc.cs           # OHLC utilities
│   └── logger.cs         # Logging utilities
│
└── exchanges/             # Exchange implementations (by country code)
    ├── kr/               # South Korea (7 exchanges)
    │   ├── upbit/
    │   ├── bithumb/
    │   └── ...
    ├── us/               # United States (26 exchanges)
    │   ├── coinbase/
    │   ├── kraken/
    │   └── ...
    ├── cn/               # China (24 exchanges)
    │   ├── okx/
    │   ├── huobi/
    │   └── ...
    └── ...               # 19 more regions (132 exchanges total)
```

## Core Components

### 1. WebSocket Client Interface (`IWebSocketClient`)

```csharp
namespace CCXT.Collector.Core.Abstractions
{
    public interface IWebSocketClient
    {
        // Properties
        string ExchangeName { get; }
        bool IsConnected { get; }
        bool IsAuthenticated { get; }
        
        // Connection Management
        Task<bool> ConnectAsync(string apiKey = null, string secretKey = null);
        Task DisconnectAsync();
        
        // Public Data Subscriptions
        Task<bool> SubscribeOrderbookAsync(string symbol);
        Task<bool> SubscribeTradesAsync(string symbol);
        Task<bool> SubscribeTickerAsync(string symbol);
        Task<bool> SubscribeCandlesAsync(string symbol, string interval);
        Task<bool> UnsubscribeAsync(string channel, string symbol);
        
        // Private Data Subscriptions
        Task<bool> SubscribeBalanceAsync();
        Task<bool> SubscribeOrdersAsync();
        Task<bool> SubscribePositionsAsync();
        
        // Callback Events - Public Data
        Action<SOrderBooks> OnOrderbookReceived { get; set; }
        Action<STrade> OnTradeReceived { get; set; }
        Action<STicker> OnTickerReceived { get; set; }
        Action<SCandle> OnCandleReceived { get; set; }
        
        // Callback Events - Private Data
        Action<SBalance> OnBalanceReceived { get; set; }
        Action<SOrders> OnOrderReceived { get; set; }
        Action<SPositions> OnPositionReceived { get; set; }
        
        // Connection Events
        Action OnConnected { get; set; }
        Action OnDisconnected { get; set; }
        Action<string> OnError { get; set; }
    }
}
```

### 2. WebSocket Client Base (`WebSocketClientBase`)

The base implementation provides:
- Automatic reconnection with exponential backoff
- Ping/pong heartbeat management
- Subscription state management
- Message queuing during reconnection
- Thread-safe operations

### 3. Unified Data Models

All exchanges return data in these standardized formats:

```csharp
// Order Book
public class SOrderBook
{
    public string exchange { get; set; }
    public string symbol { get; set; }
    public long timestamp { get; set; }
    public SOrderBookData result { get; set; }
}

// Ticker
public class STicker
{
    public string exchange { get; set; }
    public string symbol { get; set; }
    public long timestamp { get; set; }
    public STickerItem result { get; set; }
}

// Trade
public class STrade
{
    public string exchange { get; set; }
    public string symbol { get; set; }
    public long timestamp { get; set; }
    public List<STradeItem> result { get; set; }
}
```

## WebSocket Architecture

### Connection Lifecycle

1. **Initialization**: Create exchange-specific WebSocket client
2. **Connection**: Establish WebSocket connection with exchange
3. **Authentication** (Optional): Send authentication for private channels
4. **Subscription**: Subscribe to desired data channels
5. **Data Reception**: Receive and process streaming data
6. **Callback Invocation**: Deliver processed data to callbacks
7. **Heartbeat**: Maintain connection with ping/pong
8. **Reconnection**: Automatic reconnection on disconnect
9. **Cleanup**: Graceful disconnection and resource cleanup

### Message Processing Pipeline

```
WebSocket Message → Parse to JObject → Identify Message Type → 
Direct Convert to Standard Model → Validate Data → Invoke Callback
```

**Performance Note**: For optimal performance, we convert directly from JObject to standard models without creating intermediate exchange-specific model objects. This eliminates unnecessary object allocation and serialization overhead.

### Exchange-Specific Implementations

Each exchange has its own WebSocket client that:
- Implements exchange-specific WebSocket protocol
- Parses messages directly using JObject/JToken
- Converts directly from JSON to standard unified models
- Manages exchange-specific authentication

#### Standard Conversion Pattern

```csharp
public class BinanceWebSocketClient : WebSocketClientBase
{
    public override string ExchangeName => "Binance";
    protected override string WebSocketUrl => "wss://stream.binance.com:9443/ws";
    
    protected override async Task ProcessMessageAsync(string message, bool isPrivate)
    {
        // 1. Parse to JObject for efficient access
        using var doc = JsonDocument.Parse(message); 
        var json = doc.RootElement;
        
        // 2. Identify message type
        var messageType = json["e"]?.ToString();
        
        // 3. Direct conversion to standard model (no intermediate objects)
        switch (messageType)
        {
            case "depthUpdate":
                await ProcessOrderbook(json);
                break;
            case "trade":
                await ProcessTrade(json);
                break;
        }
    }
    
    private async Task ProcessOrderbook(JsonElement json)
    {
        // Direct conversion from JObject to standard model
        var orderbook = new SOrderBooks
        {
            exchange = ExchangeName,
            symbol = ConvertSymbol(json["s"].ToString()),
            timestamp = json["E"].Value<long>(),
            result = new SOrderBookData
            {
                bids = json["b"].Select(bid => new SOrderBookItem
                {
                    price = bid[0].Value<decimal>(),
                    quantity = bid[1].Value<decimal>()
                }).ToList(),
                asks = json["a"].Select(ask => new SOrderBookItem
                {
                    price = ask[0].Value<decimal>(),
                    quantity = ask[1].Value<decimal>()
                }).ToList()
            }
        };
        
        // Invoke callback with standard model
        InvokeOrderbookCallback(orderbook);
    }
}
```

### Why Direct Conversion?

1. **Performance**: Eliminates intermediate object creation and garbage collection overhead
2. **Simplicity**: Reduces code complexity by removing unnecessary mapping layers
3. **Flexibility**: Easy to adapt to API changes without modifying intermediate models
4. **Memory Efficiency**: Lower memory footprint with fewer object allocations

## Data Flow

### Real-time Data Flow

```
Exchange WebSocket Server
         ↓
    WebSocket Client
         ↓
    Message Parser
         ↓
    Format Converter
         ↓
    Unified Data Model
         ↓
    Callback Invocation
         ↓
    User Application
```

### Technical Indicator Flow

```
Market Data (OHLCV)
         ↓
    Indicator Calculator
         ↓
    Series Management
         ↓
    Value Calculation
         ↓
    Callback Delivery
```

## Technical Indicators

### Indicator Architecture

All indicators inherit from `IndicatorCalculatorBase`:

```csharp
public abstract class IndicatorCalculatorBase<T>
{
    protected List<Ohlc> OhlcList { get; set; }
    public abstract T Calculate(IOhlcv ohlcv);
    protected abstract T CalculateIndicator();
}
```

### Available Indicators (25+)

#### Trend Indicators
- SMA (Simple Moving Average)
- EMA (Exponential Moving Average)
- WMA (Weighted Moving Average)
- DEMA (Double Exponential MA)
- ZLEMA (Zero Lag EMA)
- MACD (Moving Average Convergence Divergence)
- SAR (Parabolic SAR)

#### Momentum Indicators
- RSI (Relative Strength Index)
- CMO (Chande Momentum Oscillator)
- Momentum
- ROC (Rate of Change)
- TRIX (Triple Exponential Average)

#### Volatility Indicators
- Bollinger Bands
- ATR (Average True Range)
- Envelope
- DPO (Detrended Price Oscillator)

#### Volume Indicators
- OBV (On Balance Volume)
- ADL (Accumulation/Distribution Line)
- CMF (Chaikin Money Flow)
- PVT (Price Volume Trend)
- VROC (Volume Rate of Change)

#### Market Strength
- ADX (Average Directional Index)
- Aroon
- CCI (Commodity Channel Index)
- WPR (Williams %R)

#### Advanced
- Ichimoku Cloud

## Exchange Integration

### Regional Distribution

The library supports 132 exchanges across 22 countries/regions:

| Region | Count | Major Exchanges |
|--------|-------|-----------------|
| United States | 26 | Coinbase, Kraken, Gemini |
| China | 24 | OKX, Huobi, Bybit, KuCoin |
| Europe | 13 | Bitstamp, Bitfinex, Bitvavo |
| South Korea | 7 | Upbit, Bithumb, Coinone |
| Japan | 8 | bitFlyer, Coincheck, Bitbank |
| United Kingdom | 7 | CEX.IO, Luno |
| Singapore | 8 | BitMEX, Bitrue |
| Others | 39 | Various regional exchanges |

### Exchange Implementation Status

| Status | Count | Description |
|--------|-------|-------------|
| Full Implementation | 15 | All 15 major exchanges completed |
| WebSocket Structure | 117 | Basic WebSocket client created |
| API Documentation | 44 | Documentation URLs added |
| Pending | 73 | Awaiting implementation |

### Adding New Exchange Support

1. Create folder: `src/exchanges/{country_code}/{exchange_name}/`
2. Create WebSocket client: `{ExchangeName}WebSocketClient.cs`
3. Inherit from `WebSocketClientBase`
4. Implement exchange-specific protocol
5. Add API documentation comments
6. Create tests and samples

## Performance Considerations

### Optimization Strategies

1. **Direct Callbacks**: No intermediate queuing for minimal latency
2. **Incremental Updates**: Process only changed data (orderbook deltas)
3. **Connection Pooling**: Reuse WebSocket connections
4. **Binary Protocol**: Support for binary WebSocket frames where available
5. **Compression**: Support for permessage-deflate extension
6. **Selective Subscription**: Subscribe only to required data

### Resource Management

- **Memory**: Efficient data structure reuse
- **CPU**: Optimized JSON parsing with caching
- **Network**: Minimal subscription messages
- **Threading**: Async/await pattern throughout

### Scalability

- Support for multiple simultaneous exchange connections
- Per-exchange connection management
- Independent subscription management per symbol
- Horizontal scaling through multiple instances

---

# Part 2: API Reference

## WebSocket Client Interface

### Methods

#### ConnectAsync
Establishes WebSocket connection to the exchange.

```csharp
Task<bool> ConnectAsync(string apiKey = null, string secretKey = null)
```

**Parameters:**
- `apiKey` (optional): API key for authenticated connections
- `secretKey` (optional): Secret key for authenticated connections

**Returns:** `true` if connection successful, `false` otherwise

**Example:**
```csharp
using CCXT.Collector.Binance;
using CCXT.Collector.Core.Abstractions;
using CCXT.Collector.Service;

var client = new BinanceWebSocketClient();
await client.ConnectAsync(); // Public connection
// or
await client.ConnectAsync("your-api-key", "your-secret"); // Authenticated
```

#### SubscribeOrderbookAsync
Subscribes to order book updates for a symbol.

```csharp
Task<bool> SubscribeOrderbookAsync(string symbol)
```

**Parameters:**
- `symbol`: Trading pair in format "BASE/QUOTE" (e.g., "BTC/USDT")

**Returns:** `true` if subscription successful

**Example:**
```csharp
await client.SubscribeOrderbookAsync("BTC/USDT");
```

#### SubscribeTradesAsync
Subscribes to trade updates for a symbol.

```csharp
Task<bool> SubscribeTradesAsync(string symbol)
```

**Parameters:**
- `symbol`: Trading pair in format "BASE/QUOTE"

**Returns:** `true` if subscription successful

#### SubscribeTickerAsync
Subscribes to ticker updates for a symbol.

```csharp
Task<bool> SubscribeTickerAsync(string symbol)
```

**Parameters:**
- `symbol`: Trading pair in format "BASE/QUOTE"

**Returns:** `true` if subscription successful

#### SubscribeCandlesAsync
Subscribes to candlestick/OHLCV updates.

```csharp
Task<bool> SubscribeCandlesAsync(string symbol, string interval)
```

**Parameters:**
- `symbol`: Trading pair in format "BASE/QUOTE"
- `interval`: Time interval (e.g., "1m", "5m", "1h", "1d")

**Returns:** `true` if subscription successful

### Callback Events

#### OnOrderbookReceived
Triggered when order book data is received.

```csharp
client.OnOrderbookReceived += (orderbook) =>
{
    Console.WriteLine($"Best bid: {orderbook.result.bids[0].price}");
    Console.WriteLine($"Best ask: {orderbook.result.asks[0].price}");
};
```

#### OnTradeReceived
Triggered when trade data is received.

```csharp
client.OnTradeReceived += (trade) =>
{
    foreach (var tradeItem in trade.result)
    {
        Console.WriteLine($"Trade: {tradeItem.price} @ {tradeItem.quantity}");
    }
};
```

#### OnTickerReceived
Triggered when ticker data is received.

```csharp
client.OnTickerReceived += (ticker) =>
{
    Console.WriteLine($"Last price: {ticker.result.closePrice}");
    Console.WriteLine($"24h volume: {ticker.result.volume}");
};
```

## Exchange-Specific Clients

### Supported Exchanges (132 Total)

All exchange clients inherit from `WebSocketClientBase` and implement `IWebSocketClient`.

#### Major Exchange Implementations

##### BinanceWebSocketClient
```csharp
namespace CCXT.Collector.Binance
{
    public class BinanceWebSocketClient : WebSocketClientBase
    {
        public override string ExchangeName => "Binance";
        protected override string WebSocketUrl => "wss://stream.binance.com:9443/ws";
        // Binance-specific implementation
    }
}
```

##### UpbitWebSocketClient
```csharp
namespace CCXT.Collector.Upbit
{
    public class UpbitWebSocketClient : WebSocketClientBase
    {
        public override string ExchangeName => "Upbit";
        protected override string WebSocketUrl => "wss://api.upbit.com/websocket/v1";
        // Upbit-specific implementation
    }
}
```

##### BithumbWebSocketClient
```csharp
namespace CCXT.Collector.Bithumb
{
    public class BithumbWebSocketClient : WebSocketClientBase
    {
        public override string ExchangeName => "Bithumb";
        protected override string WebSocketUrl => "wss://pubwss.bithumb.com/pub/ws";
        // Bithumb-specific implementation
    }
}
```

## Data Models

### Market Data Models

#### SOrderBooks
Order book data structure:

```csharp
public class SOrderBooks
{
    public string exchange { get; set; }    // Exchange name
    public string symbol { get; set; }      // Trading symbol
    public long timestamp { get; set; }     // Unix timestamp
    public string datetime { get; set; }    // ISO 8601 datetime
    public long nonce { get; set; }         // Message sequence number
    public SOrderBookData result { get; set; }  // Order book data
}

public class SOrderBookData
{
    public string symbol { get; set; }
    public List<SOrderBookItem> bids { get; set; }  // Buy orders
    public List<SOrderBookItem> asks { get; set; }  // Sell orders
    public long timestamp { get; set; }
    public string datetime { get; set; }
    public long nonce { get; set; }
}

public class SOrderBookItem
{
    public decimal price { get; set; }     // Price level
    public decimal quantity { get; set; }  // Quantity at price
}
```

#### STicker
Ticker data structure:

```csharp
public class STicker
{
    public string exchange { get; set; }
    public string symbol { get; set; }
    public long timestamp { get; set; }
    public string datetime { get; set; }
    public STickerItem result { get; set; }
}

public class STickerItem
{
    public string symbol { get; set; }
    public decimal closePrice { get; set; }    // Last price
    public decimal openPrice { get; set; }     // 24h open
    public decimal highPrice { get; set; }     // 24h high
    public decimal lowPrice { get; set; }      // 24h low
    public decimal volume { get; set; }        // 24h volume
    public decimal quoteVolume { get; set; }   // 24h quote volume
    public decimal percentage { get; set; }    // 24h change %
    public decimal change { get; set; }        // 24h change absolute
    public decimal bid { get; set; }           // Best bid
    public decimal ask { get; set; }           // Best ask
    public decimal vwap { get; set; }          // Volume weighted average
}
```

#### STrade
Trade data structure:

```csharp
public class STrade
{
    public string exchange { get; set; }
    public string symbol { get; set; }
    public long timestamp { get; set; }
    public string datetime { get; set; }
    public List<STradeItem> result { get; set; }
}

public class STradeItem
{
    public string id { get; set; }          // Trade ID
    public string order { get; set; }       // Order ID
    public string symbol { get; set; }      // Trading pair
    public SideType sideType { get; set; }  // Buy/Sell
    public OrderType orderType { get; set; } // Market/Limit
    public decimal price { get; set; }      // Trade price
    public decimal quantity { get; set; }   // Trade quantity
    public decimal amount { get; set; }     // Trade amount (price * quantity)
    public decimal fee { get; set; }        // Trading fee
    public long timestamp { get; set; }     // Trade timestamp
    public string datetime { get; set; }    // ISO 8601 datetime
}

public enum SideType
{
    Bid,  // Buy
    Ask   // Sell
}

public enum OrderType
{
    Limit,
    Market
}
```

### Trading Models

#### SBalance
Account balance structure:

```csharp
public class SBalance
{
    public string exchange { get; set; }
    public Dictionary<string, SBalanceItem> balances { get; set; }
    public long timestamp { get; set; }
}

public class SBalanceItem
{
    public string currency { get; set; }
    public decimal free { get; set; }     // Available balance
    public decimal used { get; set; }     // In orders
    public decimal total { get; set; }    // Total balance
}
```

#### SOrder
Order structure:

```csharp
public class SOrderItem
{
    public string id { get; set; }
    public string clientOrderId { get; set; }
    public string symbol { get; set; }
    public SideType side { get; set; }
    public OrderType type { get; set; }
    public OrderStatus status { get; set; }
    public decimal price { get; set; }
    public decimal amount { get; set; }
    public decimal filled { get; set; }
    public decimal remaining { get; set; }
    public long timestamp { get; set; }
}

public enum OrderStatus
{
    Open,
    Closed,
    Canceled,
    Expired,
    Rejected
}
```

## Technical Indicators API

### Base Indicator Interface

```csharp
public interface IIndicator<T>
{
    T Calculate(IOhlcv candle);
    T GetValue();
    void Reset();
}
```

### Using Indicators

#### Example: RSI (Relative Strength Index)

```csharp
using CCXT.Collector.Indicators.Momentum;

// Create RSI with 14-period
var rsi = new RSI(14);

// Feed OHLCV data
client.OnCandleReceived += (candle) =>
{
    // Note: candle.result is now List<SCandleItem>
    foreach (var item in candle.result)
    {
        var rsiValue = rsi.Calculate(item);
        Console.WriteLine($"RSI: {rsiValue}");
    
        // Trading signal
        if (rsiValue < 30)
            Console.WriteLine("Oversold - potential buy signal");
        else if (rsiValue > 70)
            Console.WriteLine("Overbought - potential sell signal");
    }
};
```

#### Example: MACD

```csharp
using CCXT.Collector.Indicators.Trend;

var macd = new MACD(12, 26, 9);

client.OnCandleReceived += (candle) =>
{
    // Note: candle.result is now List<SCandleItem>
    foreach (var item in candle.result)
    {
        var macdResult = macd.Calculate(item);
        Console.WriteLine($"MACD: {macdResult.MACD}");
        Console.WriteLine($"Signal: {macdResult.Signal}");
        Console.WriteLine($"Histogram: {macdResult.Histogram}");
    }
};
```

#### Example: Bollinger Bands

```csharp
using CCXT.Collector.Indicators.Volatility;

var bb = new BollingerBand(20, 2);

client.OnCandleReceived += (candle) =>
{
    // Note: candle.result is now List<SCandleItem>
    foreach (var item in candle.result)
    {
        var bands = bb.Calculate(item);
        Console.WriteLine($"Upper: {bands.Upper}");
        Console.WriteLine($"Middle: {bands.Middle}");
        Console.WriteLine($"Lower: {bands.Lower}");
    }
};
```

## Configuration

### appsettings.json

```json
{
  "appsettings": {
    "websocket.retry.waiting.milliseconds": "600",
    "websocket.retry.max.attempts": "10",
    "use.auto.start": "true",
    "auto.start.exchange.name": "binance",
    "auto.start.symbol.names": "BTC/USDT,ETH/USDT"
  },
  "logging": {
    "level": "Information",
    "console": "true",
    "file": "logs/ccxt.log"
  }
}
```

### Environment Variables

Override configuration with environment variables:

```bash
# Windows
set CCXT_EXCHANGE=binance
set CCXT_SYMBOLS=BTC/USDT,ETH/USDT
set CCXT_API_KEY=your_api_key
set CCXT_SECRET_KEY=your_secret_key

# Linux/Mac
export CCXT_EXCHANGE=binance
export CCXT_SYMBOLS=BTC/USDT,ETH/USDT
export CCXT_API_KEY=your_api_key
export CCXT_SECRET_KEY=your_secret_key
```

## Breaking Changes (v2.1.2)

### Data Model Changes
The following breaking changes were introduced in v2.1.2 for performance optimization:

#### 1. Candlestick Data Structure
```csharp
// Old (v2.1.1 and earlier)
public class SCandle
{
    public SCandleItem result { get; set; }  // Single item
}

// New (v2.1.2+)
public class SCandle
{
    public List<SCandleItem> result { get; set; }  // List of items
}
```

#### 2. Order Update Event Signature
```csharp
// Old (v2.1.1 and earlier)
public event Action<SOrder> OnOrderUpdate;

// New (v2.1.2+)
public event Action<SOrders> OnOrderUpdate;  // Container with List<SOrder>
```

#### 3. Position Update Event Signature
```csharp
// Old (v2.1.1 and earlier)
public event Action<SPosition> OnPositionUpdate;

// New (v2.1.2+)
public event Action<SPositions> OnPositionUpdate;  // Container with List<SPosition>
```

### Migration Guide

#### Updating Candle Processing Code
```csharp
// Old code
client.OnCandleReceived += (candle) =>
{
    var close = candle.result.close;  // Direct access to single item
    ProcessCandle(candle.result);
};

// New code
client.OnCandleReceived += (candle) =>
{
    foreach (var item in candle.result)  // Iterate through list
    {
        var close = item.close;
        ProcessCandle(item);
    }
};
```

#### Updating Order Processing Code
```csharp
// Old code
client.OnOrderUpdate += (order) =>
{
    Console.WriteLine($"Order {order.orderId} status: {order.status}");
};

// New code
client.OnOrderUpdate += (orders) =>
{
    foreach (var order in orders.orders)
    {
        Console.WriteLine($"Order {order.orderId} status: {order.status}");
    }
};
```

## Error Handling

### Connection Errors

```csharp
client.OnError += (error) =>
{
    Console.WriteLine($"WebSocket error: {error}");
    
    // Handle specific errors
    if (error.Contains("rate limit"))
    {
        // Wait before reconnecting
        Thread.Sleep(60000);
    }
    else if (error.Contains("invalid symbol"))
    {
        // Remove invalid subscription
        Console.WriteLine("Invalid symbol - check symbol format");
    }
    else if (error.Contains("connection closed"))
    {
        // Will auto-reconnect
        Console.WriteLine("Connection lost - reconnecting...");
    }
};
```

### Data Validation

```csharp
client.OnOrderbookReceived += (orderbook) =>
{
    // Validate data before processing
    if (orderbook?.result?.bids?.Count > 0 && 
        orderbook?.result?.asks?.Count > 0)
    {
        // Process valid data
        ProcessOrderbook(orderbook);
    }
    else
    {
        Console.WriteLine($"Invalid orderbook data for {orderbook?.symbol}");
    }
};
```

### Exception Handling

```csharp
try
{
    await client.ConnectAsync();
    await client.SubscribeOrderbookAsync("BTC/USDT");
}
catch (WebSocketException ex)
{
    Console.WriteLine($"WebSocket exception: {ex.Message}");
    // Retry logic
}
catch (TimeoutException ex)
{
    Console.WriteLine($"Connection timeout: {ex.Message}");
    // Handle timeout
}
catch (Exception ex)
{
    Console.WriteLine($"Unexpected error: {ex.Message}");
    // Log and handle
}
```

---

# Part 3: Contributing

## Getting Started

### Prerequisites

- .NET 8.0 or .NET 9.0 SDK
- Visual Studio 2022 or Visual Studio Code
- Git
- Basic knowledge of C# and async/await patterns

### Setting Up Your Development Environment

1. **Fork the Repository**
   ```bash
   # Fork on GitHub, then clone your fork
   git clone https://github.com/YOUR_USERNAME/ccxt.collector.git
   cd ccxt.collector
   ```

2. **Add Upstream Remote**
   ```bash
   git remote add upstream https://github.com/ccxt-net/ccxt.collector.git
   ```

3. **Install Dependencies**
   ```bash
   dotnet restore
   ```

4. **Build the Project**
   ```bash
   dotnet build
   ```

5. **Run Tests**
   ```bash
   dotnet test
   ```

## Development Process

### Branch Strategy

We use a simplified Git flow:

- `master` - Stable release branch
- `develop` - Development branch for next release
- `feature/*` - Feature branches
- `bugfix/*` - Bug fix branches
- `hotfix/*` - Urgent fixes for production

### Creating a Feature Branch

```bash
git checkout develop
git pull upstream develop
git checkout -b feature/your-feature-name
```

### Exchange Implementation Guide

When adding a new exchange:

1. **Create WebSocket Client**
   ```
   src/exchanges/[country_code]/[exchange_name]/
   ├── [ExchangeName]WebSocketClient.cs  # WebSocket implementation
   ├── config.cs                         # Configuration (optional)
   └── models/                           # Exchange-specific models
   ```

2. **Implement WebSocket Client**
   ```csharp
   public class NewExchangeWebSocketClient : WebSocketClientBase
   {
       public override string ExchangeName => "NewExchange";
       protected override string WebSocketUrl => "wss://...";
       protected override int PingIntervalMs => 60000;
       
       protected override async Task ProcessMessageAsync(string message)
       {
           // Parse and process WebSocket messages
       }
       
       public override async Task<bool> SubscribeOrderbookAsync(string symbol)
       {
           // Implement subscription logic
       }
       
       // Implement other required methods...
   }
   ```

3. **Follow Unified Data Model**
   - Use `SOrderBooks` for orderbook data
   - Use `STrade` for trade data
   - Use `STicker` for ticker data
   - Convert exchange format to unified format in `ProcessMessageAsync`

4. **Add Tests**
   Create test file in `tests/exchanges/[ExchangeName]Tests.cs`:
   ```csharp
   using Xunit;
   
   public class NewExchangeTests : IDisposable
   {
       private NewExchangeWebSocketClient _client;
       
       [Fact]
       public async Task Test_WebSocket_Connection() 
       {
           // Test implementation
       }
       
       public void Dispose()
       {
           _client?.Dispose();
       }
   }
   ```

5. **Create Sample**
   Add example in `samples/exchanges/[ExchangeName]Sample.cs`

## Coding Standards

### C# Style Guide

#### Naming Conventions
```csharp
// Classes, Interfaces, Methods: PascalCase
public class ExchangeClient { }
public interface IDataHandler { }
public void ProcessData() { }

// Local variables, parameters: camelCase
string symbolName = "BTC/USDT";
void ProcessTickerData(string symbol) { }

// Private fields: _camelCase with underscore
private readonly string _apiKey;

// Constants: UPPER_CASE
public const int MAX_RECONNECT_ATTEMPTS = 5;
```

#### Code Organization
```csharp
public class ExchangeClient
{
    // 1. Fields
    private readonly string _apiKey;
    
    // 2. Properties
    public string ExchangeName { get; set; }
    
    // 3. Events
    public event Action<Ticker> OnTickerReceived;
    
    // 4. Constructors
    public ExchangeClient(string apiKey) { }
    
    // 5. Public methods
    public async Task ConnectAsync() { }
    
    // 6. Private methods
    private void ProcessData() { }
}
```

#### Async/Await Best Practices
```csharp
// Always use Async suffix for async methods
public async Task<Data> GetDataAsync()
{
    // Configure await where appropriate
    await ProcessAsync().ConfigureAwait(false);
}

// Use CancellationToken for cancellable operations
public async Task LongRunningOperation(CancellationToken cancellationToken)
{
    await Task.Delay(1000, cancellationToken);
}
```

### WebSocket Implementation Standards

```csharp
public class ExchangeWebSocket
{
    // Always implement reconnection logic
    private async Task HandleDisconnection()
    {
        await Task.Delay(_reconnectDelay);
        await ReconnectAsync();
    }
    
    // Implement proper error handling
    private void OnError(Exception ex)
    {
        _logger.LogError(ex, "WebSocket error");
        // Attempt recovery
    }
    
    // Use heartbeat/ping-pong to maintain connection
    private async Task SendHeartbeat()
    {
        await _webSocket.SendAsync("ping");
    }
}
```

## Testing

### Test Requirements

- All new features must have unit tests
- Bug fixes should include regression tests
- Maintain test coverage above 80%
- Integration tests for exchange connections

### Writing Tests

We use XUnit for testing:

```csharp
using Xunit;

public class IndicatorTests
{
    [Fact]
    public void RSI_Calculate_ReturnsValidValue()
    {
        // Arrange
        var rsi = new RSI(14);
        var data = GenerateTestData();
        
        // Act
        var result = rsi.Calculate(data);
        
        // Assert
        Assert.True(result >= 0 && result <= 100);
    }
    
    [Theory]
    [InlineData(14)]
    [InlineData(21)]
    [InlineData(28)]
    public void RSI_WithDifferentPeriods_CalculatesCorrectly(int period)
    {
        var rsi = new RSI(period);
        var result = rsi.Calculate(GenerateTestData());
        Assert.InRange(result, 0, 100);
    }
}
```

### Running Tests

```bash
# Run all tests
dotnet test

# Run specific test category
dotnet test --filter Category=Unit

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"
```

## Pull Request Process

### Before Submitting

1. **Update from upstream**
   ```bash
   git fetch upstream
   git rebase upstream/develop
   ```

2. **Run tests**
   ```bash
   dotnet test
   ```

3. **Check code style**
   ```bash
   dotnet format
   ```

4. **Update documentation**

### PR Guidelines

1. **Title Format**: `[Type] Brief description`
   - Types: `[Feature]`, `[Bug]`, `[Docs]`, `[Refactor]`, `[Test]`
   - Example: `[Feature] Add Kraken exchange support`

2. **Description Template**:
   ```markdown
   ## Description
   Brief description of changes
   
   ## Type of Change
   - [ ] Bug fix
   - [ ] New feature
   - [ ] Breaking change
   - [ ] Documentation update
   
   ## Testing
   - [ ] Unit tests pass
   - [ ] Integration tests pass
   - [ ] Manual testing completed
   
   ## Checklist
   - [ ] Code follows style guidelines
   - [ ] Self-review completed
   - [ ] Documentation updated
   - [ ] No new warnings
   ```

3. **Keep PRs focused** - One feature/fix per PR

4. **Respond to feedback** - Address review comments promptly

### Review Process

1. Automated checks must pass
2. At least one maintainer review required
3. All feedback addressed
4. Final approval from maintainer
5. Squash and merge to develop

## Community

### Communication Channels

- **GitHub Issues**: Bug reports and feature requests
- **GitHub Discussions**: General discussions and questions
- **Discord**: Real-time chat (if available)
- **Email**: support@ccxt.net

### Getting Help

- Check existing issues and discussions
- Read the documentation and wiki
- Ask clear, specific questions
- Provide context and examples

### Recognition

Contributors are recognized in:
- The README.md Contributors section
- Release notes
- Special thanks in major releases

## Security Considerations

### API Key Management
- Never log API keys or secrets
- Use secure storage for credentials
- Support for read-only API keys
- Separate keys for different operations

### Network Security
- TLS/SSL for all WebSocket connections
- Certificate validation
- Support for proxy connections
- Rate limiting compliance

### Data Validation
- Input validation for all external data
- Bounds checking for numeric values
- Symbol format validation
- Timestamp verification

## Future Enhancements

### Planned Features
- [ ] Additional exchange implementations
- [ ] More technical indicators
- [ ] Advanced order types support
- [ ] Historical data retrieval
- [ ] Backtesting framework
- [ ] Strategy automation support
- [ ] Machine learning integration
- [ ] Cloud deployment templates

### Performance Improvements
- [ ] Native AOT compilation support
- [ ] SIMD optimizations for indicators
- [ ] Memory pool implementation
- [ ] Zero-allocation message processing

## License

By contributing to CCXT.Collector, you agree that your contributions will be licensed under the MIT License.

---

**Thank you for using and contributing to CCXT.Collector!**

For additional support:
- GitHub Issues: [https://github.com/ccxt-net/ccxt.collector/issues](https://github.com/ccxt-net/ccxt.collector/issues)
- Documentation: [https://github.com/ccxt-net/ccxt.collector/wiki](https://github.com/ccxt-net/ccxt.collector/wiki)
- Email: support@ccxt.net