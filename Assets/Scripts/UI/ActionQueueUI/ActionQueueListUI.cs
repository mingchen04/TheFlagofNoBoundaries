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
    [SerializeField] private BaseActionParameterObjUI baseActionParameterPre;

    private Unit unit;

    public void SetUp(Unit unit)
    {
        this.unit = unit;
        UpdateVisual();
    }

    private void UpdateVisual()
    {
        unitName.text = unit.name;

        //生成选中单位的行为队列UI

        //销毁队列中的按钮
        foreach (Transform baseActionButton in actionQueueList)
        {
            Destroy(baseActionButton.gameObject);
        }

        ActionQueue selectedUnitActionQueue = unit.GetActionQueue();
        List<BaseActionParameter> baseActionParameters = selectedUnitActionQueue.GetActions();

        for (int index = 0; index < baseActionParameters.Count; index++)
        {
            int currentIndex = index;
            BaseActionParameter baseActionParameter = baseActionParameters[index];
            Sprite baseActionIcon = ScriptiptableSOManager.Instance.GetActionIcon(baseActionParameter.baseAction);
            BaseActionParameterObjUI baseActionUI = Instantiate(baseActionParameterPre, actionQueueList);

            baseActionUI.SetUp
                (
                    baseActionIcon,
                    () =>
                    {
                        unit.CancelAction(currentIndex, baseActionParameter.baseAction);
                        UpdateVisual();
                    },
                    baseActionParameter.executeTurn,
                    index,
                    selectedUnitActionQueue
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
