using System.Collections.Generic;
using System.Data;
using System.Linq;
using UnityEngine;

public class Pathfinding : MonoBehaviour
{
    //网格寻路

    public static Pathfinding Instance { get; private set; }

    private int width;
    private int height;
    private float cellSize;

    /// <summary>
    /// 直线移动成本
    /// </summary>
    private const int MOVE_STRAIGHT_COST = 10;

    [SerializeField] private Transform gridDebugObjectPrefab; //网格调试对象 显示pathNode g h f值
    [SerializeField] private LayerMask obstaclesLayerMask;

    private GridSystemHex<PathNode> gridSystem;
    //默认寻路规则
    public static PathfindingRules[] defaultPathfindingRules = new PathfindingRules[]
    {
        PathfindingRules.Walkable, //检查是否有障碍
        PathfindingRules.HasUnit, //检查是否有单位
    };


    private void Awake()
    {
        if (Instance != null)
        {
            Debug.Log("There's more than one Pathfinding! " + transform + " - " + Instance);
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public void Setup(int width, int height, float cellSize)
    {
        this.width = width;
        this.height = height;
        this.cellSize = cellSize;

        gridSystem = new GridSystemHex<PathNode>(width, height, cellSize,
            (GridSystemHex<PathNode> g, GridPosition gridPosition) => new PathNode(gridPosition));

        //gridSystem.CreateDebugObject(gridDebugObjectPrefab);

        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < height; z++)
            {
                GridPosition gridPosition = new GridPosition(x, z);
                Vector3 worldPosition = LevelGrid.Instance.GetWorldPosition(gridPosition);
                float raycastOffsetDistance = 5f;

                //发射射线检查是否有障碍物 向下偏移避免从障碍物内部发射导致错误 也可以在项目设置 物理中设置查询命中背面为true
                bool isWalkable = Physics.Raycast(
                   worldPosition + Vector3.down * raycastOffsetDistance,
                   Vector3.up,
                   raycastOffsetDistance * 2,
                   obstaclesLayerMask);

                if (isWalkable)
                {
                    PathNode pathNode = GetNode(x, z);
                    pathNode.SetIsWalkable(false);
                }
            }
        }

    }

    /// <summary>
    /// 使用A*算法查找从起点到终点的最优路径
    /// </summary>
    /// <param name="startGridPosition">起始网格坐标</param>
    /// <param name="endGridPosition">目标网格坐标</param>
    /// <param name="pathfindingRules"> 寻路规则数组，包含以下选项：
    /// Straight 是否直线移动
    /// Walkable 是否有障碍物
    /// HasUnit 是否有单位
    /// </param>
    /// <returns>
    /// 返回网格坐标列表表示路径，第一个元素是起点，最后一个元素是终点。
    /// 如果无法到达目标则返回null。
    /// <code>
    ///  示例：查找从(0,0)到(3,2)的路径
    ///  FindPath(new GridPosition(0, 0), new GridPosition(3, 2));
    ///  可能返回：[ (0,0), (1,1), (2,1), (3,2) ]
    /// </code>
    /// </returns>
    /// <remarks>
    /// 实现要点：
    /// 1. 使用曼哈顿距离作为启发式函数
    /// 2. 支持8方向移动（含对角线）
    /// 3. 时间复杂度：最坏情况O(n)，n为网格节点数
    /// </remarks>
    public List<GridPosition> FindPath(GridPosition startGridPosition, GridPosition endGridPosition, out int pathLength,
        PathfindingRules[] pathfindingRules = null)
    {
        //设置默认路径查找规则
        if (pathfindingRules == null)
        {
            pathfindingRules = defaultPathfindingRules;
        }
        List<PathNode> openList = new List<PathNode>();
        List<PathNode> closeList = new List<PathNode>();

        PathNode startNode = gridSystem.GetGridObject(startGridPosition);
        PathNode endNode = gridSystem.GetGridObject((endGridPosition));

        openList.Add(startNode);

        //初始化所有网格节点的代价
        for (int x = 0; x < gridSystem.GetWidth(); x++)
        {
            for (int z = 0; z < gridSystem.GetHeight(); z++)
            {
                GridPosition gridPosition = new GridPosition(x, z);
                PathNode pathNode = gridSystem.GetGridObject(gridPosition);

                pathNode.SetGCost(int.MaxValue);
                pathNode.SetHCost(0);
                pathNode.CalculateFCost();
                pathNode.ResetCameFromPathNode();
            }
        }

        startNode.SetGCost(0);
        startNode.SetHCost(CalculateHeuristicDistance(startGridPosition, endGridPosition));
        startNode.CalculateFCost();

        while (openList.Count > 0)
        {
            PathNode currentNode = GetLowestFCostPathNode(openList);
            if (currentNode == endNode)
            {
                //到达最终节点
                pathLength = endNode.GetFCost();
                return CalculatePath(endNode);
            }

            openList.Remove(currentNode);
            closeList.Add(currentNode);


            foreach (PathNode neighbourNode in GetNeighourList(currentNode))
            {
                if (closeList.Contains(neighbourNode))
                {
                    continue;
                }

                if (!IsNodeValid(startNode, neighbourNode, pathfindingRules))
                {
                    closeList.Add(neighbourNode);
                    continue;
                }

                int tentativeGcost = currentNode.GetGCost() +
                    MOVE_STRAIGHT_COST;

                if (tentativeGcost < neighbourNode.GetGCost())
                {
                    neighbourNode.SetCameFromPathNode(currentNode); //设置链接节点
                    neighbourNode.SetGCost(tentativeGcost);
                    neighbourNode.SetHCost(CalculateHeuristicDistance(neighbourNode.GetGridPosition(), endGridPosition));
                    neighbourNode.CalculateFCost();

                    if (!openList.Contains(neighbourNode))
                    {
                        openList.Add(neighbourNode);
                    }
                }
            }
        }

        pathLength = 0;
        //没有找到路径
        return null;
    }

    /// <summary>
    /// 寻路验证规则
    /// </summary>
    public enum PathfindingRules
    {
        Straight, //直线移动
        Walkable, //障碍物
        HasUnit, //有单位
    }

    private bool IsNodeValid(PathNode startPathNode, PathNode endPathNode, PathfindingRules[] pathfindingRules)
    {

        if (endPathNode == null) return false;
        foreach (PathfindingRules rule in pathfindingRules)
        {
            switch (rule)
            {
                case PathfindingRules.Walkable:
                    if (!endPathNode.IsWalkable())
                    {
                        return false;
                    }
                    break;
                case PathfindingRules.HasUnit:
                    if (LevelGrid.Instance.HasAnyUnitOnGridPosition(endPathNode.GetGridPosition()))
                    {
                        return false;
                    }
                    break;
                case PathfindingRules.Straight:
                    //检查是否在一条直线上
                    if (!startPathNode.GetGridPosition().IsStraightLine(endPathNode.GetGridPosition()))
                    {
                        return false;
                    }
                    break;
            }
        }

        return true;
    }


    private int CalculateHeuristicDistance(GridPosition gridPositionA, GridPosition gridPositionB)
    {
        return Mathf.RoundToInt(MOVE_STRAIGHT_COST *
            Vector3.Distance(gridSystem.GetWorldPosition(gridPositionA),
            gridSystem.GetWorldPosition(gridPositionB)));
    }

    /// <summary>
    /// 获取移动成本距离最优的PathNode
    /// </summary>
    /// <param name="pathNodeList">节点集合</param>
    /// <returns>移动成本距离最优的PathNode</returns>
    private PathNode GetLowestFCostPathNode(List<PathNode> pathNodeList)
    {
        PathNode lowestFCostPathNode = pathNodeList[0];
        for (int i = 1; i < pathNodeList.Count; i++)
        {
            if (pathNodeList[i].GetFCost() < lowestFCostPathNode.GetFCost())
            {
                lowestFCostPathNode = pathNodeList[i];
            }

        }
        return lowestFCostPathNode;
    }

    private PathNode GetNode(int x, int z)
    {
        return gridSystem.GetGridObject(new GridPosition(x, z));
    }

    /// <summary>
    /// 获取当前节点位置6个方向的节点
    /// </summary>
    /// <param name="currentNode">当前节点</param>
    /// <returns>包含6个方向节点的数组</returns>
    private List<PathNode> GetNeighourList(PathNode currentNode)
    {
        List<PathNode> neighbourList = new List<PathNode>();
        GridPosition gridPosition = currentNode.GetGridPosition();

        bool oddRow = (gridPosition.z % 2f == 0);

        //计算8个方向的网格坐标是否合法
        bool xReduceValid = gridPosition.x - 1 >= 0;
        bool xAddValid = gridPosition.x + 1 < gridSystem.GetWidth();
        bool zReduceValid = gridPosition.z - 1 >= 0;
        bool zAddValid = gridPosition.z + 1 < gridSystem.GetHeight();

        //Left
        if (xReduceValid) neighbourList.Add(GetNode(gridPosition.x - 1, gridPosition.z + 0));

        //Right
        if (xAddValid) neighbourList.Add(GetNode(gridPosition.x + 1, gridPosition.z + 0));

        //Up
        if (zAddValid)
        {
            neighbourList.Add(GetNode(gridPosition.x + 0, gridPosition.z + 1));

            if (oddRow ? xReduceValid : xAddValid) neighbourList.Add(GetNode(gridPosition.x + (oddRow ? -1 : +1), gridPosition.z + 1));
        }
        ;

        //Down
        if (zReduceValid)
        {
            neighbourList.Add(GetNode(gridPosition.x + 0, gridPosition.z - 1));
            if (oddRow ? xReduceValid : xAddValid) neighbourList.Add(GetNode(gridPosition.x + (oddRow ? -1 : +1), gridPosition.z - 1));
        }
        ;

        return neighbourList;
    }

    /// <summary>
    /// 从终点回溯计算完整路径
    /// </summary>
    private List<GridPosition> CalculatePath(PathNode endNode)
    {
        List<PathNode> pathNodeList = new List<PathNode>();
        pathNodeList.Add(endNode);
        PathNode currentNode = endNode;

        while (currentNode.GetCameFromPathNode() != null) //如果有链接节点，添加至数组
        {
            pathNodeList.Add(currentNode.GetCameFromPathNode());
            currentNode = currentNode.GetCameFromPathNode();
        }
        pathNodeList.Reverse(); //因为从终点开始，所以反转节点

        List<GridPosition> gridPositionList = new List<GridPosition>();
        foreach (PathNode pathNode in pathNodeList)
        {
            gridPositionList.Add(pathNode.GetGridPosition());
        }

        return gridPositionList;
    }

    /// <summary>
    /// 网格对象是否有障碍物
    /// </summary>
    /// <param name="gridPosition">网格位置</param>
    /// <returns>是否有障碍物</returns>
    public bool IsWalkableGridPosition(GridPosition gridPosition)
    {
        return gridSystem.GetGridObject(gridPosition).IsWalkable();
    }

    /// <summary>
    /// 设置障碍物状态
    /// </summary>
    /// <param name="gridPosition"></param>
    /// <param name="isWalkable"></param>
    public void SetIsWalkableGridPosition(GridPosition gridPosition, bool isWalkable)
    {
        gridSystem.GetGridObject(gridPosition).SetIsWalkable(isWalkable);
    }

    /// <summary>
    /// 检查是路径是否可行
    /// </summary>
    /// <param name="startGridPositionm">开始位置</param>
    /// <param name="endGridPositionm">结束位置</param>
    /// <returns>路径通行结果</returns>
    public bool HasPath(GridPosition startGridPositionm, GridPosition endGridPositionm)
    {
        return FindPath(startGridPositionm, endGridPositionm, out int pathLength) != null;
    }

    /// <summary>
    /// 检查直线路径是否可行
    /// </summary>
    /// <param name="startGridPositionm">开始位置</param>
    /// <param name="endGridPositionm">结束位置</param>
    /// <returns>路径通行结果</returns>
    public bool HasPathStraight(GridPosition startGridPositionm, GridPosition endGridPositionm)
    {
        PathfindingRules[] pathfindingRules = new PathfindingRules[]
        {
            PathfindingRules.Walkable, //检查是否有障碍
            PathfindingRules.Straight //检查是否直线移动
        };

        return FindPath(startGridPositionm, endGridPositionm, out int pathLength, pathfindingRules) != null;
    }

    /// <summary>
    /// 获取路径长度
    /// </summary>
    /// <param name="startGridPosition"></param>
    /// <param name="endGridPositionm"></param>
    /// <returns></returns>
    public int GetPathLength(GridPosition startGridPosition, GridPosition endGridPositionm)
    {
        FindPath(startGridPosition, endGridPositionm, out int pathLength);
        return pathLength;
    }
}