using System;
using System.Collections.Generic;
using UnityEngine;

public class ShootAction : BaseAction
{
    public static event EventHandler<OnShootEventArgs> OnAnyShoot;
    public event EventHandler<OnShootEventArgs> OnShoot;
    public class OnShootEventArgs : EventArgs
    {
        public GridPosition targetUnitPosition;
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
    private GridPosition targetUnitPosition;
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
                Vector3 shootDirection = ((Vector3)targetUnitPosition - transform.position).normalized;
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
            targetUnitPosition = targetUnitPosition,
            shootingUnit = unit,
        });

        OnShoot?.Invoke(this, new OnShootEventArgs()
        {
            targetUnitPosition = targetUnitPosition,
            shootingUnit = unit,
        });

        LevelGrid.Instance.DamageUnitForGridPosition(targetUnitPosition, unit.GetAttack());
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

        HashSet<GridPosition> gridPositions =
            FogOfWarManager.Instance.ComputeHexShadowcastingSector(
                unitGridPosition,
                maxShootDistance,
                pos =>
                {
                    /*障碍物判定*/
                    return !Pathfinding.Instance.IsWalkableGridPosition(pos);
                }
            );

        validActionGridPositionList.AddRange(gridPositions);

        return validActionGridPositionList;
    }

    /// <summary>
    /// 获取所有的可以攻击到单位的网格坐标集合
    /// </summary>
    /// <param name="unitGridPosition"></param>
    /// <returns></returns>
    public List<GridPosition> GetEffectiveUnitGridPositionList(GridPosition unitGridPosition)
    {
        List<GridPosition> gridPositions = GetValidActionGridPositionList(unitGridPosition);
        gridPositions.RemoveAll(gridPosition =>
        {

            if (LevelGrid.Instance.HasAnyUnitOnGridPosition(gridPosition))
            {
                Unit targetUnit = LevelGrid.Instance.GetUnitAtGridPosition(gridPosition);
                if (targetUnit.IsEnemy() == unit.IsEnemy())
                {
                    return true;
                }
                return false;
            }

            //不存在单位则删除
            return true;
        });

        return gridPositions;
    }

    public override void TakeAction(BaseActionParameter baseActionParameter, Action onActionComplete)
    {
        List<GridPosition> gridPositions = baseActionParameter.targetPositionList;
        targetUnitPosition = gridPositions[0];

        state = State.Aiming;
        float aimingStateTime = 1f;
        stateTimer = aimingStateTime;
        canShootBullet = true;

        ActionStart(onActionComplete);
        unit.TrySpendActionPointsToTakeAction(this);
    }

    public GridPosition GetTargetUnitPosition()
    {
        return targetUnitPosition;
    }

    public int GetMaxShootDistance()
    {
        return maxShootDistance;
    }

    public int GetTargetCountAtPosition(GridPosition gridPosition)
    {
        return GetEffectiveUnitGridPositionList(gridPosition).Count;
    }

    public override EnemyAIAction GetEnemyAIAction(GridPosition gridPosition)
    {
        Unit targetUnit = LevelGrid.Instance.GetUnitAtGridPosition(gridPosition);
        int actionValue = 0;
        //单位不为空且是敌人
        if (targetUnit != null && targetUnit.IsEnemy() != unit.IsEnemy())
        {
            //优先攻击可以击杀的单位
            actionValue += 100 + Mathf.RoundToInt((1 - targetUnit.GetHealthNormalized()) * 100f);
        }

        return new EnemyAIAction
        {
            gridPosition = gridPosition,
            actionValue = actionValue,
        };
    }
}
