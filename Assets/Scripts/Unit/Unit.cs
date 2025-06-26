using System;
using System.Collections.Generic;
using UnityEngine;

public class Unit : MonoBehaviour
{
    public static event EventHandler OnAnyUnitSpawned;
    public static event EventHandler OnAnyUnitDead;
    //单位移动
    public static event EventHandler OnAnyUnitPositionChanged;

    [SerializeField] private bool isEnemy; //是否为敌人
    private int unitPriority; // 单位执行优先级

    private GridPosition gridPosition; //网格位置
    private HealthSystem healthSystem; //生命
    private SrengthSystem srengthSystem; //体力

    private BaseAction[] baseActionArray; // 存储所有行动
    private ActionQueue actionQueue; // 存储行动队列
    private bool isForecast; // 是否为预测模式
    private bool isBusy; // 是否忙碌
    private bool isDeployAction; //是否部署行动

    private void Awake()
    {
        isForecast = true;
        actionQueue = GetComponent<ActionQueue>();
        srengthSystem = GetComponent<SrengthSystem>();
        baseActionArray = GetComponents<BaseAction>();
        healthSystem = GetComponent<HealthSystem>();
    }

    private void Start()
    {
        unitPriority = UnitManager.Instance.GetFriendlyUnitList().Count;
        gridPosition = LevelGrid.Instance.GetGridPosition(transform.position);
        LevelGrid.Instance.AddUnitAtGridPosition(gridPosition, this);

        UnitActionSystem.Instance.OnBusyChanged += UnitActionSystem_OnBusyChanged;
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
        if (!isBusy) return; //如果不忙碌则不更新位置

        GridPosition newGridPosition = LevelGrid.Instance.GetGridPosition(transform.position);

        //距离大于0.1暂时不更新位置
        float stoppingDistance = 0.1f;
        if (Vector3.Distance(newGridPosition, transform.position) > stoppingDistance) return;
        SetUnitGridPosition(newGridPosition);
    }

    private void OnDestroy()
    {
        if (healthSystem != null)
            healthSystem.OnDie -= HealthSystem_OnDie;
        if (UnitActionSystem.Instance != null)
            UnitActionSystem.Instance.OnBusyChanged -= UnitActionSystem_OnBusyChanged;
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
        return srengthSystem.GetActionPoints();
    }

    /// <summary>
    /// 获取即将减少的点数
    /// </summary>
    /// <returns>实际点数和预计点数的差</returns>
    public int GetReductionPoints()
    {
        return srengthSystem.GetActionPoints() - srengthSystem.GetForecastActions();
    }

    /// <summary>
    /// 判断点数是否满足行动要求
    /// </summary>
    public bool CanspendActionPointsToTakeAction(int baseActionPointsCost)
    {
        return srengthSystem.CanspendActionPointsToTakeAction(baseActionPointsCost);
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
        return srengthSystem.TrySpendActionPointsToTakeAction(baseAction, actionParam);
    }

    public void TrySpendActionPointsToTakeAction(int actionPoint)
    {
        srengthSystem.SpendActionPoints(actionPoint);
    }

    public void SetForecast(bool forecast)
    {
        isForecast = forecast;
    }

    public int GetPriority() => unitPriority;

    /// <summary>
    /// 取消动作
    /// </summary>
    /// <param name="baseAction"></param>
    public void CancelAction(int actionsIndex, BaseAction baseAction)
    {
        UnitActionSystem.Instance.DeployActionEnd();
        List<BaseActionParameter> cancelActions = actionQueue.GetCancelActions(actionsIndex, baseAction);
        srengthSystem.CancelActionReturnPosint(cancelActions);
        OnAnyUnitPositionChanged?.Invoke(this, EventArgs.Empty);
    }

    public void CancelAction(BaseAction cancelActions)
    {
        UnitActionSystem.Instance.DeployActionEnd();
        srengthSystem.CancelActionReturnPosint(cancelActions);
        OnAnyUnitPositionChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// 添加带顺序的行动
    /// </summary>
    public void HandleOrderedAction(BaseAction action, GridPosition targetPos, int executionOrder)
    {
        SetForecast(true);

        ActionTags actionTags = ScriptiptableSOManager.Instance.GetActionTags(action);
        bool isMove = actionTags.HasFlag(ActionTags.Move);
        bool isDeploy = actionTags.HasFlag(ActionTags.Deploy); //是否为部署行动

        BaseActionParameter actionParam;
        GridPosition finalPos;
        List<GridPosition> targetPositionList;

        if (isMove)
        {
            finalPos = targetPos;
            targetPositionList = new List<GridPosition> { GetGridPosition(), targetPos };
        }
        else
        {
            finalPos = GetGridPosition(); //非移动行动最终位置为当前网格位置
            targetPositionList = new List<GridPosition> { targetPos };
        }

        if (isDeployAction)
        {
            // 复制上一次参数
            BaseActionParameter lastParam = GetActionQueue().CopyLastActionParameter();
            actionParam = lastParam;
            if (actionParam.targetPositionList.Count == 0 && isMove)
            {
                actionParam.targetPositionList = targetPositionList;
            }

            actionParam.targetPositionList.Add(targetPos);
            actionParam.unitFinalPosition = finalPos;

            actionQueue.UpdateBaseActionParameter(actionParam);
        }
        else
        {
            actionParam = new BaseActionParameter(
                action,
                targetPositionList,
                finalPos,
                executionOrder
            );

            if (!TrySpendActionPointsToTakeAction(action, actionParam)) return;
            actionQueue.AddAction(actionParam);
            if (isDeploy) SetDeselectAction(true);
        }
        Debug.Log($"{gameObject.name} {(isDeployAction ? "更新" : "添加")}行动: {action.GetActionName()} (顺序:{executionOrder}, 目标:{targetPos}, 终位:{finalPos})");
        if (isMove) OnAnyUnitPositionChanged?.Invoke(this, EventArgs.Empty);
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

    private void UnitActionSystem_OnBusyChanged(object sender, bool isBusy)
    {
        this.isBusy = isBusy;
    }

    /// <summary>
    /// 获取部署状态
    /// </summary>
    public bool GetDeselectAction()
    {
        return isDeployAction;
    }

    /// <summary>
    /// 行动部署设置
    /// </summary>
    public void SetDeselectAction(bool isDeployAction)
    {
        this.isDeployAction = isDeployAction;
    }

    /// <summary>
    /// 重新设置预测点数，常用于行动出现意外而停止，直接等于实际行动点数
    /// </summary>
    public void SetForecastActionPoints()
    {
        srengthSystem.SetForecastActionPoints();
        GridPosition newGridPosition = LevelGrid.Instance.GetGridPosition(transform.position);
        SetUnitGridPosition(newGridPosition);
    }

    private void SetUnitGridPosition(GridPosition newGridPosition)
    {
        if (newGridPosition == gridPosition) return;

        //Unit Changed Grid Position
        GridPosition oldGridPosition = gridPosition;
        gridPosition = newGridPosition;
        LevelGrid.Instance.UnitMovedGridPosition(this, oldGridPosition, newGridPosition);
    }
}
