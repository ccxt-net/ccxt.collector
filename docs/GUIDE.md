# CCXT.Collector Developer Guide

## Table of Contents

1. [Architecture](#architecture)
2. [Security](#security)
3. [API Reference](#api-reference)
4. [Contributing](#contributing)

---

## Architecture

### System Overview

CCXT.Collector is a .NET library for real-time cryptocurrency market data collection via WebSocket from 132+ exchanges worldwide, with unified data models and technical indicator calculation.

**Status (v2.1.7)**: 15 exchanges complete | 100% test coverage | System.Text.Json (20-30% faster) | 🔴 Critical: Plain text API key vulnerability

**Key Features**:
- Unified interface across all exchanges
- WebSocket-first with automatic reconnection (exponential backoff, max 60s)
- Event-driven callbacks for real-time data
- 25+ technical indicators
- Batch subscription support (11 exchanges)

### High-Level Architecture

```
Application Layer → CCXT.Collector Library → Exchange WebSocket APIs
                     ├── Callbacks (OnOrderbook, OnTrade, OnTicker...)
                     ├── Technical Indicators (SMA, EMA, RSI, MACD...)
                     ├── Data Transformation (Unified Models)
                     └── WebSocket Management (Connection, Subscription, Auth)
```

### Project Structure

```
src/
├── core/               # Abstractions (IWebSocketClient, WebSocketClientBase)
├── models/             # Market (orderbook, ticker), Trading (account, orders)
├── indicators/         # Trend, Momentum, Volatility, Volume, MarketStrength
├── utilities/          # JsonExtension, TimeExtension, Statistics, CLogger
└── exchanges/          # 132 exchanges by country (kr/, us/, cn/, etc.)
```

### Core Components

**IWebSocketClient Interface**:
```csharp
public interface IWebSocketClient
{
    string ExchangeName { get; }
    bool IsConnected { get; }
    
    // Connection
    Task<bool> ConnectAsync(string apiKey = null, string secretKey = null);
    Task DisconnectAsync();
    
    // Subscriptions
    Task<bool> SubscribeOrderbookAsync(string symbol);
    Task<bool> SubscribeTradesAsync(string symbol);
    Task<bool> SubscribeTickerAsync(string symbol);
    Task<bool> SubscribeCandlesAsync(string symbol, string interval);
    
    // Callbacks
    event Action<string, SOrderBooks> OnOrderbookReceived;
    event Action<string, List<STradeItem>> OnTradeReceived;
    event Action<string, STicker> OnTickerReceived;
    event Action<string, List<SCandleItem>> OnCandleReceived;
}
```

**WebSocketClientBase Features**:
- Exponential backoff reconnection (5s→10s→20s→40s→60s)
- Dynamic buffer management (16KB-2MB)
- Subscription tracking with auto-restoration
- Batch subscription support
- Thread-safe operations

### Data Flow

```
Exchange WebSocket → Parse (System.Text.Json) → Convert to Unified Model → 
Validate → Invoke Callback → User Application
```

### Performance

- **JSON**: System.Text.Json (20-30% faster, 15-25% less memory)
- **Buffer**: Dynamic sizing 16KB-2MB (ArrayPool planned)
- **Reconnection**: Max 10 attempts, exponential backoff
- **Batch**: 11 exchanges support batch subscriptions

### Exchange Implementation

**15 Complete**: Binance, Coinbase, Upbit, OKX, Bybit, Bitget, Huobi, Gate.io, Bithumb, Bittrex, Coinone, Korbit, KuCoin, Crypto.com

| Exchange | Batch | WebSocket URL |
|----------|-------|---------------|
| Binance | ✅ | wss://stream.binance.com:9443/ws |
| Coinbase | ❌ | wss://ws-feed.exchange.coinbase.com |
| Upbit | ✅ | wss://api.upbit.com/websocket/v1 |
| OKX | ✅ | wss://ws.okx.com:8443/ws/v5/public |
| Others | ... | See full list in implementation |

### Technical Indicators (25+)

**Categories**: Trend (SMA, EMA, MACD), Momentum (RSI, ROC), Volatility (Bollinger, ATR), Volume (OBV, ADL), Market Strength (ADX, CCI)

```csharp
var rsi = new RSI(14);
client.OnCandleReceived += (symbol, candles) => {
    foreach (var candle in candles)
        Console.WriteLine($"RSI: {rsi.Calculate(candle)}");
};
```

---

## Security

⚠️ **CRITICAL**: Plain text API key storage vulnerability - immediate fix required

### Current Issues
- API keys stored as plain text in memory
- No input validation for WebSocket messages
- Missing rate limiting

### Secure Implementation

```csharp
// Recommended secure credential provider
public interface ICredentialProvider
{
    Task<SecureCredentials> GetCredentialsAsync(string exchange);
    void ClearCredentials(string exchange);
}

// Azure Key Vault
var keyVaultClient = new SecretClient(
    new Uri("https://vault.vault.azure.net/"),
    new DefaultAzureCredential()
);

// Local development
dotnet user-secrets init
dotnet user-secrets set "Exchanges:Binance:ApiKey" "key"
```

### Input Validation

```csharp
public static bool IsValidSymbol(string symbol)
{
    return Regex.IsMatch(symbol, @"^[A-Z0-9]{2,10}[/_-]?[A-Z0-9]{2,10}$");
}
```

### Rate Limiting

```csharp
public class RateLimiter
{
    private readonly Queue<DateTime> _requestTimes = new();
    
    public async Task<bool> TryExecuteAsync()
    {
        var now = DateTime.UtcNow;
        // Remove old requests, check limit, add new request
        return _requestTimes.Count < _maxRequests;
    }
}
```

### Security Checklist
- [ ] Secure credential storage (Azure/AWS/User Secrets)
- [ ] Input validation for all external data
- [ ] Rate limiting implementation
- [ ] Secure logging (redact sensitive data)
- [ ] Regular security audits

---

## API Reference

### Installation

```bash
dotnet add package CCXT.Collector --version 2.1.7
```

### Basic Usage

```csharp
using CCXT.Collector.Exchanges;

var client = new BinanceWebSocketClient();

// Subscribe to events
client.OnOrderbookReceived += (symbol, orderbook) =>
    Console.WriteLine($"Bid: {orderbook.Bids[0]?.Price}");

// Connect and subscribe
await client.ConnectAsync();
await client.SubscribeOrderbookAsync("BTC/USDT");
```

### Batch Subscriptions (11 exchanges)

```csharp
// Add subscriptions before connecting
client.AddPendingSubscription("orderbook", "BTC/USDT");
client.AddPendingSubscription("trades", "ETH/USDT");
await client.ConnectAndSubscribeAsync();
```

### Data Models

```csharp
public class SOrderBooks
{
    public string Symbol { get; set; }
    public long Timestamp { get; set; }
    public List<SOrderBookItem> Bids { get; set; }
    public List<SOrderBookItem> Asks { get; set; }
}

public class STradeItem
{
    public string TradeId { get; set; }
    public string Side { get; set; }  // "buy" or "sell"
    public decimal Price { get; set; }
    public decimal Quantity { get; set; }
}

public class STicker
{
    public string Symbol { get; set; }
    public decimal High { get; set; }  // 24h
    public decimal Low { get; set; }   // 24h
    public decimal Last { get; set; }
    public decimal Volume { get; set; }
}
```

### Configuration

```json
{
  "CCXT": {
    "Exchanges": {
      "Binance": {
        "Enabled": true,
        "RateLimit": 1200,
        "Symbols": ["BTC/USDT", "ETH/USDT"]
      }
    },
    "Reconnection": {
      "MaxAttempts": 10,
      "InitialDelayMs": 5000,
      "MaxDelayMs": 60000
    }
  }
}
```

### Error Handling

```csharp
client.OnError += (error) => {
    if (error.Contains("rate limit"))
        Thread.Sleep(60000);
    else if (error.Contains("connection closed"))
        Console.WriteLine("Reconnecting...");
};
```

### Troubleshooting

**Connection Issues**: Check network, exchange status, firewall, WebSocket URL
**Auth Failures**: Verify credentials, permissions, IP whitelist
**Missing Data**: Check symbol format, market status, rate limits
**High Memory**: Implement throttling, discard old data, monitor buffers

---

## Contributing

### Getting Started

```bash
# Fork and clone
git clone https://github.com/YOUR_USERNAME/ccxt.collector.git
cd ccxt.collector

# Setup
git remote add upstream https://github.com/ccxt-net/ccxt.collector.git
dotnet restore
dotnet build
dotnet test
```

### Development Process

**Branches**: master (stable), develop (next), feature/*, bugfix/*, hotfix/*

### Adding New Exchange

1. Create: `src/exchanges/[country]/[exchange]/[Exchange]WebSocketClient.cs`
2. Inherit: `WebSocketClientBase`
3. Implement: ProcessMessageAsync, Subscribe methods
4. Convert: Exchange format → Unified models
5. Test: Create tests in `tests/exchanges/`

### Coding Standards

```csharp
// Naming
public class ExchangeClient { }        // PascalCase
private readonly string _apiKey;       // _camelCase
public const int MAX_ATTEMPTS = 5;     // UPPER_CASE

// Async
public async Task<Data> GetDataAsync() // Async suffix
{
    await ProcessAsync().ConfigureAwait(false);
}

// Organization
// 1. Fields → 2. Properties → 3. Events → 4. Constructors → 5. Public → 6. Private
```

### Testing

```csharp
[Fact]
public void RSI_Calculate_ReturnsValidValue()
{
    var rsi = new RSI(14);
    var result = rsi.Calculate(data);
    Assert.InRange(result, 0, 100);
}
```

### Pull Request

1. Update from upstream: `git fetch upstream && git rebase upstream/develop`
2. Run tests: `dotnet test`
3. Format: `dotnet format`
4. PR Title: `[Type] Description` (Feature, Bug, Docs, Refactor, Test)
5. Include: Description, test results, checklist

### Future Enhancements

- [ ] More exchanges (117 pending)
- [ ] Additional indicators
- [ ] Native AOT compilation
- [ ] SIMD optimizations
- [ ] Historical data support

---

**Support**: [GitHub Issues](https://github.com/ccxt-net/ccxt.collector/issues) | support@ccxt.net