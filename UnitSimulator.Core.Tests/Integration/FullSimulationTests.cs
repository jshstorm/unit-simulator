using UnitSimulator.Core.Tests.TestHelpers;
using Xunit;

namespace UnitSimulator.Core.Tests.Integration;

public class FullSimulationTests
{
    [Fact]
    public void Simulation_RunsForMultipleFrames()
    {
        var simulator = SimulationTestFactory.CreateInitializedCore(hasMoreWaves: false);
        simulator.EnqueueCommands(SimulationTestFactory.CreateDeterministicCommands());

        var first = simulator.Step();
        var second = simulator.Step();

        Assert.Equal(0, first.FrameNumber);
        Assert.Equal(1, second.FrameNumber);
    }
}
