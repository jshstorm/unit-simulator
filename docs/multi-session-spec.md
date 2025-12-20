# Multi-Session Support Specification

이 문서는 여러 클라이언트가 독립적인 시뮬레이션 세션에 접근할 수 있도록 하는 멀티 세션 지원의 기초 스펙을 정의합니다.

## 현재 상태

### 단일 컨텍스트 구조
```
┌─────────────────────────────────────────────────────┐
│                   WebSocketServer                    │
│  ┌─────────────────────────────────────────────┐    │
│  │             SimulatorCore (단일)              │    │
│  └─────────────────────────────────────────────┘    │
│                        │                            │
│         ┌──────────────┼──────────────┐             │
│         ▼              ▼              ▼             │
│    Client A       Client B       Client C           │
│    (동일 상태 공유, 명령 간섭 발생)                    │
└─────────────────────────────────────────────────────┘
```

**문제점:**
- 모든 클라이언트가 하나의 `SimulatorCore` 인스턴스를 공유
- Client A가 `start` 명령을 보내면 B, C도 동일한 시뮬레이션 상태를 봄
- Client B가 `reset` 명령을 보내면 A, C의 진행 상태도 초기화
- 독립적인 실험/테스트 불가능

## 목표 구조

### 멀티 세션 컨텍스트
```
┌─────────────────────────────────────────────────────────────┐
│                      SessionManager                          │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐       │
│  │  Session A   │  │  Session B   │  │  Session C   │       │
│  │ SimulatorCore│  │ SimulatorCore│  │ SimulatorCore│       │
│  │ SessionLogger│  │ SessionLogger│  │ SessionLogger│       │
│  └──────┬───────┘  └──────┬───────┘  └──────┬───────┘       │
│         │                 │                 │                │
│         ▼                 ▼                 ▼                │
│    Client A1          Client B1         Client C1           │
│    Client A2          Client B2                              │
│    (세션 내 공유)      (세션 내 공유)    (독립 세션)           │
└─────────────────────────────────────────────────────────────┘
```

## 설계 옵션

### Option 1: URL 라우팅 기반 세션

**엔드포인트 구조:**
```
ws://localhost:5000/ws                    # 기본 세션 (레거시 호환)
ws://localhost:5000/ws/{sessionId}        # 지정된 세션
ws://localhost:5000/ws/new                # 새 세션 생성 후 자동 연결
```

**장점:**
- 기존 클라이언트와 하위 호환 가능 (`/ws` 경로 유지)
- URL만으로 세션 지정이 명확함
- 브라우저 북마크/공유 용이

**단점:**
- HttpListener 라우팅 로직 추가 필요

### Option 2: 연결 후 세션 선택

**핸드셰이크 프로토콜:**
```json
// 클라이언트 → 서버 (연결 직후)
{
  "type": "session_request",
  "data": {
    "action": "create" | "join" | "list",
    "sessionId": "optional-session-id"
  }
}

// 서버 → 클라이언트 (세션 할당)
{
  "type": "session_assigned",
  "data": {
    "sessionId": "assigned-session-id",
    "isNew": true,
    "simulatorState": "idle" | "running" | "paused"
  }
}
```

**장점:**
- 기존 WebSocket 연결 로직 변경 최소화
- 동적 세션 전환 가능성

**단점:**
- 추가 핸드셰이크 오버헤드
- 클라이언트 측 변경 필요

### Option 3: 하이브리드 (권장)

URL 라우팅과 세션 관리 API를 조합:

**엔드포인트:**
```
# WebSocket 연결
ws://localhost:5000/ws                    # 새 세션 자동 생성
ws://localhost:5000/ws/{sessionId}        # 기존 세션 참가

# HTTP REST API
GET  /sessions                            # 세션 목록 조회
POST /sessions                            # 새 세션 생성 (연결 없이)
GET  /sessions/{sessionId}                # 세션 상태 조회
DELETE /sessions/{sessionId}              # 세션 종료
```

## 핵심 컴포넌트 설계

### SessionManager

```csharp
public class SessionManager : IDisposable
{
    private readonly ConcurrentDictionary<string, SimulationSession> _sessions;
    private readonly SessionManagerOptions _options;

    public SimulationSession CreateSession(string? sessionId = null);
    public SimulationSession? GetSession(string sessionId);
    public IEnumerable<SessionInfo> ListSessions();
    public bool RemoveSession(string sessionId);
    public void CleanupIdleSessions();
}

public class SessionManagerOptions
{
    public int MaxSessions { get; set; } = 10;
    public TimeSpan SessionTimeout { get; set; } = TimeSpan.FromHours(1);
    public TimeSpan IdleTimeout { get; set; } = TimeSpan.FromMinutes(30);
}
```

### SimulationSession

```csharp
public class SimulationSession : IDisposable
{
    public string SessionId { get; }
    public DateTime CreatedAt { get; }
    public DateTime LastActivityAt { get; private set; }

    public SimulatorCore Simulator { get; }
    public SessionLogger Logger { get; }
    public List<WebSocket> Clients { get; }

    public void UpdateActivity();
    public Task BroadcastToSessionAsync<T>(string type, T data);
}
```

### 수정된 WebSocketServer

```csharp
public class WebSocketServer : IDisposable
{
    private readonly SessionManager _sessionManager;

    private async Task HandleWebSocketConnectionAsync(
        HttpListenerContext context,
        string? requestedSessionId)
    {
        // 세션 해석/생성
        var session = requestedSessionId switch
        {
            null => _sessionManager.CreateSession(),
            "new" => _sessionManager.CreateSession(),
            _ => _sessionManager.GetSession(requestedSessionId)
                 ?? _sessionManager.CreateSession(requestedSessionId)
        };

        // 해당 세션에 클라이언트 등록
        session.Clients.Add(webSocket);

        // 세션별 메시지 처리
        await HandleClientMessagesAsync(webSocket, session);
    }
}
```

## 클라이언트 UI 변경사항

### 세션 선택 화면
```
┌─────────────────────────────────────┐
│        Session Selector             │
├─────────────────────────────────────┤
│  ○ Create New Session               │
│  ─────────────────────────────────  │
│  Existing Sessions:                 │
│  ┌─────────────────────────────────┐│
│  │ session-abc123                  ││
│  │ Created: 2min ago, Clients: 2  ││
│  │ State: Running (Frame 150)      ││
│  └─────────────────────────────────┘│
│  ┌─────────────────────────────────┐│
│  │ session-xyz789                  ││
│  │ Created: 10min ago, Clients: 0 ││
│  │ State: Idle                     ││
│  └─────────────────────────────────┘│
│                                     │
│         [Connect]                   │
└─────────────────────────────────────┘
```

### URL 기반 접근
```
http://localhost:5173/?session=abc123
http://localhost:5173/?session=new
```

## 메시지 프로토콜 확장

### 세션 관련 메시지 타입 추가

```typescript
// 서버 → 클라이언트
type ServerMessage =
  | { type: 'frame'; data: FrameData }
  | { type: 'state_change'; data: StateChangeData }
  | { type: 'session_info'; data: SessionInfo }           // NEW
  | { type: 'session_client_joined'; data: ClientInfo }   // NEW
  | { type: 'session_client_left'; data: ClientInfo }     // NEW
  | { type: 'error'; data: ErrorData };

interface SessionInfo {
  sessionId: string;
  createdAt: string;
  clientCount: number;
  simulatorState: 'idle' | 'running' | 'paused' | 'completed';
  currentFrame: number;
}
```

## 리소스 관리

### 메모리 고려사항
- 각 `SimulatorCore` 인스턴스당 메모리 사용량 추정 필요
- `FrameHistory` (기본 5000 프레임) 가 주요 메모리 소비원
- 세션당 프레임 히스토리 제한 또는 디스크 스와핑 검토

### 세션 정리 정책
```csharp
public enum SessionCleanupPolicy
{
    // 마지막 클라이언트 연결 해제 시 즉시 삭제
    ImmediateOnEmpty,

    // 유휴 타임아웃 후 삭제 (기본)
    IdleTimeout,

    // 수동 삭제만 허용
    ManualOnly
}
```

## 구현 우선순위

### Phase 1: 기본 멀티 세션
1. `SessionManager` 클래스 구현
2. `SimulationSession` 클래스 구현
3. `WebSocketServer` 라우팅 로직 수정
4. 기존 `/ws` 엔드포인트 하위 호환 유지

### Phase 2: HTTP API
1. 세션 목록 조회 API
2. 세션 상태 조회 API
3. 세션 생성/삭제 API

### Phase 3: 클라이언트 UI
1. 세션 선택 컴포넌트
2. URL 파라미터 기반 세션 연결
3. 세션 상태 표시 UI

### Phase 4: 고급 기능
1. 세션 자동 정리
2. 세션 복제 (현재 상태 기반 새 세션 생성)
3. 세션 저장/복원

## 결정된 사항

| 항목 | 결정 |
|------|------|
| 세션 ID 형식 | UUID |
| 최대 세션 수 | 100개 (데이터 드리븐, 후에 조정 가능) |
| 세션 타임아웃 | 유휴 30분 후 자동 삭제 |
| 역할 분리 | Owner(수정 권한) / Viewer(읽기 전용) |
| 소유권 관리 | Client ID 기반 (Option C) |
| Owner 연결 해제 시 | 세션 유지, 시뮬레이션 정지 상태, 명령 불가 |
| 세션 출력 디렉토리 | `output/{sessionId}/` 하위에 생성 |

## 세션별 출력 디렉토리 구조

각 세션의 결과 파일(프레임 이미지, 로그 등)은 세션 UUID를 기반으로 한 독립된 디렉토리에 저장됩니다.
이를 통해 여러 세션이 동시에 실행되어도 파일 충돌이 발생하지 않습니다.

### 디렉토리 구조

```
output/
├── {session-uuid-1}/
│   ├── frame_0000.png
│   ├── frame_0001.png
│   ├── ...
│   └── debug/
│       └── session_{session-uuid-1}_{timestamp}.json
├── {session-uuid-2}/
│   ├── frame_0000.png
│   ├── ...
│   └── debug/
│       └── session_{session-uuid-2}_{timestamp}.json
└── ...
```

### 구현 규칙

1. **세션 생성 시**: `output/{sessionId}/` 디렉토리 자동 생성
2. **프레임 이미지**: `output/{sessionId}/frame_{frameNumber:D4}.png`
3. **세션 로그**: `output/{sessionId}/debug/session_{sessionId}_{timestamp}.json`
4. **세션 종료 시**: 디렉토리는 유지 (수동 삭제 또는 정책에 따라 정리)

## 역할 기반 권한 모델

### 역할 정의

```csharp
public enum SessionRole
{
    Owner,   // 세션 생성자, 모든 명령 실행 가능
    Viewer   // 관전자, 읽기 전용 (상태 조회만 가능)
}

public class SessionClient
{
    public WebSocket Socket { get; }
    public string ClientId { get; }
    public SessionRole Role { get; }
    public DateTime JoinedAt { get; }
}
```

### 권한 매트릭스

| 명령 | Owner | Viewer |
|------|:-----:|:------:|
| start | ✓ | ✗ |
| stop | ✓ | ✗ |
| step | ✓ | ✗ |
| step_back | ✓ | ✗ |
| seek | ✓ | ✓ (로컬 뷰만) |
| reset | ✓ | ✗ |
| move | ✓ | ✗ |
| set_health | ✓ | ✗ |
| kill | ✓ | ✗ |
| revive | ✓ | ✗ |
| get_session_log | ✓ | ✓ |
| 프레임 수신 | ✓ | ✓ |

### Viewer의 로컬 Seek

Viewer도 `seek` 명령을 사용할 수 있지만, 이는 자신의 뷰에서만 프레임 히스토리를 탐색하는 것이며 실제 시뮬레이션 상태나 다른 클라이언트에게 영향을 주지 않습니다.

---

## 세션 소유권 관리 (채택: Client ID 기반)

클라이언트가 고유 ID를 생성하고 유지하여 소유권을 식별합니다.

### 클라이언트 측 구현

```typescript
// 브라우저 로컬스토리지에 Client ID 저장/조회
function getOrCreateClientId(): string {
  let clientId = localStorage.getItem('clientId');
  if (!clientId) {
    clientId = crypto.randomUUID();
    localStorage.setItem('clientId', clientId);
  }
  return clientId;
}

// WebSocket 연결 시 Client ID 전송
const ws = new WebSocket(`ws://localhost:5000/ws/new`);
ws.onopen = () => {
  ws.send(JSON.stringify({
    type: 'identify',
    data: { clientId: getOrCreateClientId() }
  }));
};
```

### 서버 측 구현

```csharp
public class SimulationSession
{
    public string SessionId { get; }
    public string? OwnerClientId { get; private set; }
    public bool HasOwner => OwnerClientId != null && IsOwnerConnected;
    public bool IsOwnerConnected { get; private set; }

    public SessionRole GetRoleForClient(string clientId)
    {
        return clientId == OwnerClientId
            ? SessionRole.Owner
            : SessionRole.Viewer;
    }

    public void OnOwnerDisconnected()
    {
        IsOwnerConnected = false;
        // 시뮬레이션이 running 상태면 자동으로 pause
        if (Simulator.State == SimulatorState.Running)
        {
            Simulator.Pause();
            BroadcastToSession("state_change", new {
                state = "paused",
                reason = "owner_disconnected"
            });
        }
    }

    public void OnOwnerReconnected()
    {
        IsOwnerConnected = true;
        BroadcastToSession("state_change", new {
            state = Simulator.State,
            reason = "owner_reconnected"
        });
    }
}
```

### Owner 연결 해제 시 동작

```
┌─────────────────────────────────────────────────────────────┐
│  Owner 연결 해제 시 시퀀스                                   │
│  ─────────────────────────────────────────────────────────  │
│                                                             │
│  1. Owner 연결 해제 감지                                     │
│  2. 시뮬레이션 running 중이면 → 자동 pause                   │
│  3. 모든 Viewer에게 알림: "owner_disconnected"               │
│  4. 세션은 유지, Viewer들은 현재 상태 관전 가능               │
│  5. 모든 수정 명령 거부 (start, stop, move 등)               │
│  6. Owner가 동일 clientId로 재연결 시 → 소유권 복구           │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

### 권한 검사 흐름

```csharp
private async Task HandleCommandAsync(SessionClient client, JsonElement commandData)
{
    var cmdType = commandData.GetProperty("type").GetString();

    // 수정 권한이 필요한 명령인지 확인
    if (RequiresOwnerPermission(cmdType))
    {
        if (client.Role != SessionRole.Owner)
        {
            await SendErrorAsync(client, "permission_denied",
                "This command requires owner permission");
            return;
        }

        if (!client.Session.IsOwnerConnected)
        {
            await SendErrorAsync(client, "owner_disconnected",
                "Session owner is disconnected. Commands are disabled.");
            return;
        }
    }

    // 명령 처리 진행...
}

private bool RequiresOwnerPermission(string cmdType)
{
    return cmdType switch
    {
        "start" or "stop" or "step" or "reset" => true,
        "move" or "set_health" or "kill" or "revive" => true,
        "step_back" => true,
        "seek" or "get_session_log" => false,  // Viewer도 가능
        _ => true  // 기본적으로 Owner 권한 요구
    };
}
```

---

## 미결 사항 (향후 검토)

1. **세션 간 통신**: 세션 간 유닛 이동 등 상호작용 가능성
