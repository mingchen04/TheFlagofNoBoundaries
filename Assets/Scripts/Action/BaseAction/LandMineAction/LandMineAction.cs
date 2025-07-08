using System;
using System.Collections.Generic;
using UnityEngine;

public class LandMineAction : BaseAction
{
    [SerializeField] private Transform landMineTransformPrefab; // 地雷预制体
    private int landMineDistance = 1;
    private const float landMineTimeMax = 0.2f;//埋地雷使用时间
    private float landMineTime = 0;

    private void Update()
    {
        if(!isActive) return;
        if (landMineTime > 0)
        {
            landMineTime -= Time.deltaTime;
        }
        else
        {
            ActionComplete();
        }
    }

    protected override void Awake()
    {
        base.Awake();
    }

    public override string GetActionName()
    {
        return "地雷";
    }

    public override EnemyAIAction GetEnemyAIAction(GridPosition gridPosition)
    {
        return new EnemyAIAction
        {
            gridPosition = gridPosition,
            actionValue = 0 // 假设地雷的行动值为10
        };
    }

    public override List<GridPosition> GetValidActionGridPositionList()
    {
        List<GridPosition> validActionGridPositionList = new List<GridPosition>();
        GridPosition unitGridPosition = unit.GetGridPosition();

        foreach (GridPosition testGridPosition in LevelGrid.Instance.GenerateHexArea(unitGridPosition, landMineDistance))
        {
            if(testGridPosition == unitGridPosition)
            {
                continue;
            }

            if (!LevelGrid.Instance.IsValidGridPosition(testGridPosition))
            {
                //网格位置不合法跳过
                continue;
            }

            if (LevelGrid.Instance.HasAnyUnitOnGridPosition(testGridPosition))
            {
                continue;
            }

            validActionGridPositionList.Add(testGridPosition);
        }
        return validActionGridPositionList;
    }

    public override void TakeAction(BaseActionParameter baseActionParameter, Action onActionComplete)
    {
        List<GridPosition> gridPositions =baseActionParameter.targetPositionList;
        ActionStart(onActionComplete);
        unit.TrySpendActionPointsToTakeAction(this);
        landMineTime = landMineTimeMax;
        LandMine landMine = Instantiate(landMineTransformPrefab, (Vector3)gridPositions[0], Quaternion.identity).GetComponent<LandMine>();
        landMine.Setup(gridPositions[0]);
    }

}
