using System;
using System.Collections.Generic;

public class AttackMagicAction : BaseAction
{
    public override string GetActionName()
    {
        return "攻击魔法";
    }

    public override EnemyAIAction GetEnemyAIAction(GridPosition gridPosition)
    {
        return new EnemyAIAction
        {
            gridPosition = gridPosition,
            actionValue = 10 // 假设攻击魔法的值为10
        };
    }

    public override List<GridPosition> GetValidActionGridPositionList()
    {
        throw new NotImplementedException();
    }

    public override void TakeAction(List<GridPosition> gridPositions, Action onActionComplete)
    {
        throw new NotImplementedException();
    }
}
