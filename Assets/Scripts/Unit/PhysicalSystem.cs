using System;
using System.Collections.Generic;
using UnityEngine;

public class PhysicalSystem : MonoBehaviour
{
    //体力系统
    public static event EventHandler OnAnyActionPointsChanged;
    public event EventHandler OnActionPointsChanged;

    private Unit unit;
    [SerializeField] private int actionPoints = 100; //初始行动点数
    private int actionPointMax;

    //预测点数
    private int forecastActionPoints;

    //是否预测
    private bool isForecast;

    private void Awake()
    {
        actionPointMax = actionPoints;
        forecastActionPoints = actionPoints;
    }

    void Start()
    {
        unit = GetComponent<Unit>();
        TurnSystem.Instance.OnTurnChanged += TurnSystem_OnTurnChanged;
    }

    private void TurnSystem_OnTurnChanged(object sender, System.EventArgs e)
    {
        if ((unit.IsEnemy() && !TurnSystem.Instance.IsPlayerTrun()) || (!unit.IsEnemy() && TurnSystem.Instance.IsPlayerTrun()))
        {
            actionPoints = actionPointMax;
            forecastActionPoints = actionPoints;
            OnAnyActionPointsChanged?.Invoke(this, EventArgs.Empty);
            OnActionPointsChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>
    /// 尝试花费行动点数来采取行动
    /// </summary>
    public bool TrySpendActionPointsToTakeAction(BaseAction baseAction, BaseActionParameter actionParam)
    {
        int baseActionPointsCost = baseAction.GetActionPointsCost(actionParam);//基础行动点数消耗
        if (CanspendActionPointsToTakeAction(baseActionPointsCost))
        {
            SpendActionPoints(baseActionPointsCost);
            return true;
        }
        else
        {
            return false;
        }
    }

    /// <summary>
    /// 判断行动点数是否满足行动要求
    /// </summary>
    public bool CanspendActionPointsToTakeAction(int baseActionPointsCost)
    {
        return (isForecast ? forecastActionPoints : actionPoints) >= baseActionPointsCost;
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
        OnActionPointsChanged?.Invoke(this, EventArgs.Empty);
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

    /// <summary>
    /// 取消动作返回行动点数
    /// </summary>
    /// <param name="baseActionPars"></param>
    public void CancelActionReturnPosint(List<BaseActionParameter> baseActionPars)
    {
        foreach (BaseActionParameter baseActionPar in baseActionPars)
        {
            forecastActionPoints += baseActionPar.baseAction.GetActionPointsCost(baseActionPar);
        }

        OnAnyActionPointsChanged?.Invoke(this, EventArgs.Empty);
        OnActionPointsChanged?.Invoke(this, EventArgs.Empty);
    }

    public void CancelActionReturnPosint(BaseAction baseAction)
    {
        forecastActionPoints += baseAction.GetActionPointsCost();
        OnAnyActionPointsChanged?.Invoke(this, EventArgs.Empty);
        OnActionPointsChanged?.Invoke(this, EventArgs.Empty);
    }

    public float GetPhysicalPointsNormalized()
    {
        return (float)actionPoints / actionPointMax;
    }
}
