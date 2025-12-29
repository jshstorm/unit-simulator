using System;
using System.Collections.Generic;
using System.Numerics;

namespace UnitSimulator.Core.Pathfinding
{
    public class PathfindingTestRunner
    {
        public PathfindingTestReport Run(PathfindingTestSettings settings)
        {
            if (settings.ObstacleDensity < 0 || settings.ObstacleDensity > 1)
            {
                throw new ArgumentOutOfRangeException(nameof(settings.ObstacleDensity), "Obstacle density must be between 0 and 1.");
            }

            var random = new Random(settings.Seed);
            var grid = new PathfindingGrid(settings.MapWidth, settings.MapHeight, settings.NodeSize);
            var obstacles = GenerateObstacles(grid, settings, random);
            var pathfinder = new AStarPathfinder(grid);

            var results = new List<PathfindingTestResult>(settings.ScenarioCount);

            for (int i = 0; i < settings.ScenarioCount; i++)
            {
                var (startNode, endNode, attempts) = PickStartEnd(grid, random, settings.MaxStartEndAttempts);
                var path = pathfinder.FindPath(startNode.WorldPosition, endNode.WorldPosition);
                var (length, nodeCount) = CalculatePathStats(path, startNode.WorldPosition);

                results.Add(new PathfindingTestResult
                {
                    ScenarioIndex = i,
                    Start = startNode.WorldPosition,
                    End = endNode.WorldPosition,
                    PathFound = path != null,
                    NodeCount = nodeCount,
                    PathLength = length,
                    AttemptsToFindStartEnd = attempts
                });
            }

            return new PathfindingTestReport
            {
                Settings = settings,
                Obstacles = obstacles,
                Results = results,
                Summary = PathfindingTestSummary.FromResults(results)
            };
        }

        private static List<PathfindingObstacle> GenerateObstacles(PathfindingGrid grid, PathfindingTestSettings settings, Random random)
        {
            var obstacles = new List<PathfindingObstacle>();
            int totalNodes = grid.Width * grid.Height;
            int targetBlockedNodes = (int)(totalNodes * settings.ObstacleDensity);
            int blockedNodes = 0;

            while (blockedNodes < targetBlockedNodes)
            {
                int sizeX = random.Next(settings.MinObstacleSizeInNodes, settings.MaxObstacleSizeInNodes + 1);
                int sizeY = random.Next(settings.MinObstacleSizeInNodes, settings.MaxObstacleSizeInNodes + 1);

                int minX = random.Next(0, Math.Max(1, grid.Width - sizeX));
                int minY = random.Next(0, Math.Max(1, grid.Height - sizeY));
                int maxX = Math.Min(grid.Width - 1, minX + sizeX - 1);
                int maxY = Math.Min(grid.Height - 1, minY + sizeY - 1);

                int newlyBlocked = 0;
                for (int x = minX; x <= maxX; x++)
                {
                    for (int y = minY; y <= maxY; y++)
                    {
                        var node = grid.GetNode(x, y);
                        if (node != null && node.IsWalkable)
                        {
                            grid.SetWalkable(x, y, false);
                            newlyBlocked++;
                        }
                    }
                }

                if (newlyBlocked > 0)
                {
                    obstacles.Add(new PathfindingObstacle
                    {
                        MinX = minX,
                        MinY = minY,
                        MaxX = maxX,
                        MaxY = maxY
                    });
                    blockedNodes += newlyBlocked;
                }

                if (obstacles.Count > totalNodes)
                {
                    break;
                }
            }

            return obstacles;
        }

        private static (PathNode start, PathNode end, int attempts) PickStartEnd(PathfindingGrid grid, Random random, int maxAttempts)
        {
            PathNode? start = null;
            PathNode? end = null;
            int attempts = 0;

            while (attempts < maxAttempts && (start == null || end == null || start == end))
            {
                attempts++;
                start ??= TryPickWalkableNode(grid, random);
                end ??= TryPickWalkableNode(grid, random);

                if (start != null && end != null)
                {
                    if (start != end)
                    {
                        return (start, end, attempts);
                    }

                    end = null;
                }
            }

            throw new InvalidOperationException("Unable to find distinct walkable start/end nodes within attempt limit.");
        }

        private static PathNode? TryPickWalkableNode(PathfindingGrid grid, Random random)
        {
            int x = random.Next(0, grid.Width);
            int y = random.Next(0, grid.Height);
            var node = grid.GetNode(x, y);
            return node != null && node.IsWalkable ? node : null;
        }

        private static (float length, int nodeCount) CalculatePathStats(List<Vector2>? path, Vector2 start)
        {
            if (path == null || path.Count == 0)
            {
                return (0f, 0);
            }

            float length = 0f;
            Vector2 prev = start;
            foreach (var point in path)
            {
                length += Vector2.Distance(prev, point);
                prev = point;
            }

            return (length, path.Count);
        }
    }
}
