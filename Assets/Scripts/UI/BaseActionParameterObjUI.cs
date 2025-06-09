using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BaseActionParameterObjUI : MonoBehaviour
{
    [SerializeField] private Button actionDetailsButton;
    [SerializeField] private Button cancelButton;
    [SerializeField] private Image actionIcon;
    [SerializeField] private TextMeshProUGUI textActionIndex;
    [SerializeField] private Button orderForwardButton; //顺序向前调整
    [SerializeField] private Button orderBackButton; //顺序向后调整



    /// <summary>
    /// 设置UI基本参数
    /// </summary>
    /// <param name="ActionSprite">行动图标</param>
    /// <param name="cancelAction">行动取消事件</param>
    /// <param name="actionQueue"> 行为队列</param>
    /// <param name="actionIndex">行动位置</param>
    /// <param name="executeTurn">行动执行回合</param>
    public void SetUp(Sprite ActionSprite, Action cancelAction, int executeTurn, int actionIndex, ActionQueue actionQueue)
    {
        actionIcon.sprite = ActionSprite;
        cancelButton.onClick.AddListener(() => { cancelAction(); });
        SetExecute(executeTurn, actionQueue, actionIndex);

        orderForwardButton.onClick.AddListener(() =>
        {
            executeTurn -= 1; //将当前行动位置向前调整
            SetExecute(executeTurn, actionQueue, actionIndex);
        });

        orderBackButton.onClick.AddListener(() =>
        {
            executeTurn += 1; //将当前行动位置向后调整
            SetExecute(executeTurn, actionQueue, actionIndex);
        });
    }

    /// <summary>
    /// 设置行动执行回合
    /// </summary>
    /// <param name="executeTurn">行动顺序</param>
    /// <param name="actionQueue">行动队列</param>
    /// <param name="actionIndex">动作位置</param>
    private void SetExecute(int executeTurn, ActionQueue actionQueue, int actionIndex)
    {
        actionQueue.ActionExecuteTurnSet(actionIndex, executeTurn);
        textActionIndex.text = (executeTurn+1).ToString();

        if (executeTurn == 0)
        {
            orderForwardButton.interactable = false; //如果行动位置为0则不能向前调整
        }
    }
}
