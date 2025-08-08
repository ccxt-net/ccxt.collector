# CCXT.Collector API Reference

## Table of Contents
1. [WebSocket Client Interface](#websocket-client-interface)
2. [Exchange-Specific Clients](#exchange-specific-clients)
3. [Data Models](#data-models)
4. [Technical Indicators](#technical-indicators)
5. [Configuration](#configuration)
6. [Error Handling](#error-handling)

## WebSocket Client Interface

### IWebSocketClient

The core interface that all exchange WebSocket clients implement.

```csharp
namespace CCXT.Collector.Library
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
        
        // Callback Events
        Action<SOrderBooks> OnOrderbookReceived { get; set; }
        Action<SCompleteOrders> OnTradeReceived { get; set; }
        Action<STicker> OnTickerReceived { get; set; }
        Action<SCandlestick> OnCandleReceived { get; set; }
        Action<SBalance> OnBalanceReceived { get; set; }
        Action<SOrder> OnOrderReceived { get; set; }
        Action<SPosition> OnPositionReceived { get; set; }
        
        // Connection Events
        Action OnConnected { get; set; }
        Action OnDisconnected { get; set; }
        Action<string> OnError { get; set; }
    }
}
```

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
    Console.WriteLine($"Trade: {trade.result.price} @ {trade.result.quantity}");
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

##### CoinbaseWebSocketClient
```csharp
namespace CCXT.Collector.Coinbase
{
    public class CoinbaseWebSocketClient : WebSocketClientBase
    {
        public override string ExchangeName => "Coinbase";
        protected override string WebSocketUrl => "wss://ws-feed.exchange.coinbase.com";
        // Coinbase-specific implementation
    }
}
```

### Exchange Client Usage

```csharp
// Create client for specific exchange
var binanceClient = new BinanceWebSocketClient();
var upbitClient = new UpbitWebSocketClient();
var coinbaseClient = new CoinbaseWebSocketClient();

// All use the same interface
await binanceClient.ConnectAsync();
await binanceClient.SubscribeOrderbookAsync("BTC/USDT");

await upbitClient.ConnectAsync();
await upbitClient.SubscribeOrderbookAsync("BTC/KRW");

await coinbaseClient.ConnectAsync();
await coinbaseClient.SubscribeOrderbookAsync("BTC/USD");
```

## Data Models

### Order Book Data

```csharp
namespace CCXT.Collector.Service
{
    public class SOrderBooks
    {
        public string exchange { get; set; }      // Exchange name
        public string symbol { get; set; }        // Trading pair
        public long timestamp { get; set; }       // Unix timestamp (ms)
        public string sequentialId { get; set; }  // Update sequence ID
        public SOrderBook result { get; set; }    // Order book data
    }
    
    public class SOrderBook
    {
        public long timestamp { get; set; }
        public List<SOrderBookItem> asks { get; set; }
        public List<SOrderBookItem> bids { get; set; }
    }
    
    public class SOrderBookItem
    {
        public decimal price { get; set; }     // Price level
        public decimal quantity { get; set; }  // Quantity at price
        public decimal amount { get; set; }    // price * quantity
        public string action { get; set; }     // "N"=New, "U"=Update, "D"=Delete
        public int count { get; set; }         // Number of orders at level
    }
}
```

### Ticker Data

```csharp
namespace CCXT.Collector.Service
{
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
        public decimal openPrice { get; set; }      // Open price
        public decimal highPrice { get; set; }      // High price
        public decimal lowPrice { get; set; }       // Low price
        public decimal closePrice { get; set; }     // Close/Last price
        public decimal prevClosePrice { get; set; } // Previous close
        public decimal change { get; set; }         // Price change
        public decimal percentage { get; set; }     // Change percentage
        public decimal volume { get; set; }         // Base volume
        public decimal quoteVolume { get; set; }    // Quote volume
        public decimal vwap { get; set; }           // Volume weighted average
        public decimal bidPrice { get; set; }       // Best bid price
        public decimal bidQuantity { get; set; }    // Best bid quantity
        public decimal askPrice { get; set; }       // Best ask price
        public decimal askQuantity { get; set; }    // Best ask quantity
        public long count { get; set; }             // Trade count
    }
}
```

### Trade Data

```csharp
namespace CCXT.Collector.Service
{
    public class SCompleteOrders
    {
        public string exchange { get; set; }
        public string symbol { get; set; }
        public long timestamp { get; set; }
        public SCompleteOrder result { get; set; }
    }
    
    public class SCompleteOrder
    {
        public string orderId { get; set; }      // Trade ID
        public long timestamp { get; set; }      // Trade timestamp
        public SideType sideType { get; set; }   // Bid or Ask
        public OrderType orderType { get; set; } // Market, Limit, etc.
        public decimal price { get; set; }       // Trade price
        public decimal quantity { get; set; }    // Trade quantity
        public decimal amount { get; set; }      // price * quantity
        public decimal fee { get; set; }         // Trading fee
        public string feeCurrency { get; set; }  // Fee currency
    }
    
    public enum SideType
    {
        Unknown = 0,
        Bid = 1,    // Buy
        Ask = 2     // Sell
    }
    
    public enum OrderType
    {
        Unknown = 0,
        Limit = 1,
        Market = 2,
        Stop = 3,
        StopLimit = 4
    }
}
```

### Candlestick Data

```csharp
namespace CCXT.Collector.Service
{
    public class SCandlestick
    {
        public string exchange { get; set; }
        public string symbol { get; set; }
        public string interval { get; set; }    // "1m", "5m", "1h", etc.
        public long timestamp { get; set; }
        public SCandleItem result { get; set; }
    }
    
    public class SCandleItem
    {
        public long openTime { get; set; }      // Candle open time
        public long closeTime { get; set; }     // Candle close time
        public decimal open { get; set; }       // Open price
        public decimal high { get; set; }       // High price
        public decimal low { get; set; }        // Low price
        public decimal close { get; set; }      // Close price
        public decimal volume { get; set; }     // Volume
        public decimal quoteVolume { get; set; } // Quote volume
        public long tradeCount { get; set; }    // Number of trades
        public bool isClosed { get; set; }      // Is candle closed
        public decimal buyVolume { get; set; }  // Taker buy volume
        public decimal buyQuoteVolume { get; set; } // Taker buy quote volume
    }
}
```

### Balance Data

```csharp
namespace CCXT.Collector.Service
{
    public class SBalance
    {
        public string exchange { get; set; }
        public string accountId { get; set; }
        public long timestamp { get; set; }
        public List<SBalanceItem> balances { get; set; }
    }
    
    public class SBalanceItem
    {
        public string currency { get; set; }    // Currency code
        public decimal free { get; set; }       // Available balance
        public decimal used { get; set; }       // Locked balance
        public decimal total { get; set; }      // Total balance
        public long updateTime { get; set; }    // Last update time
    }
}
```

### Order Data

```csharp
namespace CCXT.Collector.Service
{
    public class SOrder
    {
        public string exchange { get; set; }
        public string orderId { get; set; }
        public string clientOrderId { get; set; }
        public string symbol { get; set; }
        public OrderType type { get; set; }
        public OrderSide side { get; set; }
        public OrderStatus status { get; set; }
        public decimal price { get; set; }
        public decimal? stopPrice { get; set; }
        public decimal quantity { get; set; }
        public decimal filledQuantity { get; set; }
        public decimal remainingQuantity { get; set; }
        public decimal avgFillPrice { get; set; }
        public decimal cost { get; set; }
        public decimal fee { get; set; }
        public string feeCurrency { get; set; }
        public long createTime { get; set; }
        public long updateTime { get; set; }
        public string timeInForce { get; set; }
    }
    
    public enum OrderSide
    {
        Buy = 1,
        Sell = 2
    }
    
    public enum OrderStatus
    {
        New = 0,
        Open = 1,
        PartiallyFilled = 2,
        Filled = 3,
        Canceled = 4,
        Rejected = 5,
        Expired = 6
    }
}
```

## Technical Indicators

### Base Indicator Class

```csharp
namespace CCXT.Collector.Indicator
{
    public abstract class IndicatorCalculatorBase<T>
    {
        protected List<Ohlc> OhlcList { get; set; }
        protected int Period { get; set; }
        
        public abstract T Calculate(IOhlcv ohlcv);
        protected abstract T CalculateIndicator();
    }
}
```

### RSI (Relative Strength Index)

```csharp
var rsi = new RSI(14); // 14-period RSI

client.OnCandleReceived += (candle) =>
{
    var value = rsi.Calculate(candle.result);
    Console.WriteLine($"RSI: {value.Value}");
};
```

### MACD (Moving Average Convergence Divergence)

```csharp
var macd = new MACD(12, 26, 9); // Fast=12, Slow=26, Signal=9

client.OnCandleReceived += (candle) =>
{
    var result = macd.Calculate(candle.result);
    Console.WriteLine($"MACD: {result.MACD}, Signal: {result.Signal}, Histogram: {result.Histogram}");
};
```

### Bollinger Bands

```csharp
var bb = new BollingerBand(20, 2); // 20-period, 2 std dev

client.OnCandleReceived += (candle) =>
{
    var result = bb.Calculate(candle.result);
    Console.WriteLine($"Upper: {result.Upper}, Middle: {result.Middle}, Lower: {result.Lower}");
};
```

### Available Indicators

| Category | Indicators |
|----------|------------|
| **Trend** | SMA, EMA, WMA, DEMA, ZLEMA, MACD, SAR |
| **Momentum** | RSI, CMO, Momentum, ROC, TRIX |
| **Volatility** | Bollinger Bands, ATR, Envelope, DPO |
| **Volume** | OBV, ADL, CMF, PVT, VROC |
| **Market Strength** | ADX, Aroon, CCI, WPR |
| **Advanced** | Ichimoku Cloud |

## Configuration

### appsettings.json

```json
{
  "appsettings": {
    "websocket.retry.waiting.milliseconds": "600",
    "websocket.retry.max.attempts": "10",
    "websocket.ping.interval.seconds": "30",
    "use.auto.start": "true",
    "auto.start.exchange.name": "binance",
    "auto.start.symbol.names": "BTC/USDT,ETH/USDT"
  },
  "rabbitmq": {
    "enabled": "false",
    "hostName": "localhost",
    "port": "5672",
    "virtualHost": "/",
    "userName": "guest",
    "password": "guest",
    "rootQName": "ccxt"
  },
  "exchanges": {
    "binance": {
      "websocket.url": "wss://stream.binance.com:9443/ws",
      "api.key": "",
      "api.secret": ""
    },
    "upbit": {
      "websocket.url": "wss://api.upbit.com/websocket/v1",
      "api.key": "",
      "api.secret": ""
    }
  }
}
```

### Programmatic Configuration

```csharp
var config = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .Build();

var settings = new Settings();
config.GetSection("appsettings").Bind(settings);
```

## Error Handling

### Error Callback

```csharp
client.OnError += (error) =>
{
    Console.WriteLine($"Error: {error}");
    // Log error
    // Implement retry logic if needed
};
```

### Connection Error Handling

```csharp
client.OnDisconnected += () =>
{
    Console.WriteLine("Disconnected from exchange");
    // Automatic reconnection is handled by the base class
};
```

### Subscription Error Handling

```csharp
var success = await client.SubscribeOrderbookAsync("BTC/USDT");
if (!success)
{
    Console.WriteLine("Failed to subscribe to orderbook");
    // Retry or handle error
}
```

### Exception Types

| Exception | Description | Handling |
|-----------|-------------|----------|
| `WebSocketException` | WebSocket connection error | Automatic reconnection |
| `JsonException` | JSON parsing error | Log and skip message |
| `ArgumentException` | Invalid parameters | Validate input |
| `TimeoutException` | Operation timeout | Retry with backoff |
| `UnauthorizedException` | Authentication failure | Check API credentials |

## Best Practices

### 1. Resource Management
```csharp
using var client = new BinanceWebSocketClient();
try
{
    await client.ConnectAsync();
    // Use client
}
finally
{
    await client.DisconnectAsync();
}
```

### 2. Error Handling
```csharp
client.OnError += (error) =>
{
    logger.LogError($"WebSocket error: {error}");
    // Implement appropriate error handling
};
```

### 3. Subscription Management
```csharp
// Subscribe to multiple symbols efficiently
var symbols = new[] { "BTC/USDT", "ETH/USDT", "BNB/USDT" };
foreach (var symbol in symbols)
{
    await client.SubscribeOrderbookAsync(symbol);
    await Task.Delay(100); // Rate limiting
}
```

### 4. Data Processing
```csharp
client.OnOrderbookReceived += async (orderbook) =>
{
    // Process asynchronously to avoid blocking
    await Task.Run(() => ProcessOrderbook(orderbook));
};
```

### 5. Memory Management
```csharp
// Limit order book depth
client.OnOrderbookReceived += (orderbook) =>
{
    // Keep only top N levels
    orderbook.result.bids = orderbook.result.bids.Take(20).ToList();
    orderbook.result.asks = orderbook.result.asks.Take(20).ToList();
};
```

## Examples

### Multi-Exchange Arbitrage Monitor

```csharp
public class ArbitrageMonitor
{
    private readonly Dictionary<string, decimal> _prices = new();
    
    public async Task StartMonitoring()
    {
        var binance = new BinanceWebSocketClient();
        var coinbase = new CoinbaseWebSocketClient();
        
        binance.OnTickerReceived += (ticker) =>
        {
            _prices[$"Binance:{ticker.symbol}"] = ticker.result.closePrice;
            CheckArbitrage(ticker.symbol);
        };
        
        coinbase.OnTickerReceived += (ticker) =>
        {
            _prices[$"Coinbase:{ticker.symbol}"] = ticker.result.closePrice;
            CheckArbitrage(ticker.symbol);
        };
        
        await Task.WhenAll(
            binance.ConnectAsync(),
            coinbase.ConnectAsync()
        );
        
        await Task.WhenAll(
            binance.SubscribeTickerAsync("BTC/USDT"),
            coinbase.SubscribeTickerAsync("BTC/USD")
        );
    }
    
    private void CheckArbitrage(string symbol)
    {
        // Compare prices across exchanges
        // Alert if arbitrage opportunity exists
    }
}
```

### Technical Analysis Bot

```csharp
public class TechnicalAnalysisBot
{
    private readonly RSI _rsi = new(14);
    private readonly MACD _macd = new(12, 26, 9);
    private readonly BollingerBand _bb = new(20, 2);
    
    public async Task Start()
    {
        var client = new BinanceWebSocketClient();
        
        client.OnCandleReceived += (candle) =>
        {
            var rsiValue = _rsi.Calculate(candle.result);
            var macdValue = _macd.Calculate(candle.result);
            var bbValue = _bb.Calculate(candle.result);
            
            // Generate trading signals
            if (rsiValue.Value < 30 && candle.result.close < bbValue.Lower)
            {
                Console.WriteLine("Oversold signal detected");
            }
            else if (rsiValue.Value > 70 && candle.result.close > bbValue.Upper)
            {
                Console.WriteLine("Overbought signal detected");
            }
        };
        
        await client.ConnectAsync();
        await client.SubscribeCandlesAsync("BTC/USDT", "1h");
    }
}
```

## Support

For issues, questions, or contributions:
- GitHub Issues: https://github.com/ccxt-net/ccxt.collector/issues
- Email: support@ccxt.net
- Documentation: https://github.com/ccxt-net/ccxt.collector/wiki