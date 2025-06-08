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
    /// <param name="actionIndex">行动位置</param>
    public void setUp(Sprite ActionSprite, Action cancelAction,int actionIndex)
    {
        actionIcon.sprite = ActionSprite;
        cancelButton.onClick.AddListener(() => {cancelAction(); });
        textActionIndex.text = (actionIndex + 1).ToString();
    }
}
