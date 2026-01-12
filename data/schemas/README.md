# Data Schemas

이 디렉토리는 unit-simulator 프로젝트의 모든 게임 데이터에 대한 JSON Schema 정의를 포함합니다.

## 스키마 파일 목록

| 스키마 파일 | 대상 데이터 | 설명 |
|------------|------------|------|
| `unit-stats.schema.json` | `data/references/units.json` | 유닛 스탯 및 속성 정의 |
| `skill-reference.schema.json` | `data/references/skills.json` | 스킬/어빌리티 정의 |
| `tower-reference.schema.json` | `data/references/towers.json` | 타워 스펙 정의 (계획) |
| `wave-definition.schema.json` | `data/references/waves.json` | 웨이브 구성 정의 (계획) |

## 검증 방법

### npm 스크립트 사용 (권장)

**모든 데이터 파일 검증:**
```bash
npm run data:validate
```

**개별 데이터 파일 검증:**
```bash
npm run data:validate:units    # units.json 검증
npm run data:validate:skills   # skills.json 검증
npm run data:validate:towers   # towers.json 검증
```

### 수동 검증 (ajv-cli)
```bash
npx ajv validate -s data/schemas/unit-stats.schema.json -d data/references/units.json
```

### CI/CD 자동 검증

GitHub Actions를 통해 PR 및 main 브랜치 푸시 시 자동으로 데이터 파일을 검증합니다:
- Workflow: `.github/workflows/validate-data.yml`
- 트리거: `data/references/*.json` 또는 `data/schemas/*.schema.json` 변경 시

## JSON Schema 버전

이 프로젝트는 **JSON Schema Draft-07** 표준을 사용합니다 (ajv-cli 호환성을 위해).

## 참고 문서

- [JSON Schema 공식 문서](https://json-schema.org/)
- [Understanding JSON Schema](https://json-schema.org/understanding-json-schema/)
- [프로젝트 개발 마일스톤](../../docs/development-milestone.md#m21-데이터-스키마-표준화)
