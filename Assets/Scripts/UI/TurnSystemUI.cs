using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TurnSystemUI : MonoBehaviour
{
    [SerializeField] private Button nextTurnButton; //回合开始
    [SerializeField] private Button counterTurnButton; //反制回合
    [SerializeField] private Button finishTurnButton; //结束回合
    [SerializeField] private TextMeshProUGUI turnNumberText;
    [SerializeField] private TextMeshProUGUI enemyTurnVisualText; //回合状态显示文本


    private void Start()
    {
        nextTurnButton.onClick.AddListener(() =>
        {
            HeidAllButton();
            TurnSystem.Instance.NextTurn();
        });

        finishTurnButton.onClick.AddListener(() =>
        {
            HeidAllButton();
            StartCoroutine(TurnSystem.Instance.EndTurn());
        });

        counterTurnButton.onClick.AddListener(() =>
        {
            HeidAllButton();
            TurnSystem.Instance.EnterCounterTurn();
        });


        TurnSystem.Instance.OnTurnChanged += TurnSystem_OnTurnChanged;
        UpdateTurnText();
        UpdateEnemyTurnVisual();
        UpdateEndTurnButtonVisibility();
    }

    private void OnDestroy()
    {
        if (TurnSystem.Instance != null)
        {
            TurnSystem.Instance.OnTurnChanged -= TurnSystem_OnTurnChanged;
        }
    }

    private void TurnSystem_OnTurnChanged(object sender, System.EventArgs e)
    {
        UpdateTurnText();
        UpdateEnemyTurnVisual();
        UpdateEndTurnButtonVisibility();
    }

    public void UpdateTurnText()
    {
        turnNumberText.text = $"回合 {TurnSystem.Instance.GetTurnNumber()}";
    }

    private void UpdateEnemyTurnVisual()
    {
        TurnSystem.TurnState turnState = TurnSystem.Instance.GetTurnState();

        string turnVisualText = turnState.ToString();
        string party = TurnSystem.Instance.GetActingParty().ToString();
        switch (turnState)
        {
            case TurnSystem.TurnState.Main:
                turnVisualText = party + "回合";
                break;
            case TurnSystem.TurnState.WaitCounter:
                turnVisualText = "等待反制";
                break;
            case TurnSystem.TurnState.CounterTurn:
                turnVisualText = party + "反制回合";
                break;
        }

        enemyTurnVisualText.text = turnVisualText;
    }

    private void UpdateEndTurnButtonVisibility()
    {
        //玩家回合显示按钮 非玩家回合隐藏按钮
        TurnSystem.TurnState turnState = TurnSystem.Instance.GetTurnState();
        bool isPlayerTurn = TurnSystem.Instance.IsPlayerTurn();

        if (turnState != TurnSystem.TurnState.WaitCounter &&
            TurnSystem.Instance.GetActingParty() == TurnSystem.Party.Player)
        {
            //主回合和反制回合时显示
            nextTurnButton.gameObject.SetActive(true);
        }
        else
        {
            nextTurnButton.gameObject.SetActive(false);
        }

        //等待反制回合状态且非玩家回合时显示反制按钮和结束按钮
        if (turnState == TurnSystem.TurnState.WaitCounter &&
            TurnSystem.Instance.GetActingParty() != TurnSystem.Party.Player)
        {
            counterTurnButton.gameObject.SetActive(true);
            finishTurnButton.gameObject.SetActive(true);
        }
        else
        {
            counterTurnButton.gameObject.SetActive(false);
            finishTurnButton.gameObject.SetActive(false);
        }
    }

    private void HeidAllButton()
    {
        nextTurnButton.gameObject.SetActive(false);
        counterTurnButton.gameObject.SetActive(false);
        finishTurnButton.gameObject.SetActive(false);
    }
}
