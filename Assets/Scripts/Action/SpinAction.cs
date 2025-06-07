using System;
using System.Collections.Generic;
using UnityEngine;

public class SpinAction : BaseAction
{
    //public delegate void SpinCompleteDelegate();
    //private SpinCompleteDelegate spinCompleteDelegate;

    //using System
    //private Action<int> spinComplete; 无返回值
    //private Func<int,bool> spinCompleteFunc; 有返回值 接受int参数返回bool

    private float totalSpinAmount;

    private void Update()
    {
        if (!isActive)
        {
            return;
        }

        float spinAddAmount = 360f * Time.deltaTime;
        transform.eulerAngles += new Vector3(0, spinAddAmount, 0);
        totalSpinAmount -= spinAddAmount;

        //旋转360°
        if (totalSpinAmount <= 0)
        {
            transform.eulerAngles += new Vector3(0, totalSpinAmount, 0);
            ActionComplete();
        }

    }

    public override void TakeAction(GridPosition gridPosition, Action onSpinComplete)
    {
        ActionStart(onSpinComplete);
        ActionPointConsumption();
        totalSpinAmount = 360f;
    }

    public override string GetActionName()
    {
        return "旋转";
    }

    public override List<GridPosition> GetValidActionGridPositionList()
    {
        GridPosition unitGridPosition = unit.GetGridPosition();

        return new List<GridPosition>
        {
            unitGridPosition
        };

    }

    public override int GetActionPointsCost()
    {
        return 1;
    }

    public override EnemyAIAction GetEnemyAIAction(GridPosition gridPosition)
    {
        return new EnemyAIAction
        {
            gridPosition = gridPosition,
            actionValue = 0,
        };
    }
}
