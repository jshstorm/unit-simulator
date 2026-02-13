# UI/클라이언트 에이전트

**도메인**: sim-studio (React/TypeScript)
**역할**: 웹 기반 시뮬레이션 UI, WebSocket 클라이언트
**에이전트 타입**: 도메인 전문가 (Frontend)

---

## 담당 범위

| 영역 | 디렉토리 | 설명 |
|------|----------|------|
| React 컴포넌트 | src/components/ | 시뮬레이션 뷰어, 컨트롤 패널, HUD |
| 커스텀 훅 | src/hooks/ | WebSocket 연결, 시뮬레이션 상태 관리 |
| 서비스 | src/services/ | WebSocket 클라이언트, API 레이어 |
| 유틸리티 | src/utils/ | 좌표 변환, 수학 유틸리티 |
| 에셋 | public/assets/ | 스프라이트, 아이콘 등 게임 에셋 |
| 빌드 설정 | vite.config.ts | Vite 빌드 구성 |

## 기술 스택

- React 18+ / TypeScript (Strict mode)
- Vite 빌드
- WebSocket 실시간 통신
- Canvas 기반 시뮬레이션 렌더링

## 핵심 원칙

1. **TypeScript strict**: `as any`, `@ts-ignore` 금지
2. **프로토콜 동기화**: WebSocket 메시지 타입은 `specs/apis/*.md`와 일치
3. **단일 책임**: 컴포넌트는 한 가지 역할만
4. **React hooks 기반**: 상태 관리는 hooks로 통일

## 의존성

```
sim-studio
    └── specs/apis/ (WebSocket 프로토콜 계약)
    └── data/schemas/ (데이터 표시 참조)
```

- Server 에이전트와 `specs/apis/*.md`를 통해 WebSocket 프로토콜 공유
- 게임 데이터 스키마 참조 (표시용)

## 수정 금지 영역

- `UnitSimulator.Core/*` → 코어 에이전트 소유
- `UnitSimulator.Server/*` → 서버 에이전트 소유
- `ReferenceModels/*` → 데이터 에이전트 소유
- `data/references/*` → 데이터 에이전트 소유

## 빌드 명령어

- `npm run dev --prefix sim-studio` — 개발 서버
- `npm run build --prefix sim-studio` — 프로덕션 빌드

## 작업 완료 시 체크리스트

- [ ] `npm run build --prefix sim-studio` 통과
- [ ] TypeScript 타입 에러 없음
- [ ] WebSocket 메시지 타입이 specs/apis/와 일치
- [ ] `docs/AGENT_CHANGELOG.md`에 UI 변경 기록
