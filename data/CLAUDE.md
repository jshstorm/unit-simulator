# 데이터 관리 컨텍스트

**도메인**: data/ (JSON Schema, 게임 데이터, 빌드 스크립트)
**역할**: 게임 데이터 관리, 스키마 정의, 데이터 파이프라인
**에이전트 타입**: 데이터/모델 에이전트의 일부 (ReferenceModels/CLAUDE.md 참조)

---

## 디렉토리 구조

```
data/
├── schemas/                 # JSON Schema (Draft-07)
│   ├── unit-stats.schema.json
│   ├── skill-reference.schema.json
│   ├── tower-reference.schema.json
│   ├── wave-definition.schema.json
│   └── game-balance.schema.json
├── references/              # 원본 게임 데이터
│   ├── units.json
│   ├── skills.json
│   ├── towers.json
│   ├── waves.json
│   ├── balance.json
│   ├── buildings.json
│   └── spells.json
├── processed/               # 빌드 결과 (정규화된 데이터)
│   ├── units.json
│   ├── skills.json
│   ├── towers.json
│   ├── waves.json
│   └── balance.json
└── validation/
    └── report.md            # 검증 리포트
```

## 데이터 파이프라인

```
references/*.json → normalize.js → processed/*.json → ajv validate → report.md
```

## 명령어

| 명령 | 목적 |
|------|------|
| `npm run data:build` | 전체 파이프라인 (normalize + validate + diff) |
| `npm run data:validate` | 스키마 검증만 |
| `npm run data:normalize` | 정규화만 |

## 스키마 변경 시 영향

스키마 변경은 전체 프로젝트에 파급 → 오케스트레이터 승인 필수:
- Core: C# 모델 동기화 필요
- Server: 직렬화/역직렬화 영향
- UI: 데이터 표시 변경
- Tests: 테스트 데이터 갱신

## 검증 상태

- units.json: ✅ validated
- skills.json: ✅ validated
- towers.json: ✅ validated
- waves.json: ✅ validated
- balance.json: ✅ validated
