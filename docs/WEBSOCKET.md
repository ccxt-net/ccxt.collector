# WebSocket Documentation

Comprehensive WebSocket implementation guide for all supported cryptocurrency exchanges in CCXT.Collector.

## Table of Contents
- [Overview](#overview)
- [Unified Interface](#unified-interface)
- [Batch Subscription Patterns](#batch-subscription-patterns)
- [Exchange-Specific Documentation](#exchange-specific-documentation)
- [Connection Management](#connection-management)
- [Data Format Standards](#data-format-standards)
- [Error Handling](#error-handling)
- [Performance Optimization](#performance-optimization)

## Overview

CCXT.Collector provides a unified WebSocket interface for real-time cryptocurrency market data streaming across 15 major exchanges. Each exchange implementation follows a common pattern while respecting exchange-specific requirements.

### Key Features
- **Real-time Data Streaming**: Low-latency market data with automatic reconnection
- **Unified Data Models**: Consistent data format across all exchanges
- **Batch Subscriptions**: Optimized subscription patterns for each exchange
- **Automatic Reconnection**: Exponential backoff with up to 10 retry attempts
- **GZIP Support**: Automatic decompression for exchanges that use compression
- **High Performance**: System.Text.Json for 20-30% faster parsing

## Unified Interface

All exchange WebSocket clients inherit from `WebSocketClientBase` and implement these standard methods:

```csharp
// Connection Management
Task<bool> ConnectAsync();
Task<bool> DisconnectAsync();
Task<bool> ConnectAndSubscribeAsync();

// Subscription Methods
Task<bool> SubscribeOrderbookAsync(string symbol);
Task<bool> SubscribeTradesAsync(string symbol);
Task<bool> SubscribeTickerAsync(string symbol);
Task<bool> SubscribeCandlesAsync(string symbol, string interval);
Task<bool> UnsubscribeAsync(string channel, string symbol);

// Batch Subscription
void AddSubscription(string channel, string symbol, string interval = null);
void AddSubscriptions(List<(string channel, string symbol, string interval)> subscriptions);

// Callbacks
event Action<SOrderBook> OnOrderbookReceived;
event Action<STrade> OnTradeReceived;
event Action<STicker> OnTickerReceived;
event Action<SCandle> OnCandleReceived;
```

## Batch Subscription Patterns

Different exchanges have different requirements for WebSocket subscriptions. The batch subscription feature provides an optimized way to handle these patterns:

### Pattern Types

| Pattern | Description | Exchanges |
|---------|-------------|-----------|
| **Single Message** | ALL subscriptions must be in one message | Upbit |
| **Channel Grouping** | Group by channel type, separate messages | Bithumb, Gate.io |
| **Unified Array** | All subscriptions in single args/params array | OKX, Binance |
| **Array Batch** | Multiple symbols in array per subscription type | Korbit (v2) |
| **Individual Messages** | Separate message for each subscription | Huobi, Coinone |
| **Batched Messages** | Limited number of channels per message | Crypto.com |
| **Flexible Product IDs** | Global or per-channel product ID specification | Coinbase |

### Performance Considerations

| Exchange | Strategy | Limit/Delay | Notes |
|----------|----------|-------------|-------|
| Upbit | Single message | None | Critical - resending overrides previous |
| Bithumb | Channel groups | None | Can optimize by grouping |
| OKX | Single message | None | Efficient single message |
| Korbit | Array batch | None | v2 API with efficient batching |
| Huobi | Individual | 50ms delay | Prevents rate limiting |
| Crypto.com | Batched | 100 channels/msg | Balances efficiency and load |
| Coinone | Individual | 50ms delay | Respects connection limit |
| Gate.io | Channel groups | 50ms delay | Special formats for orderbook/candles |
| Binance | Batched | 200 streams/msg | Automatic batching for large sets |
| Coinbase | Flexible grouping | None | Global or per-channel product IDs, must subscribe within 5s |

## Exchange-Specific Documentation

### 1. Binance
- **Location**: `src/exchanges/hk/binance/`
- **WebSocket URL**: `wss://stream.binance.com:9443/ws`
- **Documentation**: https://developers.binance.com/docs/binance-spot-api-docs/websocket-api
- **Supported Markets**: USDT, BUSD, BTC, ETH, BNB

#### Features
- Multiple streams in single subscription (up to 200)
- Stream format: `{symbol}@{channel}` (e.g., `btcusdt@depth@100ms`)
- Automatic stream batching for large subscriptions
- 3-minute ping interval

#### Subscription Format
```json
{
  "method": "SUBSCRIBE",
  "params": ["btcusdt@depth@100ms", "btcusdt@trade", "ethusdt@ticker"],
  "id": 1234567890
}
```

### 2. Upbit (Korea)
- **Location**: `src/exchanges/kr/upbit/`
- **WebSocket URL**: `wss://api.upbit.com/websocket/v1`
- **Documentation**: https://docs.upbit.com/docs/upbit-quotation-websocket
- **Supported Markets**: KRW, USDT, BTC

#### Features
- **CRITICAL**: All subscriptions must be sent in a single message
- Format: KRW-BTC (reversed from standard)
- 2-minute ping interval
- No explicit unsubscribe support

#### Subscription Format
```json
[
  {"ticket": "unique-id"},
  {"type": "orderbook", "codes": ["KRW-BTC", "KRW-ETH"]},
  {"type": "trade", "codes": ["KRW-BTC"]},
  {"format": "SIMPLE"}
]
```

### 3. Bithumb (Korea)
- **Location**: `src/exchanges/kr/bithumb/`
- **WebSocket URL**: `wss://pubwss.bithumb.com/pub/ws`
- **Documentation**: https://apidocs.bithumb.com/
- **Supported Markets**: KRW

#### Features
- Separate messages per channel type
- Multiple symbols per channel supported
- Format: BTC_KRW (underscore separator)
- No explicit unsubscribe (requires resubscription)

#### Subscription Format
```json
{
  "type": "ticker",
  "symbols": ["BTC_KRW", "ETH_KRW", "XRP_KRW"],
  "tickTypes": ["24H"]
}
```

### 4. OKX
- **Location**: `src/exchanges/cn/okx/`
- **WebSocket URL**: `wss://ws.okx.com:8443/ws/v5/public`
- **Documentation**: https://www.okx.com/docs-v5/en/
- **Supported Markets**: USDT, USDC, BTC, ETH

#### Features
- Unified args array for all subscriptions
- Instrument type specification (SPOT, FUTURES, etc.)
- Format: BTC-USDT (hyphen separator)
- Efficient single message subscription

#### Subscription Format
```json
{
  "op": "subscribe",
  "args": [
    {"channel": "books", "instId": "BTC-USDT"},
    {"channel": "trades", "instId": "BTC-USDT"},
    {"channel": "tickers", "instId": "ETH-USDT"}
  ]
}
```

### 5. Huobi
- **Location**: `src/exchanges/cn/huobi/`
- **WebSocket URL**: `wss://api.huobi.pro/ws`
- **Documentation**: https://huobiapi.github.io/docs/spot/v1/en/
- **Supported Markets**: USDT, BTC, ETH, HT

#### Features
- **GZIP compression** for all messages
- Individual subscription messages
- Topic format: `market.{symbol}.{channel}`
- 20-second ping interval
- 50ms delay between subscriptions

#### Subscription Format
```json
{
  "sub": "market.btcusdt.depth.step0",
  "id": "id1"
}
```

### 6. Crypto.com
- **Location**: `src/exchanges/us/crypto/`
- **WebSocket URL**: `wss://stream.crypto.com/v2/market`
- **Documentation**: https://exchange-docs.crypto.com/exchange/v1/rest-ws/
- **Supported Markets**: USDT, USDC, USD, CRO

#### Features
- Batch up to 100 channels per message
- Format: BTC_USDT (underscore separator)
- Automatic batching for large subscriptions
- 30-second heartbeat interval

#### Subscription Format
```json
{
  "id": 1,
  "method": "subscribe",
  "params": {
    "channels": ["ticker.BTC_USDT", "trade.ETH_USDT", "book.SOL_USDT.10"]
  },
  "nonce": 1234567890
}
```

### 7. Gate.io
- **Location**: `src/exchanges/cn/gateio/`
- **WebSocket URL**: `wss://api.gateio.ws/ws/v4/`
- **Documentation**: https://www.gate.io/docs/developers/apiv4/ws/en/
- **Supported Markets**: USDT, BTC, ETH, GT

#### Features
- Multiple symbols in payload array per channel
- Special handling for orderbook (depth/interval parameters)
- Format: BTC_USDT (underscore separator)
- Channel-specific payload formats

#### Subscription Format
```json
{
  "time": 1234567890,
  "channel": "spot.tickers",
  "event": "subscribe",
  "payload": ["BTC_USDT", "ETH_USDT", "SOL_USDT"]
}
```

### 8. Korbit (Korea)
- **Location**: `src/exchanges/kr/korbit/`
- **WebSocket URL**: `wss://ws-api.korbit.co.kr/v2/public` (v2 API)
- **Documentation**: https://docs.korbit.co.kr/#WebSocket
- **Supported Markets**: KRW

#### Features
- Array-based message format for v2 API
- Format: btc_krw (lowercase with underscore)
- Supports multiple symbols per subscription
- 60-second ping interval
- Message type-based routing (ticker, orderbook, trade)

#### Subscription Format (v2 API)
```json
[
  {
    "method": "subscribe",
    "type": "ticker",
    "symbols": ["btc_krw", "eth_krw", "xrp_krw"]
  }
]
```

### 9. Coinone (Korea)
- **Location**: `src/exchanges/kr/coinone/`
- **WebSocket URL**: `wss://stream.coinone.co.kr`
- **Documentation**: https://docs.coinone.co.kr/reference/public-websocket
- **Supported Markets**: KRW

#### Features
- Individual messages for each subscription
- Topic-based subscription with quote/target currency
- Format: btc_krw (lowercase with underscore)
- 50ms delay between messages (20 connections/IP limit)

#### Subscription Format
```json
{
  "request_type": "SUBSCRIBE",
  "channel": "ORDERBOOK",
  "topic": {
    "quote_currency": "krw",
    "target_currency": "btc"
  },
  "format": "DEFAULT"
}
```

### 10. Bybit
- **Location**: `src/exchanges/cn/bybit/`
- **WebSocket URL**: `wss://stream.bybit.com/v5/public/spot`
- **Documentation**: https://bybit-exchange.github.io/docs/v5/ws/connect
- **Supported Markets**: USDT, USDC, BTC, ETH

#### Features
- Multiple subscriptions in args array
- Category specification (spot, linear, inverse)
- Format: BTCUSDT (no separator)
- 20-second ping interval

### 11. Bitget
- **Location**: `src/exchanges/cn/bitget/`
- **WebSocket URL**: `wss://ws.bitget.com/spot/v1/stream`
- **Documentation**: 
  - https://www.bitget.com/api-doc/spot/websocket/intro
  - https://www.bitget.com/api-doc/spot/websocket/public/
- **Supported Markets**: USDT, USDC, BTC, ETH, BGB

#### Features
- Multiple args in single subscription (up to 100)
- InstType specification (SPOT for spot trading)
- Format: BTCUSDT_SPBL (symbol with _SPBL suffix)
- Channel-based subscription model
- 30-second ping interval

#### Subscription Format
```json
{
  "op": "subscribe",
  "args": [
    {"instType": "SPOT", "channel": "books", "instId": "BTCUSDT_SPBL"},
    {"instType": "SPOT", "channel": "trade", "instId": "ETHUSDT_SPBL"},
    {"instType": "SPOT", "channel": "ticker", "instId": "SOLUSDT_SPBL"},
    {"instType": "SPOT", "channel": "candle1m", "instId": "BTCUSDT_SPBL"}
  ]
}
```

### 12. Kucoin
- **Location**: `src/exchanges/cn/kucoin/`
- **WebSocket URL**: Dynamic (requires token endpoint)
- **Documentation**: https://docs.kucoin.com/
- **Supported Markets**: USDT, USDC, BTC, ETH, KCS

#### Features
- Requires token endpoint for WebSocket URL
- Topic-based subscription model
- Format: BTC-USDT (hyphen separator)

### 13. Coinbase
- **Location**: `src/exchanges/us/coinbase/`
- **WebSocket URL**: `wss://ws-feed.exchange.coinbase.com`
- **Documentation**: 
  - https://docs.cdp.coinbase.com/exchange/websocket-feed/overview
  - https://docs.cdp.coinbase.com/exchange/websocket-feed/channels
- **Supported Markets**: USD, USDT, USDC, EUR, GBP

#### Features
- Multiple product IDs per subscription message
- Channel-specific or global product ID specification
- Format: BTC-USD (hyphen separator)
- Heartbeat channel for connection monitoring
- Must subscribe within 5 seconds of connection
- Supports WebSocket compression (permessage-deflate)

#### Subscription Format
```json
{
  "type": "subscribe",
  "product_ids": ["BTC-USD", "ETH-USD", "SOL-USD"],
  "channels": [
    "heartbeat",
    {
      "name": "level2",
      "product_ids": ["BTC-USD", "ETH-USD"]
    },
    {
      "name": "ticker",
      "product_ids": ["BTC-USD", "SOL-USD"]
    }
  ]
}
```

### 14. Bittrex (Closed)
- **Location**: `src/exchanges/us/bittrex/`
- **Status**: Exchange closed in December 2023
- **Protocol**: SignalR-based (Hub: c3)

### 15. Gopax (Korea)
- **Location**: `src/exchanges/kr/gopax/`
- **WebSocket URL**: `wss://wsapi.gopax.co.kr`
- **Supported Markets**: KRW

## Connection Management

### Automatic Reconnection
- Exponential backoff starting at 1 second
- Maximum delay of 60 seconds
- Up to 10 retry attempts
- Automatic resubscription on reconnect

### Ping/Pong Handling
- Exchange-specific ping intervals (20s to 180s)
- Automatic ping message sending
- Connection health monitoring

### Buffer Management
- Dynamic buffer resizing for large messages
- Initial buffer size: 16KB
- Automatic expansion for large data

## Data Format Standards

### Unified Models

#### SOrderBook
```csharp
{
  exchange: string,
  symbol: string,
  timestamp: long,
  result: {
    bids: [{ price, quantity, amount }],
    asks: [{ price, quantity, amount }]
  }
}
```

#### STrade
```csharp
{
  exchange: string,
  symbol: string,
  timestamp: long,
  result: [
    { tradeId, price, quantity, amount, sideType, timestamp }
  ]
}
```

#### STicker
```csharp
{
  exchange: string,
  symbol: string,
  timestamp: long,
  result: {
    openPrice, highPrice, lowPrice, closePrice,
    volume, quoteVolume, change, percentage,
    bidPrice, askPrice, bidQuantity, askQuantity
  }
}
```

#### SCandle
```csharp
{
  exchange: string,
  symbol: string,
  interval: string,
  timestamp: long,
  result: [
    { openTime, closeTime, open, high, low, close, volume }
  ]
}
```

## Error Handling

### Error Categories
1. **Connection Errors**: Network failures, WebSocket disconnections
2. **Subscription Errors**: Invalid symbols, unauthorized channels
3. **Data Errors**: Parsing failures, invalid data format
4. **Rate Limit Errors**: Too many requests, connection limits

### Error Recovery
- Automatic reconnection for connection errors
- Resubscription queue for failed subscriptions
- Error callbacks for application-level handling
- Graceful degradation for non-critical errors

## Performance Optimization

### Best Practices
1. **Use Batch Subscriptions**: Reduce connection overhead
2. **Limit Subscriptions**: Only subscribe to needed data
3. **Process Asynchronously**: Use callbacks efficiently
4. **Monitor Memory**: Dispose JsonDocument properly
5. **Handle Backpressure**: Implement queuing if needed

### Optimization Techniques
- System.Text.Json for faster parsing (20-30% improvement)
- Connection pooling for multiple subscriptions
- Efficient symbol conversion caching
- Minimal object allocation in hot paths

### Rate Limiting Guidelines
| Exchange | Connection Limit | Subscription Limit | Rate Limit |
|----------|-----------------|-------------------|------------|
| Binance | 5 connections | 200 streams/conn | 5000 req/min |
| Upbit | 5 connections | 5 subscriptions | - |
| Huobi | - | Minimize topics | - |
| Crypto.com | - | 100 channels/msg | 100 req/sec |
| Coinone | 20 conn/IP | - | 90 req/min |

## Testing

### Test Coverage
- Connection establishment tests
- Subscription/unsubscription tests
- Data reception validation
- Reconnection scenarios
- Error handling verification

### Test Commands
```bash
# Run all WebSocket tests
dotnet test --filter "Category=WebSocket"

# Test specific exchange
dotnet test --filter "Exchange=Binance"

# Test by region
dotnet test --filter "Region=Korea"
```

## Troubleshooting

### Common Issues

1. **Connection Timeout**
   - Check WebSocket URL
   - Verify network connectivity
   - Check firewall/proxy settings

2. **Subscription Failures**
   - Verify symbol format
   - Check rate limits
   - Ensure proper authentication (private channels)

3. **Data Not Received**
   - Verify subscription success
   - Check callback registration
   - Monitor error callbacks

4. **GZIP Decompression Errors** (Huobi)
   - Ensure proper GZIP handling
   - Check buffer sizes
   - Verify message completeness

## Migration Notes

### From REST to WebSocket
1. Replace polling loops with subscriptions
2. Implement callback handlers
3. Add connection management
4. Handle reconnection scenarios

### Version Compatibility
- v2.1.5+: System.Text.Json (faster)
- v2.1.3+: Batch subscription support
- v2.0.0+: WebSocket-first architecture

## Contributing

When adding new exchange WebSocket support:

1. **Inherit from WebSocketClientBase**
2. **Implement required abstract methods**
3. **Add batch subscription support if applicable**
4. **Follow unified data model standards**
5. **Include comprehensive error handling**
6. **Add unit tests**
7. **Update this documentation**

## References

### Official Documentation
- [Binance WebSocket Docs](https://developers.binance.com/docs/binance-spot-api-docs/websocket-api)
- [Upbit WebSocket Docs](https://docs.upbit.com/docs/upbit-quotation-websocket)
- [Bithumb API Docs](https://apidocs.bithumb.com/)
- [OKX WebSocket Docs](https://www.okx.com/docs-v5/en/)
- [Huobi WebSocket Docs](https://huobiapi.github.io/docs/spot/v1/en/)
- [Crypto.com WebSocket Docs](https://exchange-docs.crypto.com/exchange/v1/rest-ws/)
- [Gate.io WebSocket Docs](https://www.gate.io/docs/developers/apiv4/ws/en/)
- [Korbit API Docs](https://apidocs.korbit.co.kr/)
- [Coinone WebSocket Docs](https://docs.coinone.co.kr/reference/public-websocket)

### Related Files
- [CHANGELOG.md](CHANGELOG.md) - Version history and updates
- [GUIDE.md](GUIDE.md) - Developer guide
- [API.md](API.md) - API reference
- [README.md](../README.md) - Project overview