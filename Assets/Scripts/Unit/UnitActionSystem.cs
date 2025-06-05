using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;

public class UnitActionSystem : MonoBehaviour
{
    public static UnitActionSystem Instance { get; private set; }
    public event EventHandler OnSelectedUnitChanged;
    public event EventHandler OnSelectActionChanged;
    public event EventHandler<bool> OnBusyChanged;
    public event EventHandler OnActionAddQueue;


    private List<Unit> selectedUnits; //选中的单位
    private BaseAction selectedAction;
    private bool isBusy;
    private int currentExecutionOrder = 0; // 添加执行顺序管理
    private bool isExecutingGlobalActions = false;

    private void Awake()
    {
        if (Instance != null)
        {
            Debug.Log("There's more than one UnitActionSystem! " + transform + " - " + Instance);
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {

        selectedUnits = new List<Unit>();
        SelectionBoxGUI.OnClick += SelectionBoxGUI_OnClick;
    }

    private void SelectionBoxGUI_OnClick(object sender, EventArgs e)
    {
        if (isBusy)
        {
            return;
        }

        //判断是否是玩家回合
        if (!TurnSystem.Instance.IsPlayerTrun())
        {
            return;
        }

        //检测鼠标指针是否在ui上
        if (EventSystem.current.IsPointerOverGameObject())
        {
            return;
        }

        //判断点击的是否是单位
        if (TryHandleUnitSelection())
        {
            return;
        }

        if (selectedUnits.Count == 0)
        {
            return;
        }

        //判断点击的是否是选择行动的可用范围并执行
        HandleSelectedAction();
    }

    private void HandleSelectedAction()
    {
        GridPosition mouseGridPosition = LevelGrid.Instance.GetGridPosition(MouseWorld.GetPosition());

        if (!selectedAction.IsValidActionGridPosition(mouseGridPosition))
        {
            ClearSelectedUnits();
            return;
        }

        //开启预测
        selectedUnits[0].SetForecast(true);
        if (!selectedUnits[0].TrySpendActionPointsToTakeAction(selectedAction))
        {
            return;
        }

        BaseActionSO.ActionTags actionTags = ScriptiptableSOManager.Instance.GetActionTags(selectedAction);
        GridPosition unitForecastPosition = selectedUnits[0].GetGridPosition();

        //添加行动至队列
        selectedUnits[0].AddOrderedAction(selectedAction, mouseGridPosition, currentExecutionOrder);
        currentExecutionOrder++;
        OnActionAddQueue?.Invoke(this, EventArgs.Empty); //触发行动点更新
    }

    private bool TryHandleUnitSelection()
    {
        if (MouseWorld.TryGetUnit(out Unit unit))
        {
            if (unit == IsUnitSelected(unit))
            {
                //选择相同的角色不触发事件
                return false;
            }

            if (unit.IsEnemy())
            {
                //Clicked on anEnemy
                return false;
            }

            AddSelectedUnit(unit);
            return true;
        }

        return false;
    }

    public void SetSelectedAction(BaseAction baseAction)
    {
        selectedAction = baseAction;
        OnSelectActionChanged?.Invoke(this, EventArgs.Empty);
    }

    public Unit GetSelectedUnit()
    {
        if (selectedUnits.Count == 0) return null;
        return selectedUnits[0];
    }

    public List<Unit> GetSelectedUnits()
    {
        return selectedUnits;
    }

    public bool IsUnitSelected(Unit unit)
    {
        return selectedUnits.Contains(unit);
    }

    public void AddSelectedUnit(Unit selectedUnit)
    {
        List<Unit> unitList = new() { selectedUnit };
        AddSelectedUnit(unitList);
    }

    public void AddSelectedUnit(List<Unit> selectedUnits)
    {
        foreach (Unit unit in selectedUnits)
        {
            this.selectedUnits.Add(unit);
            
        }
        SetSelectedAction(selectedUnits[0].GetAction<MoveAction>());
        OnSelectedUnitChanged?.Invoke(this, EventArgs.Empty);
    }

    public void ClearSelectedUnits()
    {
        selectedUnits.Clear();
        OnSelectActionChanged?.Invoke(this, EventArgs.Empty);
        OnSelectedUnitChanged?.Invoke(this, EventArgs.Empty);
    }

    private void SetBusy()
    {
        isBusy = true;
        OnBusyChanged?.Invoke(this, isBusy);
    }

    private void ClearBusy()
    {
        isBusy = false;
        OnBusyChanged?.Invoke(this, isBusy);
    }

    public BaseAction GetSelectedAction()
    {
        return selectedAction;
    }

    /// <summary>
    /// 当有新行动添加时调用
    /// </summary>
    public void ExecuteAllQueuedActions()
    {
        if (!isExecutingGlobalActions)
        {
            StartCoroutine(ExecuteGlobalActionsCoroutine());
        }
    }

    private IEnumerator ExecuteGlobalActionsCoroutine()
    {
        isExecutingGlobalActions = true;
        SetBusy();

        while (true)
        {
            // 获取所有单位中最小执行顺序
            int currentOrder = UnitManager.Instance.GetFriendlyUnitList()
                .Select(u => u.GetCurrentActionOrder())
                .Min();

            if (currentOrder == int.MaxValue) break; // 所有行动完成

            Debug.Log($"开始执行顺序: {currentOrder}");

            bool orderCompleted = false;
            ExecuteActionsAtOrder(currentOrder, () =>
            {
                orderCompleted = true;
            });

            yield return new WaitUntil(() => orderCompleted);
        }

        isExecutingGlobalActions = false;
        currentExecutionOrder = 0;
        ClearBusy();
    }

    private void ExecuteActionsAtOrder(int order, Action onComplete)
    {
        List<Unit> units = UnitManager.Instance.GetFriendlyUnitList()
            .Where(u => u.GetCurrentActionOrder() == order)
            .OrderBy(u => u.GetPriority())
            .ToList();

        if (!units.Any())
        {
            onComplete?.Invoke();
            return;
        }

        int completedCount = 0;
        foreach (Unit unit in units)
        {
            unit.GetActionQueue().ExecuteActionsAtOrder(order, () =>
            {
                completedCount++;
                if (completedCount == units.Count())
                {
                    onComplete?.Invoke();// 所有单位完成后触发回调
                }
            });
        }
    }
}
