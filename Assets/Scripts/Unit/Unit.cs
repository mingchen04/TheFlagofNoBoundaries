using System;
using System.Collections.Generic;
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
    private PhysicalSystem physicalSystem; //体力

    private BaseAction[] baseActionArray; //存储所有行动
    private ActionQueue actionQueue; //存储行动队列
    private bool isForecast;

    private void Awake()
    {
        isForecast = true;
        actionQueue = new ActionQueue();
        physicalSystem = GetComponent<PhysicalSystem>();
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
        return physicalSystem.GetActionPoints();
    }

    //获取即将减少的点数
    public int GetReductionPoints()
    {
        return physicalSystem.GetActionPoints() - physicalSystem.GetForecastActions();
    }

    public bool CanspendActionPointsToTakeAction(BaseAction baseAction)
    {
        return physicalSystem.CanspendActionPointsToTakeAction(baseAction);
    }

    public bool TrySpendActionPointsToTakeAction(BaseAction baseAction)
    {
        return physicalSystem.TrySpendActionPointsToTakeAction(baseAction);
    }

    public void SetForecast(bool forecast)
    {
        this.isForecast = forecast;
        physicalSystem.SetForecast(forecast);
    }


    public int GetPriority() => unitPriority;

    /// <summary>
    /// 取消动作
    /// </summary>
    /// <param name="baseAction"></param>
    public void CancelAction(int actionsIndex, BaseAction baseAction)
    {
        List<BaseAction> cancelActions = actionQueue.GetCancelActions(actionsIndex, baseAction);
        physicalSystem.CancelActionReturnPosint(cancelActions);
    }

    /// <summary>
    /// 添加带顺序的行动
    /// </summary>
    public void AddOrderedAction(BaseAction action, GridPosition targetPos, int executionOrder)
    {
        SetForecast(true);
        // 获取行动标签（需确保BaseAction有获取标签的方法）
        BaseActionSO.ActionTags actionTags = ScriptiptableSOManager.Instance.GetActionTags(action);

        // 确定最终位置
        GridPosition finalPos = new GridPosition();
        bool isMove = actionTags.HasFlag(BaseActionSO.ActionTags.Move);
        if (isMove)
        {
            // 移动类行动使用目标位置
            finalPos = targetPos;           
        }
        else
        {
            // 非移动行动使用当前位置
            finalPos = GetGridPosition();
        }

        BaseActionParameter actionParam = new BaseActionParameter(
                 action,
                 targetPos,
                 finalPos, // 修正后的位置逻辑
                 executionOrder
             );

        actionQueue.AddAction(actionParam);

        Debug.Log($"{gameObject.name} 添加行动: {action.GetActionName()} " +
                 $"(顺序:{executionOrder}, 目标:{targetPos}, 终位:{finalPos})");
        if(isMove) OnAnyMoveActionUnitPositionChange?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// 获取当前最高优先级的行动顺序
    /// </summary>
    public int GetCurrentActionOrder()
    {
        return actionQueue.GetCurrentExecutionOrder();
    }
}
