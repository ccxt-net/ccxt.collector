# Changelog

All notable changes to CCXT.Collector will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [2.0.0] - 2025-08-09

### Added

#### WebSocket Architecture
- Implemented `IWebSocketClient` interface for unified WebSocket communication
- Created `WebSocketClientBase` abstract class with automatic reconnection and ping/pong support
- Added callback-based event system for real-time data delivery

#### Exchange WebSocket Implementations
- **BinanceWebSocketClient**: Full WebSocket implementation for Binance
  - Support for orderbook, trades, and ticker streams
  - Automatic symbol conversion (BTC/USDT ↔ BTCUSDT)
  - 3-minute ping interval for connection maintenance
  
- **UpbitWebSocketClient**: WebSocket client for Korean exchange Upbit
  - Support for KRW and USDT markets
  - Market code conversion (BTC/KRW ↔ KRW-BTC)
  - Korean won formatting support
  
- **BithumbWebSocketClient**: WebSocket implementation for Bithumb
  - Focus on KRW markets and payment coins
  - 30-level orderbook depth support
  - Transaction stream processing

#### Testing Infrastructure
- Created exchange-specific test suites:
  - `BinanceTests.cs`: Comprehensive tests for Binance integration
  - `UpbitTests.cs`: Korean market specific tests
  - `BithumbTests.cs`: Payment coin and arbitrage tests
- Added test orchestrator with interactive menu
- Performance and latency testing capabilities

#### Sample Projects
- Exchange-specific sample implementations:
  - `BinanceSample.cs`: Technical analysis and multi-symbol monitoring
  - `UpbitSample.cs`: KRW market analysis and premium calculation
  - `BithumbSample.cs`: Payment coin tracking and risk management
- `WebSocketExample.cs`: Comprehensive WebSocket usage examples
- Multi-exchange arbitrage detection samples

#### Documentation
- Created comprehensive API documentation (`docs/API.md`)
- Updated README with WebSocket examples and new architecture
- Enhanced CONTRIBUTING.md with development guidelines
- Added CLAUDE.md for AI-assisted development guidance

### Changed

#### Architecture Improvements
- Migrated from REST polling to WebSocket-first architecture
- Unified data models across all exchanges (`SOrderBooks`, `SCompleteOrders`, `STicker`)
- Direct callback delivery instead of queue-based processing
- Improved error handling with automatic reconnection

#### Data Flow
- Real-time streaming replaces polling mechanisms
- Direct event-driven callbacks for immediate data processing
- Removed intermediate data queuing for lower latency
- Simplified subscription management per symbol

### Technical Improvements

#### Performance
- Sub-100ms data delivery latency
- Concurrent WebSocket connections support
- Efficient incremental orderbook updates
- Memory-optimized data structures

#### Reliability
- Automatic reconnection with exponential backoff
- Connection health monitoring with ping/pong
- Subscription state management across reconnections
- Comprehensive error callbacks

#### Developer Experience
- Simplified API with async/await patterns
- Strongly-typed callback events
- Consistent data format across exchanges
- Rich sample code and documentation

## [1.5.2] - 2025-02-01

### Core Features
- Basic REST API integration for exchanges
- RabbitMQ message queue support
- Technical indicator calculations
- Polling-based data collection

---

## Migration Guide

### From v1.5.2 to Current

#### WebSocket Client Usage

**Old (REST-based):**
```csharp
var client = new BinanceClient("public");
client.StartPolling();
```

**New (WebSocket-based):**
```csharp
var client = new BinanceWebSocketClient();
await client.ConnectAsync();
await client.SubscribeOrderbookAsync("BTC/USDT");
```

#### Callback Registration

**Old:**
```csharp
// Data received through RabbitMQ
var consumer = new RabbitMQConsumer();
consumer.OnMessage += ProcessMessage;
```

**New:**
```csharp
// Direct callbacks
client.OnOrderbookReceived += (orderbook) => ProcessOrderbook(orderbook);
client.OnTradeReceived += (trade) => ProcessTrade(trade);
```

#### Data Models

**Old:**
```csharp
Orderbook orderbook = client.GetOrderbook("BTC/USDT");
```

**New:**
```csharp
// Unified data model
client.OnOrderbookReceived += (SOrderBooks orderbook) =>
{
    var bestBid = orderbook.result.bids[0];
    var bestAsk = orderbook.result.asks[0];
};
```

## Upgrade Recommendations

1. **Replace REST clients with WebSocket clients** for real-time data
2. **Update callback handlers** to use new event system
3. **Migrate from polling loops** to subscription-based model
4. **Update data processing** to handle unified data models
5. **Test reconnection scenarios** for production reliability

## Breaking Changes

- Removed polling-based data collection methods
- Changed client initialization (no more division parameter needed)
- Updated data model structures (now using `SOrderBooks`, `SCompleteOrders`, `STicker`)
- Callback signatures changed to use unified data types

## Deprecations

- `BinanceClient`, `UpbitClient`, `BithumbClient` REST clients are deprecated
- Polling services (`polling.cs`) are deprecated in favor of WebSocket
- RabbitMQ direct integration is now optional

## Known Issues

- Upbit WebSocket limited to 5 concurrent subscriptions
- Bithumb doesn't support explicit unsubscribe (requires resubscription)
- Some exchanges may have connection limits per IP

## Future Roadmap

- [ ] Add more exchange WebSocket implementations
- [ ] Implement order management via WebSocket
- [ ] Add authenticated WebSocket endpoints
- [ ] Support for futures and derivatives
- [ ] Enhanced technical indicator integration
- [ ] WebSocket connection pooling
- [ ] Built-in rate limit management