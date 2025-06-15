using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class UnitActionSystemUI : MonoBehaviour
{
    [SerializeField] private Transform actionButtonContainerTransform;
    [SerializeField] private Transform actionButtonPrefab;
    [SerializeField] private TextMeshProUGUI actionPointsText;
    private List<ActionButtonUI> actionButtonUILIst;

    private void Awake()
    {
        actionButtonUILIst = new List<ActionButtonUI>();
    }

    private void Start()
    {
        UnitActionSystem.Instance.OnSelectedUnitChanged += UnitActionSystem_OnSelectedUnitChanged;
        UnitActionSystem.Instance.OnSelectActionChanged += UnitActionSystem_OnSelectActionChanged;
        //UnitActionSystem.Instance.OnActionStarted += UnitActionSystem_OnActionStarted;

        //无法确定脚本执行顺序可能导致数据与视图不同步
        //方法一 调整项目脚本执行顺序 方法二 设置特定监听事件 OnAnyActionPointsChanged
        //TurnSystem.Instance.OnTurnChanged += TurnSystem_OnTurnChanged; 
        SrengthSystem.OnAnyActionPointsChanged += Unit_OnAnyActionPointsChanged;

        CreateUnitActionbuttons();
        UpdateSelectedVisual();
        UpdateActionPoints();
    }

    private void Unit_OnAnyActionPointsChanged(object sender, EventArgs e)
    {
        UpdateActionPoints();
    }

    private void UnitActionSystem_OnActionStarted(object sender, EventArgs e)
    {
        UpdateActionPoints();
    }

    private void UnitActionSystem_OnSelectActionChanged(object sender, System.EventArgs e)
    {
        UpdateSelectedVisual();
    }

    private void UnitActionSystem_OnSelectedUnitChanged(object sender, System.EventArgs e)
    {
        CreateUnitActionbuttons();
        UpdateSelectedVisual();
        UpdateActionPoints();
    }

    private void CreateUnitActionbuttons()
    {

        foreach (Transform buttonTransform in actionButtonContainerTransform)
        {
            Destroy(buttonTransform.gameObject);
        }
        actionButtonUILIst.Clear();

        Unit selectedUnit = UnitActionSystem.Instance.GetSelectedUnit();
        if (selectedUnit == null) return;
        foreach (BaseAction baseAction in selectedUnit.GetBaseActionArray())
        {
            Transform actionButtonTransform = Instantiate(actionButtonPrefab, actionButtonContainerTransform);
            ActionButtonUI actionButtonUI = actionButtonTransform.GetComponent<ActionButtonUI>();
            actionButtonUILIst.Add(actionButtonUI);
            actionButtonUI.SetBaseActionImage(ScriptiptableSOManager.Instance.GetActionIcon(baseAction));
            actionButtonUI.SetBaseAction(baseAction);
        }
    }

    private void UpdateSelectedVisual()
    {
        foreach (ActionButtonUI actionButtonUI in actionButtonUILIst)
        {
            actionButtonUI.UpdateSelectedVisual();
        }
    }

    public void UpdateActionPoints()
    {
        Unit unit = UnitActionSystem.Instance.GetSelectedUnit();
        if (unit == null)
        {
            actionPointsText.enabled = false;
            return;
        }

        actionPointsText.enabled = true;
        string ActionPointString = "可用行动点数: " + unit.GetActionPoints().ToString();

        if (unit.GetReductionPoints() != 0) ActionPointString += " - " + unit.GetReductionPoints();
        actionPointsText.text = ActionPointString;
    }
}
