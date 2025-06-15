using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Unit : MonoBehaviour
{
    public static event EventHandler OnAnyUnitSpawned;
    public static event EventHandler OnAnyUnitDead;
    //单位移动
    public static event EventHandler OnAnyMoveActionUnitPositionChange;

    [SerializeField] private bool isEnemy; //是否为敌人
    private int unitPriority; // 单位执行优先级

    private GridPosition gridPosition; //网格位置
    private HealthSystem healthSystem; //生命
    private SrengthSystem strengthSystem; //体力

    private BaseAction[] baseActionArray; //存储所有行动
    private ActionQueue actionQueue; //存储行动队列
    private bool isForecast; // 是否为预测模式

    private void Awake()
    {
        isForecast = true;
        actionQueue = new ActionQueue();
        strengthSystem = GetComponent<SrengthSystem>();
        baseActionArray = GetComponents<BaseAction>();
        healthSystem = GetComponent<HealthSystem>();
    }

    private void Start()
    {
        unitPriority = UnitManager.Instance.GetFriendlyUnitList().Count;

        gridPosition = LevelGrid.Instance.GetGridPosition(transform.position);
        LevelGrid.Instance.AddUnitAtGridPosition(gridPosition, this);

        healthSystem.OnDie += HealthSystem_OnDie;
        OnAnyUnitSpawned?.Invoke(this, EventArgs.Empty);
    }

    private void HealthSystem_OnDie(object sender, EventArgs e)
    {
        LevelGrid.Instance.ClearUnitAtGridPosition(gridPosition, this);
        OnAnyUnitDead?.Invoke(this, EventArgs.Empty);
        Destroy(gameObject);
    }

    private void Update()
    {
        GridPosition newGridPosition = LevelGrid.Instance.GetGridPosition(transform.position);
        if (newGridPosition != gridPosition)
        {
            //Unit Changed Grid Position
            GridPosition oldGridPosition = gridPosition;
            gridPosition = newGridPosition;
            LevelGrid.Instance.UnitMovedGridPosition(this, oldGridPosition, newGridPosition);
        }
    }

    public T GetAction<T>() where T : BaseAction
    {
        foreach (BaseAction baseAction in baseActionArray)
        {
            if (baseAction is T)
            {
                return (T)baseAction;
            }
        }
        return null;
    }
    public ActionQueue GetActionQueue() { return actionQueue; }

    public BaseAction[] GetBaseActionArray()
    {
        return baseActionArray;
    }

    public bool IsEnemy()
    {
        return isEnemy;
    }

    public GridPosition GetGridPosition()
    {
        bool isActionsComplete = actionQueue.IsActionQueueComplete(); //判断队列是否为空
        return !isActionsComplete && isForecast ? actionQueue.GetlatestUnitPosition() : gridPosition;
    }

    public Vector3 GetWorldPosition()
    {
        bool isActionsComplete = actionQueue.IsActionQueueComplete();
        return !isActionsComplete && isForecast ? actionQueue.GetlatestUnitPosition() : transform.position;
    }

    public void Damage(int damageAmount)
    {
        healthSystem.Damage(damageAmount);
    }

    public float GetHealthNormalized()
    {
        return healthSystem.GetHealthNormalized();
    }

    public int GetActionPoints()
    {
        return strengthSystem.GetActionPoints();
    }

    /// <summary>
    /// 获取即将减少的点数
    /// </summary>
    /// <returns>实际点数和预计点数的差</returns>
    public int GetReductionPoints()
    {
        return strengthSystem.GetActionPoints() - strengthSystem.GetForecastActions();
    }

    /// <summary>
    /// 判断点数是否满足行动要求
    /// </summary>
    public bool CanspendActionPointsToTakeAction(int baseActionPointsCost)
    {
        return strengthSystem.CanspendActionPointsToTakeAction(baseActionPointsCost);
    }

    /// <summary>
    /// 尝试为当前单位消耗行动点数以执行指定的行动。
    /// </summary>
    /// <param name="baseAction">要执行的基础行动（BaseAction）。</param>
    /// <param name="actionParam">
    /// （可选）行动参数对象（BaseActionParameter），
    /// 用于在消耗点数时提供额外的行动上下文（如目标位置、路径长度等）。
    /// 如果为 null，则仅根据 baseAction 的默认消耗逻辑进行处理。
    /// </param>
    /// <returns>
    /// 如果单位当前拥有足够的行动点数并成功消耗，则返回 true；
    /// 否则返回 false（如点数不足）。
    /// </returns>
    /// <remarks>
    /// 该方法会调用 StrengthSystem.TrySpendActionPointsToTakeAction 进行实际的点数消耗判断与扣除。
    /// 支持根据不同的行动类型和参数动态计算消耗（如移动距离影响消耗等）。
    /// </remarks>
    public bool TrySpendActionPointsToTakeAction(BaseAction baseAction, BaseActionParameter actionParam = null)
    {
        return strengthSystem.TrySpendActionPointsToTakeAction(baseAction, actionParam);
    }

    public void TrySpendActionPointsToTakeAction(int actionPoint)
    {
        strengthSystem.SpendActionPoints(actionPoint);
    }

    public void SetForecast(bool forecast)
    {
        this.isForecast = forecast;
        strengthSystem.SetForecast(forecast);
    }

    public int GetPriority() => unitPriority;

    /// <summary>
    /// 取消动作
    /// </summary>
    /// <param name="baseAction"></param>
    public void CancelAction(int actionsIndex, BaseAction baseAction)
    {
        List<BaseActionParameter> cancelActions = actionQueue.GetCancelActions(actionsIndex, baseAction);
        strengthSystem.CancelActionReturnPosint(cancelActions);
        OnAnyMoveActionUnitPositionChange?.Invoke(this, EventArgs.Empty);
    }

    public void CancelAction(BaseAction cancelActions)
    {
        strengthSystem.CancelActionReturnPosint(cancelActions);
        OnAnyMoveActionUnitPositionChange?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// 添加带顺序的行动
    /// </summary>
    public void HandleOrderedAction(BaseAction action, GridPosition targetPos, int executionOrder)
    {
        SetForecast(true);

        // 获取行动标签（需确保BaseAction有获取标签的方法）
        BaseActionSO.ActionTags actionTags = ScriptiptableSOManager.Instance.GetActionTags(action);

        bool isMove = actionTags.HasFlag(BaseActionSO.ActionTags.Move);
        // 如果是移动行动，则最终位置为目标格子，否则为当前格子
        GridPosition finalPos = isMove ? targetPos : GetGridPosition();
        BaseActionParameter actionParam;

        bool isDeployAction = UnitActionSystem.Instance.IsDeployAction(); // 检查是否为部署行动
        if (isDeployAction)
        {
            actionParam = GetActionQueue().CopyLastActionParameter();
            actionParam.targetPositionList.Add(targetPos); // 更新目标点
            if (isMove) actionParam.unitFinalPosition = targetPos;// 更新最终位置为目标位置
        }
        else
        {
            actionParam = new(
                    action,
                    new List<GridPosition> { targetPos },
                    finalPos, // 修正后的位置逻辑
                    executionOrder
                );
        }

        if (!TrySpendActionPointsToTakeAction(action, actionParam)) return;

        if (!isDeployAction)
        {
            actionQueue.AddAction(actionParam); // 如果不是部署行动，则添加到行动队列
            if (isMove) UnitActionSystem.Instance.DeployActionStart(); // 开始部署行动
        }
        else
        {
            strengthSystem.CancelActionReturnPosint(new List<BaseActionParameter> { GetActionQueue().CopyLastActionParameter() });
            actionQueue.UpdateBaseActionParameter(actionParam); // 更新当前部署行动的参数
        }


        Debug.Log($"{gameObject.name} ${(isDeployAction ? "更新" : "添加")}行动: {action.GetActionName()} " +
                    $"(顺序:{executionOrder}, 目标:{targetPos}, 终位:{finalPos})");

        if (isMove) OnAnyMoveActionUnitPositionChange?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// 获取当前最高优先级的行动顺序
    /// </summary>
    public int GetCurrentActionOrder()
    {
        return actionQueue.GetCurrentExecutionOrder();
    }

    public BaseAction GetBaseActionOfType(BaseAction selBaseAction)
    {
        foreach (BaseAction baseAction in baseActionArray)
        {
            if (baseAction.GetType() == selBaseAction.GetType())
            {
                return baseAction;
            }
        }
        return null;
    }

    public bool IsForecast()
    {
        return isForecast;
    }
}
