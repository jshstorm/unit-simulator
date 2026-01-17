using System.Numerics;

namespace UnitSimulator.Core.Pathfinding
{
    public class PathfindingGrid
    {
        private readonly int _width;
        private readonly int _height;
        private readonly float _nodeSize;
        private readonly PathNode[,] _grid;

        public int Width => _width;
        public int Height => _height;
        public float NodeSize => _nodeSize;

        public PathfindingGrid(float mapWidth, float mapHeight, float nodeSize)
        {
            _nodeSize = nodeSize;
            _width = (int)(mapWidth / nodeSize);
            _height = (int)(mapHeight / nodeSize);
            _grid = new PathNode[_width, _height];

            for (int x = 0; x < _width; x++)
            {
                for (int y = 0; y < _height; y++)
                {
                    var worldPos = new Vector2(x * nodeSize + nodeSize / 2, y * nodeSize + nodeSize / 2);
                    _grid[x, y] = new PathNode(x, y, worldPos);
                }
            }
        }

        public PathNode? GetNode(int x, int y)
        {
            if (x >= 0 && x < _width && y >= 0 && y < _height)
            {
                return _grid[x, y];
            }
            return null;
        }

        public PathNode? NodeFromWorldPoint(Vector2 worldPosition)
        {
            int x = (int)(worldPosition.X / _nodeSize);
            int y = (int)(worldPosition.Y / _nodeSize);
            return GetNode(x, y);
        }

        public bool SetWalkable(int x, int y, bool isWalkable)
        {
            var node = GetNode(x, y);
            if (node == null)
            {
                return false;
            }

            node.IsWalkable = isWalkable;
            return true;
        }

        public bool SetWalkableWorld(Vector2 worldPosition, bool isWalkable)
        {
            var node = NodeFromWorldPoint(worldPosition);
            if (node == null)
            {
                return false;
            }

            node.IsWalkable = isWalkable;
            return true;
        }

        public void SetWalkableRect(Vector2 min, Vector2 max, bool isWalkable)
        {
            int minX = Math.Clamp((int)(min.X / _nodeSize), 0, _width - 1);
            int minY = Math.Clamp((int)(min.Y / _nodeSize), 0, _height - 1);
            int maxX = Math.Clamp((int)(max.X / _nodeSize), 0, _width - 1);
            int maxY = Math.Clamp((int)(max.Y / _nodeSize), 0, _height - 1);

            for (int x = minX; x <= maxX; x++)
            {
                for (int y = minY; y <= maxY; y++)
                {
                    _grid[x, y].IsWalkable = isWalkable;
                }
            }
        }

        /// <summary>
        /// 원형 영역의 이동 가능 여부를 설정합니다.
        /// 원의 중심과 반경을 기준으로 해당 영역의 모든 노드를 설정합니다.
        /// </summary>
        /// <param name="center">원의 중심 (월드 좌표)</param>
        /// <param name="radius">원의 반경</param>
        /// <param name="isWalkable">이동 가능 여부</param>
        public void SetWalkableCircle(Vector2 center, float radius, bool isWalkable)
        {
            // 원을 포함하는 사각형 범위 계산
            int minX = Math.Clamp((int)((center.X - radius) / _nodeSize), 0, _width - 1);
            int minY = Math.Clamp((int)((center.Y - radius) / _nodeSize), 0, _height - 1);
            int maxX = Math.Clamp((int)((center.X + radius) / _nodeSize), 0, _width - 1);
            int maxY = Math.Clamp((int)((center.Y + radius) / _nodeSize), 0, _height - 1);

            float radiusSq = radius * radius;

            for (int x = minX; x <= maxX; x++)
            {
                for (int y = minY; y <= maxY; y++)
                {
                    var node = _grid[x, y];
                    float dx = node.WorldPosition.X - center.X;
                    float dy = node.WorldPosition.Y - center.Y;
                    float distSq = dx * dx + dy * dy;

                    // 노드 중심이 원 내부에 있으면 설정
                    if (distSq <= radiusSq)
                    {
                        node.IsWalkable = isWalkable;
                    }
                }
            }
        }

        /// <summary>
        /// IObstacleProvider로부터 장애물 정보를 받아 그리드에 적용합니다.
        /// </summary>
        /// <param name="provider">장애물 정보 제공자</param>
        public void ApplyObstacles(IObstacleProvider provider)
        {
            // 사각형 장애물 적용
            foreach (var (min, max) in provider.GetUnwalkableRects())
            {
                SetWalkableRect(min, max, false);
            }

            // 원형 장애물 적용
            foreach (var (center, radius) in provider.GetUnwalkableCircles())
            {
                SetWalkableCircle(center, radius, false);
            }
        }

        public void ResetAllNodes()
        {
            foreach (var node in _grid)
            {
                node.ResetCosts();
            }
        }
    }
}
