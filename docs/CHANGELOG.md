# Changelog

All notable changes to CCXT.Collector will be documented in this file.

---

## Current Status: v2.1.7 (2025-08-14)

**Assessment**: C+ (Requires Security Fixes) | **.NET 8.0/9.0/10.0**

### Status Summary
- ✅ **Architecture**: Clean separation, 15/132 exchanges complete
- ✅ **Performance**: 20-30% faster with System.Text.Json  
- 🔴 **Security**: Critical API key vulnerability
- ✅ **Testing**: 100% coverage for major exchanges

### Implementation Progress

#### Complete (15 exchanges)
Binance, Coinbase, Upbit, OKX, Bybit, Bitget, Huobi, Gate.io, Bithumb, Bittrex, Coinone, Korbit, KuCoin, Crypto.com

#### Priority Targets (65+ incomplete)
1. Kraken, Gemini, Bitstamp (High volume)
2. Deribit, Bitmex (Derivatives)
3. Regional leaders by demand

### Critical Actions Required
🔴 **Immediate**: Secure credential management, input validation, rate limiting  
🟠 **Week 1**: Complete major exchanges, specific exceptions, nullable types  
🟡 **Month 1**: Connection pooling, OpenTelemetry, regional exchanges

---

## [Unreleased] - 2025-08-14

### Added
- Security documentation (GUIDE.md)
- Consolidated technical reference (GUIDE.md)
- .NET 10.0 support

### Changed
- Documentation consolidated (13→4 files) and compressed GUIDE.md by 75%
- Target frameworks: .NET 8.0/9.0/10.0
- SAR indicator now uses StandardDeviationSample

### Removed
- Legacy logging methods (WriteQ/O/X/C)
- Obsolete StandardDeviation (replaced with StandardDeviationSample)
- Redundant documentation files (ANALYSIS, WEBSOCKET, SECURITY, TECHNICAL-REFERENCE)

### Security
- Critical API key vulnerability identified
- Secure patterns documented (Azure/AWS vaults)

---

## [2.1.7] - 2025-08-13

### Added
- Enhanced sample testing utilities (menu system, batch tests)
- Centralized `ExchangeParsingHelpers` for symbols/intervals
- Crypto.com full WebSocket implementation

### Changed
- Refactored 11 exchanges to use centralized parsing
- Standardized instType/symbol formats
- Coinbase: level2→level2_batch

### Fixed
- Crypto.com: Message processing and data parsing
- Coinone: V2 API compatibility
- Coinbase: Authentication and URLs
- Bitget: InstType "sp", symbol format

---

## [2.1.6] - 2025-08-12

### Added
- Unified subscription handling (`MarkSubscriptionActive`)
- Auto-resubscription on reconnect
- WebSocket test suite (15 exchanges)
- ChannelManager with batch support
- Nullable types, SourceLink, analyzers

### Fixed
- KuCoin: Complete protocol rewrite
- Korbit: V2 API implementation

### Performance
- Statistics O(n×period)→O(n) complexity
- JsonExtensions epoch handling to 2100
- LinqExtension optimizations

---

## [2.1.5] - 2025-08-11

### Highlights
- ✅ Newtonsoft.Json→System.Text.Json migration
- 20-30% faster parsing, 15-25% less memory
- Batch subscription management
- Comprehensive test framework

### Batch Subscription Support (11 exchanges)
Upbit, Bithumb, Coinbase, OKX, Korbit, Huobi, Crypto.com, Coinone, Gate.io, Binance, Bitget

---

## Previous Versions

**2.1.4**: Sample improvements, WebSocket stability  
**2.1.3**: 15 exchanges complete implementation  
**2.1.2**: Batch callbacks (90% reduction)  
**2.1.1**: Dynamic buffers, exponential backoff  
**2.1.0**: Market struct, 40-60% symbol conversion improvement  
**2.0.0**: WebSocket architecture (132 exchanges)  
**1.5.2**: REST API baseline

---

## Migration Guide

### v2.1.x → v2.2.x
- Implement ICredentialProvider
- Add input validation
- Update exception handling

### v1.x → v2.x
- REST→WebSocket streaming
- Polling→Subscription model
- New callback system

---

## Support

**GitHub**: https://github.com/ccxt-net/ccxt.collector  
**Issues**: https://github.com/ccxt-net/ccxt.collector/issues