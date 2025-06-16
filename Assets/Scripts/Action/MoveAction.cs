using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MoveAction : BaseAction
{
    public event EventHandler OnStartMoving;
    public event EventHandler OnStopMoving;

    private List<Vector3> positionList; //完整路径
    private int currentPositionIndex;

    protected override void Awake()
    {
        base.Awake();
    }

    public void Update()
    {
        if (!isActive) return;

        Vector3 targetPosition = positionList[currentPositionIndex];
        Vector3 moveDirection = (targetPosition - transform.position).normalized;

        float stoppingDistance = .1f;
        if (Vector3.Distance(transform.position, targetPosition) > stoppingDistance)
        {
            float moveSpeed = 4f;
            transform.position += moveSpeed * Time.deltaTime * moveDirection;
        }
        else
        {
            unit.TrySpendActionPointsToTakeAction(GetMoveGridActionPoint(currentPositionIndex));
            currentPositionIndex++;

            if (currentPositionIndex >= positionList.Count)
            {
                OnStopMoving?.Invoke(this, EventArgs.Empty);
                ActionComplete();
            }
        }

        float rotateSpeed = 10f;
        transform.forward = Vector3.Slerp(transform.forward, moveDirection, Time.deltaTime * rotateSpeed);
    }

    public override void TakeAction(List<GridPosition> gridPositions, Action actionCompleted)
    {
        unit.SetForecast(false);
        currentPositionIndex = 0;
        positionList = new List<Vector3>();
        List<GridPosition> path = GetPathForTargetPositions(gridPositions);

        positionList = path.Select(gridPosition => (Vector3)gridPosition).ToList();

        ActionStart(actionCompleted);
        OnStartMoving?.Invoke(this, EventArgs.Empty);
    }

    public override List<GridPosition> GetValidActionGridPositionList()
    {
        List<GridPosition> validActionGridPositionList = new();
        GridPosition unitGridPosition = unit.GetGridPosition();

        int maxMoveDistance = GetMaxMoveDistance();
        if (maxMoveDistance == 0) return validActionGridPositionList;

        for (int x = -maxMoveDistance; x <= maxMoveDistance; x++)
        {
            for (int z = -maxMoveDistance; z <= maxMoveDistance; z++)
            {
                GridPosition offsetGridPosition = new GridPosition(x, z);
                GridPosition testGridPosition = unitGridPosition + offsetGridPosition;

                if (!LevelGrid.Instance.IsValidGridPosition(testGridPosition)) continue;
                if (unitGridPosition == testGridPosition) continue;
                if (LevelGrid.Instance.HasAnyUnitOnGridPosition(testGridPosition)) continue;
                if (!Pathfinding.Instance.IsWalkableGridPosition(testGridPosition)) continue;
                if (!Pathfinding.Instance.HasPath(unit.GetGridPosition(), testGridPosition)) continue;
                if (unit.GetActionQueue().IsLastActionParameterTargetPosition(testGridPosition)) continue;

                int pathfindingDistanceMultiplier = 10;
                if (Pathfinding.Instance.GetPathLength(unit.GetGridPosition(), testGridPosition)
                    > maxMoveDistance * pathfindingDistanceMultiplier)
                {
                    continue;
                }

                validActionGridPositionList.Add(testGridPosition);
            }
        }
        return validActionGridPositionList;
    }

    public override string GetActionName()
    {
        return "移动";
    }

    public override EnemyAIAction GetEnemyAIAction(GridPosition gridPosition)
    {
        int targetCountAtGridPosition = unit.GetAction<ShootAction>().GetTargetCountAtPosition(gridPosition);
        return new EnemyAIAction
        {
            gridPosition = gridPosition,
            actionValue = targetCountAtGridPosition * 10,
        };
    }

    /// <summary>
    /// 获取行动点数消耗（基于实际路径格数）
    /// </summary>
    public override int GetActionPointsCost(BaseActionParameter baseActionParameter = null)
    {
        if (baseActionParameter == null) return 0;
        var path = GetPathForTargetPositions(baseActionParameter.targetPositionList);
        int moveGrids = path.Count;
        return GetTotalMoveActionPoints(moveGrids);
    }

    /// <summary>
    /// 根据目标点列表获取完整路径（不包含第一个起点）
    /// </summary>
    public List<GridPosition> GetPathForTargetPositions(List<GridPosition> targetPositions)
    {
        List<GridPosition> path = new List<GridPosition>();
        if (targetPositions == null || targetPositions.Count < 2) return path;

        for (int i = 1; i < targetPositions.Count; i++)
        {
            GridPosition start = targetPositions[i - 1];
            GridPosition end = targetPositions[i];
            List<GridPosition> segment = Pathfinding.Instance.FindPath(start, end, out int pathCount);
            if (segment.Count > 1)
            {
                // 跳过第一个点（起点），避免重复
                path.AddRange(segment.Skip(1));
            }
        }
        return path;
    }

    /// <summary>
    /// 获取总的行动点数（每一格都递增）
    /// </summary>
    private int GetTotalMoveActionPoints(int moveGrids, int startCount = 0)
    {
        int total = 0;
        for (int i = 1; i <= moveGrids; i++)
        {
            total += GetMoveGridActionPoint(startCount + i);
        }
        return total;
    }

    /// <summary>
    /// 获取当前移动格数的行动点数
    /// </summary>
    private static int GetMoveGridActionPoint(int gridIndex)
    {
        if (gridIndex == 0) return 0;
        float cost = 10f;
        for (int i = 1; i < gridIndex; i++)
        {
            cost *= 1.2f;
        }
        return Mathf.CeilToInt(cost);
    }

    /// <summary>
    /// 获取最大可移动格数（连续部署时自动递增）
    /// </summary>
    private int GetMaxMoveDistance()
    {
        int maxMoveDistance = 0;
        int startCount = 0;
        if (UnitActionSystem.Instance.IsDeployAction())
        {
            var actions = unit.GetActionQueue().GetActions();
            if (actions.Count > 0)
            {
                var lastParam = actions.Last();
                var path = GetPathForTargetPositions(lastParam.targetPositionList);
                startCount = path.Count;
            }
        }
        while (unit.CanspendActionPointsToTakeAction(GetTotalMoveActionPoints(maxMoveDistance, startCount)))
        {
            maxMoveDistance++;
        }
        return maxMoveDistance > 0 ? maxMoveDistance - 1 : 0;
    }
}
