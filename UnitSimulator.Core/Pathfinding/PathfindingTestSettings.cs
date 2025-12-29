using System;

namespace UnitSimulator.Core.Pathfinding
{
    public class PathfindingTestSettings
    {
        public int Seed { get; set; } = Environment.TickCount;
        public float MapWidth { get; set; } = GameConstants.SIMULATION_WIDTH;
        public float MapHeight { get; set; } = GameConstants.SIMULATION_HEIGHT;
        public float NodeSize { get; set; } = GameConstants.UNIT_RADIUS;
        public float ObstacleDensity { get; set; } = 0.15f;
        public int MinObstacleSizeInNodes { get; set; } = 2;
        public int MaxObstacleSizeInNodes { get; set; } = 6;
        public int ScenarioCount { get; set; } = 25;
        public int MaxStartEndAttempts { get; set; } = 200;
    }
}
