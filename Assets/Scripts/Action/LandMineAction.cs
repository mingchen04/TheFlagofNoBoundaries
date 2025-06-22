using System;
using System.Collections.Generic;

public class LandMineAction : BaseAction
{
    public override string GetActionName()
    {
        return "地雷";
    }

    public override EnemyAIAction GetEnemyAIAction(GridPosition gridPosition)
    {
        return new EnemyAIAction
        {
            gridPosition = gridPosition,
            actionValue = 10 // 假设地雷的行动值为10
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

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
