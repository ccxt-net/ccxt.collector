# CCXT.Collector Architecture Documentation

## Table of Contents
1. [System Overview](#system-overview)
2. [Project Structure](#project-structure)
3. [Core Components](#core-components)
4. [WebSocket Architecture](#websocket-architecture)
5. [Data Flow](#data-flow)
6. [Technical Indicators](#technical-indicators)
7. [Exchange Integration](#exchange-integration)
8. [Performance Considerations](#performance-considerations)

## System Overview

CCXT.Collector is a comprehensive .NET library designed for real-time cryptocurrency market data collection and analysis. The system is built on a WebSocket-first architecture with callback-based event handling, providing low-latency data streaming from 132+ cryptocurrency exchanges worldwide.

### Key Design Principles

- **Unified Interface**: All exchanges expose the same data models and callback interfaces
- **Real-time First**: WebSocket connections prioritized over REST API polling
- **Event-Driven**: Callback-based architecture for asynchronous data handling
- **Modular Design**: Clear separation between library, service, indicator, and exchange layers
- **Resilient Connections**: Automatic reconnection with exponential backoff
- **Performance Optimized**: Direct callback invocation without intermediate queuing

## Project Structure

```
src/
├── Core/                     # Core framework components
│   ├── Abstractions/        # Interfaces and base classes
│   │   ├── IWebSocketClient.cs  # WebSocket interface definition
│   │   └── WebSocketClientBase.cs # Base WebSocket implementation
│   ├── Configuration/       # Configuration management
│   │   ├── config.cs       # Configuration classes
│   │   └── settings.cs     # Application settings
│   └── Infrastructure/      # Infrastructure components
│       ├── factory.cs      # Factory pattern implementations
│       ├── logger.cs       # Logging infrastructure
│       └── selector.cs     # Selector utilities
│
├── Models/                  # Data models and structures
│   ├── Market/             # Market data models
│   │   ├── orderbook.cs   # Order book data structures
│   │   ├── ticker.cs      # Ticker data structures
│   │   ├── ohlcv.cs       # OHLCV candle data
│   │   └── candle.cs      # Candlestick data
│   ├── Trading/            # Trading related models
│   │   ├── account.cs     # Account/balance structures
│   │   ├── trading.cs     # Trading data structures
│   │   └── complete.cs    # Complete order structures
│   └── WebSocket/          # WebSocket specific models
│       ├── apiResult.cs   # API result models
│       ├── wsResult.cs    # WebSocket result models
│       └── message.cs     # Message structures
│
├── Indicators/             # Technical indicators
│   ├── Base/              # Base indicator classes
│   │   └── IndicatorCalculatorBase.cs
│   ├── Trend/             # Trend indicators
│   │   ├── SMA.cs, EMA.cs, WMA.cs
│   │   ├── MACD.cs, SAR.cs
│   │   └── DEMA.cs, ZLEMA.cs
│   ├── Momentum/          # Momentum indicators
│   │   ├── RSI.cs, ROC.cs
│   │   ├── CMO.cs, Momentum.cs
│   │   └── TRIX.cs
│   ├── Volatility/        # Volatility indicators
│   │   ├── BollingerBand.cs
│   │   ├── ATR.cs, Envelope.cs
│   │   └── DPO.cs
│   ├── Volume/            # Volume indicators
│   │   ├── OBV.cs, ADL.cs
│   │   ├── CMF.cs, PVT.cs
│   │   └── VROC.cs, Volume.cs
│   ├── MarketStrength/    # Market strength indicators
│   │   ├── ADX.cs, Aroon.cs
│   │   └── CCI.cs, WPR.cs
│   ├── Advanced/          # Advanced indicators
│   │   └── Ichimoku.cs
│   └── Series/            # Indicator series data
│       └── Various serie classes
│
├── Utilities/              # Utility classes
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
public interface IWebSocketClient
{
    // Connection Management
    Task<bool> ConnectAsync(string apiKey = null, string secretKey = null);
    Task DisconnectAsync();
    
    // Public Data Subscriptions
    Task<bool> SubscribeOrderbookAsync(string symbol);
    Task<bool> SubscribeTradesAsync(string symbol);
    Task<bool> SubscribeTickerAsync(string symbol);
    Task<bool> SubscribeCandlesAsync(string symbol, string interval);
    
    // Private Data Subscriptions
    Task<bool> SubscribeBalanceAsync();
    Task<bool> SubscribeOrdersAsync();
    
    // Callback Events
    Action<SOrderBooks> OnOrderbookReceived { get; set; }
    Action<SCompleteOrders> OnTradeReceived { get; set; }
    Action<STicker> OnTickerReceived { get; set; }
    Action<SCandlestick> OnCandleReceived { get; set; }
    Action<SBalance> OnBalanceReceived { get; set; }
    Action<SOrder> OnOrderReceived { get; set; }
    
    // Connection Events
    Action OnConnected { get; set; }
    Action OnDisconnected { get; set; }
    Action<string> OnError { get; set; }
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
public class SOrderBooks
{
    public string exchange { get; set; }
    public string symbol { get; set; }
    public long timestamp { get; set; }
    public SOrderBook result { get; set; }
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
public class SCompleteOrders
{
    public string exchange { get; set; }
    public string symbol { get; set; }
    public long timestamp { get; set; }
    public SCompleteOrder result { get; set; }
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
WebSocket Message → Parse JSON → Identify Message Type → 
Convert to Unified Format → Validate Data → Invoke Callback
```

### Exchange-Specific Implementations

Each exchange has its own WebSocket client that:
- Implements exchange-specific WebSocket protocol
- Handles exchange-specific message formats
- Converts data to unified format
- Manages exchange-specific authentication

Example structure:
```csharp
public class BinanceWebSocketClient : WebSocketClientBase
{
    public override string ExchangeName => "Binance";
    protected override string WebSocketUrl => "wss://stream.binance.com:9443/ws";
    
    protected override async Task ProcessMessageAsync(string message, bool isPrivate)
    {
        // Parse Binance-specific message format
        // Convert to unified data model
        // Invoke appropriate callback
    }
}
```

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
| Full Implementation | 3 | Binance, Upbit, Bithumb |
| WebSocket Structure | 129 | Basic WebSocket client created |
| API Documentation | 44 | Documentation URLs added |
| Pending | 88 | Awaiting implementation |

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

## Conclusion

CCXT.Collector provides a robust, scalable, and performant architecture for cryptocurrency market data collection. The modular design allows for easy extension while maintaining consistency across all supported exchanges. The WebSocket-first approach ensures low-latency data delivery, while the callback-based architecture provides flexibility for various use cases.