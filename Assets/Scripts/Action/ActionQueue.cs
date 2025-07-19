using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// 存储和操作单位的行为队列
/// </summary>
public class ActionQueue : MonoBehaviour
{
    public static event EventHandler OnAnyActionAddQueue;
    public static event EventHandler OnAnyActionCompleted; //行为结束触发视图更新
    private List<BaseActionParameter> pendingActions; //行为队列
    private Unit unit; //当前单位


    private void Awake()
    {
        unit = GetComponent<Unit>();
    }

    public ActionQueue()
    {
        pendingActions = new List<BaseActionParameter>();
    }

    public void AddAction(BaseActionParameter baseActionParameter)
    {
        pendingActions.Add(baseActionParameter);
        OnAnyActionAddQueue?.Invoke(this, EventArgs.Empty);
    }

    //判断行为队列是否全部完成
    public bool IsActionQueueComplete()
    {
        return pendingActions.Count == 0;
    }

    public List<BaseActionParameter> GetActions()
    {
        return pendingActions;
    }

    /// <summary>
    /// 获取队列中Unit最新的位置
    /// </summary>
    public GridPosition GetlatestUnitPosition()
    {
        //^1代表数组中最后一位
        return pendingActions[^1].unitFinalPosition;
    }

    /// <summary>
    /// 获取所有已经取消的动作
    /// </summary>
    /// <param name="actionsIndex">取消动作的位置</param>
    /// <param name="cancelAction">取消动作</param>
    /// <returns></returns>
    public List<BaseActionParameter> GetCancelActions(int actionsIndex, BaseAction cancelAction)
    {

        List<BaseActionParameter> cancelActions = new List<BaseActionParameter>();
        ActionTags actionTags = ScriptiptableSOManager.Instance.GetActionTags(cancelAction);
        if (actionTags.HasFlag(ActionTags.Move)) //包含位移则取消后续所有行动
        {
            cancelActions =
                pendingActions
                .Skip(actionsIndex) //表示从下标actionsIndex开始截取
                .ToList();
            pendingActions.RemoveRange(actionsIndex, pendingActions.Count - actionsIndex);
        }
        else
        {
            cancelActions.Add(pendingActions[actionsIndex]);
            pendingActions.RemoveRange(actionsIndex, 1);
        }

        return cancelActions;
    }

    /// <summary>
    /// 获取当前队列的最高优先级顺序
    /// </summary>
    public int GetCurrentExecutionOrder()
    {
        return pendingActions.Count > 0
            ? pendingActions[0].executeTurn
            : int.MaxValue;
    }

    /// <summary>
    /// 获取指定顺序的所有行动
    /// </summary>
    private List<BaseActionParameter> GetActionsAtOrder(int order)
    {
        return pendingActions
            .Where(a => a.executeTurn == order)
            .ToList();
    }

    /// <summary>
    /// 执行指定顺序的所有行动
    /// </summary>
    public void ExecuteActionsAtOrder(int order, Action onComplete)
    {
        List<BaseActionParameter> actions = GetActionsAtOrder(order);
        if (actions.Count == 0)
        {
            onComplete?.Invoke();
            return;
        }

        int completedCount = 0;
        foreach (BaseActionParameter action in actions)
        {
            action.baseAction.TakeAction(action, () =>
            {
                completedCount++;
                if (completedCount >= actions.Count)
                {
                    pendingActions.RemoveAll(a => a.executeTurn == order);
                    OnAnyActionCompleted?.Invoke(this, EventArgs.Empty);
                    onComplete?.Invoke();
                }
            });
        }
    }

    /// <summary>
    /// 设置指定行动的执行顺序
    /// </summary>
    /// <param name="index">动作位置</param>
    /// <param name="executeTurn">执行顺序</param>
    public void ActionExecuteTurnSet(int index, int executeTurn)
    {
        pendingActions[index].executeTurn = executeTurn;
    }

    /// <summary>
    /// 返回最后一个动作的执行顺序，如果没有动作则返回-1
    /// </summary>
    public int EndActionExecuteTurnGet()
    {
        return pendingActions.Count > 0
            ? pendingActions.Last().executeTurn
            : -1;
    }

    public void UpdateBaseActionParameter(BaseActionParameter baseActionParameter)
    {
        int lastActionParActPoint = CopyLastActionParameter().GetActionPoint();
        int actPoint = baseActionParameter.GetActionPoint() - lastActionParActPoint; //更新动作所增加的消耗行动点

        if (!unit.CanspendActionPointsToTakeAction(actPoint)) //检查是否可以消耗行动点
        {
            Debug.LogWarning("行动点不足，无法执行该动作。");
            return;
        }

        unit.TrySpendActionPointsToTakeAction(actPoint);
        if(baseActionParameter is MoveActionParameter moveActionParameter)
        {
            pendingActions[^1] = moveActionParameter;
        }
        else
        {
            pendingActions[^1] = baseActionParameter; //更新当前动作的参数
        }
            
        OnAnyActionAddQueue?.Invoke(this, EventArgs.Empty);
    }


    /// <summary>
    /// 拷贝最后一个动作的参数
    /// </summary>
    /// <returns>最后一个动作的参数</returns>
    public BaseActionParameter CopyLastActionParameter()
    {
        BaseActionParameter last = pendingActions.Last();
        // 深拷贝 targetPositionList，避免引用同一个列表
        List<GridPosition> newTargetList = new List<GridPosition>(last.targetPositionList);

        BaseActionParameter copyBaseActionParameter = new BaseActionParameter(
            last.baseAction,
            newTargetList,
            last.unitFinalPosition,
            last.executeTurn
        );
        return copyBaseActionParameter;
    }

    public MoveActionParameter CopyLastMoveActionParameter()
    {
        MoveActionParameter last = pendingActions[^1] as MoveActionParameter;
        // 深拷贝 targetPositionList，避免引用同一个列表
        List<GridPosition> newTargetList = new List<GridPosition>(last.targetPositionList);

        MoveActionParameter copyMoveActionParameter = new(
            (MoveAction)last.baseAction,
            newTargetList,
            last.unitFinalPosition,
            last.executeTurn
        );
        copyMoveActionParameter.SetMovePath(last.GetMovePathForGridPosition()); //拷贝移动路径

        return copyMoveActionParameter;
    }

    /// <summary>
    /// 检查最后一个动作的参数是否包含指定目标位置
    /// </summary>
    public bool IsLastActionParameterTargetPosition(GridPosition targetPosition)
    {
        if (pendingActions.Count == 0) return false;
        BaseActionParameter lastAction = pendingActions.Last();
        ActionTags actionTags = ScriptiptableSOManager.Instance.GetActionTags(lastAction.baseAction);
        bool isMove = actionTags.HasFlag(ActionTags.Move); // 检查是否为移动行动
        if (!isMove) return false; //非移动行动不检查目标位置
        return lastAction.targetPositionList.Contains(targetPosition);
    }

    /// <summary>
    /// 停止执行行动队列中的所有行动
    /// </summary>
    public void ActionQueueStop()
    {
        if (pendingActions.Count == 0) return; // 如果没有待执行的行动则不执行
        pendingActions.Last().baseAction.ActionStop(); // 停止最后一个行动
        pendingActions.Clear(); //取消所有队列的动作
        unit.ReSetUnitParam(); // 重置单位参数
    }

    public List<GridPosition> GetAllPath()
    {
        List<GridPosition> allPaths = new List<GridPosition>();

        foreach (BaseActionParameter baseActionParameter in pendingActions)
        {
            if(baseActionParameter is MoveActionParameter moveActionParameter)
            {
                allPaths.AddRange(moveActionParameter.GetMovePathForGridPosition());
            }
        }

        return allPaths;
    }

    public List<MoveActionParameter> GetAllMoveActionParameters()
    {
        return pendingActions
            .Where(a => a is MoveActionParameter)
            .Select(a => (MoveActionParameter)a)
            .ToList();
    }
}
