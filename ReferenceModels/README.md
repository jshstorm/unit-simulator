# Reference Models 작성 규칙

목표: Google Sheets의 시트 컬럼 ↔ C# 모델 속성 간 매핑을 명확히 하고, 모델이 XML과 상호 직렬화(serialize/deserialize) 가능하도록 규정합니다.

## 요약 규칙
- 모델은 ReferenceModels 프로젝트에 둡니다.
- UnitMove(앱)는 ReferenceModels를 참조하여 데이터 주도 모델을 사용합니다.
- 모든 프로젝트 타깃 프레임워크는 .NET 10 (net10.0) 입니다.

## 시트 포맷 규칙
1. 첫 번째 행: 컬럼명(헤더) — 모델의 필드/속성과 매핑됩니다. 대소문자 무시, 공백은 '_' 또는 제거하여 매핑 권장.
2. 두 번째 행: 간단한 타입 선언과 주석 — 예: "int // 설명" 또는 "string // 설명". 이 행은 컬럼의 타입 정보를 제공합니다.
   - 지원 타입: string, int, long, float, double, bool, DateTime
   - 예: `Health` 컬럼의 두 번째 행에 `int // 체력` 이면 해당 컬럼은 정수형으로 파싱합니다.
3. 세 번째 행부터 레코드(데이터) 시작.
4. 빈 행(완전히 비어 있는 행)이 나타나면 그 행부터 아래의 모든 행은 무시합니다. 즉 첫 번째 빈 행 이전의 행까지만 유효한 레코드로 취급합니다.

## 모델 클래스 규칙
- public 클래스이자 public parameterless 생성자를 가져야 합니다.
- 각 속성은 public get/set 이어야 합니다.
- 기본 매핑: 헤더명과 동일한 속성명을 사용(대소문자 무시).
- 명시적 매핑: [SheetColumn("HeaderName")] 어트리뷰트 사용 가능.
- XML 직렬화 가능해야 함: System.Xml.Serialization.XmlSerializer로 직렬화/역직렬화가 가능하도록 public 속성 사용.

## 변환/에러 처리 규칙
- 빈 문자열 값은 null(참조 타입) 또는 해당 타입의 기본값으로 처리합니다.
- 타입 변환 실패 시: 로그를 남기고 해당 행은 건너뜁니다(또는 정책에 따라 기본값 사용).

## 보안
- 민감 정보(예: 서비스 계정 키)는 이 프로젝트나 config에 직접 저장하지 마십시오.


## 예시
- 스프레드시트 헤더:
  | Id | Name | Health | SpawnTime |
- 두 번째 행(타입):
  | int // id | string // 이름 | int // 체력 | DateTime // 소환시간 |