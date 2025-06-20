using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MoveAction : BaseAction
{
    public event EventHandler OnStartMoving;
    public event EventHandler OnStopMoving;

    private List<Vector3> movePathWorldPositions;
    private int currentPathIndex;

    protected override void Awake()
    {
        base.Awake();
    }

    public void Update()
    {
        if (!isActive) return;

        Vector3 targetPosition = movePathWorldPositions[currentPathIndex];
        Vector3 moveDirection = (targetPosition - transform.position).normalized;

        float stoppingDistance = 0.1f;
        if (Vector3.Distance(transform.position, targetPosition) > stoppingDistance)
        {
            float moveSpeed = 4f;
            transform.position += moveSpeed * Time.deltaTime * moveDirection;
        }
        else
        {
            currentPathIndex++;
            unit.TrySpendActionPointsToTakeAction(GetMoveStepActionPointCost(currentPathIndex));
            if (currentPathIndex >= movePathWorldPositions.Count)
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
        currentPathIndex = 0;
        var path = BuildMovePathFromTargets(gridPositions);
        movePathWorldPositions = path.Select(pos => (Vector3)pos).ToList();

        ActionStart(actionCompleted);
        OnStartMoving?.Invoke(this, EventArgs.Empty);
    }

    public override List<GridPosition> GetValidActionGridPositionList()
    {
        List<GridPosition> validTargets = new();
        GridPosition unitGridPosition = unit.GetGridPosition();

        int maxSteps = GetMaxMoveStepsByActionPoints();
        if (maxSteps == 0) return validTargets;

        for (int x = -maxSteps; x <= maxSteps; x++)
        {
            for (int z = -maxSteps; z <= maxSteps; z++)
            {
                GridPosition offset = new GridPosition(x, z);
                GridPosition testPos = unitGridPosition + offset;

                if (!LevelGrid.Instance.IsValidGridPosition(testPos)) continue;
                if (unitGridPosition == testPos) continue;
                if (LevelGrid.Instance.HasAnyUnitOnGridPosition(testPos)) continue;
                if (!Pathfinding.Instance.IsWalkableGridPosition(testPos)) continue;
                if (!Pathfinding.Instance.HasPath(unit.GetGridPosition(), testPos)) continue;
                if (unit.GetActionQueue().IsLastActionParameterTargetPosition(testPos)) continue;

                int pathfindingDistanceMultiplier = 10;
                if (Pathfinding.Instance.GetPathLength(unit.GetGridPosition(), testPos) > maxSteps * pathfindingDistanceMultiplier)
                {
                    continue;
                }

                validTargets.Add(testPos);
            }
        }
        return validTargets;
    }

    public override string GetActionName() => "移动";

    public override EnemyAIAction GetEnemyAIAction(GridPosition gridPosition)
    {
        int targetCount = unit.GetAction<ShootAction>().GetTargetCountAtPosition(gridPosition);
        return new EnemyAIAction
        {
            gridPosition = gridPosition,
            actionValue = targetCount * 10,
        };
    }

    public override int GetActionPointsCost(BaseActionParameter baseActionParameter = null)
    {
        if (baseActionParameter == null) return 0;
        var path = BuildMovePathFromTargets(baseActionParameter.targetPositionList);
        int moveSteps = path.Count;
        return CalculateTotalMoveActionPoints(moveSteps);
    }

    /// <summary>
    /// 根据目标点列表构建完整路径（不含起点重复）
    /// </summary>
    private List<GridPosition> BuildMovePathFromTargets(List<GridPosition> targetPositions)
    {
        List<GridPosition> path = new List<GridPosition>();
        if (targetPositions == null || targetPositions.Count < 2) return path;

        for (int i = 1; i < targetPositions.Count; i++)
        {
            GridPosition start = targetPositions[i - 1];
            GridPosition end = targetPositions[i];
            var segment = Pathfinding.Instance.FindPath(start, end, out int _);
            if (segment.Count > 1)
            {
                path.AddRange(segment.Skip(1)); // 跳过起点
            }
        }
        return path;
    }

    /// <summary>
    /// 计算总行动点消耗
    /// </summary>
    private int CalculateTotalMoveActionPoints(int moveSteps, int alreadyPlannedSteps = 0)
    {
        int total = 0;
        for (int i = 1; i <= moveSteps; i++)
        {
            total += GetMoveStepActionPointCost(alreadyPlannedSteps + i);
        }
        return total;
    }

    /// <summary>
    /// 获取第N格的行动点消耗
    /// </summary>
    private static int GetMoveStepActionPointCost(int stepIndex)
    {
        if (stepIndex == 0) return 0;
        float cost = 10f;
        for (int i = 1; i < stepIndex; i++)
        {
            cost *= 1.2f;
        }
        return Mathf.CeilToInt(cost);
    }

    /// <summary>
    /// 根据体力值获取最大可移动步数
    /// </summary>
    private int GetMaxMoveStepsByActionPoints()
    {
        
        int maxSteps = 0;
        int alreadyPlannedSteps = 0;

        if (unit.GetDeselectAction())
        {
            List<BaseActionParameter> actions = unit.GetActionQueue().GetActions();
            if (actions.Count > 0)
            {
                BaseActionParameter lastParam = unit.GetActionQueue().CopyLastActionParameter();
                
                List<GridPosition> path = BuildMovePathFromTargets(lastParam.targetPositionList);
                alreadyPlannedSteps = path.Count;
            }
        }

        while (unit.CanspendActionPointsToTakeAction(CalculateTotalMoveActionPoints(maxSteps, alreadyPlannedSteps)))
        {
            maxSteps++;
        }
        return maxSteps > 0 ? maxSteps - 1 : 0;
    }
}
