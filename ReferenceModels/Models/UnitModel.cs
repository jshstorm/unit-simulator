using System;
using System.Xml.Serialization;
using ReferenceModels.Attributes;

namespace ReferenceModels.Models
{
    [XmlRoot("Unit")]
    public class UnitModel
    {
        // 첫 번째 행(헤더)에 Id로 적으면 자동 매핑됨
        [SheetColumn("Id")]
        [XmlElement("Id")]
        public int Id { get; set; }

        [SheetColumn("Name")]
        [XmlElement("Name")]
        public string? Name { get; set; }

        [SheetColumn("Health")]
        [XmlElement("Health")]
        public int Health { get; set; }

        [SheetColumn("Speed")]
        [XmlElement("Speed")]
        public float Speed { get; set; }

        [SheetColumn("IsEnemy")]
        [XmlElement("IsEnemy")]
        public bool IsEnemy { get; set; }

        [SheetColumn("SpawnTime")]
        [XmlElement("SpawnTime")]
        public DateTime? SpawnTime { get; set; }

        /// <summary>
        /// 유닛의 충돌 반지름 (원 크기)
        /// </summary>
        [SheetColumn("Radius")]
        [XmlElement("Radius")]
        public float Radius { get; set; }

        /// <summary>
        /// 유닛의 회전 속도 (라디안/프레임)
        /// </summary>
        [SheetColumn("TurnSpeed")]
        [XmlElement("TurnSpeed")]
        public float TurnSpeed { get; set; }

        /// <summary>
        /// 유닛의 공격 사거리
        /// </summary>
        [SheetColumn("AttackRange")]
        [XmlElement("AttackRange")]
        public float AttackRange { get; set; }

        /// <summary>
        /// 유닛의 역할. 유효 값: "Melee" (근접), "Ranged" (원거리).
        /// Google Sheets 및 XML 직렬화와의 호환성을 위해 문자열로 저장됩니다.
        /// </summary>
        [SheetColumn("Role")]
        [XmlElement("Role")]
        public string? Role { get; set; }
    }
}