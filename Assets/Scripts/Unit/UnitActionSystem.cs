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

    private List<Unit> selectedUnits = new(); //选中的单位
    private BaseAction selectedAction;
    private bool isDeployAction; //是否部署行动
    private bool isBusy;
    private bool isExecutingGlobalActions = false;
    private List<Unit> underwayUnits = new(); //正在进行的单位列表

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
        LevelGrid.Instance.OnAnyUnitMoveGridPosition += LevelGrid_OnAnyUnitPositionChanged;
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

        bool isAction = false;
        isDeployAction = false; //重置部署行动状态

        foreach (Unit unit in selectedUnits)
        {

            BaseAction currentUnitAction = unit.GetBaseActionOfType(selectedAction); //获取当前单位的同一类型的行动
            if (unit.GetDeselectAction()) isDeployAction = true;
            int actionExecutionOrder = unit.GetActionQueue().EndActionExecuteTurnGet() + (isDeployAction ? 0 : 1);

            if (!currentUnitAction.IsValidActionGridPosition(mouseGridPosition)) continue;
            if (currentUnitAction == null) continue;
            unit.HandleOrderedAction(currentUnitAction, mouseGridPosition, actionExecutionOrder);

            isAction = true;
        }

        if (selectedAction.GetActionTags().HasFlag(ActionTags.Deploy))
        {
            isDeployAction = true;
            OnDeselectActionChanged?.Invoke(this, isDeployAction);
        }

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

        if (selectedUnits.Count > 0 && selectedAction == null) SetSelectedAction(selectedUnits[0].GetAction<MoveAction>());
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
            //ClearSelectedUnits(false);
            //AddSelectedUnit(UnitManager.Instance.GetFriendlyUnitList());
            StartCoroutine(ExecuteGlobalActionsCoroutine());
        }
    }

    private IEnumerator ExecuteGlobalActionsCoroutine()
    {
        isExecutingGlobalActions = true;
        SetBusy();
        List<Unit> closeUnit = new List<Unit>();

        while (true)
        {
            // 获取所有单位中最小执行顺序

            List<Unit> units = TurnSystem.Instance.IsPlayerTrun() ?
                UnitManager.Instance.GetFriendlyUnitList() :
                UnitManager.Instance.GetEnemyUnitList();
            units = units.FindAll(u => !closeUnit.Contains(u));

            int currentOrder = int.MaxValue;
            if (units.Any()) 
            {
                currentOrder = units
                .Select(u => u.GetCurrentActionOrder())
                .Min();
            }// 没有单位可执行
 
            if (currentOrder == int.MaxValue)
            {
                NextTurn();
                break;
            } // 所有行动完成

            Debug.Log($"开始执行顺序: {currentOrder}");

            bool orderCompleted = false;
            closeUnit.AddRange
                (
                    ExecuteActionsAtOrder(currentOrder, () => { orderCompleted = true; })
                );


            yield return new WaitUntil(() => orderCompleted);
        }

        isExecutingGlobalActions = false;
        ClearBusy();
    }

    /// <summary>
    /// 执行指定顺序的所有单位行动 并输出正在进行的单位列表
    /// </summary>
    /// <param name="order"></param>
    /// <param name="onComplete"></param>

    private List<Unit> ExecuteActionsAtOrder(int order, Action onComplete)
    {
        List<Unit> closeUnit = new List<Unit>();

        List<Unit> units = (TurnSystem.Instance.IsPlayerTrun() ?
            UnitManager.Instance.GetFriendlyUnitList() :
            UnitManager.Instance.GetEnemyUnitList())
            .Where(u => u.GetCurrentActionOrder() == order)
            .OrderBy(u => u.GetPriority())
            .ToList();
        underwayUnits = units; // 更新正在进行的单位列表
        if (!units.Any())
        {
            onComplete?.Invoke();
            return closeUnit;
        }

        int completedCount = 0;
        foreach (Unit unit in units)
        {
            unit.GetActionQueue().ExecuteActionsAtOrder(order, () =>
            {
                Debug.Log($"单位 {unit.name} 完成行动，顺序: {order}");

                if (unit.GetActionQueue().IsActionQueueComplete())
                {
                    closeUnit.Add(unit);
                }
                completedCount++;
                if (completedCount == units.Count())
                {
                    onComplete?.Invoke();// 所有单位完成后触发回调
                }
            });
        }
        return closeUnit;
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

    /// <summary>
    /// 检查一组单位的移动目标是否发生冲突（如多个单位目标格相同），
    /// 并停止所有冲突单位的集合。
    /// 优化点：
    /// 1. 支持多个单位同时冲突（如3个单位目标相同，全部判为无效）。
    /// 2. 变量命名更语义化，注释更详细。
    /// 3. 只处理正在进行的MoveAction。
    /// </summary>
    public void StopInvalidMoveUnits()
    {
        // 记录每个目标格对应的所有单位
        Dictionary<GridPosition, List<Unit>> targetMap = new();

        //集中管理所有正在进行的单位 保存目标位置
        foreach (Unit unit in underwayUnits)
        {
            ActionQueue actionQueue = unit.GetActionQueue();
            BaseActionParameter underwayAction = actionQueue.GetUnderwayAction();
            if (underwayAction == null) continue;

            // 只处理移动行动
            if (underwayAction.baseAction is BaseMoveAction baseMove)
            {
                if (!baseMove.GetIsActive()) continue; // 确保移动行动正在进行中
                GridPosition targetPos = baseMove.GetTargetPosition();
                if (targetPos == GridPosition.nullGridPosition) continue; // 跳过无效目标位置
                if ((GridPosition)unit.GetWorldPosition() == targetPos) continue; // 跳过当前单位位置与目标位置相同的情况

                if (!targetMap.ContainsKey(targetPos))
                {
                    targetMap[targetPos] = new List<Unit>();
                }
                targetMap[targetPos].Add(unit);
            }
        }

        foreach (KeyValuePair<GridPosition, List<Unit>> kvp in targetMap)
        {
            List<Unit> units = kvp.Value;
            Action<Action>[] actions = new Action<Action>[units.Count];

            // 如果多个单位目标格相同
            if (units.Count > 1)
            {
                // 所有目标相同的单位都暂停
                for (int i = 0; i < units.Count; i++)
                {
                    Unit unit = units[i];
                    // 暂停所有冲突单位的行动
                    BaseMoveAction moveAction = unit.GetActionQueue().GetUnderwayAction().baseAction as BaseMoveAction;
                    Action<Action> resumeAction = moveAction.SuspendAction();
                    actions[i] = resumeAction; //保存恢复行动的方法
                }

                StartCoroutine(ExecuteActionsSequentially(actions));
            }
        }
    }

    /// <summary>
    /// 按顺序启动目标的恢复方法，在到达目标位置时再启动下一个
    /// </summary>
    private IEnumerator ExecuteActionsSequentially(Action<Action>[] actions)
    {
        for (int i = 0; i < actions.Length; i++)
        {
            bool ReachTarget = false;
            // 执行每个恢复行动的委托,并设置到达目标的回调方法
            actions[i](() =>
            {
                ReachTarget = true;
            });
            yield return new WaitUntil(() => ReachTarget); // 等待单位到达目标位置
        }
    }

    private void LevelGrid_OnAnyUnitPositionChanged(object sender, EventArgs e)
    {
        StopInvalidMoveUnits();
    }

    private void NextTurn()
    {
        if (!TurnSystem.Instance.IsPlayerTrun())
        {
            TurnSystem.Instance.NextTurn();
        }
    }
}
