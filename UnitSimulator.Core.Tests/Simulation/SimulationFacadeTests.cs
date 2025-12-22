using System.Numerics;
using UnitSimulator.Core.Contracts;
using UnitSimulator.Core.Tests.TestHelpers;
using Xunit;

namespace UnitSimulator.Core.Tests.Simulation;

public class SimulationFacadeTests
{
    [Fact]
    public void Facade_InitializesAndReportsStatus()
    {
        var facade = new SimulationFacade();
        Assert.Equal(SimulationStatus.Uninitialized, facade.Status);

        facade.Initialize(new GameConfig { HasMoreWaves = true });

        Assert.Equal(SimulationStatus.Initialized, facade.Status);
    }

    [Fact]
    public void Facade_EnqueuesSpawnCommand()
    {
        var facade = SimulationTestFactory.CreateFacade();

        facade.SpawnUnit(new Vector2(1600, 500), UnitRole.Melee, UnitFaction.Enemy, hp: 10);
        var frame = facade.Step();

        Assert.Equal(1, frame.LivingEnemyCount);
    }
}
