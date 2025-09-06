using System;
using System.Collections;
using UnityEngine;

public class TurnSystem : MonoBehaviour
{
    public static TurnSystem Instance { get; private set; }
    public event EventHandler OnTurnChanged;
    private int turnNumber = 1; //回合数
    private bool isPlayerTurn = true;
    private TurnState turnState; //回合状态

    public enum TurnState
    {
        Main, //主回合
        WaitCounter, //等待反制回合
        CounterTurn, //反制回合
    }

    private void Awake()
    {
        if (Instance != null)
        {
            Debug.Log("There's more than one TurnSystem! " + transform + " - " + Instance);
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        turnState = TurnState.Main;
    }

    public void NextTurn()
    {
        StartCoroutine(NextTurnCoroutine());
    }

    private IEnumerator NextTurnCoroutine()
    {
        bool firstDone = false;
        UnitActionSystem.Instance.ExecuteAllQueuedActions(IsPlayerTurn(), () => { firstDone = true; });
        yield return new WaitUntil(() => firstDone);

        bool secondDone = false;
        UnitActionSystem.Instance.ExecuteAllQueuedActions(!IsPlayerTurn(), () => { secondDone = true; });
        yield return new WaitUntil(() => secondDone);

        if (turnState != TurnState.WaitCounter && turnState != TurnState.CounterTurn)
        {
            if (!IsPlayerTurn())
            {
                Debug.Log("反制");
                turnState = TurnState.WaitCounter;
                OnTurnChanged?.Invoke(this, EventArgs.Empty);
                yield break;
            }
        }

        EndTurn();
    }

    public int GetTurnNumber()
    {
        return turnNumber;
    }

    //判断是否为玩家回合
    public bool IsPlayerTurn()
    {
        if (turnState == TurnState.CounterTurn || turnState == TurnState.WaitCounter)
        {
            return !isPlayerTurn;
        }
        return isPlayerTurn;
    }

    public void EndTurn()
    {
        Debug.Log("回合结束");
        turnState = TurnState.Main;
        turnNumber++;
        //若不进入反制回合则直接执行慢速动作并切换阵营
        UnitActionSystem.Instance.ExecuteAllQueuedActions(IsPlayerTurn());

        isPlayerTurn = !isPlayerTurn;
        OnTurnChanged?.Invoke(this, EventArgs.Empty);
    }

    //反制回合
    public void CounterTurnNext()
    {
        Debug.Log("反制回合");
        turnState = TurnState.CounterTurn;
        OnTurnChanged?.Invoke(this, EventArgs.Empty);
    }

    public TurnState GetTurnState()
    {
        return turnState;
    }
}
