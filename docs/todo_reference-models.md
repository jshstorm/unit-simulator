# TODO: ReferenceModels Implementation Plan

목표: ReferenceModels의 스펙/구현 격차를 메우고, 각 단계마다 문서 갱신 후 커밋이 가능한 형태로 작업을 분리한다.

## Stage 0: 범위/계약 확정
**목적**: 데이터 테이블 범위와 스킬/검증 정책을 확정해 후속 작업의 기준을 고정.
**작업**
- 추가 테이블 목록 확정 (`waves`, `balance`, `strings` 등).
- 스킬 다형성 방식 결정 (단일 모델 + 변환기 vs 폴리모픽 모델).
- 로드 시 검증 실패 정책 결정 (중단/테이블 스킵/경고 로그).
- Google Sheets 연동 필요 여부 결정.
**문서 갱신**
- `docs/development-milestone.md`에 확정된 범위/정책 반영.
**커밋**
- `docs: define reference models scope and policies`

## Stage 1: 추가 테이블 모델/핸들러/등록
**목적**: `waves.json` 등 추가 테이블을 ReferenceModels에서 로드 가능하게 한다.
**작업**
- `ReferenceModels/Models`에 테이블별 모델 추가.
- `ReferenceModels/Infrastructure/ReferenceHandlers.cs`에 파서 추가.
- `ReferenceModels/Infrastructure/ReferenceManager.cs`에 기본 핸들러 등록 및 접근자 추가.
**검증**
- 샘플 JSON 로드 성공 여부(간단 샘플로 확인).
**문서 갱신**
- `docs/development-milestone.md`의 Reference System 섹션 갱신.
**커밋**
- `feat: add reference models for additional tables`

## Stage 2: UnitReference 기반 유닛 생성 경계 정의
**목적**: Unit 생성 로직의 위치와 경계를 명확히 한다.
**작업**
- `UnitSimulator.Core`에 `IUnitFactory`/`UnitFactory` 설계 및 구현.
- `UnitReference`는 데이터 전용 유지(생성 로직은 Core로 이동).
- ReferenceManager를 통해 스킬/레퍼런스 lookup 연계.
**검증**
- 기존 유닛 생성 경로가 새 팩토리를 통해 동작하는지 확인.
**문서 갱신**
- `docs/development-milestone.md`의 사용 예시 갱신.
**커밋**
- `feat: add unit factory backed by reference models`

## Stage 3: 스킬 다형성/변환기 도입
**목적**: `SkillReference`를 런타임 어빌리티 데이터로 변환 가능한 구조로 정리.
**작업**
- Core에 `AbilityData` 계층 추가(타입별 데이터 클래스).
- `SkillReference` → `AbilityData` 변환기(`SkillReferenceMapper` 등) 구현.
- `SkillReferenceValidator`와 타입 매핑 일치 보장.
**검증**
- 스킬 타입별 변환 케이스 테스트 추가.
**문서 갱신**
- `docs/development-milestone.md`에 다형성 구현 방식 명시.
**커밋**
- `feat: add ability data mapping from skill references`

## Stage 4: 로드 시 검증 통합 + 실패 정책
**목적**: 레퍼런스 로드 단계에서 검증 결과를 일관되게 처리한다.
**작업**
- `ReferenceManager.LoadAll()`에 `validateOnLoad` 옵션 추가.
- 테이블별 validator 매핑(레지스트리) 구현.
- 검증 결과를 담는 `LoadResult` 구조 추가.
**검증**
- 검증 실패 정책에 따라 로그/실패 처리 확인.
**문서 갱신**
- `docs/development-milestone.md`에 검증 정책/흐름 반영.
**커밋**
- `feat: add validation on reference load`

## Stage 5: JSON Schema 검증 파이프라인
**목적**: 스키마 기반 데이터 검증을 CI/로컬에서 재현 가능하게 구축.
**작업**
- `data/schemas/`에 `units.schema.json`, `skills.schema.json` 등 추가.
- `package.json`에 `data:validate` 스크립트 구현(스키마 검증 도구 선택).
- 검증 실패 시 빌드 중단 정책 설정.
**검증**
- `data/processed/` 또는 `data/references/`에 대한 스키마 검증 실행.
**문서 갱신**
- `docs/development-milestone.md` 및 `docs/development-guide.md` 업데이트.
**커밋**
- `feat: add json schema validation pipeline`

## Stage 6: Google Sheets 연동 (옵션)
**목적**: Sheets를 원본으로 유지해야 하는 경우에만 데이터 import 경로 제공.
**작업**
- `data:import` 스크립트 추가(시트 → raw).
- 인증/키 관리 정책 문서화.
- raw → processed 변환 규칙 명시.
**검증**
- 최소 1개 시트에 대한 import/변환 결과 확인.
**문서 갱신**
- `docs/development-milestone.md` 및 `docs/development-guide.md` 갱신.
**커밋**
- `feat: add optional sheets import pipeline`
