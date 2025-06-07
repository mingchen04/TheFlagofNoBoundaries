using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SocialPlatforms;

public class ShootAction : BaseAction
{
    public static event EventHandler<OnShootEventArgs> OnAnyShoot;
    public event EventHandler<OnShootEventArgs> OnShoot;
    public class OnShootEventArgs : EventArgs
    {
        public Unit targetUnit;
        public Unit shootingUnit;
    }
    private enum State
    {
        Aiming,
        Shooting,
        Cooloff,
    }

    [SerializeField] private int maxShootDistance = 2;
    [SerializeField] private LayerMask obstaclesLayerMask;

    private State state;
    private float stateTimer;
    private Unit targetUnit;
    private bool canShootBullet;

    private void Update()
    {
        if (!isActive)
        {
            return;
        }

        stateTimer -= Time.deltaTime;

        switch (state)
        {
            case State.Aiming:
                float rotateSpeed = 10f;
                Vector3 shootDirection = (targetUnit.GetWorldPosition() - transform.position).normalized;
                transform.forward = Vector3.Slerp(transform.forward, shootDirection, rotateSpeed * Time.deltaTime);
                break;
            case State.Shooting:
                if (canShootBullet)
                {
                    Shoot();
                    canShootBullet = false;
                }
                break;
            case State.Cooloff:
                break;
        }

        if (stateTimer <= 0f)
        {
            NextState();
        }

    }

    private void Shoot()
    {
        OnAnyShoot?.Invoke(this, new OnShootEventArgs()
        {
            targetUnit = targetUnit,
            shootingUnit = unit,
        });

        OnShoot?.Invoke(this, new OnShootEventArgs()
        {
            targetUnit = targetUnit,
            shootingUnit = unit,
        });

        targetUnit.Damage(40);
    }

    private void NextState()
    {
        switch (state)
        {
            case State.Aiming:
                state = State.Shooting;
                float shootingStateTime = 0.1f;
                stateTimer = shootingStateTime;
                break;
            case State.Shooting:

                state = State.Cooloff;
                float coolOffStateTime = 0.5f;
                stateTimer = coolOffStateTime;
                break;
            case State.Cooloff:
                ActionComplete();
                break;
        }
    }

    public override string GetActionName()
    {
        return "射击";
    }

    public override List<GridPosition> GetValidActionGridPositionList()
    {
        GridPosition unitGridPosition = unit.GetGridPosition();
        return GetValidActionGridPositionList(unitGridPosition);
    }

    public List<GridPosition> GetValidActionGridPositionList(GridPosition unitGridPosition)
    {
        List<GridPosition> validActionGridPositionList = new List<GridPosition>();

        Vector3 unitWorldPos = unit.GetWorldPosition();
        float unitShoulderHeight = 1.7f;
        Vector3 unitShoulderPos = unitWorldPos + Vector3.up * unitShoulderHeight;
        bool isUnitEnemy = unit.IsEnemy();
        LevelGrid levelGrid = LevelGrid.Instance;

        List<GridPosition> gridPositionList = new List<GridPosition>();

        foreach (GridPosition testGridPosition in LevelGrid.Instance.GenerateHexArea(unitGridPosition, maxShootDistance))
        {
            if (!levelGrid.IsValidGridPosition(testGridPosition))
            {
                //网格位置不合法跳过
                continue;
            }

            Unit targetUnit = levelGrid.GetUnitAtGridPosition(testGridPosition);
            if (targetUnit == null || targetUnit.IsEnemy() == isUnitEnemy)
            {
                continue;
            }  

            Vector3 unitWorldPosition = levelGrid.GetWorldPosition(unitGridPosition);
            Vector3 shootDir = (targetUnit.GetWorldPosition() - unitWorldPosition).normalized;

            //发射射线检测途中有无障碍物
            if (Physics.Raycast(unitShoulderPos, //射线开始位置
                shootDir, //射线方向
                Vector3.Distance(unitWorldPosition, targetUnit.GetWorldPosition()), //射线距离
                obstaclesLayerMask)) //射线命中图层
            {
                continue;
            }

            validActionGridPositionList.Add(testGridPosition);
        }

        return validActionGridPositionList;
    }

    public override void TakeAction(GridPosition gridPosition, Action ClearBusy)
    {
        targetUnit = LevelGrid.Instance.GetUnitAtGridPosition(gridPosition);
        if (targetUnit == null)
        {
            return;
        }

        state = State.Aiming;
        float aimingStateTime = 1f;
        stateTimer = aimingStateTime;
        canShootBullet = true;

        ActionStart(ClearBusy);
        ActionPointConsumption();
    }

    public Unit GetTargetUnit()
    {
        return targetUnit;
    }

    public int GetMaxShootDistance()
    {
        return maxShootDistance;
    }

    public int GetTargetCountAtPosition(GridPosition gridPosition)
    {
        return GetValidActionGridPositionList(gridPosition).Count;
    }

    public override EnemyAIAction GetEnemyAIAction(GridPosition gridPosition)
    {
        targetUnit = LevelGrid.Instance.GetUnitAtGridPosition(gridPosition);

        return new EnemyAIAction
        {
            gridPosition = gridPosition,
            actionValue = 100 + Mathf.RoundToInt((1 - targetUnit.GetHealthNormalized()) * 100f),
        };
    }
}
