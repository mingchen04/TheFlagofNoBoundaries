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
        if (!TurnSystem.Instance.IsPlayerTrun())
        {
            state = State.TakingTurn;
            timer = 2f;
        }

    }

    private void Update()
    {
        if (TurnSystem.Instance.IsPlayerTrun())
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
                        //TurnSystem.Instance.NextTurn();
                        UnitActionSystem.Instance.ExecuteAllQueuedActions();
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
}
