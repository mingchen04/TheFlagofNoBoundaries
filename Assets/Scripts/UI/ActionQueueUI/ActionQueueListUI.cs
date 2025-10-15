using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ActionQueueListUI : MonoBehaviour
{
    [SerializeField] private Transform actionQueueScrollView;
    [SerializeField] private Transform actionQueueList;
    [SerializeField] private TextMeshProUGUI unitName;
    [SerializeField] private BaseActionParameterUI baseActionParameterPre;
    [SerializeField] private Transform placeholderTransform; //占位符对象
    private Unit unit;

    public void SetUp(Unit unit)
    {
        this.unit = unit;
        ActionQueue actionQueue = unit.GetActionQueue();
        UpdateVisual();
    }

    private void UpdateVisual()
    {
        if (unit == null) return;
        unitName.text = unit.name;
        //销毁队列中的按钮
        foreach (Transform baseActionButton in actionQueueList)
        {
            Destroy(baseActionButton.gameObject);
        }

        //生成选中单位的行为队列UI
        ActionQueue selectedUnitActionQueue = unit.GetActionQueue();
        List<BaseActionParameter> baseActionParameters = selectedUnitActionQueue.GetActions();
        BaseActionParameter selectUnitLostAction = selectedUnitActionQueue.GetSlowAction(); //获取选中单位的丢失动作
        if (selectUnitLostAction != null)
        {
            //baseActionParameters.Insert(0, selectUnitLostAction);
        }
        
        for (int index = 0; index < baseActionParameters.Count; index++)
        {
            BaseActionParameter lastBaseActionParameter = index > 0 ? baseActionParameters[index - 1] : null;
            BaseActionParameter baseActionParameter = baseActionParameters[index];

            if (baseActionParameter.executeTurn > index)
            {
                //如果当前动作的执行回合数大于当前索引，则需要在队列中添加占位符
                int executeTurnDifference = index > 0 ?
                    (baseActionParameter.executeTurn - lastBaseActionParameter.executeTurn - 1) //计算当前动作与上一个动作之间的占位符数量
                    :
                    (baseActionParameter.executeTurn - index);

                for (int placeholderIndex = 0; placeholderIndex < executeTurnDifference; placeholderIndex++)
                {
                    //在队列中添加占位符
                    Instantiate(placeholderTransform, actionQueueList);
                }
            }

            Sprite baseActionIcon = ScriptiptableSOManager.Instance.GetActionIcon(baseActionParameter.baseAction);
            ActionSpeed actionSpeed = baseActionParameter.actionSpeed;
            BaseActionParameterUI baseActionUI = Instantiate(baseActionParameterPre, actionQueueList);

            int currentIndex = index;// 保持当前索引的副本，避免闭包问题
            baseActionUI.SetUp
                (
                    actionSpeed,
                    baseActionIcon,
                    () => { unit.CancelAction(currentIndex, baseActionParameter.baseAction); },
                    baseActionParameter.executeTurn,
                    index,
                    selectedUnitActionQueue,
                    UpdateVisual
                );
        }

        //启动协程等待两帧更新Transform大小再进行滚动至最右侧显示最新动作
        StartCoroutine(DelayedScroll());
    }


    private IEnumerator DelayedScroll()
    {
        // 等待两帧确保布局更新
        yield return null; // 第一帧：ContentSizeFitter计算
        yield return null; // 第二帧：ScrollRect应用新尺寸
        if (actionQueueScrollView != null)
        {
            actionQueueScrollView.GetComponent<ScrollRect>().horizontalNormalizedPosition = 1; // 0=最右，1=最左
        }
    }
}
