using System;
using System.Collections.Generic;
using UnityEngine;

public class Unit : MonoBehaviour
{
    public static event EventHandler OnAnyUnitSpawned;
    public static event EventHandler OnAnyUnitDead;
    public event EventHandler OnActionAddQueueSuccess; //行为添加到队列时触发动画
    public event EventHandler OnActionAddQueueFailed; //行为添加到队列失败时触发视图更新

    //单位移动
    public static event EventHandler OnAnyUnitPositionChanged;
    public event EventHandler OnBuffListChange; //buff变化

    [SerializeField] private bool isEnemy; //是否为敌人
    private int unitPriority; // 单位执行优先级

    [Header("基础属性")]
    [SerializeField] private int baseAttack = 30; //攻击力
    [SerializeField] private int baseDefense = 5; //防御力
    [SerializeField] private int fastActionPointsCost = 1; //快速行动点数消耗
    [SerializeField] private int baseVisionRange = 5; //视野范围

    private GridPosition gridPosition; //网格位置
    private HealthSystem healthSystem; //生命
    private SrengthSystem srengthSystem; //体力

    private BaseAction[] baseActionArray; // 存储所有行动
    private ActionQueue actionQueue; // 存储行动队列
    private List<BaseBuff> buffList; //buff栏
    private bool isForecast; // 是否为预测模式
    private bool isBusy; // 是否忙碌
    private bool isDeployAction; //是否部署行动
    private UnitVision unitVision; // 视野组件


    private void Awake()
    {
        isForecast = true;
        unitVision = GetComponent<UnitVision>();
        buffList = new List<BaseBuff>();
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

    /// <summary>
    /// 添加带顺序的行动
    /// </summary>
    public bool HandleOrderedAction(BaseAction action, GridPosition targetPos, int executionOrder)
    {
        if (actionQueue.HandleOrderedAction(action, targetPos, executionOrder))
        {
            OnActionAddQueueSuccess?.Invoke(this, EventArgs.Empty);
            return true;
        }
        else
        {
            OnActionAddQueueFailed?.Invoke(this, EventArgs.Empty);
            SetDeselectAction(false);
            return false;
        }
    }

    public void OnAnyUnitPositionChangedInvoke()
    {
        OnAnyUnitPositionChanged?.Invoke(this, EventArgs.Empty);
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
        if (selBaseAction == null) return GetAction<MoveAction>();

        foreach (BaseAction baseAction in baseActionArray)
        {
            if (baseAction.GetType() == selBaseAction.GetType())
            {
                return baseAction;
            }
        }
        return GetAction<MoveAction>();
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
    /// 重新设置unit参数，常用于行动出现意外而停止
    /// </summary>
    public void ReSetUnitParam()
    {
        srengthSystem.SetForecastActionPoints(); // 重置预测行动点数
        GridPosition newGridPosition = transform.position; // 转化网格位置归整
        transform.position = newGridPosition;
        SetUnitGridPosition(newGridPosition);
    }

    public void SetUnitGridPosition(GridPosition newGridPosition)
    {
        if (newGridPosition == gridPosition) return;

        //Unit Changed Grid Position
        GridPosition oldGridPosition = gridPosition;
        gridPosition = newGridPosition;
        LevelGrid.Instance.UnitMovedGridPosition(this, oldGridPosition, newGridPosition);
    }

    public void AddBuff(BaseBuff baseBuff)
    {
        buffList.Add(baseBuff);
        OnBuffListChange?.Invoke(this, EventArgs.Empty);
    }

    // 计算最终属性（可被Buff修饰）
    public int GetAttack()
    {
        int value = baseAttack;
        foreach (BaseBuff buff in buffList)
        {
            value = buff.ModifyAttack(value);
        }
        return value;
    }

    public int GetDefense()
    {
        int value = baseDefense;
        foreach (BaseBuff buff in buffList)
        {
            value = buff.ModifyDefense(value);
        }
        return value;
    }

    public int GetVisionRange()
    {
        int value = baseVisionRange;
        foreach (BaseBuff buff in buffList)
        {
            value = buff.ModifyVisionRange(value);
        }
        return value;
    }

    public int GetFastActionPointsCost()
    {
        int value = fastActionPointsCost;
        foreach (BaseBuff buff in buffList)
        {
            value = buff.ModifyFastActionPointsCost(value);
        }
        return value;
    }

    public List<BaseBuff> GetBuffList()
    {
        return buffList;
    }

    public void SetVisible(bool visible)
    {
        unitVision.SetVisible(visible);
    }
}
