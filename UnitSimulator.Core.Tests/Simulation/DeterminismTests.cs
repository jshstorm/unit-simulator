using UnitSimulator.Core.Tests.TestHelpers;
using Xunit;

namespace UnitSimulator.Core.Tests.Simulation;

public class DeterminismTests
{
    [Fact]
    public void Simulation_SameCommandSequence_ProducesSameOutput()
    {
        var sim1 = SimulationTestFactory.CreateInitializedCore(hasMoreWaves: false);
        var sim2 = SimulationTestFactory.CreateInitializedCore(hasMoreWaves: false);
        var commands = SimulationTestFactory.CreateDeterministicCommands(seed: 4242);

        sim1.EnqueueCommands(commands);
        sim2.EnqueueCommands(commands);

        for (var i = 0; i < 20; i++)
        {
            var frame1 = sim1.Step();
            var frame2 = sim2.Step();
            Assert.Equal(frame1.ToJson(), frame2.ToJson());
        }
    }
}
