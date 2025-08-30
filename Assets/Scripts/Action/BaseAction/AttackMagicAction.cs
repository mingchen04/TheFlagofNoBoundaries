using System;
using System.Collections.Generic;
using UnityEngine;

public class AttackMagicAction : BaseAction
{
    private int maxMagicDistance = 3; // 设攻击魔法的最大距离为3
    private const float MagicTimeMax = 0.2f;//魔法发动时间
    private float MagicTime = 0;

    private void Update()
    {
        if (!isActive) return;

        if (MagicTime > 0)
        {
            MagicTime -= Time.deltaTime;
        }
        else
        {
            ActionComplete();
        }
    }

    public override string GetActionName()
    {
        return "攻击魔法";
    }

    public override EnemyAIAction GetEnemyAIAction(GridPosition gridPosition)
    {
        return new EnemyAIAction
        {
            gridPosition = gridPosition,
            actionValue = 0 // 假设攻击魔法的值为10
        };
    }

    public override List<GridPosition> GetValidActionGridPositionList()
    {
        List<GridPosition> validActionGridPositionList = new();
        GridPosition unitGridPosition = unit.GetGridPosition();

        foreach (GridPosition testGridPosition in LevelGrid.Instance.GenerateHexArea(unitGridPosition, maxMagicDistance))
        {

            if (!LevelGrid.Instance.IsValidGridPosition(testGridPosition))
            {
                //网格位置不合法跳过
                continue;
            }

            validActionGridPositionList.Add(testGridPosition);
        }

        return validActionGridPositionList;
    }

    public override void TakeAction(BaseActionParameter baseActionParameter, Action onActionComplete)
    {
        List<GridPosition> gridPositions = baseActionParameter.targetPositionList;
        Unit targetUnit = LevelGrid.Instance.GetUnitAtGridPosition(gridPositions[0]);
        ActionStart(onActionComplete);
        if (targetUnit == null)
        {
            ActionComplete();
            return;
        }

        unit.TrySpendActionPointsToTakeAction(GetActionPointsCost());
        targetUnit.AddBuff(new AtkUpBuff(targetUnit, 20, 3));
        MagicTime = MagicTimeMax;
    }
}
