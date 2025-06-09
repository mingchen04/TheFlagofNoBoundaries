using System.Collections.Generic;
using UnityEngine;

public class ActionQueueUI : MonoBehaviour
{
    [SerializeField] private Transform ActionQueueScrollView;

    private void Start()
    {
        UnitActionSystem.Instance.OnActionAddQueue += UnitActionSystem_OnActionAddQueue;
        UnitActionSystem.Instance.OnSelectedUnitChanged += UnitActionSystem_OnSelectedUnitChanged;
        ActionQueue.OnAnyActionCompleted += ActionQueue_OnAnyActionCompleted;

        UpdateVisual();
    }

    private void UpdateVisual()
    {
        //销毁所有队列
        foreach (Transform ActionQueueListView in transform.Find("ActionQueueGrid"))
        {
            Destroy(ActionQueueListView.gameObject);
        }

        List<Unit> selectedUnits = UnitActionSystem.Instance.GetSelectedUnits();
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
        Transform actionQueueListTransform = Instantiate(ActionQueueScrollView, transform.Find("ActionQueueGrid"));
        ActionQueueListUI actionQueueListUI = actionQueueListTransform.GetComponent<ActionQueueListUI>();
        actionQueueListUI.SetUp(unit);
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
}
