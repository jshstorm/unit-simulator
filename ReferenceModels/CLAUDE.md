# 데이터/모델 에이전트 (ReferenceModels)

**도메인**: ReferenceModels + data/
**역할**: 게임 데이터 모델, JSON Schema, 데이터 파이프라인 관리
**에이전트 타입**: 도메인 전문가 (Data Architecture)

---

## 담당 범위

### C# (ReferenceModels/)

| 시스템 | 디렉토리 | 설명 |
|--------|----------|------|
| 데이터 모델 | Models/ | UnitReference, TowerReference, SkillReference 등 |
| 열거형 | Models/Enums/ | 게임 열거형 (UnitType, DamageType 등) |
| 데이터 로딩 | Infrastructure/ | JSON → C# 모델 변환 |
| 검증 | Validation/ | 데이터 무결성 검증 |

### JSON (data/)

| 영역 | 디렉토리 | 설명 |
|------|----------|------|
| 스키마 정의 | data/schemas/ | JSON Schema Draft-07 |
| 원본 데이터 | data/references/ | units, skills, towers, waves, balance |
| 처리된 데이터 | data/processed/ | 정규화된 데이터 (빌드 결과) |
| 검증 리포트 | data/validation/ | 검증 결과 |

## 핵심 원칙

1. **불변 데이터**: `record` 타입 또는 `init` 속성만 사용
2. **스키마 우선**: 스키마 정의 → 데이터 작성 → 검증 순서
3. **파이프라인 필수**: 데이터 변경 시 `npm run data:build` 실행
4. **동기화**: JSON Schema와 C# 모델 항상 일치

## 데이터 파이프라인

```
data/references/*.json (원본)
        ↓ npm run data:normalize (scripts/normalize.js)
data/processed/*.json (정규화)
        ↓ npm run data:validate (ajv 스키마 검증)
data/validation/report.md (검증 리포트)
```

**빌드 명령어**:
- `npm run data:build` — normalize + validate + diff (전체)
- `npm run data:validate` — 스키마 검증만

## 코딩 규칙

- C# 모델: `{Entity}Reference` 네이밍 (예: `UnitReference`, `TowerReference`)
- JSON Schema: `{entity}-{type}.schema.json` (예: `unit-stats.schema.json`)
- `[JsonPropertyName]` 속성으로 JSON 키 매핑 명시
- nullable 필드는 `?` 명시

## 의존성

```
ReferenceModels
    └── (독립 — 외부 의존성 없음)

data/
    └── scripts/ (Node.js 빌드 도구)
```

- Core와 Server가 ReferenceModels를 **읽기 전용**으로 참조
- 스키마 변경은 모든 모듈에 영향 → 오케스트레이터 승인 필요

## 수정 금지 영역

- `UnitSimulator.Core/*` → 코어 에이전트 소유
- `UnitSimulator.Server/*` → 서버 에이전트 소유
- `sim-studio/*` → UI 에이전트 소유

## 스키마 변경 프로토콜

1. `data/schemas/*.schema.json` 변경
2. `npm run data:validate` 통과 확인
3. C# 모델 (`Models/`) 동기화
4. `docs/AGENT_CHANGELOG.md`에 ⚠️ 스키마 변경 기록
5. 영향받는 에이전트: Core (모델 참조), Server (직렬화)

## 작업 완료 시 체크리스트

- [ ] `npm run data:validate` 통과
- [ ] `dotnet build ReferenceModels` 통과
- [ ] `dotnet test ReferenceModels.Tests` 통과
- [ ] JSON Schema와 C# 모델 동기화 확인
- [ ] 스키마 변경 시 `docs/AGENT_CHANGELOG.md`에 기록
