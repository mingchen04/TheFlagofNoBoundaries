using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public abstract class BaseMoveAction : BaseAction
{
    public abstract event EventHandler OnStartMoving;
    public abstract event EventHandler OnStopMoving;

    protected bool IsWalking = false;
    protected List<Vector3> movePathWorldPositions;
    protected int currentPathIndex;
    protected GridPosition targetPosition = GridPosition.nullGridPosition;
    protected Action ArriveTargetLocationCall; //到达目标位置时的回调
    protected Pathfinding.PathfindingRules[] defaultPathfindingRules = Pathfinding.defaultPathfindingRules;


    /// <summary>
    /// 检查目标位置是否可达，并处理路径冲突
    /// </summary>
    protected bool TargetPositionStance()
    {
        Unit targetUnit = LevelGrid.Instance.GetUnitAtGridPosition(targetPosition);
        bool IsWalking = true;
        if (targetUnit != null && targetUnit != unit)
        {
            IsWalking = false; //停止行走动画
            if (targetUnit.GetAction<BaseMoveAction>().isActive)
            {
                // 单位正在移动，等待其离开格子                
                bool isTargetUnitA = targetUnit.GetAction<BaseMoveAction>().GetTargetPosition() == unit.GetGridPosition();
                bool isTargetUnitB = targetPosition == targetUnit.GetGridPosition();
                if (isTargetUnitA && isTargetUnitB)
                {
                    //路径冲突，结束双方行动
                    unit.GetActionQueue().ActionQueueStop();
                    targetUnit.GetActionQueue().ActionQueueStop();
                }
            }
            else
            {
                // 如果目标单位不再移动，路径被占用停止行动
                unit.GetActionQueue().ActionQueueStop();
            }
        }

        // 目标格可达，可以继续移动
        return IsWalking;
    }

    public GridPosition GetTargetPosition()
    {
        return targetPosition;
    }

    public void UpdatePath()
    {
        MoveActionParameter lastParam = unit.GetActionQueue().CopyLastMoveActionParameter();

        if (movePathWorldPositions == null || movePathWorldPositions.Count == 0)
        {
            List<GridPosition> path = BuildMovePathFromTargets(lastParam.targetPositionList);
            movePathWorldPositions = path.Select(gridPosition => (Vector3)gridPosition).ToList();
        }

        lastParam.SetMovePath(movePathWorldPositions.Select(worldPosition => (GridPosition)worldPosition).ToList());
        unit.GetActionQueue().UpdateBaseActionParameter(lastParam);
        movePathWorldPositions.Clear();
    }


    /// <summary>
    /// 体力消耗模式
    /// </summary>
    protected enum SrengthConsumption
    {
        everyStep, // 每一步移动消耗体力
        immediate, // 立即消耗体力
    }

    /// <summary>
    /// 控制单位移动到目标位置
    /// </summary>
    /// <param name="moveSpeed">移动速度</param>
    /// <param name="srengthConsumption">体力消耗模式</param>
    /// <param name="srengthConsumptionCost">体力消耗点数</param>
    protected void UnitMove(float moveSpeed = 4f,
        SrengthConsumption srengthConsumption = SrengthConsumption.immediate,
        int srengthConsumptionCost = 0)
    {
        if (srengthConsumption == SrengthConsumption.immediate && srengthConsumptionCost != 0)
        {
            unit.TrySpendActionPointsToTakeAction(srengthConsumptionCost);
        }

        Vector3 moveDirection = ((Vector3)targetPosition - transform.position).normalized;
        float stoppingDistance = 0.1f;
        if (Vector3.Distance(transform.position, targetPosition) > stoppingDistance)
        {
            transform.position += moveSpeed * Time.deltaTime * moveDirection;
        }
        else
        {
            ArriveTargetLocationCall?.Invoke(); // 到达目标位置时调用回调
            ArriveTargetLocationCall = null; // 清除回调，避免重复调用

            currentPathIndex++;
            GridPosition oldTargetPosition = targetPosition;
            if (movePathWorldPositions.Count > currentPathIndex)
            {
                targetPosition = movePathWorldPositions[currentPathIndex];
            }

            unit.SetUnitGridPosition(oldTargetPosition);

            //每到达一格消耗体力
            if (srengthConsumption == SrengthConsumption.everyStep)
            {
                unit.TrySpendActionPointsToTakeAction(srengthConsumptionCost);
            }

            if (currentPathIndex >= movePathWorldPositions.Count)
            {
                targetPosition = GridPosition.nullGridPosition;
                currentPathIndex = 0; // 重置索引
                ActionComplete();
            }
        }

        float rotateSpeed = 10f;
        transform.forward = Vector3.Slerp(transform.forward, moveDirection, Time.deltaTime * rotateSpeed);
    }


    /// <summary>
    /// 根据目标点列表构建完整路径（不含起点重复）
    /// </summary>
    protected List<GridPosition> BuildMovePathFromTargets(List<GridPosition> targetPositions)
    {
        List<GridPosition> path = new();
        if (targetPositions == null || targetPositions.Count < 2) return path;
        for (int i = 1; i < targetPositions.Count; i++)
        {
            GridPosition start = targetPositions[i - 1];
            GridPosition end = targetPositions[i];
            List<GridPosition> segment = Pathfinding.Instance.FindPath(start, end, out int _ , defaultPathfindingRules);
            if (segment.Count > 1)
            {
                path.AddRange(segment.Skip(1)); // 跳过起点
            }
        }
        return path;
    }

    /// <summary>
    /// 暂停行动，并返回一个新的Action用于恢复行动，并且在到达目标格子时执行回调,用于单位目标相同时间隔进行移动
    /// </summary>
    /// <returns>恢复行动回调方法</returns>
    public Action<Action> SuspendAction()
    {
        isActive = false;

        // 等效于 Action action =new Action(() => { });
        void action(Action ArriveTargetLocationCall)
        {
            isActive = true;

            //附加到达目标位置的回调，避免短时间多次冲突覆盖上一个协程的回调
            this.ArriveTargetLocationCall += ArriveTargetLocationCall;
        }

        return action;
    }

    public override void TakeAction(BaseActionParameter baseActionParameter, Action onActionComplete)
    {
        unit.SetForecast(false);
        currentPathIndex = 0;
        targetPosition = GridPosition.nullGridPosition; // 重置目标位置
        movePathWorldPositions = (baseActionParameter as MoveActionParameter).GetMovePathForWorldPosition();

        ActionStart(onActionComplete);
    }

    protected override void ActionComplete()
    {
        movePathWorldPositions.Clear();
        base.ActionComplete();
    }
}
