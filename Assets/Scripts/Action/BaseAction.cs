using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public abstract class BaseAction : MonoBehaviour
{
    public static event EventHandler OnAnyActionStarted;
    public static event EventHandler OnAnyActionCompleted;

    //protected 子类可以访问
    protected Unit unit;
    protected bool isActive;
    protected Action onActionComplete;
    protected string actionName;
 
    //virtual 可重写
    protected virtual void Awake()
    {
        unit = GetComponent<Unit>();
    }

    /// <returns>动作名称</returns>
    public abstract string GetActionName();

    public abstract void TakeAction(GridPosition gridPosition, Action onActionComplete);

    public virtual bool IsValidActionGridPosition(GridPosition gridPosition)
    {
        List<GridPosition> validActionGridPositionList = GetValidActionGridPositionList();
        return validActionGridPositionList.Contains(gridPosition); //检查数组中包含的值
    }

    /// <summary>
    /// 获取可以进行行动的的网格坐标集合
    /// </summary>
    /// <returns>可选的GridPosition集合</returns>
    public abstract List<GridPosition> GetValidActionGridPositionList();

    public virtual int GetActionPointsCost(BaseActionParameter baseActionParameter = null)
    {
        return 10;
    }

    protected void ActionStart(Action onActionComplete)
    {
        //关闭预测
        unit.SetForecast(false);
        if (!unit.CanspendActionPointsToTakeAction(GetActionPointsCost()))
        {
            return;
        }

        isActive = true;
        this.onActionComplete = onActionComplete;
        OnAnyActionStarted?.Invoke(this, EventArgs.Empty);
    }

    protected void ActionComplete()
    {
        isActive = false;
        onActionComplete?.Invoke();
        OnAnyActionCompleted?.Invoke(this, EventArgs.Empty);
    }

    public Unit GetUnit()
    {
        return unit;
    }

    public EnemyAIAction GetBestEnemyAIAction()
    {
        List<EnemyAIAction> enemyAIActionList = new List<EnemyAIAction>();
        List<GridPosition> validActionGridPositionList = GetValidActionGridPositionList();

        foreach (GridPosition gridPosition in validActionGridPositionList)
        {
            EnemyAIAction enemyAIAction = GetEnemyAIAction(gridPosition);
            enemyAIActionList.Add(enemyAIAction);
        }

        if (enemyAIActionList.Count > 0)
        {
            enemyAIActionList.Sort((EnemyAIAction a, EnemyAIAction b) => b.actionValue - a.actionValue); //根据actionValue排序最优行动
            List<EnemyAIAction> eqEnemyAIActionList = enemyAIActionList.FindAll((EnemyAIAction enemyAction) =>
            {
                return enemyAction.actionValue == enemyAIActionList[0].actionValue;
            });

            System.Random rng = new System.Random();
            int randomIndex = rng.Next(eqEnemyAIActionList.Count());
            return eqEnemyAIActionList.ElementAt(randomIndex);
        }
        else
        {
            //没有可执行的行动
            return null;
        }
    }

    public abstract EnemyAIAction GetEnemyAIAction(GridPosition gridPosition);

}
