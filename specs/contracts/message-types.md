# 인터페이스 계약: 메시지 타입

**소유자**: 오케스트레이터 (변경 시 승인 필요)
**구현**: Server 에이전트 (C#), UI 에이전트 (TypeScript)
**참조**: specs/apis/*.md

---

## 목적

Server와 sim-studio(UI) 간 WebSocket 통신에 사용되는 공유 메시지 타입을 정의합니다.
양쪽 에이전트가 동일한 프로토콜을 구현해야 합니다.

## 메시지 형식 (공통)

```json
{
  "type": "MessageType",
  "sessionId": "uuid-string",
  "payload": { ... }
}
```

## 등록된 메시지 타입

### 세션 관리

| 메시지 타입 | 방향 | 설명 |
|------------|------|------|
| `CreateSession` | Client → Server | 새 세션 생성 요청 |
| `SessionCreated` | Server → Client | 세션 생성 완료 응답 |
| `JoinSession` | Client → Server | 기존 세션 참가 |
| `LeaveSession` | Client → Server | 세션 나가기 |

### 시뮬레이션 제어

| 메시지 타입 | 방향 | 설명 |
|------------|------|------|
| `StartSimulation` | Client → Server | 시뮬레이션 시작 |
| `PauseSimulation` | Client → Server | 일시정지 |
| `ResumeSimulation` | Client → Server | 재개 |
| `StepSimulation` | Client → Server | 단일 프레임 진행 |
| `SimulationState` | Server → Client | 프레임 상태 브로드캐스트 |

### 게임 액션

| 메시지 타입 | 방향 | 설명 |
|------------|------|------|
| `PlaceUnit` | Client → Server | 유닛 배치 |
| `PlaceTower` | Client → Server | 타워 배치 |
| `ActivateTowerSkill` | Client → Server | 타워 스킬 발동 |
| `StartWave` | Client → Server | 웨이브 시작 |

## 새 메시지 타입 추가 프로토콜

1. `specs/apis/`에 새 API 명세 문서 작성
2. 이 문서에 메시지 타입 등록
3. 오케스트레이터 승인
4. Server 에이전트: C# DTO + 핸들러 구현
5. UI 에이전트: TypeScript 타입 + 서비스 구현
6. Tests 에이전트: 통합 테스트 추가
7. `docs/AGENT_CHANGELOG.md`에 기록

## 변경 이력

| 날짜 | 변경 | 영향 |
|------|------|------|
| 2026-02-13 | 초안 작성 | - |
