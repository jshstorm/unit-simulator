# 프로젝트 계획: Unit-Simulator

이 문서는 진행 중인 작업과 완료된 작업을 추적합니다.

---

## 타워 스킬 시스템

**상태**: 🟡 진행 중 (파일럿 테스트)  
**시작일**: 2026-01-09  

### 목적
타워가 자동 공격 외에 특수 스킬을 발동할 수 있게 하여 게임플레이 다양성 향상

### 범위
**포함**:
- 스킬 발동 메커니즘
- 쿨다운 관리
- 효과 적용 (데미지, 버프, 디버프)
- WebSocket API

**제외**:
- 스킬 업그레이드 시스템
- 스킬 연출/이펙트
- UI 구현 (Phase 2)

### 영향 프로젝트
| 프로젝트 | 변경 내용 | 우선순위 |
|----------|-----------|----------|
| UnitSimulator.Core | TowerSkillSystem, SkillEffect | P0 |
| UnitSimulator.Server | TowerSkillHandler | P0 |
| ReferenceModels | TowerSkillReference | P1 |
| sim-studio | 스킬 발동 UI | P2 (별도 작업) |

### 마일스톤
1. [x] 기능 정의서 작성 (specs/features/tower-skill-system.md)
2. [x] WebSocket API 설계 (specs/apis/tower-skill-api.md)
3. [ ] Core 로직 구현 (TowerSkillSystem.cs)
4. [ ] Server 핸들러 구현 (TowerSkillHandler.cs)
5. [ ] xUnit 테스트 작성 및 통과
6. [ ] 코드 리뷰 및 PR

### 리스크
| 리스크 | 영향 | 대응 |
|--------|------|------|
| 기존 전투 시스템과 상호작용 복잡 | 높음 | 인터페이스로 분리, 단위 테스트 강화 |
| 스킬 밸런스 문제 | 중간 | ReferenceModels로 데이터 외부화 |
| 동시성 이슈 (멀티 세션) | 중간 | 세션별 상태 격리 확인 |

### 검증 방법
1. **단위 테스트**: `TowerSkillSystem.ActivateSkillAsync()` 테스트 통과
2. **통합 테스트**: WebSocket 요청/응답 검증
3. **시나리오 테스트**: 
   - 스킬 발동 → 효과 적용 → 쿨다운 시작
   - 쿨다운 중 재발동 시도 → 에러 응답
   - 유효하지 않은 타워/스킬 → 에러 응답

---

## 완료된 작업

### 에이전트 인프라 구축
**상태**: ✅ 완료  
**완료일**: 2026-01-09

- `.claude/agents/` 6개 에이전트 정의
- `.claude/commands/` 4개 명령어 정의
- `.claude/skills/` 6개 스킬 정의
- `AGENTS.md`, `CLAUDE.md` 갱신
- `mcp.json` 설정

---

## 백로그

### 대기 중
- [ ] 유닛 사망 이벤트 콜백
- [ ] 시뮬레이션 스냅샷 저장/복원
- [ ] 성능 프로파일링 도구
- [ ] sim-studio 스킬 UI

### 아이디어
- 타워 조합 스킬 (여러 타워 협동)
- 스킬 이펙트 시스템 (시각적 연출)
- 스킬 커스터마이징 (업그레이드 경로)
