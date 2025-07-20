using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MoveAction : BaseAction
{
    public event EventHandler OnStartMoving;
    public event EventHandler OnStopMoving;

    private bool IsWalking = false;
    private List<Vector3> movePathWorldPositions;
    private int currentPathIndex;
    private GridPosition targetPosition;

    protected override void Awake()
    {
        base.Awake();
    }

    public void Update()
    {
        if (!isActive) return;

        targetPosition = movePathWorldPositions[currentPathIndex];
        if (!TargetPositionStance()) return;

        if (!IsWalking)
        {
            OnStartMoving?.Invoke(this, EventArgs.Empty);
        }

        Vector3 moveDirection = ((Vector3)targetPosition - transform.position).normalized;
        float stoppingDistance = 0.1f;
        if (Vector3.Distance(transform.position, targetPosition) > stoppingDistance)
        {
            float moveSpeed = 4f;
            transform.position += moveSpeed * Time.deltaTime * moveDirection;
        }
        else
        {
            currentPathIndex++;
            unit.SetUnitGridPosition(targetPosition);
            unit.TrySpendActionPointsToTakeAction(GetMoveStepActionPointCost(currentPathIndex));
            if (currentPathIndex >= movePathWorldPositions.Count)
            {
                ActionComplete();
            }
        }

        float rotateSpeed = 10f;
        transform.forward = Vector3.Slerp(transform.forward, moveDirection, Time.deltaTime * rotateSpeed);
    }

    private bool TargetPositionStance()
    {
        Unit targetUnit = LevelGrid.Instance.GetUnitAtGridPosition(targetPosition);

        if (targetUnit != null && targetUnit != unit)
        {
            if (targetUnit.GetAction<MoveAction>().isActive)
            {
                // 单位正在移动，等待其离开格子
                IsWalking = false; //停止行走动画
                OnStopMoving?.Invoke(this, EventArgs.Empty);

                bool isTargetUnitA = targetUnit.GetAction<MoveAction>().GetTargetPosition() == unit.GetGridPosition();
                bool isTargetUnitB = targetPosition == targetUnit.GetGridPosition();
                if (isTargetUnitA && isTargetUnitB)
                {
                    //路径冲突，结束双方行动
                    unit.GetActionQueue().ActionQueueStop();
                    targetUnit.GetActionQueue().ActionQueueStop();
                }
            }
            else
            {
                // 如果目标单位不再移动，路径被占用停止行动
                unit.GetActionQueue().ActionQueueStop();
            }
            return false;
        }

        IsWalking = true; //停止行走动画
        OnStartMoving?.Invoke(this, EventArgs.Empty);
        // 目标格可达，可以继续移动
        return true;
    }

    public override void ActionStop()
    {
        OnStopMoving?.Invoke(this, EventArgs.Empty);
        base.ActionStop();
    }

    public override void TakeAction(BaseActionParameter baseActionParameter, Action onActionComplete)
    {

        unit.SetForecast(false);
        currentPathIndex = 0;
        movePathWorldPositions = (baseActionParameter as MoveActionParameter).GetMovePathForWorldPosition();

        ActionStart(onActionComplete);
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

        List<GridPosition> path = BuildMovePathFromTargets(baseActionParameter.targetPositionList);
        movePathWorldPositions = path.Select(gridPosition => (Vector3)gridPosition).ToList();
        int moveSteps = path.Count;
        return CalculateTotalMoveActionPoints(moveSteps);
    }

    /// <summary>
    /// 根据目标点列表构建完整路径（不含起点重复）
    /// </summary>
    private List<GridPosition> BuildMovePathFromTargets(List<GridPosition> targetPositions)
    {
        List<GridPosition> path = new();
        if (targetPositions == null || targetPositions.Count < 2) return path;
        for (int i = 1; i < targetPositions.Count; i++)
        {
            GridPosition start = targetPositions[i - 1];
            GridPosition end = targetPositions[i];
            List<GridPosition> segment = Pathfinding.Instance.FindPath(start, end, out int _);
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
                MoveActionParameter lastParam = unit.GetActionQueue().CopyLastMoveActionParameter();

                List<GridPosition> path = lastParam.GetMovePathForGridPosition();
                string debugPath = string.Join(", ", path.Select(p => p.ToString()));
                alreadyPlannedSteps = path.Count;
            }
        }

        while (unit.CanspendActionPointsToTakeAction(CalculateTotalMoveActionPoints(maxSteps, alreadyPlannedSteps)))
        {
            maxSteps++;
        }
        return maxSteps > 0 ? maxSteps - 1 : 0;
    }

    public void UpdatePath()
    {
        MoveActionParameter lastParam = unit.GetActionQueue().CopyLastMoveActionParameter();
        lastParam.SetMovePath(movePathWorldPositions.Select(worldPosition => (GridPosition)worldPosition).ToList());
        unit.GetActionQueue().UpdateBaseActionParameter(lastParam);
    }

    protected override void ActionComplete()
    {
        if (!isActive) return;
        
        base.ActionComplete();
        unit.ShowPathLine();
        OnStopMoving?.Invoke(this, EventArgs.Empty);
    }

    public GridPosition GetTargetPosition()
    {
        return targetPosition;
    }
}
