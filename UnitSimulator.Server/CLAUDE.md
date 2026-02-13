# 서버/통신 에이전트

**도메인**: UnitSimulator.Server
**역할**: WebSocket 서버, 세션 관리, 메시지 라우팅
**에이전트 타입**: 도메인 전문가 (Networking & Sessions)

---

## 담당 범위

| 시스템 | 파일/디렉토리 | 설명 |
|--------|-------------|------|
| WebSocket 서버 | WebSocketServer.cs | 서버 진입점, 연결 관리 |
| 세션 관리 | SimulationSession.cs, SessionManager.cs | 시뮬레이션 세션 생명주기 |
| 클라이언트 | SessionClient.cs | 클라이언트 표현 |
| 메시지 핸들러 | Handlers/ | WebSocket 메시지 처리 |
| 메시지 타입 | Messages/ | 요청/응답 DTO 정의 |
| 웨이브 관리 | WaveManager.cs | 웨이브 커맨드 생성 |
| 렌더링 | Renderer.cs | ImageSharp 기반 서버사이드 렌더링 |
| 로깅 | SessionLogger.cs | 세션 이벤트 로깅 |
| CLI | Program.cs | 서버 CLI 진입점 |

## 핵심 원칙

1. **async/await 필수**: 모든 I/O 작업은 비동기
2. **세션 격리**: 각 세션은 독립적, 상태 공유 없음
3. **JSON 직렬화**: System.Text.Json 사용, `[JsonPropertyName]` 속성 명시
4. **요청/응답 페어링**: 모든 요청에 대응하는 응답 정의

## WebSocket 메시지 형식

```json
{
  "type": "MessageType",
  "sessionId": "uuid",
  "payload": { }
}
```

## 코딩 규칙

- DTO는 `record` 타입 + `init` 속성 사용
- 핸들러 네이밍: `{Entity}Handler.cs`
- 메시지 네이밍: `{Action}{Entity}Request` / `{Action}{Entity}Response`
- 에러 응답에 명확한 에러 메시지 포함

## 의존성

```
UnitSimulator.Server
    ├── UnitSimulator.Core (시뮬레이션 로직 호출)
    └── ReferenceModels (데이터 모델 참조)
```

- **Core**: `ISimulationEngine` 인터페이스를 통해 시뮬레이션 로직 호출
- **ReferenceModels**: 참조 데이터 접근
- **specs/apis/**: sim-studio(UI)와의 WebSocket 프로토콜 계약

## 수정 금지 영역

- `UnitSimulator.Core/Contracts/*` → 오케스트레이터 관리
- `sim-studio/*` → UI 에이전트 소유
- `data/*` → 데이터 에이전트 소유

## 인터페이스 계약

- **specs/apis/*.md**: sim-studio와의 WebSocket 프로토콜 정의
- 프로토콜 변경 시 오케스트레이터 승인 + UI 에이전트 동기화 필요

## 작업 완료 시 체크리스트

- [ ] `dotnet build UnitSimulator.Server` 통과
- [ ] WebSocket 메시지 직렬화/역직렬화 확인
- [ ] 새 핸들러에 에러 처리 포함
- [ ] specs/apis/ 명세와 구현 일치 확인
- [ ] `docs/AGENT_CHANGELOG.md`에 API 변경 기록
