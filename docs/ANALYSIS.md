# CCXT.Collector 코드베이스 기술 분석

작성일: 2025-08-12  
대상 브랜치: `master`  
분석 버전: v2.1.5 (Newtonsoft → System.Text.Json 마이그레이션 완료 상태)

---
## 1. 개요
`CCXT.Collector`는 다중 암호화폐 거래소 WebSocket 실시간 데이터(호가, 체결, 티커, 캔들) 및 사설 채널(잔고, 주문, 포지션)을 통합 수집하기 위한 .NET 라이브러리입니다. 핵심은 공통 추상(WebSocketClientBase)과 거래소별 구현, 그리고 구독 상태/통계를 관리하는 `ChannelManager`로 구성됩니다.

### 주요 목표 영역
- 다중 거래소 표준화된 데이터 이벤트 스트림 제공
- 재사용 가능한 WebSocket 연결/구독 추상화
- 가벼운 JSON 파싱 (System.Text.Json + 확장 메서드)
- 테스트를 통한 기본 통합 검증 (xUnit)

---
## 2. 아키텍처 레이어 구조
| 레이어 | 위치 | 역할 |
|--------|------|------|
| Abstractions | `src/core/abstractions` | WebSocket 인터페이스, 베이스 클래스 (연결/재접속/구독 추상) |
| Infrastructure | `src/core/infrastructure` | 채널/구독 상태 관리(`ChannelManager`) |
| Exchanges | `src/exchanges/*` | 거래소별 WebSocket 클라이언트 구현 (심볼/메시지 파서/인증) |
| Models | `src/models/*` | 표준화된 DTO (예: `STicker`, `STrade`, `SOrderBook`, `SCandle`) |
| Utilities | `src/utilities` | JSON/시간/통계/확장 메서드 (`JsonExtensions`, `TimeExtension` 등) |
| Tests | `tests` | 거래소별 WebSocket 통합 테스트 |
| Samples | `samples` | 라이브러리 사용 예시 |

### 핵심 흐름 (개략)
1. 사용자: `ChannelManager.RegisterExchangeClient()` 로 거래소 클라이언트 등록
2. 사용자: `RegisterChannelAsync()` 로 구독 의도 저장 (대기 상태)
3. 사용자: `ApplyBatchSubscriptionsAsync()` 호출 → 실제 WebSocket 연결 + 개별/일괄 구독 수행
4. 거래소별 구현: 메시지 수신 후 파싱 → `Invoke*Callback()` → 외부 이벤트 핸들러
5. `ChannelManager` 는 (현재) 이벤트 직접 Hook 미흡 → 메시지 카운트는 파생 구조 확장 필요

---
## 3. WebSocketClientBase 분석
| 기능 | 장점 | 한계/리스크 |
|------|------|-------------|
| 연결/재접속 | 단순/명확, 백오프 상한 60s | 지터 없음(동시 폭주 가능), 재구독 누락 |
| 구독 관리 | `_subscriptions` 에 메타 저장 | 재접속 후 복구 로직 미구현 (`ResubscribeAsync` placeholder) |
| Ping 처리 | Timer + override 가능 | 거래소별 heartbeat 특성 추상화 부족 (Pong 처리 Hook 없음) |
| 수신 루프 | 텍스트/바이너리 모두 처리, 동적 버퍼 확장 | ArrayPool 미사용 → 대형 메시지 GC 부담 가능 |
| 에러 처리 | 예외 catch 후 재접속 | 단일 메시지 파싱 실패가 전체 재접속 트리거 가능 |
| 인증 | 분리/공용 소켓 선택 가능 | 실제 응답 검증/타임아웃 로직 단순 (고도화 필요) |

### 재접속 설계 개선 필요 포인트
- 활성 구독 Snapshot 저장 → 재연결 후 자동 재구독
- 영구 실패(최대 시도 초과) 이벤트 (예: `OnPermanentFailure`) 추가 고려
- 지수 백오프 + 랜덤 지터(e.g. 0.2 ~ 0.4 가중) 적용

---
## 4. ChannelManager 분석
| 항목 | 상태 | 개선 제안 |
|------|------|-----------|
| 대기 구독 저장 | `ConcurrentDictionary<string,List<>>` + lock | Immutable snapshot / ConcurrentQueue 전환 검토 |
| 이벤트 후 채널 통계 | 수동 갱신 필요 | WebSocketClientBase에서 업데이트 Hook 주입 (`IChannelObserver`) |
| 빈 채널 시 Disconnect | 즉시 해제 | Idle Grace Period(예: 30~60s) 후 해제 옵션 |
| Batch 구독 | 단순 순차 실행 | 거래소별 실제 batch 지원 분기 또는 병렬화(속도) |

---
## 5. JsonExtensions 평가 (`src/utilities/JsonExtension.cs`)
### 장점
- 숫자/문자열 혼용 안전 파싱
- Epoch 초·밀리초 구분 (`>= 1_000_000_000_000`)
- 공용 Null/Undefined 체크 및 안전 접근 패턴 제공

### 한계/리스크 및 제안
| 메서드/패턴 | 문제 | 개선 |
|-------------|------|------|
| `TryGetArray` | 빈 배열(false) ↔ 미존재 구분 불가 | 빈 배열 허용 또는 `TryGetNonEmptyArray` 명시적 명명 |
| `GetUnixTimeOrDefault` | 숫자 epoch 미지원 | 숫자 처리 추가 / ISO 실패 시 epoch fallback |
| `GetDateTimeOffsetOrDefault` | 매직 넘버 휴리스틱 | Threshold 상수화 + 확장 정책 주입 |
| Silent default 반환 | 디버깅 어려움 | (옵션) Diagnostics 빌드에서 실패 로깅 Hook |
| `FirstOrUndefined` | default(JsonElement) 반환 | ValueKind 검사 유틸 추가 (`IsDefinedElement`) |

---
## 6. 주요 리스크 Top 8 (우선순위)
| 순위 | 항목 | 영향 | 심각도 |
|------|------|------|--------|
| 1 | 재접속 시 자동 재구독 미구현 | 데이터 공백/손실 | High |
| 2 | 단일 메시지 파싱 실패 → 전체 재접속 | 불필요한 연결 부하 | High |
| 3 | 버퍼 확장 GC 압박 (대형 메시지) | 성능/메모리 증가 | Medium |
| 4 | 백오프 지터 없음 | 동시 폭주(스파이크) | Medium |
| 5 | `JsonExtensions.TryGetArray` 빈 배열 구분 실패 | 의미적 오판 | Medium |
| 6 | Dispose 비동기 fire-and-forget | 자원 누수/중단 | Medium |
| 7 | Channel 통계 이벤트 미연결 | 관측성 부족 | Low |
| 8 | `GetUnixTimeOrDefault` 숫자 미지원 | 불일치/누락 | Low |

---
## 7. 개선 로드맵 (단계별)
### Phase 1 (안정성 & 정확성)
1. 활성 구독 재구독 로직: 재접속 성공 시 `_subscriptions` 중 `IsActive` == true 재등록
2. 메시지 파싱 보호: `ProcessMessageAsync` 내부(또는 래퍼) try/catch → 실패 카운터 누적 후 N회 이상일 때만 재접속
3. `JsonExtensions` 보강 (빈 배열/epoch 숫자 지원, 헬퍼 추가)
4. IAsyncDisposable 도입(WebSocketClientBase) + Dispose 경로 정리

### Phase 2 (성능 & 관측)
1. ArrayPool<byte> 사용, Binary 누적 구조 Span 기반 최적화
2. 백오프: jitter (random 0~30%) + 지수 성장 (2^n * base, 상한) 적용
3. Hook 인터페이스 (`IChannelObserver`) 로 Invoke* 시 채널 통계 자동 갱신
4. Lightweight Metrics: 평균 메시지 크기, 파싱 ms (Stopwatch) 노출

### Phase 3 (확장성)
1. 파서 계층 분리: Raw → Normalized DTO Adapter Pattern
2. 인증 전략 인터페이스(서명/타임스탬프 다양성) 분리
3. 심볼 포맷터 전략 (거래소별 대소문자/구분자 통일) 추가
4. 오더북 증분 처리/검증 모듈 (Checksum, Sequence Gap) 도입

### Phase 4 (고급 기능)
1. Backpressure/Throttle (호가 폭주 시 샘플링 정책)
2. 압축(gzip/deflate) 자동 해제 + Payload 크기 기준 Adaptive Buffer
3. 영구 실패 회로 차단(Circuit Breaker) + 반개방 상태 재시도
4. 다중 전송 프로토콜(REST fallback / gRPC streaming) 추상화

---
## 8. 제안되는 구체 패치 스니펫(요약 개념)
(실제 적용 시 관련 파일 편집 필요 – 여기선 개념만 명시)

```csharp
// WebSocketClientBase 재접속 후 재구독 의사코드
private async Task RestoreSubscriptionsAsync() {
    foreach (var kv in _subscriptions.Values.Where(s => s.IsActive)) {
        await ResubscribeAsync(kv);
    }
}

protected async Task HandleReconnectAsync() {
    // ...기존 로직...
    var ok = await ConnectAsync();
    if (ok) await RestoreSubscriptionsAsync();
}
```

```csharp
// JsonExtensions 개선 (숫자 epoch 지원)
public static long GetUnixTimeOrDefault(this JsonElement e, string field, long @default=0) {
    if (!e.TryGetProperty(field, out var p)) return @default;
    if (p.ValueKind == JsonValueKind.Number && p.TryGetInt64(out var n)) return NormalizeEpoch(n);
    if (p.ValueKind == JsonValueKind.String) {
        var s = p.GetString();
        if (long.TryParse(s, out var n)) return NormalizeEpoch(n);
        if (DateTimeOffset.TryParse(s, out var dto)) return dto.ToUnixTimeMilliseconds();
    }
    return @default;
    static long NormalizeEpoch(long raw) => raw >= 1_000_000_000_000 ? raw : raw * 1000; // 초→ms
}
```

---
## 9. 테스트 전략 보강 제안
| 카테고리 | 신규 테스트 | 목적 |
|----------|-------------|------|
| JSON 파서 | Epoch 초/밀리초, 빈 배열, 잘못된 숫자 | 회귀 방지 |
| 재접속 | 인위적 연결 종료 후 자동 재구독 | 연속성 검증 |
| 파싱 실패 격리 | 잘못된 메시지 N회 발생 | 재접속 임계 테스트 |
| 오더북 | 증분 적용 & 시퀀스 gap | 데이터 정합성 |
| 부하 | 다량 심볼 구독 + 메시지 폭주 모킹 | 성능/GC 관찰 |

---
## 10. 코드 스타일/메타 정리
| 항목 | 현 상태 | 제안 |
|------|---------|------|
| csproj 중복 | `PackageLicenseFile` 2회 선언 | 1회로 통일 |
| 경고 억제 | 각 프로젝트 NoWarn 중복 | Directory.Build.props 공유 |
| 파일명 | `JsonExtension.cs` 내부 클래스 `JsonExtensions` | 파일명 복수형 통일 |
| 로깅 | `Console.WriteLine` 직접 호출 | `ILogger` DI 적용 |
| Dispose | Sync/Async 혼재 | IAsyncDisposable + 명시 Await |

---
## 11. 요약 (Executive Summary)
- 구조는 명료하고 확장성을 위한 추상 레이어가 확보됨.
- 가장 큰 공백은 재접속 후 상태(구독) 복구와 신뢰성 향상(부분 실패 격리) 부분.
- JSON 마이그레이션은 완료되었으나 Epoch/배열/진단 개선 여지 존재.
- 중기적으로 성능(버퍼 재사용, 백오프 지터) 및 관측성(메트릭, 로깅) 도입이 유지보수 효율을 크게 향상시킬 것.
- 제안된 Phase 순서대로 적용 시 리스크 감소와 가시성 향상 효과가 누적될 전망.

---
## 12. 다음 액션 권장 (바로 착수 가능 순)
1. 재접속 자동 재구독 + JsonExtensions 보강 패치
2. ArrayPool 기반 수신 루프 최적화
3. IAsyncDisposable 도입 및 Dispose 경로 정리
4. 파서/재접속 단위 테스트 추가
5. 로깅 추상화(ILogger)로 Console 제거

필요 시 각 항목별 구체 구현 패치를 요청해 주세요.

---
(끝)
