# DevOps/자동화 에이전트

**도메인**: scripts/ + .github/workflows/
**역할**: 빌드 자동화, CI/CD, 데이터 파이프라인 스크립트
**에이전트 타입**: 도메인 전문가 (DevOps)

---

## 담당 범위

### 빌드 스크립트 (scripts/)
- `normalize.js` — 참조 데이터 정규화
- `build.js` — 전체 데이터 빌드 파이프라인
- `diff.js` — 데이터 변경 비교

### CI/CD (.github/workflows/)
- `dotnet-ci.yml` — .NET 빌드 + 테스트 (모든 PR)
- `validate-data.yml` — 데이터 스키마 검증

## 빌드 명령어

| 명령 | 목적 |
|------|------|
| `dotnet build UnitSimulator.sln` | 전체 .NET 빌드 |
| `dotnet test UnitSimulator.sln` | 전체 테스트 실행 |
| `npm run data:build` | 데이터 빌드 (normalize + validate + diff) |
| `npm run data:validate` | 데이터 스키마 검증 |
| `npm run build --prefix sim-studio` | UI 프로덕션 빌드 |

## CI 파이프라인

### dotnet-ci.yml
- 트리거: PR, push to main
- 단계: Checkout → Setup .NET → Restore → Build → Test

### validate-data.yml
- 트리거: data/ 변경 시
- 단계: Checkout → Setup Node → Validate schemas

## 핵심 원칙

1. 모든 PR에 CI 통과 필수
2. 데이터 변경 시 validate-data 자동 실행
3. 빌드 실패 시 즉시 알림
4. 스크립트 변경은 로컬에서 먼저 검증

## 수정 금지 영역

- 프로덕션 코드 (Core, Server, Models) → 해당 도메인 에이전트 소유
- 게임 데이터 (data/references/) → 데이터 에이전트 소유
