using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ActionQueueUI : MonoBehaviour
{
    [SerializeField] private BaseActionParameterObjUI baseActionParameterPre;
    [SerializeField] private Transform ActionQueueScrollView;

    private void Start()
    {
        UnitActionSystem.Instance.OnActionAddQueue += UnitActionSystem_OnActionAddQueue;
        UnitActionSystem.Instance.OnSelectedUnitChanged += UnitActionSystem_OnSelectedUnitChanged;
        ActionQueue.OnAnyActionCompleted += ActionQueue_OnAnyActionCompleted;

        UpdateVisual();
    }

    private void ActionQueue_OnAnyActionCompleted(object sender, System.EventArgs e)
    {
        UpdateVisual();
    }

    private void UnitActionSystem_OnSelectedUnitChanged(object sender, System.EventArgs e)
    {
        UpdateVisual();
    }


    private void UnitActionSystem_OnActionAddQueue(object sender, System.EventArgs e)
    {
        UpdateVisual();
    }

    private void UpdateVisual()
    {
        //销毁所有队列
        foreach (Transform ActionQueueView in transform)
        {
            Destroy(ActionQueueView.gameObject);
        }

        List<Unit> selectedUnits = UnitActionSystem.Instance.GetSelectedUnits();
        if (selectedUnits.Count == 0) return;

        foreach (Unit unit in selectedUnits)
        {
            GenerateActionQueue(unit);
        }
    }

    /// <summary>
    /// 生成队列
    /// </summary>
    /// <param name="unit">根据Unit行动内容生成队列</param>
    private void GenerateActionQueue(Unit unit)
    {
        //生成选中单位的行为队列UI
        Transform selectedUnit_actionQueueScrollView = Instantiate(ActionQueueScrollView, transform);
        Transform actionQueueList = selectedUnit_actionQueueScrollView.Find("ActionQueueList");
        TextMeshProUGUI unitName = selectedUnit_actionQueueScrollView.Find("UnitName/UnitNameText").GetComponent<TextMeshProUGUI>();
        unitName.text = unit.name;

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
        StartCoroutine(DelayedScroll(selectedUnit_actionQueueScrollView));
    }

    private IEnumerator DelayedScroll(Transform actionQueueScrollView)
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
