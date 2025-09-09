using System;
using System.Collections.Generic;
using UnityEngine;

public class SrengthSystem : MonoBehaviour
{
    //体力系统
    public static event EventHandler OnAnyActionPointsChanged;
    public event EventHandler OnActionPointsChanged;

    private Unit unit;
    [SerializeField] private int actionPoints = 100; //初始行动点数
    private int actionPointMax;

    //预测点数
    private int forecastActionPoints;

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
        if (TurnSystem.Instance.GetTurnState() != TurnSystem.TurnState.Main)
        {
            return;
        }

        if (unit.IsEnemy() != !TurnSystem.Instance.IsPlayerTurn())
        {
            return;
        }

        

        actionPoints = actionPointMax;
        forecastActionPoints = actionPoints;
        OnAnyActionPointsChanged?.Invoke(this, EventArgs.Empty);
        OnActionPointsChanged?.Invoke(this, EventArgs.Empty);
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
        //如果是预测则使用预测点数，否则使用实际点数
        int isActionPoints = unit.IsForecast() ? forecastActionPoints : actionPoints;
        return isActionPoints >= baseActionPointsCost;
    }

    public void SpendActionPoints(int amount)
    {
        if (unit.IsForecast())
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

    public float GetStrengthPointsNormalized()
    {
        return (float)actionPoints / actionPointMax;
    }

    public void SetForecastActionPoints()
    {
        forecastActionPoints = actionPoints;
    }
}
