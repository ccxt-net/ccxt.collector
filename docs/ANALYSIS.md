# CCXT.Collector 코드베이스 기술 분석

# CCXT.Collector Technical Codebase Analysis

Created: 2025-08-12  
Last Updated: 2025-08-12 (includes latest commits)  
Target Branch: `master`  
Baseline Version: v2.1.5 (Newtonsoft → System.Text.Json migration completed)  
Scope of Update: batch subscription / SubscriptionInfo / automatic resubscription implementation commits included

---
## 1. Overview
`CCXT.Collector` is a .NET library that unifies multi-exchange cryptocurrency WebSocket real-time data (order book, trades, ticker, candles) plus private channels (balance, orders, positions). Core structure: a shared abstraction (`WebSocketClientBase`), per-exchange implementations, and a `ChannelManager` that tracks subscription state/statistics.

### Primary Goal Areas
1. Provide standardized event streams across multiple exchanges
2. Reusable WebSocket connection + subscription abstraction
3. Lightweight JSON parsing (System.Text.Json + extension helpers)
4. Base validation via integration tests (xUnit)

---
## 2. Architecture Layers
| Layer | Path | Role |
|-------|------|------|
| Abstractions | `src/core/abstractions` | WebSocket interfaces & base (connect/reconnect/subscribe abstraction) |
| Infrastructure | `src/core/infrastructure` | Channel/subscription state (`ChannelManager`) |
| Exchanges | `src/exchanges/*` | Exchange-specific WebSocket clients (symbols/message parsing/auth) |
| Models | `src/models/*` | Standardized DTOs (`STicker`, `STrade`, `SOrderBook`, `SCandle`) |
| Utilities | `src/utilities` | JSON/Time/Stats extensions (`JsonExtensions`, `TimeExtension`, etc.) |
| Tests | `tests` | Exchange WebSocket integration tests |
| Samples | `samples` | Usage examples |

### Core Flow (Simplified)
1. User registers an exchange client: `ChannelManager.RegisterExchangeClient()`
2. User records intended subscriptions: `RegisterChannelAsync()` (pending state)
3. User triggers batch application: `ApplyBatchSubscriptionsAsync()` → connect + execute subscriptions
4. Exchange client receives raw message → parse → `Invoke*Callback()` → external handlers
5. `ChannelManager` currently lacks direct hooks for metrics; message counts would need an observer extension

---
## 3. WebSocketClientBase Analysis
| Aspect | Strength | Limitation / Risk |
|--------|----------|-------------------|
| Connect/Reconnect | Simple & clear, 60s cap | No jitter (thundering herd risk), prior gaps now improved but still tunable |
| Subscription Management | `_subscriptions` + `SubscriptionInfo` (creation/active/last timestamps) | Auto-restore implemented (`RestoreActiveSubscriptionsAsync` + `ResubscribeAsync`), but (1) no retry threshold policy (2) private vs public channel recovery not differentiated |
| Ping Handling | Timer + overridable | Heartbeat abstraction thin (no explicit Pong hook) |
| Receive Loop | Handles text/binary, dynamic buffer growth | No ArrayPool → potential GC pressure for large payloads |
| Error Handling | Catch + reconnect | Single parse failure can still escalate to reconnect (needs threshold) |
| Auth | Supports separate/private socket selection | Response validation & timeout logic minimal |

### Reconnect Design Status & Further Improvements
Current: `RestoreActiveSubscriptionsAsync()` snapshots active (`IsActive=true`) entries and re-subscribes by channel type (`orderbook`, `trades`, `ticker`, `candles`). Successful paths invoke `MarkSubscriptionActive()` to update timestamps.

Needed Improvements:
1. Retry/backoff & cumulative failure threshold events (`OnPermanentFailure` missing)
2. Exponential backoff + random jitter (current approach monotonic without jitter)
3. Separate sequencing of private (authenticated) vs public socket recovery
4. Concurrency-limited parallel resubscribe for large subscription sets (currently strictly sequential)
5. Formal key normalization strategy for variant channel keys (kline/candles variants) via strategy abstraction

---
## 4. ChannelManager Analysis
| Item | Current | Suggested Improvement |
|------|---------|-----------------------|
| Pending Subscription Store | `ConcurrentDictionary<string,List<>>` + lock | Immutable snapshot or ConcurrentQueue + periodic drain |
| Post-event Channel Metrics | Manual update required | Inject observer hook (`IChannelObserver`) from base callbacks |
| Disconnect on Empty | Immediate | Grace period (30–60s) configurable idle timeout |
| Batch Subscription | Sequential loop | Exchange capability aware branching / limited parallelization |

---
## 5. JsonExtensions Review (`src/utilities/JsonExtension.cs`)
### Strengths
* Safe parsing across string/number ambiguity
* Epoch second vs millisecond heuristic (`>= 1_000_000_000_000`)
* Consistent null/undefined checks and access patterns

### Limitations & Suggestions
| Method/Pattern | Issue | Improvement |
|---------------|-------|-------------|
| `TryGetArray` | Cannot distinguish missing vs empty | Allow empty or add `TryGetNonEmptyArray` |
| `GetUnixTimeOrDefault` | No direct numeric epoch handling for all forms | Add numeric branch & ISO fallback |
| `GetDateTimeOffsetOrDefault` | Magic number heuristic embedded | Externalize thresholds + injection policy |
| Silent default returns | Debug visibility low | Optional diagnostics logging hook in DEBUG |
| `FirstOrUndefined` | Returns default(JsonElement) sentinel | Provide `IsDefinedElement` helper |

---
## 6. Top Risks (Prioritized)
| Rank | Risk | Impact | Severity | Status |
|------|------|--------|----------|--------|
| 1 | Single message parse failure → full reconnect | Unnecessary connection churn | High | Open |
| 2 | Buffer growth → GC pressure | Performance/memory cost | Medium | Open |
| 3 | No backoff jitter | Reconnect spikes | Medium | Open |
| 4 | No retry / threshold on resubscribe failure | Hidden partial data loss | Medium | Partial (base present) |
| 5 | Empty array indistinguishable (`TryGetArray`) | Semantic misinterpretation | Medium | Open |
| 6 | Fire-and-forget async dispose | Resource leak / abrupt close | Medium | Open |
| 7 | No channel metrics observer | Low observability | Low | Open |
| 8 | Numeric epoch unsupported in helper | Inconsistent time parsing | Low | Open |
| (Resolved) | Auto resubscribe snapshot restore | Former #1 risk | (Mitigated) | Implemented |

---
## 7. Improvement Roadmap
### Phase 1 (Stability & Correctness)
1. (Done) Active subscription restore: `RestoreActiveSubscriptionsAsync` + `ResubscribeAsync` + `MarkSubscriptionActive`
2. Message parse protection: wrapper with failure threshold before reconnect (Pending)
3. `JsonExtensions` reinforcement (empty array handling, numeric epoch, helpers) (Partial: numeric epoch still missing)
4. Introduce IAsyncDisposable in `WebSocketClientBase` & structured disposal (Pending)

### Phase 2 (Performance & Observability)
1. Adopt `ArrayPool<byte>` and Span-based binary handling
2. Exponential backoff with jitter (0–30%)
3. Observer hook (`IChannelObserver`) for automatic stats on `Invoke*`
4. Lightweight metrics: avg message size, parse latency (Stopwatch)

### Phase 3 (Extensibility)
1. Parser layer abstraction: Raw → Normalized DTO adapter
2. Authentication strategy interface (signature/timestamp variants)
3. Symbol formatter strategy (case & delimiter normalization)
4. Order book incremental & sequence gap validation module (checksum support)

### Phase 4 (Advanced Capabilities)
1. Backpressure / throttling (sampling under bursty order book flow)
2. Compression (gzip/deflate) auto handling + adaptive buffer by payload size
3. Circuit breaker with half-open retries for persistent failures
4. Multi-transport abstraction (REST fallback / gRPC streaming)

---
## 8. Proposed Patch Snippets (Conceptual Only)
Auto-resubscribe code is already implemented; below are forward-looking improvement sketches.

```csharp
// (Planned) Parse failure threshold example
private int _consecutiveParseFailures;
private const int MaxParseFailuresBeforeReconnect = 5;

private async Task SafeProcessMessageAsync(string json) {
    try {
       await ProcessMessageAsync(json);
       _consecutiveParseFailures = 0;
    } catch (Exception ex) {
       _consecutiveParseFailures++;
       RaiseError($"Parse error #{_consecutiveParseFailures}: {ex.Message}");
       if (_consecutiveParseFailures >= MaxParseFailuresBeforeReconnect)
           await ForceReconnectAsync("Parse failure threshold");
    }
}
```

```csharp
// (Planned) Numeric epoch support (sec/ms auto-detect)
public static long GetUnixTimeOrDefault(this JsonElement e, string field, long @default = 0) {
    if (!e.TryGetProperty(field, out var p)) return @default;
    switch (p.ValueKind) {
        case JsonValueKind.Number:
            if (p.TryGetInt64(out var n1)) return Normalize(n1);
            break;
        case JsonValueKind.String:
            var s = p.GetString();
            if (long.TryParse(s, out var n2)) return Normalize(n2);
            if (DateTimeOffset.TryParse(s, out var dto)) return dto.ToUnixTimeMilliseconds();
            break;
    }
    return @default;
    static long Normalize(long raw) => raw >= 1_000_000_000_000 ? raw : raw * 1000; // sec→ms
}
```

---
## 9. Test Strategy Enhancements
| Category | New Tests | Purpose |
|----------|-----------|---------|
| JSON Parser | Epoch sec/ms, empty array, malformed numbers | Regression prevention |
| Reconnect | Forced disconnect then auto-restore | Continuity validation |
| Parse Failure Isolation | Inject N malformed payloads | Threshold reconnect logic |
| Order Book | Incremental apply & sequence gap | Data integrity |
| Load | Many symbols + burst messages | Performance / GC observation |

---
## 10. Code Style / Meta
| Item | Current | Suggestion |
|------|---------|------------|
| csproj duplication | `PackageLicenseFile` declared twice | Single declaration |
| Warning suppression | Repeated NoWarn per project | Centralize in Directory.Build.props |
| File naming | `JsonExtension.cs` contains `JsonExtensions` | Align filename plural |
| Logging | Direct `Console.WriteLine` usage | Introduce `ILogger` DI |
| Dispose | Mixed sync/async fire-and-forget | Implement IAsyncDisposable + awaited cleanup |

---
## 11. Executive Summary
* Architecture is clear with extensible abstraction layers.
* Auto-resubscribe (active subscription snapshot restore) mitigates prior top continuity risk.
* Remaining stability concerns: immediate reconnect on any parse failure, no jitter, unmanaged buffer growth.
* JSON enhancements (numeric epoch, empty array semantics) and observability (metrics, structured logging) are next priority.
* Phase 1.1 complete; subsequent phases will compound resilience and visibility gains.

---
## 12. Recommended Next Actions (Immediate Order)
1. (Done) Automatic resubscribe (`RestoreActiveSubscriptionsAsync`)
2. JsonExtensions enhancements (numeric epoch / empty array / diagnostics)
3. ArrayPool-based receive loop optimization
4. Introduce IAsyncDisposable & disposal path refactor
5. Parse failure threshold & reconnect policy (exponential backoff + jitter)
6. Logging abstraction (ILogger) removal of direct Console
7. Metrics/observer hook (`IChannelObserver` or callback wrapping)

---
## 13. Post-Original Update Summary
| Area | Change Summary | Impact |
|------|----------------|--------|
| WebSocketClientBase | Added `RestoreActiveSubscriptionsAsync`, `ResubscribeAsync`, `MarkSubscriptionActive`, `SubscriptionInfo` | Subscription continuity |
| Exchanges | Unified `Subscribe*` implementations call `MarkSubscriptionActive` | State consistency |
| Batch Subscription | Expanded from subset to broad coverage, test coverage increased | Faster & consistent initial load |
| Tests | Added integration tests for 11+ exchanges | Improved regression detection |
| Samples | Consolidated multi-sample set | Lower maintenance overhead |
| Models | New `SubscriptionInfo` | Timestamp & metadata tracking |

The original top risk (#1) is resolved; focus shifts to parse failure isolation, jitter, and memory optimization.

---
(End)
필요 시 각 항목별 구체 구현 패치를 요청해 주세요.

---
(끝)
