using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TurnSystemUI : MonoBehaviour
{
    [SerializeField] private Button endTurnButton;
    [SerializeField] private Button counterTurnButton; //反制回合
    [SerializeField] private Button finishTurnButton; //结束回合
    [SerializeField] private TextMeshProUGUI turnNumberText;
    [SerializeField] private TextMeshProUGUI enemyTurnVisualText; //回合状态显示文本


    private void Start()
    {
        endTurnButton.onClick.AddListener(() =>
        {
            TurnSystem.Instance.NextTurn();
        });

        finishTurnButton.onClick.AddListener(() =>
        {
            TurnSystem.Instance.EndTurn();
        });

        counterTurnButton.onClick.AddListener(() =>
        {
            TurnSystem.Instance.CounterTurnNext();
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
        enemyTurnVisualText.text = TurnSystem.Instance.IsPlayerTurn() ? "我方回合" : "敌方回合";
    }

    private void UpdateEndTurnButtonVisibility()
    {
        //玩家回合显示按钮 非玩家回合隐藏按钮
        TurnSystem.TurnState turnState = TurnSystem.Instance.GetTurnState();
        bool isPlayerTurn = TurnSystem.Instance.IsPlayerTurn();

        if (turnState != TurnSystem.TurnState.WaitCounter) 
        { 
            //反制回合 和 主回合时显示
            endTurnButton.gameObject.SetActive(isPlayerTurn);
        }
        else
        {
            //等待反制回合时隐藏结束按钮
            endTurnButton.gameObject.SetActive(!isPlayerTurn);
        }

        //等待反制回合状态且非玩家回合时显示反制按钮和结束按钮
        if (turnState == TurnSystem.TurnState.WaitCounter && isPlayerTurn)
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
}
