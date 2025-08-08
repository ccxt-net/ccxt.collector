# CCXT.Collector

[![NuGet](https://img.shields.io/nuget/v/CCXT.Collector.svg)](https://www.nuget.org/packages/CCXT.Collector)
[![License](https://img.shields.io/github/license/ccxt-net/ccxt.collector)](https://github.com/ccxt-net/ccxt.collector/blob/master/LICENSE.txt)
[![Build Status](https://img.shields.io/github/actions/workflow/status/ccxt-net/ccxt.collector/build.yml)](https://github.com/ccxt-net/ccxt.collector/actions)
[![.NET](https://img.shields.io/badge/.NET-8.0%20|%209.0-512BD4)](https://dotnet.microsoft.com)

A powerful .NET library for real-time cryptocurrency exchange data collection with unified WebSocket streaming and technical indicator analysis.

[English](#english) | [한국어](#korean)

---

<a name="english"></a>
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
| API Documentation | 44 | 88 | - |
| Full Implementation | 3 (Binance, Upbit, Bithumb) | 10 | 119 |

## 📦 Installation

### NuGet Package Manager
```bash
Install-Package CCXT.Collector -Version 2.0.0
```

### .NET CLI
```bash
dotnet add package CCXT.Collector --version 2.0.0
```

### Package Reference
```xml
<PackageReference Include="CCXT.Collector" Version="2.0.0" />
```

## 🚀 Quick Start

### Basic WebSocket Connection

```csharp
using CCXT.Collector.Binance;
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
        
        client.OnTradeReceived += (trade) =>
        {
            Console.WriteLine($"Trade: {trade.symbol} - Price: {trade.result.price}, Amount: {trade.result.amount}");
        };
        
        client.OnConnected += () => Console.WriteLine("✅ Connected to Binance");
        client.OnError += (error) => Console.WriteLine($"❌ Error: {error}");
        
        // Connect and subscribe to markets
        await client.ConnectAsync();
        await client.SubscribeOrderbookAsync("BTC/USDT");
        await client.SubscribeTradesAsync("BTC/USDT");
        await client.SubscribeTickerAsync("BTC/USDT");
        
        // Keep the connection alive
        Console.ReadLine();
        
        // Cleanup
        await client.DisconnectAsync();
    }
}
```

### Technical Indicator Analysis

```csharp
using CCXT.Collector.Indicator;
using System;

class TechnicalAnalysis
{
    static void SetupIndicators(ExchangeClient client)
    {
        // Create indicator calculators
        var rsi = new RSI(14);
        var macd = new MACD(12, 26, 9);
        var bb = new BollingerBand(20, 2);
        
        // Register callback for OHLCV data
        client.OnOhlcvReceived += (ohlcv) =>
        {
            // Update indicators with new data
            rsi.Calculate(ohlcv);
            macd.Calculate(ohlcv);
            bb.Calculate(ohlcv);
            
            // Get indicator values
            Console.WriteLine($"RSI: {rsi.Value}");
            Console.WriteLine($"MACD: {macd.MACD}, Signal: {macd.Signal}");
            Console.WriteLine($"BB Upper: {bb.Upper}, Middle: {bb.Middle}, Lower: {bb.Lower}");
        };
    }
}
```

### Multi-Exchange Data Collection

```csharp
using CCXT.Collector.Binance;
using CCXT.Collector.Upbit;
using CCXT.Collector.Bithumb;
using System.Threading.Tasks;

class MultiExchangeCollector
{
    public async Task StartCollection()
    {
        // Initialize WebSocket clients for multiple exchanges
        var binanceClient = new BinanceWebSocketClient();
        var upbitClient = new UpbitWebSocketClient();
        var bithumbClient = new BithumbWebSocketClient();
        
        // Set up unified callbacks - all exchanges use same data format
        binanceClient.OnTickerReceived += (ticker) =>
        {
            ProcessUnifiedTicker("Binance", ticker);
        };
        
        upbitClient.OnTickerReceived += (ticker) =>
        {
            ProcessUnifiedTicker("Upbit", ticker);
        };
        
        bithumbClient.OnTickerReceived += (ticker) =>
        {
            ProcessUnifiedTicker("Bithumb", ticker);
        };
        
        // Connect all exchanges
        await Task.WhenAll(
            binanceClient.ConnectAsync(),
            upbitClient.ConnectAsync(),
            bithumbClient.ConnectAsync()
        );
        
        // Subscribe to markets
        await binanceClient.SubscribeTickerAsync("BTC/USDT");
        await upbitClient.SubscribeTickerAsync("BTC/KRW");
        await bithumbClient.SubscribeTickerAsync("BTC/KRW");
    }
    
    private void ProcessUnifiedTicker(string exchange, STicker ticker)
    {
        // All data is in unified format regardless of exchange
        Console.WriteLine($"[{exchange}] {ticker.symbol}: Price={ticker.result.closePrice:F2}, " +
                         $"Volume={ticker.result.volume:F2}, Change={ticker.result.percentage:F2}%");
    }
}
```

## 📊 Available Technical Indicators

### Trend Indicators
- **SMA** (Simple Moving Average)
- **EMA** (Exponential Moving Average)
- **WMA** (Weighted Moving Average)
- **DEMA** (Double Exponential Moving Average)
- **ZLEMA** (Zero Lag Exponential Moving Average)
- **MACD** (Moving Average Convergence Divergence)
- **SAR** (Parabolic SAR)

### Momentum Indicators
- **RSI** (Relative Strength Index)
- **CMO** (Chande Momentum Oscillator)
- **Momentum**
- **ROC** (Rate of Change)
- **TRIX** (Triple Exponential Average)

### Volatility Indicators
- **Bollinger Bands**
- **ATR** (Average True Range)
- **Envelope**
- **DPO** (Detrended Price Oscillator)

### Volume Indicators
- **OBV** (On Balance Volume)
- **ADL** (Accumulation/Distribution Line)
- **CMF** (Chaikin Money Flow)
- **PVT** (Price Volume Trend)
- **VROC** (Volume Rate of Change)

### Market Strength
- **ADX** (Average Directional Index)
- **Aroon**
- **CCI** (Commodity Channel Index)
- **WPR** (Williams %R)

### Advanced
- **Ichimoku Cloud**

## ⚙️ Configuration

### appsettings.json

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

### WebSocket-Based Architecture

```
CCXT.Collector/
├── src/
│   ├── library/          # Core library components (CCXT.Collector.Library)
│   │   ├── IWebSocketClient.cs      # WebSocket interface with callbacks
│   │   ├── WebSocketClientBase.cs   # Base WebSocket implementation
│   │   ├── config.cs                # Configuration management
│   │   ├── restclient.cs            # REST API client
│   │   └── ...                      # Factory, settings, extensions
│   ├── service/          # Service layer (CCXT.Collector.Service)
│   │   ├── orderbook.cs             # Order book data models
│   │   ├── ticker.cs                # Ticker data models
│   │   ├── trading.cs               # Trading data models
│   │   ├── complete.cs              # Complete order models
│   │   └── ...                      # OHLCV, account, candle models
│   ├── indicator/        # Technical indicators (CCXT.Collector.Indicator)
│   │   ├── IndicatorCalculatorBase.cs # Base indicator class
│   │   ├── RSI.cs, MACD.cs, SMA.cs   # Momentum indicators
│   │   ├── BollingerBand.cs, ATR.cs  # Volatility indicators
│   │   └── ...                       # 25+ indicators total
│   └── exchanges/        # Exchange implementations (by country code)
│       ├── kr/           # South Korea (7 exchanges)
│       │   ├── upbit/UpbitWebSocketClient.cs
│       │   ├── bithumb/BithumbWebSocketClient.cs
│       │   └── coinone/, korbit/, gopax/, probit/, okcoinkr/
│       ├── us/           # United States (26 exchanges)
│       │   ├── coinbase/CoinbaseWebSocketClient.cs
│       │   ├── kraken/KrakenWebSocketClient.cs
│       │   └── gemini/, bittrex/, poloniex/, and 21 more/
│       ├── cn/           # China (24 exchanges)
│       │   ├── okx/OkxWebSocketClient.cs
│       │   ├── huobi/HuobiWebSocketClient.cs
│       │   └── bybit/, kucoin/, gateio/, mexc/, and 18 more/
│       ├── hk/           # Hong Kong
│       │   └── binance/BinanceWebSocketClient.cs
│       └── ...           # 18 more country/region folders
├── tests/
│   ├── exchanges/        # Exchange-specific test suites
│   │   ├── BinanceTests.cs
│   │   ├── UpbitTests.cs
│   │   └── BithumbTests.cs
│   └── Program.cs        # Test orchestrator
├── samples/
│   ├── exchanges/        # Exchange-specific examples
│   │   ├── BinanceSample.cs
│   │   ├── UpbitSample.cs
│   │   └── BithumbSample.cs
│   └── WebSocketExample.cs  # WebSocket usage examples
└── docs/                 # Documentation
    ├── ARCHITECTURE.md   # System architecture
    ├── API_REFERENCE.md  # API documentation
    ├── CHANGELOG.md      # Version history
    └── MIGRATION_GUIDE.md # Migration guide
```

### Data Flow

1. **WebSocket Connection** → Each exchange client maintains persistent WebSocket connection
2. **Subscribe to Channels** → Subscribe to orderbook, trades, ticker channels per symbol
3. **Receive & Process** → Raw data converted to unified format
4. **Callback Invocation** → Direct delivery to registered callback functions
5. **Technical Analysis** → Optional indicator calculation on received data

## 🤝 Contributing

We welcome contributions! Please see our [Contributing Guide](docs/CONTRIBUTING.md) for details.

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
- [Documentation](https://github.com/ccxt-net/ccxt.collector/wiki)
- [Bug Reports](https://github.com/ccxt-net/ccxt.collector/issues)

## 👥 Related Projects

- [CCXT.NET](https://github.com/ccxt-net/ccxt.net) - The base CCXT library for .NET
- [CCXT.Simple](https://github.com/ccxt-net/ccxt.simple) - Simplified exchange interface

---

<a name="korean"></a>
## 📊 개요

CCXT.Collector는 전 세계 암호화폐 거래소의 WebSocket을 통해 실시간 시장 데이터를 수신하고 기술적 지표를 계산하는 종합적인 라이브러리입니다. 여러 거래소의 데이터를 처리하기 위한 통합 인터페이스를 제공하여 트레이딩 봇, 시장 분석 도구, 데이터 수집 시스템을 쉽게 구축할 수 있습니다.

### ✨ 주요 기능

- 🚀 **실시간 WebSocket 스트리밍** - 저지연 시장 데이터 스트리밍
- 🔄 **통합 데이터 클래스** - 모든 거래소에서 일관된 데이터 형식
- 📈 **25개 이상의 기술 지표** - 거래소/마켓별 실시간 계산
- 🔌 **콜백 아키텍처** - 비동기 이벤트 기반 데이터 처리
- 🔐 **자동 재연결** - 탄력적인 WebSocket 연결 관리
- 📦 **RabbitMQ 통합** - 분산 시스템을 위한 메시지 큐 지원

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
