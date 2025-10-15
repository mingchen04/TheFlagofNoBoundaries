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
    private BaseActionParameter underwayAction; //正在进行的行动
    private BaseActionParameter slowAction = null; //队列中的慢速行动
    private Unit unit; //当前单位

    private void Awake()
    {
        unit = GetComponent<Unit>();
    }

    public ActionQueue()
    {
        pendingActions = new List<BaseActionParameter>();
    }

    public bool HandleOrderedAction(BaseAction action, GridPosition targetPos, int executionOrder)
    {
        //检测动作合法性
        if (!IsActionSpeedValid()) return false;
        bool fastCountValid =
            action.GetActionSpeed() == ActionSpeed.fast
            &&
            GetActionSpeedFastCount() >= unit.GetFastActionPointsCost();

        unit.SetForecast(true);

        ActionTags actionTags = ScriptiptableSOManager.Instance.GetActionTags(action);
        bool isMove = actionTags.HasFlag(ActionTags.Move);
        bool isDeploy = actionTags.HasFlag(ActionTags.Deploy);

        BaseActionParameter actionParam;
        GridPosition finalPos;
        List<GridPosition> targetPositionList;

        #region 部署移动处理

        if (!isDeploy)
        {
            unit.SetDeselectAction(false);
        }

        if (isMove)
        {
            finalPos = targetPos;
            targetPositionList = new List<GridPosition> { unit.GetGridPosition(), targetPos };
        }
        else
        {
            finalPos = unit.GetGridPosition();
            targetPositionList = new List<GridPosition> { targetPos };
        }

        if (unit.GetDeselectAction()&& !unit.GetActionQueue().IsActionQueueComplete())
        {
            // 复制上一次参数
            BaseActionParameter lastParam = CopyLastMoveActionParameter();
            actionParam = lastParam;
            actionParam.targetPositionList.Add(targetPos);
            actionParam.unitFinalPosition = finalPos;

            bool IsUpdateSuccess;
            if (actionParam is MoveActionParameter moveActionParameter)
            {
                IsUpdateSuccess = UpdateBaseActionParameter(moveActionParameter);
            }
            else
            {
                IsUpdateSuccess = UpdateBaseActionParameter(actionParam);
            }
            if (!IsUpdateSuccess)
            {
                // 更新失败，可能是行动点不足或其他限制
                return IsUpdateSuccess;
            }
            Debug.Log($"{gameObject.name} 更新行动: {action.GetActionName()} (顺序:{executionOrder}, 目标:{targetPos}, 终位:{finalPos}) 体力消耗{actionParam.GetActionPoint()}");
        }
        else
        {
            if (isDeploy) unit.SetDeselectAction(true);
            // 根据标签或类型实例化不同参数
            if (isMove && action is BaseMoveAction baseMove)
            {
                actionParam = new MoveActionParameter(
                    baseMove,
                    // 如果到达快速行动点上限则设置为慢速，否则使用默认速度
                    fastCountValid ? ActionSpeed.slow : baseMove.GetActionSpeed(),
                    targetPositionList,
                    finalPos,
                    executionOrder
                );
            }
            else
            {
                actionParam = new BaseActionParameter(
                    action,
                    fastCountValid ? ActionSpeed.slow : action.GetActionSpeed(),
                    targetPositionList,
                    finalPos,
                    executionOrder
                );
            }

            if (!unit.TrySpendActionPointsToTakeAction(action, actionParam)) return false;

            pendingActions.Add(actionParam);

            Debug.Log($"{gameObject.name}  添加行动: {action.GetActionName()} (顺序:{executionOrder}, 目标:{targetPos}, 终位:{finalPos}) 体力消耗 {actionParam.GetActionPoint()}");
        }

        if (isMove)
        {
            MoveActionParameter moveActionParameter = (actionParam as MoveActionParameter);
            (moveActionParameter.baseAction as BaseMoveAction).UpdatePath();
            unit.OnAnyUnitPositionChangedInvoke();
        }

        OnAnyActionAddQueue?.Invoke(this, EventArgs.Empty);
        return true; // 返回true表示成功添加行动
        #endregion
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
        int order = int.MaxValue;

        if (pendingActions.Count > 0)
        {
            order = pendingActions[0].executeTurn;
        }

        if (slowAction != null)
        {
            order = slowAction.executeTurn;
        }

        return order;
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

        if (slowAction != null && slowAction.baseAction != null)
        {
            slowAction.baseAction.TakeAction(slowAction, () =>
            {
                slowAction = null; //清空慢速行动
                ExecuteActionsAtOrder(order, onComplete); //慢速行动结束后继续执行当前顺序的其他行动
                OnAnyActionCompleted?.Invoke(this, EventArgs.Empty);
            });
            return; //如果有慢速行动正在进行则直接返回
        }

        if (actions.Count == 0)
        {
            onComplete?.Invoke();
            return;
        }

        int completedCount = 0;
        foreach (BaseActionParameter action in actions)
        {
            underwayAction = action; //记录当前正在进行的行动
            if (action.actionSpeed == ActionSpeed.slow)
            {
                Debug.Log("慢速行动稍后执行");
                action.actionSpeed = ActionSpeed.quickness;
                slowAction = action; //记录慢速行动
                pendingActions.Clear(); //清空队列
                OnAnyActionCompleted?.Invoke(this, EventArgs.Empty);
                onComplete?.Invoke();
                return; //慢速行动单独执行，结束后直接返回
            }

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

    public bool UpdateBaseActionParameter(BaseActionParameter baseActionParameter)
    {
        int lastActionParActPoint = CopyLastActionParameter().GetActionPoint();
        int actPoint = baseActionParameter.GetActionPoint() - lastActionParActPoint; //更新动作所增加的消耗行动点

        if (!unit.CanspendActionPointsToTakeAction(actPoint)) //检查是否可以消耗行动点
        {
            return false;
        }

        unit.TrySpendActionPointsToTakeAction(actPoint);
        if (baseActionParameter is MoveActionParameter moveActionParameter)
        {
            pendingActions[^1] = moveActionParameter;
        }
        else
        {
            pendingActions[^1] = baseActionParameter; //更新当前动作的参数
        }

        return true; //返回true表示更新成功
    }


    /// <summary>
    /// 拷贝最后一个动作的参数
    /// </summary>
    /// <returns>最后一个动作的参数</returns>
    public BaseActionParameter CopyLastActionParameter()
    {
        if (pendingActions.Count == 0)
        {
            return null;
        }

        BaseActionParameter last = pendingActions.Last();
        // 深拷贝 targetPositionList，避免引用同一个列表
        List<GridPosition> newTargetList = new List<GridPosition>(last.targetPositionList);

        BaseActionParameter copyBaseActionParameter = new BaseActionParameter(
            last.baseAction,
            last.actionSpeed,
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
            (BaseMoveAction)last.baseAction,
            last.actionSpeed,
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
        unit.ReSetUnitParam(); // 重置单位参数
        underwayAction.baseAction.ActionStop(); // 如果有正在进行的行动则结束它
        if (pendingActions.Count == 0) return; // 如果没有待执行的行动则不执行
        
        pendingActions.RemoveAll(a => a.executeTurn == underwayAction.executeTurn); // 移除所有与正在进行的行动相同执行顺序的行动

        bool isValid = false;
        if (pendingActions.Count != 0)
        {
            // 检查下一个方法是否有效
            BaseActionParameter nextBaseActionParameter = pendingActions[0];
            
            bool isMove = nextBaseActionParameter.baseAction.GetActionTags().HasFlag(ActionTags.Move);

            GridPosition unitFinalPosition = nextBaseActionParameter.unitFinalPosition;
            GridPosition targetPosition = nextBaseActionParameter.targetPositionList[0];

            isValid =
                nextBaseActionParameter.baseAction.IsValidActionGridPosition
                (
                    isMove ? unitFinalPosition : targetPosition
                );

            Debug.Log($"{nextBaseActionParameter.baseAction.GetUnit().name} 检查动作合法 {nextBaseActionParameter.baseAction.name} : {isValid}");
        }       

        if (isValid)
        {
            //如果下一个行动有效，则重新计算消耗点数
            int actionPointsCost = 0;
            pendingActions.ForEach(BaseActionParameter =>
            {
                actionPointsCost += BaseActionParameter.GetActionPoint();
            });
            unit.SetForecast(true); // 重新设置预测状态
            unit.TrySpendActionPointsToTakeAction(actionPointsCost); // 尝试消耗行动点
        }
        else
        {
            // 如果下一个行动无效，则清空队列
            pendingActions.Clear(); //取消所有队列的动作
        }
    }

    public List<GridPosition> GetAllPath()
    {
        List<GridPosition> allPaths = new();

        foreach (BaseActionParameter baseActionParameter in pendingActions)
        {
            if (baseActionParameter is MoveActionParameter moveActionParameter)
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

    public BaseActionParameter GetUnderwayAction()
    {
        return underwayAction;
    }

    /// <summary>
    /// 检查队列中是否存在慢速行动
    /// </summary>
    private bool IsActionSpeedSlow()
    {
        foreach (BaseActionParameter action in pendingActions)
        {
            if (action.actionSpeed == ActionSpeed.slow)
            {
                return true;
            }
        }
        return false; // 如果没有慢速行动则返回false
    }

    /// <summary>
    /// 获取队列中快速行动的数量
    /// </summary>
    /// <returns>快速行动的数量</returns>
    private int GetActionSpeedFastCount()
    {
        int fastActionCount = 0; // 快速行动计数器

        foreach (BaseActionParameter action in pendingActions)
        {
            if (action.baseAction.GetActionSpeed() == ActionSpeed.fast)
            {
                fastActionCount++; // 计数快速行动
            }
        }

        return fastActionCount;
    }

    private bool IsActionSpeedValid()
    {
        if (IsActionSpeedSlow())
        {
            return false; // 如果发现有慢速行动则返回false
        }

        return true;
    }

    public BaseActionParameter GetSlowAction()
    {
        return slowAction;
    }

    /// <summary>
    /// 队列已满
    /// </summary>
    public bool IsActionQueueFull()
    {
        bool isFull = false;

        if (!IsActionSpeedValid()) isFull = true;
        if (pendingActions.Count >= 10) isFull = true;

        return isFull;
    }

}
