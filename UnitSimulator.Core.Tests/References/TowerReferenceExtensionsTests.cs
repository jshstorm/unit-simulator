using System.Numerics;
using FluentAssertions;
using Xunit;

namespace UnitSimulator.Core.Tests.References;

public class TowerReferenceExtensionsTests
{
    [Fact]
    public void CreateTower_WithPrincessType_ShouldSetCorrectFields()
    {
        // Arrange
        var towerRef = new TowerReference
        {
            DisplayName = "Princess Tower",
            Type = TowerType.Princess,
            MaxHP = 3000,
            Damage = 100,
            AttackSpeed = 1.25f,
            AttackRadius = 350f,
            Radius = 100f,
            CanTarget = TargetType.GroundAndAir
        };

        // Act
        var tower = towerRef.CreateTower(1, UnitFaction.Friendly, new Vector2(200, 800));

        // Assert
        tower.Id.Should().Be(1);
        tower.Type.Should().Be(TowerType.Princess);
        tower.Faction.Should().Be(UnitFaction.Friendly);
        tower.Position.Should().Be(new Vector2(200, 800));
        tower.Radius.Should().Be(100f);
        tower.AttackRange.Should().Be(350f);
        tower.MaxHP.Should().Be(3000);
        tower.CurrentHP.Should().Be(3000);
        tower.Damage.Should().Be(100);
        tower.AttackSpeed.Should().Be(1.25f);
        tower.CanTarget.Should().Be(TargetType.GroundAndAir);
        tower.IsActivated.Should().BeTrue(); // Princess는 자동 활성화
        tower.AttackCooldown.Should().Be(0f);
    }

    [Fact]
    public void CreateTower_WithKingType_ShouldNotBeActivated()
    {
        // Arrange
        var towerRef = new TowerReference
        {
            DisplayName = "King Tower",
            Type = TowerType.King,
            MaxHP = 4800,
            Damage = 110,
            AttackSpeed = 1.0f,
            AttackRadius = 350f,
            Radius = 150f,
            CanTarget = TargetType.GroundAndAir
        };

        // Act
        var tower = towerRef.CreateTower(2, UnitFaction.Enemy, new Vector2(500, 1000));

        // Assert
        tower.Type.Should().Be(TowerType.King);
        tower.IsActivated.Should().BeFalse(); // King은 기본적으로 비활성화
        tower.CurrentHP.Should().Be(4800);
    }

    [Fact]
    public void CreateTower_WithInitialHP_ShouldOverrideMaxHP()
    {
        // Arrange
        var towerRef = new TowerReference
        {
            Type = TowerType.Princess,
            MaxHP = 3000
        };

        // Act
        var tower = towerRef.CreateTower(
            1,
            UnitFaction.Friendly,
            new Vector2(100, 100),
            initialHP: 1500
        );

        // Assert
        tower.MaxHP.Should().Be(3000);
        tower.CurrentHP.Should().Be(1500);
    }

    [Fact]
    public void CreateTower_WithIsActivated_ShouldOverrideDefault()
    {
        // Arrange
        var kingRef = new TowerReference
        {
            Type = TowerType.King,
            MaxHP = 4800
        };

        // Act
        var tower = kingRef.CreateTower(
            1,
            UnitFaction.Friendly,
            new Vector2(500, 500),
            isActivated: true  // King을 처음부터 활성화
        );

        // Assert
        tower.Type.Should().Be(TowerType.King);
        tower.IsActivated.Should().BeTrue();
    }

    [Fact]
    public void CreateTower_Integration_WithReferenceManager()
    {
        // Arrange
        var manager = new ReferenceManager();
        var towers = new System.Collections.Generic.Dictionary<string, TowerReference>
        {
            ["princess_tower"] = new TowerReference
            {
                DisplayName = "Princess Tower",
                Type = TowerType.Princess,
                MaxHP = 2534,
                Damage = 90,
                AttackSpeed = 0.8f,
                AttackRadius = 7.5f,
                Radius = 2.5f,
                CanTarget = TargetType.GroundAndAir
            }
        };
        manager.RegisterTable(new ReferenceTable<TowerReference>("towers", towers));

        // Act
        var princessRef = manager.Towers!.Get("princess_tower");
        var tower = princessRef!.CreateTower(10, UnitFaction.Enemy, new Vector2(800, 800));

        // Assert
        princessRef.DisplayName.Should().Be("Princess Tower");
        tower.Damage.Should().Be(90);
        tower.AttackSpeed.Should().Be(0.8f);
    }
}
