using System.IO;
using System.Numerics;
using FluentAssertions;
using Xunit;

namespace UnitSimulator.Core.Tests.References;

public class ReferenceManagerTests
{
    private readonly string _testDataPath;

    public ReferenceManagerTests()
    {
        // 테스트용 임시 디렉토리 생성
        _testDataPath = Path.Combine(Path.GetTempPath(), "unit-sim-test-refs");
        Directory.CreateDirectory(_testDataPath);
    }

    [Fact]
    public void LoadAll_WithValidJson_ShouldLoadUnits()
    {
        // Arrange
        var json = @"{
            ""test_unit"": {
                ""displayName"": ""Test Unit"",
                ""maxHP"": 100,
                ""damage"": 50,
                ""moveSpeed"": 5.0,
                ""layer"": ""Ground""
            }
        }";
        File.WriteAllText(Path.Combine(_testDataPath, "units.json"), json);

        var manager = ReferenceManager.CreateWithDefaultHandlers();

        // Act
        manager.LoadAll(_testDataPath, _ => { });

        // Assert
        manager.HasTable("units").Should().BeTrue();
        manager.Units.Should().NotBeNull();
        manager.Units!.Count.Should().Be(1);

        var unit = manager.Units.Get("test_unit");
        unit.Should().NotBeNull();
        unit!.DisplayName.Should().Be("Test Unit");
        unit.MaxHP.Should().Be(100);
        unit.Damage.Should().Be(50);
    }

    [Fact]
    public void LoadAll_WithSkills_ShouldParseSkills()
    {
        // Arrange
        var unitsJson = @"{
            ""golem"": {
                ""displayName"": ""Golem"",
                ""maxHP"": 5000,
                ""skills"": [
                    ""golem_death_spawn"",
                    ""golem_death_damage""
                ]
            }
        }";
        var skillsJson = @"{
            ""golem_death_spawn"": { ""type"": ""DeathSpawn"", ""spawnUnitId"": ""golemite"", ""spawnCount"": 2 },
            ""golem_death_damage"": { ""type"": ""DeathDamage"", ""damage"": 200, ""radius"": 50 }
        }";
        File.WriteAllText(Path.Combine(_testDataPath, "units.json"), unitsJson);
        File.WriteAllText(Path.Combine(_testDataPath, "skills.json"), skillsJson);

        var manager = ReferenceManager.CreateWithDefaultHandlers();
        manager.LoadAll(_testDataPath, _ => { });

        // Act
        var golem = manager.Units!.Get("golem");

        // Assert
        golem.Should().NotBeNull();
        golem!.Skills.Should().HaveCount(2);
        manager.Skills.Should().NotBeNull();
        manager.Skills!.Count.Should().Be(2);
    }

    [Fact]
    public void LoadAll_NoHandler_ShouldWarnAndSkip()
    {
        // Arrange
        File.WriteAllText(Path.Combine(_testDataPath, "unknown.json"), "{}");

        var manager = new ReferenceManager();
        var warnings = new System.Collections.Generic.List<string>();

        // Act
        manager.LoadAll(_testDataPath, msg => warnings.Add(msg));

        // Assert
        warnings.Should().Contain(w => w.Contains("No handler") && w.Contains("unknown"));
        manager.HasTable("unknown").Should().BeFalse();
    }

    [Fact]
    public void UnitReference_CreateUnit_ShouldApplyStats()
    {
        // Arrange
        var unitRef = new UnitReference
        {
            DisplayName = "Test Knight",
            MaxHP = 1000,
            Damage = 100,
            MoveSpeed = 4.5f,
            Radius = 25f,
            Role = UnitRole.Melee,
            Layer = MovementLayer.Ground,
            CanTarget = TargetType.Ground
        };

        // Act
        var unit = unitRef.CreateUnit("test_knight", 1, UnitFaction.Friendly, new Vector2(100, 100));

        // Assert
        unit.HP.Should().Be(1000);
        unit.Damage.Should().Be(100);
        unit.Speed.Should().Be(4.5f);
        unit.Radius.Should().Be(25f);
        unit.Role.Should().Be(UnitRole.Melee);
        unit.Layer.Should().Be(MovementLayer.Ground);
        unit.CanTarget.Should().Be(TargetType.Ground);
        unit.Faction.Should().Be(UnitFaction.Friendly);
        unit.Position.Should().Be(new Vector2(100, 100));
    }

    [Fact]
    public void SkillReference_ToAbilityData_ShouldConvertCorrectly()
    {
        // Arrange
        var deathSpawnRef = new SkillReference
        {
            Type = "DeathSpawn",
            SpawnUnitId = "minion",
            SpawnCount = 3,
            SpawnRadius = 40f
        };

        var shieldRef = new SkillReference
        {
            Type = "Shield",
            MaxShieldHP = 500,
            BlocksStun = true
        };

        // Act
        var deathSpawn = deathSpawnRef.ToAbilityData() as DeathSpawnData;
        var shield = shieldRef.ToAbilityData() as ShieldData;

        // Assert
        deathSpawn.Should().NotBeNull();
        deathSpawn!.SpawnUnitId.Should().Be("minion");
        deathSpawn.SpawnCount.Should().Be(3);
        deathSpawn.SpawnRadius.Should().Be(40f);

        shield.Should().NotBeNull();
        shield!.MaxShieldHP.Should().Be(500);
        shield.BlocksStun.Should().BeTrue();
    }

    [Fact]
    public void UnitReference_CreateUnit_ShouldResolveSkills()
    {
        // Arrange
        var manager = new ReferenceManager();
        var skills = new System.Collections.Generic.Dictionary<string, SkillReference>
        {
            ["test_shield"] = new SkillReference
            {
                Type = "Shield",
                MaxShieldHP = 200
            }
        };
        manager.RegisterTable(new ReferenceTable<SkillReference>("skills", skills));

        var unitRef = new UnitReference
        {
            DisplayName = "Test Guard",
            Skills = new System.Collections.Generic.List<string> { "test_shield" }
        };

        // Act
        var unit = unitRef.CreateUnit("test_guard", 2, UnitFaction.Friendly, new Vector2(0, 0), manager);

        // Assert
        unit.HasAbility(AbilityType.Shield).Should().BeTrue();
        unit.MaxShieldHP.Should().Be(200);
    }

    [Fact]
    public void ReferenceTable_GetAll_ShouldReturnAllItems()
    {
        // Arrange
        var data = new System.Collections.Generic.Dictionary<string, UnitReference>
        {
            ["unit_a"] = new UnitReference { DisplayName = "Unit A" },
            ["unit_b"] = new UnitReference { DisplayName = "Unit B" },
            ["unit_c"] = new UnitReference { DisplayName = "Unit C" }
        };
        var table = new ReferenceTable<UnitReference>("test", data);

        // Act
        var all = table.GetAll().ToList();

        // Assert
        all.Should().HaveCount(3);
        table.Keys.Should().Contain("unit_a", "unit_b", "unit_c");
    }
}
