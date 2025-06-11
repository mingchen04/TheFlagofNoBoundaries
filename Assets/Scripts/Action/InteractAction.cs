using System;
using System.Collections.Generic;

public class InteractAction : BaseAction
{
    private int maxInteractDistance = 1;

    private void Update()
    {
        if (!isActive)
        {
            return;
        }
    }

    public override string GetActionName()
    {
        return "互动";
    }

    public override EnemyAIAction GetEnemyAIAction(GridPosition gridPosition)
    {
        return new EnemyAIAction
        {
            gridPosition = gridPosition,
            actionValue = 0,
        };
    }

    public override List<GridPosition> GetValidActionGridPositionList()
    {
        List<GridPosition> validActionGridPositionList = new List<GridPosition>();
        GridPosition unitGridPosition = unit.GetGridPosition();

        foreach (GridPosition testGridPosition in LevelGrid.Instance.GenerateHexArea(unitGridPosition, maxInteractDistance))
        {

            if (!LevelGrid.Instance.IsValidGridPosition(testGridPosition))
            {
                //网格位置不合法跳过
                continue;
            }

            IInteractable interactable = LevelGrid.Instance.GetInteractableAtGridPosition(testGridPosition);
            if (interactable == null)
            {
                continue;
            }

            validActionGridPositionList.Add(testGridPosition);
        }


        return validActionGridPositionList;
    }

    public override void TakeAction(GridPosition gridPosition, Action onInteractComplete)
    {
        IInteractable interactable = LevelGrid.Instance.GetInteractableAtGridPosition(gridPosition);
        if (interactable == null)
        {
            return;
        }

        ActionStart(onInteractComplete);
        unit.TrySpendActionPointsToTakeAction(this);
        interactable.Interact(OnInteractComplete);
    }

    public int GetMaxSwordDistance()
    {
        return maxInteractDistance;
    }

    private void OnInteractComplete()
    {
        ActionComplete();
    }
}
