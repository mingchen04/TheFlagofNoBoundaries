using System;
using System.Collections.Generic;
using UnityEngine;

public class SwordAction : BaseAction
{
    public static event EventHandler OnAnySwordHit;
    public event EventHandler OnSwordActionStarted;
    public event EventHandler OnSwordActionCompleted;

    private int maxSworldDistance = 1;

    private enum State
    {
        SwingingSwordBeforeHit,
        SwingingSwordAfterHit,
    }

    private State state;
    private float stateTimer;
    private GridPosition targetUnitPosition;

    private void Update()
    {
        if (!isActive)
        {
            return;
        }

        stateTimer -= Time.deltaTime;

        switch (state)
        {
            case State.SwingingSwordBeforeHit:
                float rotateSpeed = 10f;
                Vector3 aimDir = ((Vector3)targetUnitPosition - transform.position).normalized;
                transform.forward = Vector3.Slerp(transform.forward, aimDir, rotateSpeed * Time.deltaTime);
                break;
            case State.SwingingSwordAfterHit:
                break;
        }

        if (stateTimer <= 0f)
        {
            NextState();
        }
    }

    private void NextState()
    {
        switch (state)
        {
            case State.SwingingSwordBeforeHit:
                state = State.SwingingSwordAfterHit;
                float afterHitStateTime = 0.8f;
                stateTimer = afterHitStateTime;
                LevelGrid.Instance.DamageUnitForGridPosition(targetUnitPosition, unit.GetAttack());
                OnAnySwordHit?.Invoke(this, EventArgs.Empty);
                break;
            case State.SwingingSwordAfterHit:
                OnSwordActionCompleted?.Invoke(this, EventArgs.Empty);
                ActionComplete();
                break;
        }
    }


    public override EnemyAIAction GetEnemyAIAction(GridPosition gridPosition)
    {
        return new EnemyAIAction
        {
            gridPosition = gridPosition,
            actionValue = 200,
        };
    }

    public override List<GridPosition> GetValidActionGridPositionList()
    {
        List<GridPosition> validActionGridPositionList = new List<GridPosition>();
        GridPosition unitGridPosition = unit.GetGridPosition();

        foreach (GridPosition testGridPosition in LevelGrid.Instance.GenerateHexArea(unitGridPosition, maxSworldDistance))
        {
            if (!LevelGrid.Instance.IsValidGridPosition(testGridPosition))
            {
                //网格位置不合法跳过
                continue;
            }

            if (!LevelGrid.Instance.HasAnyUnitOnGridPosition(testGridPosition))
            {
                continue;
            }

            Unit targetUnit = LevelGrid.Instance.GetUnitAtGridPosition(testGridPosition);

            if (targetUnit.IsEnemy() == unit.IsEnemy())
            {
                continue;
            }

            validActionGridPositionList.Add(testGridPosition);
        }
        return validActionGridPositionList;
    }

    public override void TakeAction(BaseActionParameter baseActionParameter, Action onActionComplete)
    {
        List<GridPosition> gridPositions = baseActionParameter.targetPositionList;
        targetUnitPosition = gridPositions[0];
        ActionStart(onActionComplete);
        unit.TrySpendActionPointsToTakeAction(this);
        float swordBeforeHitTime = 0.7f;
        stateTimer = swordBeforeHitTime;
        state = State.SwingingSwordBeforeHit;
        OnSwordActionStarted?.Invoke(this, EventArgs.Empty);
    }

    public int GetMaxSwordDistance()
    {
        return maxSworldDistance;
    }
}
