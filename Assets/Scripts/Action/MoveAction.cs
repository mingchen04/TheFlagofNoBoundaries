using System;
using System.Collections.Generic;
using UnityEngine;

public class MoveAction : BaseAction
{
    //public delegate void MoveCompleteDelegate();

    public event EventHandler OnStartMoving;
    public event EventHandler OnStopMoving;

    [SerializeField] private int maxMoveDistance = 2;

    private List<Vector3> positionList;
    private int currentPositionIndex;

    public void Update()
    {
        if (!isActive)
        {
            return;
        }
        Vector3 targetPosition = positionList[currentPositionIndex];

        Vector3 moveDirection = (targetPosition - transform.position).normalized; //计算向量

        float stoppingDistance = .1f;
        if (Vector3.Distance(transform.position, targetPosition) > stoppingDistance) //计算两个坐标之间的距离大于0.1则行走
        {
            float moveSpeed = 4f;
            transform.position += moveSpeed * Time.deltaTime * moveDirection;
        }
        else
        {
            currentPositionIndex++;
            if (currentPositionIndex >= positionList.Count)
            {
                OnStopMoving?.Invoke(this, EventArgs.Empty);
                ActionComplete();
            }
        }

        float rotateSpeed = 10f;
        transform.forward = Vector3.Slerp(transform.forward, moveDirection, Time.deltaTime * rotateSpeed);
    }

    public override void TakeAction(GridPosition gridPosition, Action actionCompleted)
    {
        ActionStart(actionCompleted);
        ActionPointConsumption();

        currentPositionIndex = 0;
        positionList = new List<Vector3>();
        //TakeAction使用Unit.GetGridPosition() 请确认ActionStart事件在之前发生才能获取实际坐标
        List<GridPosition> gridPositionList = Pathfinding.Instance.FindPath(unit.GetGridPosition(), gridPosition, out int pathLength); //调用A*寻路

        foreach (GridPosition gridPositionItem in gridPositionList)
        {
            positionList.Add(LevelGrid.Instance.GetWorldPosition(gridPositionItem));
        }

        OnStartMoving?.Invoke(this, EventArgs.Empty);
    }

    public override List<GridPosition> GetValidActionGridPositionList()
    {
        List<GridPosition> validActionGridPositionList = new List<GridPosition>();
        GridPosition unitGridPosition = unit.GetGridPosition();
        for (int x = -maxMoveDistance; x <= maxMoveDistance; x++)
        {
            for (int z = -maxMoveDistance; z <= maxMoveDistance; z++)
            {
                GridPosition offsetGridPosition = new GridPosition(x, z);
                GridPosition testGridPosition = unitGridPosition + offsetGridPosition;

                if (!LevelGrid.Instance.IsValidGridPosition(testGridPosition))
                {
                    //网格位置不合法跳过
                    continue;
                }

                if (unitGridPosition == testGridPosition)
                {
                    //网格位置等于选中对象位置跳过
                    continue;
                }

                if (LevelGrid.Instance.HasAnyUnitOnGridPosition(testGridPosition))
                {
                    continue;
                }

                if (!Pathfinding.Instance.IsWalkableGridPosition(testGridPosition))
                {
                    continue;
                }

                if (!Pathfinding.Instance.HasPath(unit.GetGridPosition(), testGridPosition))
                {
                    continue;
                }

                int pathfindingDistanceMultiplier = 10;
                //判断寻路移动距离的是否大于移动最大值
                if (Pathfinding.Instance.GetPathLength(unit.GetGridPosition(), testGridPosition)
                    > maxMoveDistance * pathfindingDistanceMultiplier)
                {
                    continue;
                }

                validActionGridPositionList.Add(testGridPosition);
            }
        }
        return validActionGridPositionList;

    }

    public override string GetActionName()
    {
        return "移动";
    }

    public override EnemyAIAction GetEnemyAIAction(GridPosition gridPosition)
    {
        //可以执行射击动作的坐标数量
        int targetCountAtGridPosition = unit.GetAction<ShootAction>().GetTargetCountAtPosition(gridPosition);

        //敌人数量越多 移动优先值越高
        return new EnemyAIAction
        {
            gridPosition = gridPosition,
            actionValue = targetCountAtGridPosition * 10,
        };
    }
}
