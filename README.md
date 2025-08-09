# CCXT.Collector

[![NuGet](https://img.shields.io/nuget/v/CCXT.Collector.svg)](https://www.nuget.org/packages/CCXT.Collector)
[![.NET](https://img.shields.io/badge/.NET-8.0%20%7C%209.0-blue.svg)](https://dotnet.microsoft.com/)
[![License](https://img.shields.io/github/license/ccxt-net/ccxt.collector)](https://github.com/ccxt-net/ccxt.collector/blob/master/LICENSE.txt)

A powerful .NET library for real-time cryptocurrency exchange data collection with unified WebSocket streaming and technical indicator analysis.

---

## 📊 Overview

CCXT.Collector is a comprehensive library that connects to cryptocurrency exchanges worldwide via WebSocket to receive real-time market data and calculate technical indicators. It provides a unified interface for handling data from multiple exchanges, making it easy to build trading bots, market analysis tools, and data collection systems.

### ✨ Key Features

- 🚀 **Real-time WebSocket Streaming** - Low-latency market data streaming
- 🔄 **Unified Data Classes** - Consistent data format across all exchanges
- 📈 **25+ Technical Indicators** - Real-time calculation per exchange/market
- 🔌 **Callback Architecture** - Asynchronous event-driven data handling
- 🔐 **Automatic Reconnection** - Resilient WebSocket connection management

### 🏢 Supported Exchanges (132 Total)

#### Major Exchanges by Region

| Region | Exchanges | Count |
|--------|-----------|-------|
| 🇺🇸 United States | Coinbase, Kraken, Gemini, Bittrex, Poloniex, Phemex, Crypto.com, and 19 more | 26 |
| 🇨🇳 China | Binance*, OKX, Huobi, Bybit, KuCoin, Gate.io, MEXC, Bitget, and 16 more | 24 |
| 🇰🇷 South Korea | Upbit, Bithumb, Coinone, Korbit, Gopax, Probit, OKCoinKR | 7 |
| 🇯🇵 Japan | bitFlyer, Coincheck, Bitbank, Zaif, and 4 more | 8 |
| 🇪🇺 Europe | Bitstamp, Bitfinex, Bitvavo, EXMO, WhiteBIT, and 8 more | 13 |
| 🇬🇧 United Kingdom | Bitfinex, Bitstamp, CEX.IO, Luno, and 3 more | 7 |
| 🇸🇬 Singapore | BitMEX*, Bitrue, Coins.ph, and 5 more | 8 |
| 🌍 Other Regions | Deribit (UAE), BTC Markets (AU), Bitso (MX), NDAX (CA), and more | 39 |

*Note: Exchange locations indicate registration/headquarters, not service availability

#### Implementation Status (v2.1.3)

| Feature | Implemented | In Progress | Planned |
|---------|------------|-------------|----------|
| WebSocket Clients | 132 | - | - |
| Korean Exchange WebSockets | 5 (Upbit, Bithumb, Coinone, Korbit, Gopax) | 2 (OKCoinKR, Probit) | - |
| Major Exchange Implementations | **15 (100% Complete)** | - | - |
| Full WebSocket Implementation | **15** (All kimp.client exchanges) | - | 117 |

#### ✅ All 15 Major Exchanges from kimp.client (100% Complete)
Binance, Bitget, Bithumb, Bittrex, Bybit, Coinbase, Coinone, Crypto.com, Gate.io, Huobi, Korbit, Kucoin, OKX, Upbit - **All functional with standardized WebSocket streaming**

## 📦 Installation

### NuGet Package Manager
```bash
Install-Package CCXT.Collector -Version 2.1.3
```

### .NET CLI
```bash
dotnet add package CCXT.Collector --version 2.1.3
```

### Package Reference
```xml
<PackageReference Include="CCXT.Collector" Version="2.1.3" />
```

### ⚠️ Breaking Changes in v2.1.3
- Complete WebSocket implementations for Gate.io and Bittrex
- Standardized data models across all 15 major exchanges
- See [CHANGELOG](docs/CHANGELOG.md#213---2025-01-09) for complete details

### ⚠️ Breaking Changes in v2.1.2
- `SCandlestick.result` changed from single item to `List<SCandleItem>`
- `OnOrderUpdate` event now uses `SOrders` container instead of single `SOrder`
- `OnPositionUpdate` event now uses `SPositions` container instead of single `SPosition`
- See [Migration Guide](docs/GUIDE.md#breaking-changes-v212) for details

## 🚀 Quick Start

### Basic WebSocket Connection

```csharp
using CCXT.Collector.Binance;
using CCXT.Collector.Service;
using System;
using System.Threading.Tasks;

class Program
{
    static async Task Main(string[] args)
    {
        // Create WebSocket client
        var client = new BinanceWebSocketClient();
        
        // Register callbacks for real-time data
        client.OnOrderbookReceived += (orderbook) =>
        {
            Console.WriteLine($"Orderbook update: {orderbook.symbol}");
            Console.WriteLine($"Best bid: {orderbook.result.bids[0].price} @ {orderbook.result.bids[0].quantity}");
            Console.WriteLine($"Best ask: {orderbook.result.asks[0].price} @ {orderbook.result.asks[0].quantity}");
        };
        
        client.OnConnected += () => Console.WriteLine("✅ Connected to Binance");
        client.OnError += (error) => Console.WriteLine($"❌ Error: {error}");
        
        // Connect and subscribe to markets
        await client.ConnectAsync();
        
        // Using the new Market-based subscription (more efficient)
        var market = new Market("BTC", "USDT");
        await client.SubscribeOrderbookAsync(market);
        await client.SubscribeTradesAsync(market);
        
        // Or using traditional string format (backward compatible)
        await client.SubscribeTickerAsync("BTC/USDT");
        
        // Keep the connection alive
        Console.WriteLine("Press any key to stop...");
        Console.ReadKey();
        
        // Cleanup
        await client.DisconnectAsync();
    }
}
```

### Multi-Exchange Data Collection

```csharp
using CCXT.Collector.Binance;
using CCXT.Collector.Upbit;
using CCXT.Collector.Service;

// Initialize multiple exchanges
var binanceClient = new BinanceWebSocketClient();
var upbitClient = new UpbitWebSocketClient();

// Set up unified callbacks - all exchanges use same data format
Action<STicker> processTicker = (ticker) =>
{
    Console.WriteLine($"[{ticker.exchange}] {ticker.symbol}: " +
                     $"Price={ticker.result.closePrice:F2}, " +
                     $"Volume={ticker.result.volume:F2}");
};

binanceClient.OnTickerReceived += processTicker;
upbitClient.OnTickerReceived += processTicker;

// Connect and subscribe
await Task.WhenAll(
    binanceClient.ConnectAsync(),
    upbitClient.ConnectAsync()
);

// Use Market struct for cleaner code
var btcUsdt = new Market("BTC", "USDT");
var btcKrw = new Market("BTC", "KRW");

await binanceClient.SubscribeTickerAsync(btcUsdt);
await upbitClient.SubscribeTickerAsync(btcKrw);
```

## 📊 Technical Indicators

The library includes 25+ technical indicators. See the [Developer Guide](docs/GUIDE.md#technical-indicators) for the complete list and usage examples.

## ⚙️ Configuration

```json
{
  "appsettings": {
    "websocket.retry.waiting.milliseconds": "600",
    "use.auto.start": "true",
    "auto.start.exchange.name": "binance",
    "auto.start.symbol.names": "BTC/USDT,ETH/USDT"
  }
}
```

## 🏗️ Architecture

For detailed architecture and system design, see the [Developer Guide](docs/GUIDE.md#system-overview).

### Project Structure

```
CCXT.Collector/
├── src/
│   ├── Core/             # Core framework components
│   ├── Models/           # Data models and structures
│   ├── Indicators/       # Technical indicators (25+ indicators)
│   ├── Utilities/        # Utility classes
│   └── exchanges/        # Exchange implementations (132 exchanges)
│       ├── kr/           # South Korea (7 exchanges)
│       ├── us/           # United States (26 exchanges)
│       ├── cn/           # China (24 exchanges)
│       └── ...           # 18 more country/region folders
├── tests/                # Test suites
├── samples/              # Example implementations
└── docs/                 # Documentation
```

## 📚 Documentation

- [Developer Guide](docs/GUIDE.md) - Complete architecture, API reference, and contributing guide
- [Deployment Guide](docs/DEPLOYMENT.md) - Production deployment instructions
- [Roadmap & Tasks](docs/ROADMAP.md) - Development roadmap and current tasks
- [Changelog](docs/CHANGELOG.md) - Version history and release notes

## 🤝 Contributing

We welcome contributions! Please see our [Contributing Guide](docs/GUIDE.md#contributing) for details.

1. Fork the repository
2. Create your feature branch (`git checkout -b feature/AmazingFeature`)
3. Commit your changes (`git commit -m 'Add some AmazingFeature'`)
4. Push to the branch (`git push origin feature/AmazingFeature`)
5. Open a Pull Request

## 📄 License

This project is licensed under the MIT License - see the [LICENSE.txt](LICENSE.txt) file for details.

## 🔗 Links

- [NuGet Package](https://www.nuget.org/packages/CCXT.Collector)
- [GitHub Repository](https://github.com/ccxt-net/ccxt.collector)
- [Documentation Wiki](https://github.com/ccxt-net/ccxt.collector/wiki)
- [Bug Reports](https://github.com/ccxt-net/ccxt.collector/issues)

## 👥 Related Projects

- [CCXT.NET](https://github.com/ccxt-net/ccxt.net) - The base CCXT library for .NET
- [CCXT.Simple](https://github.com/ccxt-net/ccxt.simple) - Simplified exchange interface

## 💬 Support

- **Issues**: [GitHub Issues](https://github.com/ccxt-net/ccxt.collector/issues)
- **Email**: support@ccxt.net
- **Discord**: [Join our Discord](https://discord.gg/ccxt)

## 👥 Team

### **Core Development Team**
- **SEONGAHN** - Lead Developer & Project Architect ([lisa@odinsoft.co.kr](mailto:lisa@odinsoft.co.kr))
- **YUJIN** - Senior Developer & Exchange Integration Specialist ([yoojin@odinsoft.co.kr](mailto:yoojin@odinsoft.co.kr))
- **SEJIN** - Software Developer & API Implementation ([saejin@odinsoft.co.kr](mailto:saejin@odinsoft.co.kr))

---

**Built with ❤️ by the ODINSOFT Team**