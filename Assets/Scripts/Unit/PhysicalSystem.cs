using System;
using System.Collections.Generic;
using UnityEngine;

public class PhysicalSystem : MonoBehaviour
{
    //体力系统
    public static event EventHandler OnAnyActionPointsChanged;

    private Unit unit;
    private const int ACTION_POINT_MAX = 9;
    private int actionPoints = ACTION_POINT_MAX;
    //预测点数
    private int forecastActionPoints = ACTION_POINT_MAX;

    //是否预测
    private bool isForecast;

    void Start()
    {
        unit = GetComponent<Unit>();
        TurnSystem.Instance.OnTurnChanged += TurnSystem_OnTurnChanged;
    }

    private void TurnSystem_OnTurnChanged(object sender, System.EventArgs e)
    {
        if ((unit.IsEnemy() && !TurnSystem.Instance.IsPlayerTrun()) || (!unit.IsEnemy() && TurnSystem.Instance.IsPlayerTrun()))
        {
            actionPoints = ACTION_POINT_MAX;
            forecastActionPoints = actionPoints;
            OnAnyActionPointsChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>
    /// 尝试花费行动点数来采取行动
    /// </summary>
    public bool TrySpendActionPointsToTakeAction(BaseAction baseAction)
    {
        if (CanspendActionPointsToTakeAction(baseAction))
        {
            SpendActionPoints(baseAction.GetActionPointsCost());
            return true;
        }
        else
        {
            return false;
        }
    }

    /// <summary>
    /// 可以花费行动点数来采取行动
    /// </summary>
    public bool CanspendActionPointsToTakeAction(BaseAction baseAction)
    {
        if ((isForecast ? forecastActionPoints : actionPoints) >= baseAction.GetActionPointsCost())
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    public void SpendActionPoints(int amount)
    {
        if (isForecast)
        {
            forecastActionPoints -= amount;
        }
        else
        {
            actionPoints -= amount;
        }

        OnAnyActionPointsChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// 设置队列时使用预测点数 执行行动使用实际点数
    /// </summary>
    public void SetForecast(bool isForecast)
    {
        this.isForecast = isForecast;
    }

    public int GetActionPoints()
    {
        return actionPoints;
    }

    public int GetForecastActions()
    {
        return forecastActionPoints;
    }

    //取消动作返回行动点数
    public void CancelActionReturnPosint(List<BaseAction> baseActions)
    {
        foreach (BaseAction baseAction in baseActions)
        {
            forecastActionPoints += baseAction.GetActionPointsCost();
        }

        OnAnyActionPointsChanged?.Invoke(this, EventArgs.Empty);
    }
}
