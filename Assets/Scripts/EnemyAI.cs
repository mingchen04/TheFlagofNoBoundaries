using System;
using UnityEngine;

public class EnemyAI : MonoBehaviour
{
    private enum State
    {
        WaitingForEnemyTurn,
        TakingTurn,
        Busy,
    }

    private State state;
    private float timer;

    private void Awake()
    {
        state = State.WaitingForEnemyTurn;
    }

    private void Start()
    {
        TurnSystem.Instance.OnTurnChanged += TurnSystem_OnTurnChanged;
    }

    private void TurnSystem_OnTurnChanged(object sender, System.EventArgs e)
    {
        if (TurnSystem.Instance.GetActingParty() == TurnSystem.Party.Enemy
            &&
            TurnSystem.Instance.GetTurnState() != TurnSystem.TurnState.WaitCounter)
        {
            state = State.TakingTurn;
            timer = 2f;
        }

    }

    private void Update()
    {
        if (TurnSystem.Instance.GetActingParty() != TurnSystem.Party.Enemy)
        {
            return;
        }

        switch (state)
        {
            case State.WaitingForEnemyTurn:
                break;
            case State.TakingTurn:
                timer -= Time.deltaTime;
                if (timer <= 0)
                {
                    state = State.Busy;
                    if (TryTakeEnemyAIAction(SetStateTakingTurn))
                    {
                        state = State.TakingTurn;
                    }
                    else
                    {
                        // 敌方单位无法继续行动
                        TurnSystem.Instance.NextTurn();
                    }
                }
                break;
            case State.Busy:
                break;
        }
    }

    private void SetStateTakingTurn()
    {
        timer = 0.5f;
        state = State.TakingTurn;
    }

    private bool TryTakeEnemyAIAction(Action onEnemyAIActionComplete)
    {
        foreach (Unit enemyUnit in UnitManager.Instance.GetEnemyUnitList())
        {
            if (enemyUnit.GetActionQueue().IsActionQueueFull())
            {
                //行动队列满了
                continue;
            }

            if (TryTakeEnemyAIAction(enemyUnit, onEnemyAIActionComplete))
            {
                return true;
            }
        }

        return false;
    }

    private bool TryTakeEnemyAIAction(Unit enemyUnit, Action onEnemyAIActionComplete)
    {
        EnemyAIAction bestEnemyAIAction = null;
        BaseAction bestBaseAction = null;

        foreach (BaseAction baseAction in enemyUnit.GetBaseActionArray())
        {
            if (!enemyUnit.CanspendActionPointsToTakeAction(baseAction.GetActionPointsCost()))
            {
                //敌人行动点数不够
                continue;
            }

            if (bestEnemyAIAction == null)
            {
                bestEnemyAIAction = baseAction.GetBestEnemyAIAction();
                bestBaseAction = baseAction;
            }
            else
            {
                EnemyAIAction testEnemyAIAction = baseAction.GetBestEnemyAIAction();

                //根据优先值选择行动
                if (testEnemyAIAction != null && testEnemyAIAction.actionValue > bestEnemyAIAction.actionValue)
                {
                    bestEnemyAIAction = testEnemyAIAction;
                    bestBaseAction = baseAction;
                }
            }
        }

        if (bestEnemyAIAction != null && enemyUnit.TrySpendActionPointsToTakeAction(bestBaseAction, null))
        {
            int ActionQueueOrder = enemyUnit.GetActionQueue().EndActionExecuteTurnGet();
            bool isDeploy = bestBaseAction.GetActionTags().HasFlag(ActionTags.Deploy);
            bool ActionQueueEndAction = false;
            int endOrder;

            BaseActionParameter lastActionPar = enemyUnit.GetActionQueue().CopyLastActionParameter();

            if (lastActionPar != null)
            {
                ActionQueueEndAction = lastActionPar.baseAction == bestBaseAction;
            }

            if (ActionQueueEndAction && isDeploy)
            {
                endOrder = enemyUnit.GetActionQueue().EndActionExecuteTurnGet();
            }
            else
            {
                endOrder = ActionQueueOrder + 1;
            }

            enemyUnit.HandleOrderedAction(bestBaseAction, bestEnemyAIAction.gridPosition, endOrder);
            onEnemyAIActionComplete?.Invoke();
            //bestBaseAction.TakeAction(new List<GridPosition> { bestEnemyAIAction.gridPosition }, onEnemyAIActionComplete);
            return true;
        }
        else
        {
            return false;
        }
    }

    /// <summary>
    /// 计算是否进入反制回合 当单位体力大于20并且距离4以内有敌方单位时返回真
    /// </summary>
    public bool ShouldEnterCounterTurn()
    {
        foreach (Unit enemyUnit in UnitManager.Instance.GetEnemyUnitList())
        {
            if (enemyUnit.GetActionPoints() > 20 && HasPlayerInRange(enemyUnit))
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// 检查周围是否有玩家单位
    /// </summary>    
    private bool HasPlayerInRange(Unit enemyUnit)
    {
        foreach (Unit playerUnit in UnitManager.Instance.GetFriendlyUnitList())
        {
            GridPosition enemyPosition = enemyUnit.GetGridPosition();
            GridPosition playerPosition = playerUnit.GetGridPosition();

            if (Vector3.Distance(enemyPosition, playerPosition) < 4)
            {
                int pathfindingDistanceMultiplier = 10;
                int PathLength = Pathfinding.Instance.GetPathLength(enemyPosition, playerPosition,
                    new Pathfinding.PathfindingRules[] { Pathfinding.PathfindingRules.Walkable })
                    / pathfindingDistanceMultiplier;
                Debug.Log("距离小于4 检查路径长度" + PathLength);
                Debug.Log($"{enemyUnit.name} 坐标 {enemyPosition} - {playerUnit.name} 坐标 {playerPosition}");

                if (PathLength <= 2)
                {
                    return true;
                }
            }
        }
        return false;
    }
}
