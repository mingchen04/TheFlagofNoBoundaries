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
    private Unit targetUnit;

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
                Vector3 aimDir = (targetUnit.GetWorldPosition() - transform.position).normalized;
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
                targetUnit.Damage(90);
                OnAnySwordHit?.Invoke(this, EventArgs.Empty);
                break;
            case State.SwingingSwordAfterHit:
                OnSwordActionCompleted?.Invoke(this, EventArgs.Empty);
                ActionComplete();
                break;
        }
    }

    public override string GetActionName()
    {
        return "劈砍";
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

    public override void TakeAction(GridPosition gridPosition, Action onActionComplete)
    {
        targetUnit = LevelGrid.Instance.GetUnitAtGridPosition(gridPosition);
        if (targetUnit == null)
        {
            unit.CancelAction(this);
            ActionStart(onActionComplete);
            ActionComplete();
            return;
        }

        ActionStart(onActionComplete);
        ActionPointConsumption();
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
