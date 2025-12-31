# ReferenceModels 마이그레이션 계획

## 개요

UnitSimulator.Core/References에 있는 레퍼런스 모델 클래스들을 ReferenceModels 프로젝트로 마이그레이션하여, ReferenceModels를 독립적인 스키마 정의 및 검증 레이어로 확립합니다.

## 목표

1. **역할 명확화**: ReferenceModels를 독립적인 스키마/검증 레이어로 확립
2. **의존성 역전**: Core → ReferenceModels 참조 구조로 변경
3. **테스트 강화**: ReferenceModels에서 스키마 검증 및 테스트 수행
4. **레거시 제거**: XML 기반 구식 모델 제거

## 현재 구조

### UnitSimulator.Core/References/ (마이그레이션 대상)
- `UnitReference.cs` - 유닛 레퍼런스 모델
- `SkillReference.cs` - 스킬 레퍼런스 모델
- `ReferenceTable.cs` - 제네릭 테이블 컨테이너
- `ReferenceManager.cs` - 테이블 로드/관리 매니저
- `ReferenceHandlers.cs` - JSON 파싱 핸들러

### ReferenceModels/ (현재 레거시)
- `Models/UnitModel.cs` - 구식 XML 기반 모델
- `Serialization/XmlModelSerializer.cs` - 레거시 직렬화
- `Attributes/SheetColumnAttribute.cs` - 레거시 속성

### 의존성 상황
- UnitSimulator.Core: 34개 파일에서 사용
- UnitSimulator.Server: Core 참조로 간접 사용
- unit-dev-tool: Core 참조로 간접 사용

## 최종 구조

```
ReferenceModels/
├── docs/
│   └── migration-plan.md         # 이 문서
├── Models/
│   ├── UnitReference.cs          # from Core
│   ├── SkillReference.cs         # from Core
│   └── Enums/
│       ├── UnitRole.cs
│       ├── MovementLayer.cs
│       ├── TargetType.cs
│       └── TargetPriority.cs
├── Infrastructure/
│   ├── IReferenceTable.cs        # from Core
│   ├── ReferenceTable.cs         # from Core
│   ├── ReferenceManager.cs       # from Core
│   └── ReferenceHandlers.cs      # from Core
├── Validation/
│   ├── IValidator.cs
│   ├── UnitReferenceValidator.cs
│   ├── SkillReferenceValidator.cs
│   └── ValidationResult.cs
└── ReferenceModels.csproj
```

## 구현 단계

### Phase 1: ReferenceModels 프로젝트 준비

#### 1.1 프로젝트 구조 설정
- [x] docs/ 디렉토리 생성 및 이 문서 작성
- [ ] Models/, Infrastructure/, Validation/ 디렉토리 생성
- [ ] Models/Enums/ 디렉토리 생성
- [ ] csproj 파일 업데이트 (System.Text.Json 참조 추가)

#### 1.2 레거시 파일 제거
- [ ] Models/UnitModel.cs 삭제
- [ ] Serialization/XmlModelSerializer.cs 삭제
- [ ] Attributes/SheetColumnAttribute.cs 삭제

#### 1.3 커밋
```
feat: Phase 1 - ReferenceModels 프로젝트 구조 준비 및 레거시 제거
```

---

### Phase 2: 코드 마이그레이션

#### 2.1 Enum 타입 마이그레이션
Core에서 ReferenceModels/Models/Enums/로 이동:
- [ ] UnitRole.cs
- [ ] MovementLayer.cs
- [ ] TargetType.cs
- [ ] TargetPriority.cs

네임스페이스: `ReferenceModels.Models.Enums`

#### 2.2 Infrastructure 클래스 마이그레이션
이동 순서 (의존성 고려):
1. [ ] IReferenceTable.cs → ReferenceModels/Infrastructure/
2. [ ] ReferenceTable.cs → ReferenceModels/Infrastructure/
3. [ ] ReferenceHandlers.cs → ReferenceModels/Infrastructure/
4. [ ] ReferenceManager.cs → ReferenceModels/Infrastructure/

네임스페이스: `ReferenceModels.Infrastructure`

#### 2.3 Models 클래스 마이그레이션
- [ ] UnitReference.cs → ReferenceModels/Models/
- [ ] SkillReference.cs → ReferenceModels/Models/

네임스페이스: `ReferenceModels.Models`

**주의사항**:
- UnitReference와 SkillReference는 Enum 타입들과 Infrastructure에 의존
- using 문 업데이트 필요

#### 2.4 커밋
```
feat: Phase 2 - 레퍼런스 모델 및 인프라 클래스 마이그레이션
```

---

### Phase 3: Validation 레이어 추가

#### 3.1 Validation 인프라 구축
- [ ] IValidator.cs 생성
- [ ] ValidationResult.cs 생성

#### 3.2 Validator 구현
- [ ] UnitReferenceValidator.cs 생성
  - MaxHP > 0
  - MoveSpeed >= 0
  - Radius > 0
  - DisplayName 비어있지 않음 (경고)

- [ ] SkillReferenceValidator.cs 생성
  - Type 비어있지 않음
  - 타입별 필수 필드 검증

#### 3.3 ReferenceManager에 검증 통합
- [ ] ValidateAllTables 메서드 추가
- [ ] LoadAll에 validateOnLoad 파라미터 추가

#### 3.4 커밋
```
feat: Phase 3 - 레퍼런스 검증 레이어 추가
```

---

### Phase 4: UnitSimulator.Core 업데이트

#### 4.1 프로젝트 참조 추가
- [ ] UnitSimulator.Core.csproj에 ReferenceModels 참조 추가

#### 4.2 using 문 업데이트
모든 Core 파일에서:
- [ ] Core 내 모든 .cs 파일 검색
- [ ] `UnitReference`, `SkillReference`, `ReferenceManager` 등 사용하는 파일 식별
- [ ] using 문 추가:
  ```csharp
  using ReferenceModels.Models;
  using ReferenceModels.Models.Enums;
  using ReferenceModels.Infrastructure;
  ```

#### 4.3 References 폴더 제거
- [ ] UnitSimulator.Core/References/ 디렉토리 삭제 확인
- [ ] 참조하는 파일이 없는지 재확인

#### 4.4 커밋
```
feat: Phase 4 - Core 프로젝트를 ReferenceModels 참조로 업데이트
```

---

### Phase 5: 다른 프로젝트 업데이트

#### 5.1 UnitSimulator.Server 업데이트
- [ ] csproj에 ReferenceModels 참조 추가 (필요시)
- [ ] using 문 업데이트 (필요시)

#### 5.2 UnitSimulator.Core.Tests 업데이트
- [ ] csproj에 ReferenceModels 참조 추가
- [ ] using 문 업데이트
- [ ] References/ 테스트 코드 확인

#### 5.3 unit-dev-tool 업데이트
- [ ] csproj에 ReferenceModels 참조 추가 (필요시)
- [ ] using 문 업데이트 (필요시)

#### 5.4 커밋
```
feat: Phase 5 - Server, Tests, Tools 프로젝트 업데이트
```

---

### Phase 6: 빌드 검증 및 컴파일 오류 수정

#### 6.1 개별 프로젝트 빌드
- [ ] `dotnet build ReferenceModels/ReferenceModels.csproj`
- [ ] 컴파일 오류 수정
- [ ] `dotnet build UnitSimulator.Core/UnitSimulator.Core.csproj`
- [ ] 컴파일 오류 수정
- [ ] `dotnet build UnitSimulator.Server/UnitSimulator.Server.csproj`
- [ ] 컴파일 오류 수정
- [ ] `dotnet build tools/unit-dev-tool/UnitDevTool.csproj`
- [ ] 컴파일 오류 수정

#### 6.2 전체 솔루션 빌드
- [ ] `dotnet build`
- [ ] 모든 컴파일 오류 수정

#### 6.3 테스트 실행
- [ ] `dotnet test UnitSimulator.Core.Tests/`
- [ ] 테스트 실패 수정

#### 6.4 통합 검증
- [ ] 서버 실행 확인
- [ ] unit-dev-tool 실행 확인
- [ ] 레퍼런스 로드 로그 확인

#### 6.5 최종 커밋
```
feat: Phase 6 - 컴파일 오류 수정 및 마이그레이션 완료
```

---

## 검증 항목

### 컴파일 검증
- [ ] ReferenceModels 독립 빌드 성공
- [ ] UnitSimulator.Core 빌드 성공
- [ ] UnitSimulator.Server 빌드 성공
- [ ] unit-dev-tool 빌드 성공

### 기능 검증
- [ ] JSON 파일 로드 정상 동작
- [ ] 유닛 생성 정상 동작
- [ ] 스킬 바인딩 정상 동작
- [ ] 서버 시뮬레이션 정상 동작

### 테스트 검증
- [ ] UnitSimulator.Core.Tests 모두 통과
- [ ] Validation 테스트 통과 (새로 추가 시)

---

## 주의사항

1. **점진적 마이그레이션**: Phase 별로 진행, 각 Phase마다 커밋
2. **컴파일 확인**: Phase 6까지는 컴파일 오류 무시, 최종 단계에서 일괄 수정
3. **네임스페이스 일관성**: ReferenceModels 내에서 일관된 네임스페이스 사용
4. **커밋 단위**: Phase 단위로 커밋하여 롤백 가능하도록 구성

---

## 예상 효과

1. **관심사 분리**: 스키마/검증과 시뮬레이션 로직 완전 분리
2. **재사용성**: ReferenceModels를 다른 프로젝트에서도 사용 가능
3. **테스트 용이성**: 스키마 검증을 독립적으로 테스트
4. **유지보수성**: 레퍼런스 모델 변경 시 영향 범위 명확화
5. **확장성**: 새 레퍼런스 타입 추가 시 ReferenceModels에만 집중
