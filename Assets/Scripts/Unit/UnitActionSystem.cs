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
    public event EventHandler<bool> OnDeselectActionChanged;

    private List<Unit> selectedUnits; //选中的单位
    private BaseAction selectedAction;
    private bool isDeployAction; //是否部署行动
    private bool isBusy;
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
        if (isBusy) return;
        //判断是否是玩家回合
        if (!TurnSystem.Instance.IsPlayerTrun()) return;
        //检测鼠标指针是否在ui上
        if (EventSystem.current.IsPointerOverGameObject()) return;

        GridPosition mouseGridPosition = LevelGrid.Instance.GetGridPosition(MouseWorld.GetPosition());

        //点击地板时检查是否有行动 没有行动可执行则选择单位
        if (!TryHandleSelectedAction(mouseGridPosition))
        {
            //判断点击的是否是单位
            if (TryHandleUnitSelection(mouseGridPosition))
            {
                return;
            }
        }
    }

    private bool TryHandleSelectedAction(GridPosition mouseGridPosition)
    {
        if (selectedUnits.Count == 0) return false;
        if (!LevelGrid.Instance.IsValidGridPosition(mouseGridPosition)) return false;
        if (!selectedAction.IsValidActionGridPosition(mouseGridPosition)) return false;

        bool isAction = false;
        isDeployAction = false; //重置部署行动状态

        foreach (Unit unit in selectedUnits)
        {
            if (unit.GetDeselectAction()) isDeployAction = true;
            int actionExecutionOrder = unit.GetActionQueue().EndActionExecuteTurnGet() + 1;
            if (isDeployAction)
            {
                actionExecutionOrder = unit.GetActionQueue().EndActionExecuteTurnGet();
            }
            else
            {
                actionExecutionOrder = unit.GetActionQueue().EndActionExecuteTurnGet() + 1;
            }

            BaseAction currentUnitAction = unit.GetBaseActionOfType(selectedAction); //获取当前单位的同一类型的行动

            if (!currentUnitAction.IsValidActionGridPosition(mouseGridPosition)) continue;
            if (currentUnitAction == null) continue;
            unit.HandleOrderedAction(currentUnitAction, mouseGridPosition, actionExecutionOrder);
            isAction = true;
        }

        if (isDeployAction) OnDeselectActionChanged?.Invoke(this, isDeployAction);

        return isAction;
    }

    private bool TryHandleUnitSelection(GridPosition mouseGridPosition)
    {
        DeployActionEnd(); //结束部署行动状态
        if (MouseWorld.TryGetUnit(out Unit unit))
        {
            if (unit.IsEnemy()) return false;
            //选择相同的角色不触发事件
            if (unit == IsUnitSelected(unit)) return false;
            else
            {
                ClearSelectedUnits();
                AddSelectedUnit(unit);
                return true;
            }
        }
        //点击地板时清除选中单位
        ClearSelectedUnits();
        return false;
    }

    public void SetSelectedAction(BaseAction baseAction)
    {
        DeployActionEnd();
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

        if (selectedUnits.Count > 0) SetSelectedAction(selectedUnits[0].GetAction<MoveAction>());
        OnSelectedUnitChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// 清除选中单位
    /// </summary>
    /// <param name="playerControl">是否由玩家控制</param>
    public void ClearSelectedUnits(bool playerControl = true)
    {
        // 如果是玩家控制并且按下Shift键，则不清除选中单位
        if (playerControl && InputManager.Instance.IsShiftButtonDown())
        {
            return;
        }

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
    /// 执行所有队列
    /// </summary>
    public void ExecuteAllQueuedActions()
    {
        if (!isExecutingGlobalActions)
        {
            DeployActionEnd(); // 确保结束部署行动状态
            ClearSelectedUnits(false);
            AddSelectedUnit(UnitManager.Instance.GetFriendlyUnitList());
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
        ClearBusy();
        ClearSelectedUnits(false);
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

    /// <summary>
    /// 行动部署结束
    /// </summary>
    public void DeployActionEnd()
    {
        isDeployAction = false;
        foreach (Unit unit in selectedUnits)
        {
            unit.SetDeselectAction(isDeployAction);
        }

        OnDeselectActionChanged?.Invoke(this, isDeployAction);
    }
}
