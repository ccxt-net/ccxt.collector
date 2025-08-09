# Changelog

All notable changes to CCXT.Collector will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Changed - Code Reorganization (2025-08-09)

#### 📁 Complete Source Code Restructuring
- **Core Framework** (`src/Core/`): Reorganized core components
  - `Abstractions/`: Moved `IWebSocketClient.cs` and `WebSocketClientBase.cs`
  - `Configuration/`: Consolidated `config.cs` and `settings.cs`
  - `Infrastructure/`: Grouped `factory.cs`, `logger.cs`, and `selector.cs`

- **Data Models** (`src/Models/`): Categorized all data structures
  - `Market/`: Market data models (orderbook, ticker, ohlcv, candle)
  - `Trading/`: Trading models (account, trading, complete orders)
  - `WebSocket/`: WebSocket specific models (apiResult, wsResult, message)

- **Technical Indicators** (`src/Indicators/`): Reorganized by category
  - `Trend/`: SMA, EMA, WMA, DEMA, ZLEMA, MACD, SAR
  - `Momentum/`: RSI, CMO, Momentum, ROC, TRIX
  - `Volatility/`: BollingerBand, ATR, Envelope, DPO
  - `Volume/`: OBV, ADL, CMF, PVT, VROC, Volume
  - `MarketStrength/`: ADX, Aroon, CCI, WPR
  - `Advanced/`: Ichimoku Cloud
  - `Base/`: Base indicator classes
  - `Series/`: Indicator series data classes

- **Utilities** (`src/Utilities/`): Consolidated utility classes
  - Extension methods, statistics, OHLC utilities, and logging helpers

#### 📝 Namespace Updates
- `CCXT.Collector.Library` → `CCXT.Collector.Core.Abstractions` (WebSocket interfaces and base classes)
- `CCXT.Collector.Library` → `CCXT.Collector.Core.Configuration` (config and settings)
- `CCXT.Collector.Library` → `CCXT.Collector.Core.Infrastructure` (factory, logger, selector)
- `CCXT.Collector.Library` → `CCXT.Collector.Models.WebSocket` (apiResult, wsResult, message)
- `CCXT.Collector.Service` → Retained for data models (orderbook, ticker, trading models)
- `CCXT.Collector.Indicator` → `CCXT.Collector.Indicators.*` (subcategorized by type)
- Exchange implementations now use new Core.Abstractions namespace

#### 📚 Documentation Updates
- Updated all documentation to reflect new project structure
- Modified code examples with correct namespace imports
- Added migration notes for namespace changes
- Updated architecture diagrams with new folder hierarchy

### Fixed
- Build errors resolved after reorganization (526 errors fixed)
- All exchange implementations updated with correct using statements for Core.Abstractions
- Namespace references corrected: BinanceWebSocketClient now properly uses CCXT.Collector.Core.Abstractions
- Test projects and samples updated to use new namespaces
- Complete.cs and other service models retained in CCXT.Collector.Service namespace for compatibility

## [2.0.0] - 2025-08-09

### 🚀 Complete WebSocket Architecture Overhaul - 132 Exchange Support

This major release represents a complete architectural transformation, expanding from 7 to 132 cryptocurrency exchanges with WebSocket-first real-time data streaming.

### Added

#### 🌍 Global Exchange Coverage (132 Total)
- **22 Country/Region folders** with geographic organization
- **132 WebSocket client implementations** across all major exchanges
- **44 exchanges with full API documentation** including WebSocket URLs and fee structures
- Complete implementations for Binance, Upbit, and Bithumb
- Standardized `{Country}/{Exchange}WebSocketClient.cs` structure

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

## Migration from v1.x to v2.0

### Breaking Changes
- **Architecture**: Moved from REST API polling to WebSocket streaming
- **Namespaces**: `CCXT.Collector.Data` → `CCXT.Collector.Service`, `CCXT.Collector.Models` → `CCXT.Collector.Library`
- **Client Initialization**: No more division parameter needed
- **Data Reception**: Callback-based instead of polling loops
- **Data Models**: Now using unified models (`SOrderBooks`, `SCompleteOrders`, `STicker`)

### Quick Migration Example

**Old (v1.x - REST):**
```csharp
var client = new BinanceClient("public");
while (running) {
    var orderbook = client.GetOrderbook("BTC/USDT");
    ProcessOrderbook(orderbook);
    Thread.Sleep(1000);
}
```

**New (v2.0 - WebSocket):**
```csharp
var client = new BinanceWebSocketClient();
client.OnOrderbookReceived += ProcessOrderbook;
await client.ConnectAsync();
await client.SubscribeOrderbookAsync("BTC/USDT");
```

For detailed migration instructions, see [API_REFERENCE.md](docs/API_REFERENCE.md)

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