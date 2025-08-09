# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

**Important**: 
1. Always read and understand the documentation files in the `docs/` folder to maintain comprehensive knowledge of the project. These documents contain critical information about the project's architecture, implementation details, and version history.
2. After completing each task or implementation, immediately update the relevant documentation to reflect the changes. Keep all documentation synchronized with the current state of the codebase.

## Project Overview

CCXT.Collector is a .NET library that connects to cryptocurrency exchanges worldwide via WebSocket to receive real-time market data (orderbook, trades, ticker, etc.) and delivers it to callback functions using unified data classes. Additionally, it analyzes the data per exchange and market to calculate technical indicators in real-time, providing these indicator values through callbacks. This allows developers to handle both raw market data and technical analysis from different exchanges with a consistent interface.

### Recent Major Updates (2025-08-09)
- Complete WebSocket architecture implementation for real-time data streaming
- Exchange-specific WebSocket clients (Binance, Upbit, Bithumb) with callback-based event system
- Comprehensive test suites separated by exchange with performance and integration tests
- Sample projects demonstrating real-world usage patterns for each exchange
- Full documentation suite including API reference and migration guide
- **Code Reorganization**: Complete restructuring of source code into logical categories:
  - Core abstractions and infrastructure moved to `Core/` folder
  - Data models categorized into `Models/Market/`, `Models/Trading/`, and `Models/WebSocket/`
  - Technical indicators reorganized by type in `Indicators/` with subcategories
  - Utility classes consolidated in `Utilities/` folder

## Build and Development Commands

### Building the Project
```bash
# Build the solution
dotnet build

# Build specific target framework
dotnet build -f net8.0
dotnet build -f net9.0

# Build in Release mode
dotnet build -c Release

# Restore packages
dotnet restore
```

### Running Tests
```bash
# Run tests
dotnet run --project tests/ccxt.tests.csproj

# Run samples
dotnet run --project samples/ccxt.samples.csproj
```

### Publishing
```bash
# Create NuGet package
dotnet pack src/ccxt.collector.csproj -c Release

# Publish for specific runtime (example for Ubuntu)
dotnet publish -c Release -r ubuntu.18.04-x64 -f net8.0
```

## Architecture Overview

### Core Components (After Reorganization)

1. **Core Framework** (`src/Core/`):
   - **Abstractions** (`Core/Abstractions/`):
     - `IWebSocketClient.cs` - WebSocket client interface defining callback events
     - `WebSocketClientBase.cs` - Base implementation with reconnection, ping/pong, subscription management
   - **Configuration** (`Core/Configuration/`):
     - `config.cs` - Configuration classes
     - `settings.cs` - Application settings
   - **Infrastructure** (`Core/Infrastructure/`):
     - `factory.cs` - Factory pattern implementations
     - `logger.cs` - Logging infrastructure
     - `selector.cs` - Selector utilities

2. **Data Models** (`src/Models/`):
   - **Market Models** (`Models/Market/`):
     - `orderbook.cs` - Order book data structures
     - `ticker.cs` - Ticker data structures
     - `ohlcv.cs` - OHLCV candle data
     - `candle.cs` - Candlestick data
   - **Trading Models** (`Models/Trading/`):
     - `account.cs` - Account/balance structures
     - `trading.cs` - Trading data structures
     - `complete.cs` - Complete order structures
   - **WebSocket Models** (`Models/WebSocket/`):
     - `apiResult.cs` - API result models
     - `wsResult.cs` - WebSocket result models
     - `message.cs` - Message structures

3. **Technical Indicators** (`src/Indicators/`): Organized by category:
   - **Base** (`Indicators/Base/`): `IndicatorCalculatorBase.cs`
   - **Trend** (`Indicators/Trend/`): SMA, EMA, WMA, DEMA, ZLEMA, MACD, SAR
   - **Momentum** (`Indicators/Momentum/`): RSI, CMO, Momentum, ROC, TRIX
   - **Volatility** (`Indicators/Volatility/`): BollingerBand, ATR, Envelope, DPO
   - **Volume** (`Indicators/Volume/`): OBV, ADL, CMF, PVT, VROC, Volume
   - **MarketStrength** (`Indicators/MarketStrength/`): ADX, Aroon, CCI, WPR
   - **Advanced** (`Indicators/Advanced/`): Ichimoku
   - **Series** (`Indicators/Series/`): Various serie classes for indicator data

4. **Utilities** (`src/Utilities/`):
   - `extension.cs` - Extension methods
   - `Statistics.cs` - Statistical calculations
   - `Ohlc.cs` - OHLC utilities
   - `logger.cs` - Logging utilities

5. **Exchange Implementations** (`src/exchanges/`): Each exchange has:
   - **WebSocket Client** (e.g., `BinanceWebSocketClient.cs`) - Real-time data streaming
   - **Callback Events**: OnOrderbookReceived, OnTradeReceived, OnTickerReceived, etc.

### Message Queue Integration

The system uses RabbitMQ for message queue communication with predefined exchange names and queue patterns:
- Snapshot queue: `{RootQName}_snapshot_queue`
- Logger exchange: `{RootQName}_logger_exchange`
- Orderbook exchange: `{RootQName}_orderbook_exchange`
- Ticker exchange: `{RootQName}_ticker_exchange`
- Trading exchange: `{RootQName}_trading_exchange`

### WebSocket-based Data Flow Architecture

1. **WebSocket Connection**: 
   - Each exchange has dedicated WebSocket client (e.g., `BinanceWebSocketClient`)
   - Automatic connection management with reconnection support
   - Exchange-specific WebSocket URL and protocol handling

2. **Subscription Management**:
   - Subscribe to specific channels: `SubscribeOrderbookAsync()`, `SubscribeTradesAsync()`, `SubscribeTickerAsync()`
   - Per-symbol subscription with unified interface across exchanges
   - Automatic resubscription on reconnection

3. **Data Reception & Processing**:
   - Raw WebSocket messages processed by exchange-specific handlers
   - Exchange format converted to unified data classes (`SOrderBooks`, `SCompleteOrders`, `STicker`)
   - Incremental updates handled appropriately (orderbook deltas, trade streams)

4. **Callback Delivery**:
   - Direct callback invocation for real-time data delivery
   - Event-driven architecture with typed callbacks for each data type
   - No intermediate queuing - direct delivery to registered handlers

5. **Technical Analysis** (Optional):
   - Calculates technical indicators using received market data
   - Per-exchange and per-market indicator calculation
   - Delivered through same callback mechanism

6. **Error Handling & Recovery**:
   - Automatic reconnection with exponential backoff
   - Error callbacks for monitoring connection health
   - Subscription state management across reconnections

### Real-time Technical Analysis Flow

1. **Data Aggregation**: Collects OHLCV data per exchange and market symbol
2. **Indicator Calculation**: Updates indicator values with each new data point
3. **Series Management**: Maintains historical series data required for indicator calculations
4. **Callback Triggers**: Sends calculated indicator values to registered callbacks
5. **Performance Optimization**: Uses efficient algorithms to minimize calculation overhead

### Key Dependencies

- **RabbitMQ.Client**: For message queue communication
- **Newtonsoft.Json**: JSON serialization
- **Microsoft.Extensions.Configuration**: Configuration management
- **System.Text.Json**: Additional JSON support

### Configuration

The application uses `appsettings.json` for configuration:
- Exchange-specific settings (polling intervals, snapshot counters)
- RabbitMQ connection settings
- Auto-start configuration for exchanges and symbols
- WebSocket retry settings

### Key Features

- **Real-time WebSocket Streaming**: Primary method for receiving market data with minimal latency
- **Unified Data Classes**: All exchanges return data in the same format, simplifying multi-exchange integration
- **Callback-based Architecture**: Register callbacks to handle incoming data asynchronously
- **Real-time Technical Indicators**: Calculates and delivers technical indicator values per exchange/market
- **Multi-indicator Support**: Over 25 technical indicators available for real-time analysis
- **Automatic Reconnection**: WebSocket connections automatically reconnect on disconnection
- **Incremental Orderbook Updates**: Efficiently processes orderbook deltas to maintain current state
- **Per-market Analysis**: Independent indicator calculation for each exchange and trading pair

### Exchange-Specific Notes

- **Binance**: Full WebSocket support for all market data streams
- **Upbit**: WebSocket for real-time data, REST API fallback available
- **Bitmex**: WebSocket with position and margin trading data streams
- **Deribit**: WebSocket using JSON-RPC protocol for derivatives
- **Bithumb, Gemini, ItBit**: WebSocket implementations for spot trading

### Target Frameworks

The project targets both .NET 8.0 and .NET 9.0, allowing compatibility with different runtime environments.