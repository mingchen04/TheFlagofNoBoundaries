using System;
using System.Collections.Generic;
using UnityEngine;

public class GrenadeAction : BaseAction
{
    [SerializeField] private Transform grenadeProjectilePrefab;

    private int maxThrowDistance = 7;

    private void Update()
    {
        if (!isActive)
        {
            return;
        }
    }

    public override string GetActionName()
    {
        return "手雷";
    }

    public override EnemyAIAction GetEnemyAIAction(GridPosition gridPosition)
    {
        return new EnemyAIAction() { actionValue = 0, gridPosition = gridPosition };
    }

    public override List<GridPosition> GetValidActionGridPositionList()
    {
        List<GridPosition> validActionGridPositionList = new List<GridPosition>();
        GridPosition unitGridPosition = unit.GetGridPosition();

        foreach (GridPosition testGridPosition in LevelGrid.Instance.GenerateHexArea(unitGridPosition, maxThrowDistance))
        {

            if (!LevelGrid.Instance.IsValidGridPosition(testGridPosition))
            {
                //网格位置不合法跳过
                continue;
            }

            validActionGridPositionList.Add(testGridPosition);
        }
        return validActionGridPositionList;
    }

    public override void TakeAction(List<GridPosition> gridPositions, Action actionCompleted)

    {
        ActionStart(actionCompleted);
        unit.TrySpendActionPointsToTakeAction(this);
        Transform grenadeProjectileTransform = Instantiate(grenadeProjectilePrefab, unit.GetWorldPosition(), Quaternion.identity);
        GrenadeProjectile grenadeProjectile = grenadeProjectileTransform.GetComponent<GrenadeProjectile>();
        grenadeProjectile.Setup(gridPositions[0], OnGrenadeBehaviourComplete);
    }

    private void OnGrenadeBehaviourComplete()
    {
        ActionComplete();
    }
}
