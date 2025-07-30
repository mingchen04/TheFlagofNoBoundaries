using System;
using System.Collections.Generic;

public class MobileSlashAction : BaseMoveAction
{
    public override event EventHandler OnStartMoving;
    public override event EventHandler OnStopMoving;

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
        throw new NotImplementedException();
    }

}
