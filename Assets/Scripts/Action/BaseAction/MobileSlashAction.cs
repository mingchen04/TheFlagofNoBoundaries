using System;
using System.Collections.Generic;
using UnityEngine;

public class MobileSlashAction : BaseMoveAction
{
    public override event EventHandler OnStartMoving;
    public override event EventHandler OnStopMoving;
    [SerializeField] private int maxSteps = 5;

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

        UnitMove();
    }

    public override string GetActionName()
    {
        return "移动斩";
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

}
