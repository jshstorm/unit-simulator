namespace ReferenceModels.Models.Enums;

/// <summary>
/// 유닛의 이동 레이어를 정의합니다.
/// Ground 유닛은 지형/장애물 영향을 받고, Air 유닛은 지형을 무시합니다.
/// </summary>
public enum MovementLayer
{
    Ground,  // 지상 유닛 - 지형/충돌 영향 받음
    Air      // 공중 유닛 - 지형 무시, 공중 유닛끼리만 충돌
}
