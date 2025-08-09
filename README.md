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
- 📦 **RabbitMQ Integration** - Optional message queue support for distributed systems

### 🏢 Supported Exchanges (132 Total)

#### Major Exchanges by Region

| Region | Exchanges | Count |
|--------|-----------|-------|
| 🇺🇸 United States | Coinbase, Kraken, Gemini, Bittrex, Poloniex, Phemex, and 20 more | 26 |
| 🇨🇳 China | Binance*, OKX, Huobi, Bybit, KuCoin, Gate.io, MEXC, and 17 more | 24 |
| 🇰🇷 South Korea | Upbit, Bithumb, Coinone, Korbit, Gopax, Probit, OKCoinKR | 7 |
| 🇯🇵 Japan | bitFlyer, Coincheck, Bitbank, Zaif, and 4 more | 8 |
| 🇪🇺 Europe | Bitstamp, Bitfinex, Bitvavo, EXMO, WhiteBIT, and 8 more | 13 |
| 🇬🇧 United Kingdom | Bitfinex, Bitstamp, CEX.IO, Luno, and 3 more | 7 |
| 🇸🇬 Singapore | BitMEX*, Bitrue, Coins.ph, and 5 more | 8 |
| 🌍 Other Regions | Deribit (UAE), BTC Markets (AU), Bitso (MX), NDAX (CA), and more | 39 |

*Note: Exchange locations indicate registration/headquarters, not service availability

#### Implementation Status

| Feature | Implemented | In Progress | Planned |
|---------|------------|-------------|----------|
| WebSocket Clients | 132 | - | - |
| Korean Exchange WebSockets | 5 (Upbit, Bithumb, Coinone, Korbit, Gopax) | 2 (OKCoinKR, Probit) | - |
| API Documentation | 44 | 88 | - |
| Full Implementation | 5 (Binance, Upbit, Bithumb, Coinone, Korbit) | 8 | 119 |

## 📦 Installation

### NuGet Package Manager
```bash
Install-Package CCXT.Collector -Version 2.1.0
```

### .NET CLI
```bash
dotnet add package CCXT.Collector --version 2.1.0
```

### Package Reference
```xml
<PackageReference Include="CCXT.Collector" Version="2.1.0" />
```

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
  },
  "rabbitmq": {
    "hostName": "localhost",
    "port": "5672",
    "virtualHost": "/",
    "userName": "guest",
    "password": "guest"
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

<a name="korean"></a>
## 📊 한국어 개요

CCXT.Collector는 전 세계 암호화폐 거래소의 WebSocket을 통해 실시간 시장 데이터를 수신하고 기술적 지표를 계산하는 종합적인 라이브러리입니다. 

### ✨ 주요 기능

- 🚀 **실시간 WebSocket 스트리밍** - 저지연 시장 데이터 스트리밍
- 🔄 **통합 데이터 클래스** - 모든 거래소에서 일관된 데이터 형식
- 📈 **25개 이상의 기술 지표** - 거래소/마켓별 실시간 계산
- 🔌 **콜백 아키텍처** - 비동기 이벤트 기반 데이터 처리
- 🔐 **자동 재연결** - 탄력적인 WebSocket 연결 관리
- 📦 **RabbitMQ 통합** - 분산 시스템을 위한 메시지 큐 지원

자세한 내용은 [Developer Guide](docs/GUIDE.md)를 참조하세요.

---

**Built with ❤️ by the ODINSOFT Team**