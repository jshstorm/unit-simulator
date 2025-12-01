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
    }
}