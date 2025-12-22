using System;
using System.Collections.Generic;
using System.Numerics;
using UnitSimulator.Core;
using UnitSimulator.Core.Contracts;

namespace UnitSimulator.Core.Tests.TestHelpers;

internal static class SimulationTestFactory
{
    internal static SimulatorCore CreateInitializedCore(bool hasMoreWaves = false, int currentWave = 0)
    {
        var simulator = new SimulatorCore();
        simulator.Initialize();
        simulator.CurrentWave = currentWave;
        simulator.HasMoreWaves = hasMoreWaves;
        return simulator;
    }

    internal static SimulationFacade CreateFacade(ISimulationObserver? observer = null, GameConfig? config = null)
    {
        var facade = new SimulationFacade(observer);
        facade.Initialize(config ?? new GameConfig { HasMoreWaves = false });
        return facade;
    }

    internal static IReadOnlyList<ISimulationCommand> CreateDeterministicCommands(int seed = 1337)
    {
        var rng = new Random(seed);
        var spawn1 = new Vector2(rng.Next(1500, 1700), rng.Next(480, 520));
        var move1 = new Vector2(rng.Next(1350, 1550), rng.Next(480, 540));
        var move2 = new Vector2(rng.Next(1300, 1500), rng.Next(500, 560));
        var spawn2 = new Vector2(rng.Next(1650, 1750), rng.Next(500, 540));
        var move3 = new Vector2(rng.Next(1500, 1600), rng.Next(520, 580));

        return new List<ISimulationCommand>
        {
            new SpawnUnitCommand(0, spawn1, UnitRole.Melee, UnitFaction.Enemy, HP: 10),
            new MoveUnitCommand(1, 1, UnitFaction.Enemy, move1),
            new DamageUnitCommand(2, 1, UnitFaction.Enemy, 3),
            new MoveUnitCommand(3, 1, UnitFaction.Enemy, move2),
            new SpawnUnitCommand(5, spawn2, UnitRole.Ranged, UnitFaction.Enemy, HP: 8),
            new MoveUnitCommand(6, 2, UnitFaction.Enemy, move3)
        };
    }
}
