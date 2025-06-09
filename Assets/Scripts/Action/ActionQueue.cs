using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 存储和操作单位的行为队列
/// </summary>
public class ActionQueue
{
    public static event EventHandler OnAnyActionCompleted; //行为结束触发视图更新
    private List<BaseActionParameter> pendingActions; //行为队列
    private GridPosition lastActionUnitPosition; //存储上一次行动的单位位置

    public ActionQueue()
    {
        pendingActions = new List<BaseActionParameter>();
    }

    public void AddAction(BaseActionParameter baseActionParameter)
    {
        pendingActions.Add(baseActionParameter);
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
    public List<BaseAction> GetCancelActions(int actionsIndex, BaseAction cancelAction)
    {

        List<BaseAction> cancelActions = new List<BaseAction>();
        BaseActionSO.ActionTags actionTags = ScriptiptableSOManager.Instance.GetActionTags(cancelAction);
        if (actionTags.HasFlag(BaseActionSO.ActionTags.Move)) //包含位移则取消后续所有行动
        {
            cancelActions =
                pendingActions
                .Skip(actionsIndex) //表示从下标actionsIndex开始截取
                .Select(actionParam => actionParam.baseAction) //返回baseAction组成新数组
                .ToList();
            pendingActions.RemoveRange(actionsIndex, pendingActions.Count - actionsIndex);
        }
        else
        {
            cancelActions.Add(pendingActions[actionsIndex].baseAction);
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
    public List<BaseActionParameter> GetActionsAtOrder(int order)
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
            action.baseAction.TakeAction(action.targetGridPosition, () =>
            {
                completedCount++;
                if (completedCount == actions.Count)
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
    /// 返回最后一个动作的执行顺序，如果没有动作则返回0
    /// </summary>
    public int EndActionExecuteTurnGet()
    {
        return pendingActions.Count > 0
            ? pendingActions.Last().executeTurn
            : -1;
    }
}
