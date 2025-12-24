# Web-based Sim Studio Prototype

이 문서는 `sim-studio` 소스 코드를 기준으로 현재 Sim Studio 구현 구조를 요약합니다.

## 상위 구조

- 엔트리: `sim-studio/src/main.tsx`가 `StrictMode` 아래에서 `sim-studio/src/App.tsx`를 렌더링합니다.
- 앱 셸: `sim-studio/src/App.tsx`가 세션 흐름, WebSocket 상태, UI 레이아웃을 총괄합니다.
- 타입: `sim-studio/src/types.ts`가 .NET 프레임/유닛 페이로드와 커맨드 메시지를 매핑합니다.
- 네트워킹: `sim-studio/src/hooks/useWebSocket.ts`가 세션 기반 WebSocket 연결, 재연결 로직, 커맨드 전송을 담당합니다.

## 뷰와 레이아웃

앱은 `App.tsx`에서 두 개의 탭을 제공합니다:

1. **Simulation** (기본)
   - 연결 전에 세션 선택 화면을 거칩니다.
   - 메인 레이아웃은 시뮬레이션 뷰와 사이드바로 구성됩니다.
2. **Data Editor**
   - REST 엔드포인트 기반의 JSON 데이터 파일 브라우저/편집기입니다.

## Simulation 뷰 구성 요소

- `sim-studio/src/components/SimulationCanvas.tsx`
  - 월드 그리드와 유닛을 캔버스에 팬/줌으로 렌더링합니다.
  - 유닛 선택(클릭), 패닝(드래그), 줌(휠)을 지원합니다.
  - 캔버스를 클릭하면 선택된 유닛에 이동 커맨드를 전송합니다.
  - 타겟 마커, 체력바, 방향 벡터, 선택 링을 그립니다.
- `sim-studio/src/components/UnitStateViewer.tsx`
  - 아군/적군 유닛을 체력/좌표/상태와 함께 목록으로 표시합니다.
  - 선택 강조 및 상태 플래그(이동 중, 사거리 내, 사망)를 표시합니다.
- `sim-studio/src/components/CommandPanel.tsx`
  - 선택된 유닛에 대해 이동, 체력 설정, 처치, 부활 커맨드를 전송합니다.
  - 뷰어 역할이거나 연결이 없을 때 입력을 비활성화합니다.
- `sim-studio/src/components/SimulationControls.tsx`
  - 재생/일시정지, 프레임 단위 스텝, 리셋, 탐색(seek)을 제공합니다.
  - 연결 상태 및 재생 상태와 동기화됩니다.

## 세션 흐름

- `sim-studio/src/components/SessionSelector.tsx`
  - `/sessions`에서 세션 목록을 가져와 생성/참가를 지원합니다.
  - 세션 상태, 프레임, 클라이언트 수, 마지막 활동 시간을 표시합니다.
  - 오너 연결 해제 시 읽기 전용 경고를 표시합니다.
- `useWebSocket.ts`는 `ws://localhost:5000/ws/{sessionId|new}`에 연결합니다.
  - 지속적인 client id로 identify 메시지를 전송합니다.
  - 세션 역할(`owner` vs `viewer`)과 오너 연결 상태를 추적합니다.
  - 다운로드/탐색을 위한 프레임 로그를 유지합니다.
  - 지수 백오프로 자동 재연결합니다.

## Data Editor

- `sim-studio/src/components/DataEditor.tsx`
  - `/data/files`에서 파일 목록을 불러옵니다.
  - `/data/file?path=...`로 로드/저장/삭제합니다.
  - 원시 JSON 편집과 배열 레코드 단위 편집을 모두 지원합니다.
  - ETag로 저장 충돌을 감지합니다.

## Data Editor 개선 계획

목표는 "JSON 원본" 중심의 단순 편집기를 "테이블 기반 데이터 브라우저 + 규약 기반 편집기"로 전환하는 것입니다.

- 데이터 조회 우선 UX
  - 기본 화면은 Excel 형태의 테이블 그리드로 표시합니다.
  - 행 선택 시 사이드 패널에서 상세 필드 편집을 제공합니다.
  - 빠른 탐색을 위해 검색/필터/정렬을 기본 제공하며, 최근 수정/즐겨찾기 필터를 포함합니다.
- 레코드 편집 흐름
  - 테이블에서 레코드를 선택 -> 필드 편집 -> 즉시 검증 -> 저장 큐에 추가.
  - 변경 사항은 "미저장 배지"와 변경 내역(diff)로 표시합니다.
  - 대량 수정은 다중 선택 + 일괄 편집 UI로 처리합니다.
- 스키마/규약 정의 (스크립트 영역)
  - "규약 스크립트"를 별도 탭 또는 파일로 제공하여 프로그래머/기획자가 정의합니다.
  - 스키마는 테이블 단위와 필드 단위로 구분합니다.
  - 예시 규약 (개념):
    - 테이블명/설명, 기본 정렬 키, 표시 컬럼
    - 필드 타입 (string, number, boolean, enum, vector 등)
    - 필드 제약 (required, min/max, unique, regex)
    - 읽기 전용/계산식 필드, 기본값, 표시 포맷
- 입력 제한 및 검증
  - 규약에 따라 허용되지 않는 값은 입력 단계에서 차단합니다.
  - 저장 전/후 검증 결과를 표/필드 단위로 보여줍니다.
  - 규약 미정의 필드는 "알 수 없음"으로 표시하고 편집 제한을 걸 수 있습니다.
- 데이터 모델 및 저장
  - 실제 저장 대상은 JSON이지만, 테이블 뷰는 스키마에 맞춰 배열/객체를 평탄화해 보여줍니다.
  - 저장 시 스키마 기반으로 원본 JSON을 재구성합니다.
  - ETag 충돌 시 재로딩/머지 안내를 제공합니다.

## 규약 스크립트 결정 항목 (협의 필요)

아래 항목은 규약 스크립트 설계를 위해 확정이 필요한 질문 목록입니다. 각 항목은 결정 전/후 상태를 관리합니다.

| 항목 | 질문 | 상태 | 결정 |
| --- | --- | --- | --- |
| 형식 | `JSON Schema`, `Custom DSL`, `TypeScript config`, `YAML` 중 선호가 있나요? | 결정 | YAML 기반 JSON Schema + `x-ui` 확장 |
| 위치 | 규약 파일 위치는 어디가 적절한가요? (예: `sim-studio/public/schema`, 서버 데이터 폴더, JSON 옆 `*.schema.*`) | 결정 | `sim-studio/config/schema` (git 관리) |
| 버전 | 스키마 버전 관리는 어떻게 하나요? (파일별/전역/마이그레이션 규칙) | 결정 | git 이력으로 관리, 별도 버저닝 불필요 |
| 검증 타이밍 | 입력 즉시/저장 직전/둘 다 중 무엇을 기본으로 할까요? | 결정 | 저장 시에만 검증 |
| 커스텀 타입 | `Vector2`, `Color`, `Range`, `Enum`, `Tags` 등 도메인 타입을 지원할까요? | 결정 | 지원함 (세부 목록/표현은 추후 확정) |
| 계산/읽기 전용 | 계산 필드 또는 읽기 전용 필드가 필요한가요? | 결정 | 읽기 전용 필드 필요 (예: uid는 자동 생성, 수정 불가) |
| 참조 규칙 | 다른 테이블 참조(외래키 유사 제약)가 필요한가요? | 결정 | uid-string 기반 참조는 허용, 외래키 강제 규약은 없음(로더가 처리) |
| 표시 포맷 | 단위/소수점/포맷 규칙이 필요한가요? | 결정 | 커스텀 타입 기반 포맷 지원, `|`, `[]` 토큰으로 페어/구분자/배열 표현 |
| 권한/역할 | 역할별 편집 제한이 필요한가요? | 결정 | 별도 권한 없음, 접근 가능하면 누구나 수정 |
| 편집자 | 비개발자도 규약을 수정해야 하나요? | 결정 | 가능 (YAML로 직접 편집) |

## 규약 스크립트 결정 로그 (단계별 관리)

이 섹션은 결정된 항목과 현재 상태를 단계적으로 기록합니다. 결정이 날 때마다 상태를 갱신합니다.

| 단계 | 범위 | 결정 상태 | 결정 내용 | 비고 |
| --- | --- | --- | --- | --- |
| 1 | 규약 형식/위치 | 결정 | 형식: YAML 기반 JSON Schema + `x-ui` 확장 | 위치: `sim-studio/config/schema` (git 관리) |
| 2 | 버전 관리 | 결정 | git 이력으로 관리, 별도 버저닝 불필요 | - |
| 3 | 검증 타이밍 | 결정 | 저장 시에만 검증 | - |
| 4 | 커스텀 타입 | 결정 | 커스텀 타입 지원 (세부 목록/표현 추후 확정) | - |
| 5 | 읽기 전용/계산 필드 | 결정 | uid 등 자동 생성 값은 수정 불가 | - |
| 6 | 참조 규칙 | 결정 | uid-string 기반 참조 허용, 외래키 강제 규약 없음 | 로더 처리 |
| 7 | UI 편집 규칙/포맷 | 결정 | 커스텀 타입별 포맷, `|`, `[]` 토큰 기반 표현 | - |
| 8 | 편집자/워크플로우 | 결정 | 비개발자도 규약 수정 가능 (YAML 직접 편집) | - |
| 9 | 권한/워크플로우 | 결정 | 별도 권한 없음, 접근 가능하면 누구나 수정 | - |
| 10 | 타입/검증 규칙 | 진행 중 | 엔지니어 선규약 범위 내에서만 조정 | 세부 규칙 협의 필요 |
| 4 | UI 편집 규칙/포맷 | 대기 | - | - |
| 5 | 권한/워크플로우 | 대기 | - | - |

## 규약 스크립트 스펙 (확정)

현재 확정된 규약 스크립트 스펙은 다음과 같습니다.

- 파일 형식: YAML
- 기반 규격: JSON Schema
- UI 확장: 스키마에 `x-ui` 메타데이터를 추가하여 컬럼/라벨/표시 포맷을 정의
- 파일 위치: `sim-studio/config/schema` (git 관리)
- 버전 관리: git 이력으로 관리, 별도 버저닝 불필요
- 검증 타이밍: 저장 시에만 검증
- 커스텀 타입: 필요 (세부 목록/표현은 추후 확정)
- 읽기 전용 필드: uid 등 자동 생성 값은 수정 불가
- 참조 규칙: uid-string 기반 참조 허용, 외래키 강제 규약 없음 (로더 처리)
- 표시 포맷: 커스텀 타입별 포맷 지원, `|`, `[]` 토큰으로 페어/구분자/배열 표현
- 권한: 별도 권한 없음, 접근 가능하면 누구나 수정
- 편집자: 비개발자도 규약 수정 가능 (YAML 직접 편집)
- 타입/검증 규칙: 엔지니어 선규약 범위 내에서만 조정

## 규약 스크립트 스펙 (초안 예시)

아래는 기본 문법 확인을 위한 초안입니다. 실제 규약은 추후 협의로 확정합니다.

```yaml
$schema: "https://json-schema.org/draft/2020-12/schema"
title: "Unit Data"
type: "array"
items:
  type: "object"
  required: ["id", "name", "hp", "speed", "position"]
  properties:
    id:
      type: "integer"
      minimum: 1
      x-ui:
        label: "ID"
        column: true
        width: 80
    name:
      type: "string"
      minLength: 1
      x-ui:
        label: "Name"
        column: true
        width: 160
    hp:
      type: "integer"
      minimum: 0
      maximum: 100
      x-ui:
        label: "HP"
        column: true
        format: "percent-0-100"
    speed:
      type: "number"
      minimum: 0
      maximum: 20
      x-ui:
        label: "Speed"
        column: true
        format: "float-1"
    position:
      type: "object"
      required: ["x", "y"]
      properties:
        x:
          type: "number"
          minimum: 0
          maximum: 1200
          x-ui:
            label: "Pos X"
            column: true
            format: "float-1"
        y:
          type: "number"
          minimum: 0
          maximum: 720
          x-ui:
            label: "Pos Y"
            column: true
            format: "float-1"
    isActive:
      type: "boolean"
      default: true
      x-ui:
        label: "Active"
        column: true
    tag:
      type: ["string", "null"]
      x-ui:
        label: "Tag"
        column: false
```

### 초안에서 다루는 규약 범위

- sign/unsign: `minimum`/`maximum`과 `integer`/`number`로 표현
- range: `minimum`/`maximum`
- nullable: `type`에 `"null"` 포함

이 초안을 기준으로 enum, regex, unique, 참조 규칙 등은 다음 단계에서 협의합니다.

## x-ui 메타데이터 초안 스펙 (제안)

아래는 Data Editor의 테이블/폼 렌더링을 위한 `x-ui` 메타데이터 초안입니다. 필요에 따라 수정합니다.

### 공통 필드 (모든 속성에서 사용 가능)

- `label`: 표시 이름
- `column`: 테이블 컬럼 노출 여부 (boolean)
- `order`: 컬럼/필드 순서 (number)
- `width`: 컬럼 너비 (px)
- `help`: 도움말 텍스트
- `readonly`: 편집 불가 (uid 등 자동 생성 필드)
- `placeholder`: 입력 플레이스홀더

### 입력 위젯 지정

- `editor`: `"text" | "number" | "toggle" | "select" | "textarea" | "vector2" | "list" | "pair"`
- `options`: `select`용 옵션 리스트 (string 배열 또는 `{ label, value }[]`)

### 표시/포맷 지정

- `format`: 표시 문자열 규칙 (예: `"float-1"`, `"int"`, `"percent-0-100"`)
- `separator`: 배열 표시 구분자 (기본: `","`)
- `pairToken`: 페어 표시 토큰 (기본: `"|"`)
- `arrayToken`: 배열 표시 토큰 (기본: `"[]"`)
- `display`: `"inline" | "block"`

### 예시

```yaml
properties:
  uid:
    type: "string"
    x-ui:
      label: "UID"
      column: true
      readonly: true
      width: 180
  position:
    type: "object"
    properties:
      x: { type: "number" }
      y: { type: "number" }
    x-ui:
      label: "Position"
      column: true
      editor: "vector2"
      format: "float-1"
      pairToken: "|"
  tags:
    type: "array"
    items: { type: "string" }
    x-ui:
      label: "Tags"
      column: false
      editor: "list"
      separator: ","
      arrayToken: "[]"
```

## 커스텀 타입 초안 목록 (제안)

Data Editor에서 자주 쓰일 것으로 보이는 도메인 타입을 우선 제안합니다. 실제 채택 여부는 추후 조정합니다.

- `uid-string`
  - 설명: UID 문자열 (예: CRC 기반 자동 생성)
  - 편집: `readonly: true`
  - 저장 검증: 문자열 길이/패턴 체크 가능
- `vector2`
  - 설명: 2차원 좌표 (x, y)
  - 편집: `editor: "vector2"`, `pairToken: "|"`
  - 표시: `x|y` 또는 `x|y[]` (배열일 경우)
- `range`
  - 설명: 구간 값 (min, max)
  - 편집: `editor: "pair"` 또는 별도 2칸 입력
  - 표시: `min|max`
- `enum`
  - 설명: 열거형 문자열/숫자
  - 편집: `editor: "select"`, `options` 사용
- `weighted-list`
  - 설명: 아이템과 가중치 쌍 리스트
  - 편집: `editor: "list"`, 아이템은 `value|weight` 형식
  - 표시: `value|weight, value|weight`
- `tags`
  - 설명: 문자열 배열 (태그)
  - 편집: `editor: "list"`, `separator: ","`
  - 표시: `tag1, tag2`
- `color`
  - 설명: 색상 (hex 또는 rgba)
  - 편집: `editor: "text"` 또는 color picker 대응
  - 표시: `#RRGGBB` 또는 `rgba(...)`
- `angle`
  - 설명: 각도 값 (도/라디안)
  - 편집: `editor: "number"`
  - 표시: `deg` 또는 `rad` 포맷 지원

### 커스텀 타입 표기 메타데이터 (제안)

스키마의 `x-ui` 또는 `x-type`에 커스텀 타입 명시:

```yaml
properties:
  uid:
    type: "string"
    x-type: "uid-string"
    x-ui:
      label: "UID"
      readonly: true
  position:
    type: "object"
    x-type: "vector2"
    x-ui:
      label: "Position"
      editor: "vector2"
```

## 유틸리티

- `sim-studio/src/utils/frameLogDownload.ts`
  - 수집된 프레임 로그를 JSON 파일로 다운로드합니다.

## 런타임 상호작용

- 키보드 단축키: 좌/우 화살표로 이전/다음 프레임 이동.
- 권한: 세션 오너만 시뮬레이션 제어/커맨드 전송이 가능합니다.
