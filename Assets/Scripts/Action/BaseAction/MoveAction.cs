using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MoveAction : BaseMoveAction
{
    public override event EventHandler OnStartMoving;
    public override event EventHandler OnStopMoving;
    [SerializeField] private int moveCost = 10; // 每步移动的基础体力消耗

    protected override void Awake()
    {
        base.Awake();
    }

    public void Update()
    {
        if (!isActive)
        {
            if (IsWalking)
            {
                IsWalking = false; // 停止行走动画
                OnStopMoving?.Invoke(this, EventArgs.Empty);
            }
            return;
        }

        if (targetPosition == GridPosition.nullGridPosition)
        {
            targetPosition = movePathWorldPositions[currentPathIndex];
            UnitActionSystem.Instance.StopInvalidMoveUnits();
        }

        if (TargetPositionStance())
        {
            OnStartMoving?.Invoke(this, EventArgs.Empty);
        }
        else
        {
            ArriveTargetLocationCall?.Invoke(); // 如果目标位置不可达，调用回调避免阻碍队列
            OnStopMoving?.Invoke(this, EventArgs.Empty);
            return;
        }

        //设置消耗模式和消耗体力值
        UnitMove(srengthConsumption: SrengthConsumption.everyStep, srengthConsumptionCost: GetMoveStepActionPointCost(currentPathIndex + 1));
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
    public override EnemyAIAction GetEnemyAIAction(GridPosition gridPosition)
    {
        int targetCount = unit.GetAction<ShootAction>().GetEffectiveUnitGridPositionList(gridPosition).Count;
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
    /// 计算总行动点消耗
    /// </summary>
    private int CalculateTotalMoveActionPoints(int moveSteps, int alreadyPlannedSteps = 0)
    {
        int total = 0;
        for (int i = 1; i <= moveSteps; i++)
        {
            total += GetMoveStepActionPointCost(alreadyPlannedSteps + i);
        }

        if (TurnSystem.Instance.IsCounterTurn())
        {
            total *= 2; // 反击回合消耗翻倍
        }
        return total;
    }

    /// <summary>
    /// 获取第N格的行动点消耗
    /// </summary>
    private int GetMoveStepActionPointCost(int stepIndex)
    {        
        if (stepIndex == 0) return 0;
        float cost = moveCost;        

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

    protected override void ActionComplete()
    {
        if (!isActive) return;

        base.ActionComplete();
        OnStopMoving?.Invoke(this, EventArgs.Empty);
    }

    protected override void ActionStart(Action onActionComplete)
    {
        base.ActionStart(onActionComplete);
        IsWalking = true; // 开始行走动画
    }

    public override void ActionStop()
    {
        base.ActionStop();
    }
}
