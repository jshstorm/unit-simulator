using System.Numerics;
using Xunit;

namespace UnitSimulator.Core.Tests.Simulation;

public class FrameDataTests
{
    [Fact]
    public void FrameData_SerializesAndDeserializes()
    {
        var friendlies = new List<Unit>
        {
            new Unit(new Vector2(100, 100), 20f, 4.5f, 0.08f, UnitRole.Melee, 100, 1, UnitFaction.Friendly)
        };
        var enemies = new List<Unit>
        {
            new Unit(new Vector2(200, 200), 20f, 4.0f, 0.1f, UnitRole.Ranged, 10, 1, UnitFaction.Enemy)
        };

        var frame = FrameData.FromSimulationState(5, friendlies, enemies, new Vector2(300, 300), 1, hasMoreWaves: false);
        var json = frame.ToJson();
        var restored = FrameData.FromJson(json);

        Assert.NotNull(restored);
        Assert.Equal(frame.FrameNumber, restored!.FrameNumber);
        Assert.Equal(frame.CurrentWave, restored.CurrentWave);
        Assert.Equal(frame.LivingFriendlyCount, restored.LivingFriendlyCount);
        Assert.Equal(frame.LivingEnemyCount, restored.LivingEnemyCount);
    }
}
