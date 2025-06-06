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

    /// <summary>
    /// 设置UI基本参数
    /// </summary>
    /// <param name="ActionSprite">行动图标</param>
    /// <param name="cancelAction">行动取消事件</param>
    /// <param name="actionIndex">行动位置</param>
    public void setUp(Sprite ActionSprite, Action cancelAction,int actionIndex)
    {
        actionIcon.sprite = ActionSprite;
        cancelButton.onClick.AddListener(() => { Debug.Log("杀杀杀"); cancelAction(); });
        textActionIndex.text = actionIndex.ToString();
    }
}
