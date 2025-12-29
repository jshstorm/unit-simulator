using System.Numerics;
using UnitSimulator.Core.Pathfinding;
using Xunit;
using Xunit.Abstractions;

namespace UnitSimulator.Core.Tests.Pathfinding;

public class PathfindingTests
{
    private readonly ITestOutputHelper _output;

    public PathfindingTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void FindPath_ReturnsNull_WhenStartOrEndBlocked()
    {
        var grid = new PathfindingGrid(100, 100, 10);
        var pathfinder = new AStarPathfinder(grid);

        grid.SetWalkable(0, 0, false);
        var pathFromBlockedStart = pathfinder.FindPath(new Vector2(1, 1), new Vector2(50, 50));
        Assert.Null(pathFromBlockedStart);

        grid.SetWalkable(0, 0, true);
        grid.SetWalkable(5, 5, false);
        var pathToBlockedEnd = pathfinder.FindPath(new Vector2(1, 1), new Vector2(55, 55));
        Assert.Null(pathToBlockedEnd);
    }

    [Fact]
    public void FindPath_AvoidsBlockedNodes()
    {
        var grid = new PathfindingGrid(100, 100, 10);
        var pathfinder = new AStarPathfinder(grid);

        grid.SetWalkable(1, 1, false);

        var path = pathfinder.FindPath(new Vector2(5, 5), new Vector2(35, 35));
        Assert.NotNull(path);
        foreach (var waypoint in path!)
        {
            var node = grid.NodeFromWorldPoint(waypoint);
            Assert.NotNull(node);
            Assert.True(node!.IsWalkable);
        }
    }

    [Fact]
    public void FindPath_DoesNotCutCorners()
    {
        var grid = new PathfindingGrid(20, 20, 10);
        var pathfinder = new AStarPathfinder(grid);

        grid.SetWalkable(1, 0, false);
        grid.SetWalkable(0, 1, false);

        var path = pathfinder.FindPath(new Vector2(5, 5), new Vector2(15, 15));
        Assert.Null(path);
    }

    [Fact]
    public void FindPath_VisualizesPath_ForSpecifiedFromTo()
    {
        const float nodeSize = 1f;
        var grid = new PathfindingGrid(10, 10, nodeSize);
        var pathfinder = new AStarPathfinder(grid);

        BlockVerticalWall(grid, x: 4, gapY: 5);
        var start = NodeCenter(0, 0, nodeSize);
        var end = NodeCenter(9, 9, nodeSize);

        var path = pathfinder.FindPath(start, end);

        Assert.NotNull(path);
        var visualization = RenderGrid(grid, path!, start, end, nodeSize);
        _output.WriteLine("Path visualization (success):");
        _output.WriteLine(visualization);
    }

    [Fact]
    public void FindPath_ReportsFailure_ForBlockedFromTo()
    {
        const float nodeSize = 1f;
        var grid = new PathfindingGrid(6, 6, nodeSize);
        var pathfinder = new AStarPathfinder(grid);

        for (int x = 0; x < grid.Width; x++)
        {
            grid.SetWalkable(x, 3, false);
        }

        var start = NodeCenter(1, 1, nodeSize);
        var end = NodeCenter(4, 5, nodeSize);

        var path = pathfinder.FindPath(start, end);

        Assert.Null(path);
        var visualization = RenderGrid(grid, null, start, end, nodeSize);
        _output.WriteLine("Path visualization (failure):");
        _output.WriteLine(visualization);
    }

    private static void BlockVerticalWall(PathfindingGrid grid, int x, int gapY)
    {
        for (int y = 0; y < grid.Height; y++)
        {
            if (y == gapY)
            {
                continue;
            }
            grid.SetWalkable(x, y, false);
        }
    }

    private static Vector2 NodeCenter(int x, int y, float nodeSize)
    {
        return new Vector2((x + 0.5f) * nodeSize, (y + 0.5f) * nodeSize);
    }

    private static string RenderGrid(PathfindingGrid grid, List<Vector2>? path, Vector2 start, Vector2 end, float nodeSize)
    {
        var pathNodes = new HashSet<(int x, int y)>();
        if (path != null)
        {
            foreach (var point in path)
            {
                var node = grid.NodeFromWorldPoint(point);
                if (node != null)
                {
                    pathNodes.Add((node.X, node.Y));
                }
            }
        }

        var startNode = grid.NodeFromWorldPoint(start);
        var endNode = grid.NodeFromWorldPoint(end);

        var lines = new List<string>();
        for (int y = grid.Height - 1; y >= 0; y--)
        {
            var line = new char[grid.Width];
            for (int x = 0; x < grid.Width; x++)
            {
                var node = grid.GetNode(x, y);
                if (node == null)
                {
                    line[x] = '?';
                    continue;
                }

                if (startNode != null && node.X == startNode.X && node.Y == startNode.Y)
                {
                    line[x] = 'S';
                    continue;
                }

                if (endNode != null && node.X == endNode.X && node.Y == endNode.Y)
                {
                    line[x] = 'E';
                    continue;
                }

                if (!node.IsWalkable)
                {
                    line[x] = '#';
                    continue;
                }

                line[x] = pathNodes.Contains((x, y)) ? '*' : '.';
            }
            lines.Add(new string(line));
        }

        return string.Join(Environment.NewLine, lines);
    }
}
