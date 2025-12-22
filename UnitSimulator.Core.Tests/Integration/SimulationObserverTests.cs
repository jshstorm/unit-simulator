using System.Numerics;
using UnitSimulator.Core.Contracts;
using UnitSimulator.Core.Tests.TestHelpers;
using Xunit;

namespace UnitSimulator.Core.Tests.Integration;

public class SimulationObserverTests
{
    [Fact]
    public void Observer_ReceivesFrameAndSpawnEvents()
    {
        var observer = new TestObserver();
        var facade = SimulationTestFactory.CreateFacade(observer);

        facade.SpawnUnit(new Vector2(1600, 500), UnitRole.Melee, UnitFaction.Enemy, hp: 10);
        facade.Step();

        Assert.Equal(1, observer.FramesAdvanced);
        Assert.Equal(1, observer.UnitsSpawned);
    }

    private sealed class TestObserver : ISimulationObserver
    {
        public int FramesAdvanced { get; private set; }
        public int UnitsSpawned { get; private set; }

        public void OnFrameAdvanced(FrameData frameData)
        {
            FramesAdvanced++;
        }

        public void OnUnitSpawned(Unit unit)
        {
            UnitsSpawned++;
        }

        public void OnUnitDied(Unit unit, Unit? killer)
        {
        }

        public void OnUnitDamaged(Unit unit, int damage, Unit? attacker)
        {
        }

        public void OnWaveStarted(int waveNumber)
        {
        }

        public void OnSimulationComplete(string reason)
        {
        }
    }
}
