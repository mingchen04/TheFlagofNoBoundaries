using System;
using System.Collections;
using UnityEngine;

public class TurnSystem : MonoBehaviour
{
    public static TurnSystem Instance { get; private set; }
    public event EventHandler OnTurnChanged;
    private int turnNumber = 1; //回合数
    private bool isPlayerTurn = true;
    private Party actingParty = Party.Player; //当前行动阵营
    private TurnState turnState; //回合状态
    [SerializeField] private EnemyAI enemyAI;

    public enum TurnState
    {
        Main, //主回合
        WaitCounter, //等待反制回合
        CounterTurn, //反制回合
    }

    public enum Party
    {
        Player,
        Enemy,
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
        //检查当前行动方 是否为玩家
        bool isPlayerTurn = actingParty == Party.Player;


        bool firstDone = false;
        UnitActionSystem.Instance.ExecuteAllQueuedActions(isPlayerTurn, () => { firstDone = true; });
        yield return new WaitUntil(() => firstDone);

        bool secondDone = false;
        UnitActionSystem.Instance.ExecuteAllQueuedActions(!isPlayerTurn, () => { secondDone = true; });
        yield return new WaitUntil(() => secondDone);

        if (turnState != TurnState.WaitCounter)
        {
            turnState = TurnState.WaitCounter;

            if (isPlayerTurn)//非敌方回合敌方计算反制回合优先级
            {
                Debug.Log("等待敌方反制回合");
                if (enemyAI.ShouldEnterCounterTurn())
                {
                    EnterCounterTurn(Party.Enemy);
                    yield break;
                }
            }
            else //非玩家回合让玩家选择是否进入反制回合
            {
                Debug.Log("等待我方反制回合");
                OnTurnChanged?.Invoke(this, EventArgs.Empty);
                yield break;
            }
        }

        StartCoroutine(EndTurn());
    }

    public IEnumerator EndTurn()
    {
        bool actionsDone = false;

        Debug.Log("回合结束");
        ExecuteAllQueuedActions(actingParty == Party.Player, () => { actionsDone = true; });

        yield return new WaitUntil(() => actionsDone);

        turnState = TurnState.Main;
        turnNumber++;
        isPlayerTurn = !isPlayerTurn;

        if (isPlayerTurn) { actingParty = Party.Player; }
        else { actingParty = Party.Enemy; }


        OnTurnChanged?.Invoke(this, EventArgs.Empty);
    }

    private void ExecuteAllQueuedActions(bool playerTurn, Action actionComplete = null)
    {
        UnitActionSystem.Instance.ExecuteAllQueuedActions(playerTurn, actionComplete);
    }

    /// <summary>
    /// 进入反制回合
    /// </summary>
    /// <param name="actingParty">进入反制回合的一方</param>
    public void EnterCounterTurn(Party actingParty = Party.Player)
    {
        Debug.Log("反制回合");
        this.actingParty = actingParty;
        turnState = TurnState.CounterTurn;
        OnTurnChanged?.Invoke(this, EventArgs.Empty);
    }

    public TurnState GetTurnState()
    {
        return turnState;
    }
    public int GetTurnNumber()
    {
        return turnNumber;
    }

    //判断是否为玩家回合
    public bool IsPlayerTurn()
    {
        return isPlayerTurn;
    }

    //获取当前行动阵营
    public Party GetActingParty()
    {
        return actingParty;
    }

    public bool IsCounterTurn()
    {
        return turnState == TurnState.CounterTurn;
    }
}
