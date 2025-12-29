using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using UnitSimulator;

namespace UnitSimulator.Core.Pathfinding
{
    public class PathfindingTestReport
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        public PathfindingTestSettings Settings { get; set; } = new();
        public List<PathfindingObstacle> Obstacles { get; set; } = new();
        public List<PathfindingTestResult> Results { get; set; } = new();
        public PathfindingTestSummary Summary { get; set; } = new();
        public DateTime GeneratedAtUtc { get; set; } = DateTime.UtcNow;

        public string ToJson()
        {
            return JsonSerializer.Serialize(this, JsonOptions);
        }

        public void SaveToJson(string filePath)
        {
            var json = ToJson();
            File.WriteAllText(filePath, json);
        }
    }

    public class PathfindingObstacle
    {
        public int MinX { get; set; }
        public int MinY { get; set; }
        public int MaxX { get; set; }
        public int MaxY { get; set; }
    }

    public class PathfindingTestResult
    {
        public int ScenarioIndex { get; set; }
        public SerializableVector2 Start { get; set; } = new();
        public SerializableVector2 End { get; set; } = new();
        public bool PathFound { get; set; }
        public int NodeCount { get; set; }
        public float PathLength { get; set; }
        public int AttemptsToFindStartEnd { get; set; }
    }

    public class PathfindingTestSummary
    {
        public int TotalScenarios { get; set; }
        public int SuccessfulPaths { get; set; }
        public int FailedPaths { get; set; }
        public float SuccessRate { get; set; }
        public float AveragePathLength { get; set; }
        public float AverageNodeCount { get; set; }

        public static PathfindingTestSummary FromResults(IEnumerable<PathfindingTestResult> results)
        {
            var resultList = results.ToList();
            int total = resultList.Count;
            int success = resultList.Count(r => r.PathFound);
            int failed = total - success;
            float avgLength = success == 0 ? 0f : (float)resultList.Where(r => r.PathFound).Average(r => r.PathLength);
            float avgNodes = success == 0 ? 0f : (float)resultList.Where(r => r.PathFound).Average(r => r.NodeCount);
            float rate = total == 0 ? 0f : (float)success / total;

            return new PathfindingTestSummary
            {
                TotalScenarios = total,
                SuccessfulPaths = success,
                FailedPaths = failed,
                SuccessRate = rate,
                AveragePathLength = avgLength,
                AverageNodeCount = avgNodes
            };
        }
    }
}
