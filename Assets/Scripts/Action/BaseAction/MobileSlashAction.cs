using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 移动斩动作
/// </summary>

public class MobileSlashAction : BaseMoveAction
{
    public override event EventHandler OnStartMoving;
    public override event EventHandler OnStopMoving;
    [SerializeField] private int maxSteps = 3;

    private bool SpendActionPoints = false;
    private int oldCurrentPathIndex = 0;

    protected override void Awake()
    {
        base.Awake();
        //设置寻路规则
        defaultPathfindingRules = new Pathfinding.PathfindingRules[]
        {
            Pathfinding.PathfindingRules.Walkable, //障碍物判定
            Pathfinding.PathfindingRules.Straight //只能走直线
        };
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

        if (currentPathIndex + 1 == movePathWorldPositions.Count)
        {
            Debug.Log("检查目标位置");
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
        }

        if (oldCurrentPathIndex != currentPathIndex)
        {
            oldCurrentPathIndex = currentPathIndex;
            //对经过的格子进行伤害判定
            Unit tarGetUnit = LevelGrid.Instance.GetUnitAtGridPosition(unit.GetGridPosition());
            if (tarGetUnit != unit)
            {
                tarGetUnit?.Damage(unit.GetAttack());
            }
        }

        if (!SpendActionPoints)
        {
            UnitMove(moveSpeed: 10f, srengthConsumptionCost: GetActionPointsCost());
            SpendActionPoints = true;
        }
        else
        {
            UnitMove(moveSpeed: 10f);
        }

    }

    public override EnemyAIAction GetEnemyAIAction(GridPosition gridPosition)
    {
        return new EnemyAIAction
        {
            gridPosition = gridPosition,
            actionValue = 0
        };
    }

    public override List<GridPosition> GetValidActionGridPositionList()
    {
        List<GridPosition> validTargets = new();
        GridPosition unitGridPosition = unit.GetGridPosition();

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
                if (!Pathfinding.Instance.HasPathStraight(unit.GetGridPosition(), testPos)) continue;
                if (unit.GetActionQueue().IsLastActionParameterTargetPosition(testPos)) continue;
                if (!unit.GetGridPosition().IsStraightLine(testPos)) continue; //判断是否在一条直线上

                validTargets.Add(testPos);
            }
        }
        return validTargets;
    }

    protected override void ActionComplete()
    {
        oldCurrentPathIndex = 0;
        SpendActionPoints = false;
        base.ActionComplete();
    }

}
