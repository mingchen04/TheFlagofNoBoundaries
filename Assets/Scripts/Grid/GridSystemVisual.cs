using System;
using System.Collections.Generic;
using UnityEngine;

public class GridSystemVisual : MonoBehaviour
{
    public static GridSystemVisual Instance { get; private set; }

    [Serializable]
    public struct GridVisualTypeMaterial
    {
        public GridVisualType gridVisualType;
        public Material material;
    }

    public enum GridVisualType
    {
        White,
        Blue,
        Red,
        RedSoft,
        Yellow
    }

    [SerializeField] private Transform gridSystemVisualSinglePrefab;
    [SerializeField] private List<GridVisualTypeMaterial> gridVisualTypeMaterialsList;

    private GridSystemVisualSingle[,] gridSystemVisualSingleArray;

    /// <summary>
    /// 最后选择的网格视图
    /// </summary>
    private GridSystemVisualSingle lastSelectedGridSystemVisualSingle;

    private void Awake()
    {
        if (Instance != null)
        {
            Debug.Log("There's more than one GridSystemVisual! " + transform + " - " + Instance);
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        gridSystemVisualSingleArray = new GridSystemVisualSingle[LevelGrid.Instance.GetWidth(), LevelGrid.Instance.GetHight()];
        for (int x = 0; x < LevelGrid.Instance.GetWidth(); x++)
        {
            for (int z = 0; z < LevelGrid.Instance.GetHight(); z++)
            {
                Vector3 worldPosition = LevelGrid.Instance.GetWorldPosition(new GridPosition(x, z));
                Transform gridSystemVisualTransform =
                    Instantiate(gridSystemVisualSinglePrefab, worldPosition, Quaternion.identity);

                gridSystemVisualSingleArray[x, z] = gridSystemVisualTransform.GetComponent<GridSystemVisualSingle>();
            }
        }

        UnitActionSystem.Instance.OnSelectActionChanged += UnitActionSystem_OnSelectActionChanged;
        UnitActionSystem.Instance.OnDeselectActionChanged += UnitActionSystem_OnDeselectActionChanged;
        LevelGrid.Instance.OnAnyUnitMoveGridPosition += LevelGrid_OnAnyUnitMoveGridPosition;
        BaseAction.OnAnyActionCompleted += BaseAction_OnAnyActionCompleted;
        Unit.OnAnyUnitPositionChanged += Unit_OnMoveActionUnitPositionChange;
        UpdateGridVisual();
    }

    private void UnitActionSystem_OnDeselectActionChanged(object sender, bool e)
    {
        UpdateGridVisual();
    }

    private void Unit_OnMoveActionUnitPositionChange(object sender, EventArgs e)
    {
        UpdateGridVisual(sender as Unit);
    }

    private void BaseAction_OnAnyActionCompleted(object sender, EventArgs e)
    {
        UpdateGridVisual();
    }

    private void LevelGrid_OnAnyUnitMoveGridPosition(object sender, System.EventArgs e)
    {
        UpdateGridVisual();
    }

    private void UnitActionSystem_OnSelectActionChanged(object sender, System.EventArgs e)
    {
        UpdateGridVisual();
    }

    private void Update()
    {
        Vector3 mouseWorld = MouseWorld.GetPosition();
        GridPosition gridPosition = LevelGrid.Instance.GetGridPosition(mouseWorld);
        if (LevelGrid.Instance.IsValidGridPosition(gridPosition))
        {
            if (lastSelectedGridSystemVisualSingle != null) lastSelectedGridSystemVisualSingle.hideSelected();
            lastSelectedGridSystemVisualSingle = gridSystemVisualSingleArray[gridPosition.x, gridPosition.z];
            lastSelectedGridSystemVisualSingle.showSelected();
        }

        //UpdateGridVisual();
    }

    public void HidegridSystemVisual()
    {
        foreach (GridSystemVisualSingle gridSystemVisualSingle in gridSystemVisualSingleArray)
        {
            gridSystemVisualSingle.Hide();
        }
    }

    private void ShowGridPositionRangeSquare(GridPosition gridPosition, int range, GridVisualType gridVisualType)
    {
        List<GridPosition> gridPositionList = new List<GridPosition>();

        foreach (GridPosition testGridPosition in LevelGrid.Instance.GenerateHexArea(gridPosition, range))
        {
            if (!LevelGrid.Instance.IsValidGridPosition(testGridPosition))
            {
                //网格位置不合法跳过
                continue;
            }
            gridPositionList.Add(testGridPosition);
        }
        ShowGridPositionList(gridPositionList, gridVisualType);
    }

    public void ShowGridPositionList(List<GridPosition> gridPositionList, GridVisualType gridVisualType)
    {
        foreach (GridPosition gridPosition in gridPositionList)
        {
            gridSystemVisualSingleArray[gridPosition.x, gridPosition.z].Show(GetGridVisualTypeMaterial(gridVisualType));
        }
    }

    private void UpdateGridVisual(Unit unit = null)
    {
        //若unit不为，则只更新该unit的行动范围格子
        if (unit != null)
        {
            BaseAction selectedAction = UnitActionSystem.Instance.GetSelectedAction();
            BaseAction thisAction = unit.GetBaseActionOfType(selectedAction);

            if (thisAction != null)
            {
                // 只隐藏属于当前unit且不被其他单位包含的格子
                HideUnitExclusiveGridVisual(unit, thisAction);

                // 显示该unit的行动范围
                ShowUnitActionGridVisual(unit, thisAction);
            }
            return;
        }

        HidegridSystemVisual();

        List<Unit> selectedUnits = UnitActionSystem.Instance.GetSelectedUnits();
        if (selectedUnits == null || selectedUnits.Count == 0) return;

        BaseAction selectedActionAll = UnitActionSystem.Instance.GetSelectedAction();
        ShowUnitActionGridVisual(selectedUnits, selectedActionAll);
    }

    /// <summary>
    /// 隐藏只属于当前unit的可行动格子（不会影响其他单位的高亮）
    /// </summary>
    private void HideUnitExclusiveGridVisual(Unit unit, BaseAction thisAction)
    {
        HashSet<GridPosition> otherUnitsPositions = GetOtherUnitsActionPositions(unit);

        foreach (GridPosition pos in thisAction.GetValidActionGridPositionList())
        {
            if (!otherUnitsPositions.Contains(pos))
            {
                gridSystemVisualSingleArray[pos.x, pos.z].Hide();
            }
        }
    }

    /// <summary>
    /// 获取所有其他选中单位的可行动格子集合
    /// </summary>
    private HashSet<GridPosition> GetOtherUnitsActionPositions(Unit excludeUnit)
    {
        List<Unit> selectedUnits = UnitActionSystem.Instance.GetSelectedUnits();
        BaseAction selectedAction = UnitActionSystem.Instance.GetSelectedAction();

        HashSet<GridPosition> positions = new HashSet<GridPosition>();
        foreach (Unit otherUnit in selectedUnits)
        {
            if (otherUnit == excludeUnit) continue;
            BaseAction otherAction = otherUnit.GetBaseActionOfType(selectedAction);
            if (otherAction == null) continue;
            foreach (var pos in otherAction.GetValidActionGridPositionList())
            {
                positions.Add(pos);
            }
        }
        return positions;
    }

    private void ShowUnitActionGridVisual(List<Unit> units, BaseAction baseAction)
    {
        foreach (Unit selectedUnit in units)
        {
            BaseAction selectedAction = selectedUnit.GetBaseActionOfType(baseAction);
            ShowUnitActionGridVisual(selectedUnit, selectedAction);
        }
    }

    private void ShowUnitActionGridVisual(Unit selectedUnit, BaseAction baseAction)
    {
        GridVisualType gridVisualType;
        //根据选择的行动显示不同颜色的网格
        switch (baseAction)
        {
            default:
            case MoveAction moveAction:
                gridVisualType = GridVisualType.White;
                break;
            case SpinAction spinAction:
                gridVisualType = GridVisualType.Blue;
                break;
            case GrenadeAction grenadeAction:
                gridVisualType = GridVisualType.Yellow;
                break;
            case LandMineAction landMineAction:
                gridVisualType = GridVisualType.Red;
                break;

            case ShootAction shootAction:
                gridVisualType = GridVisualType.Red;
                ShowGridPositionRangeSquare(selectedUnit.GetGridPosition(), shootAction.GetMaxShootDistance(), GridVisualType.RedSoft);
                break;
            case SwordAction swordAction:
                gridVisualType = GridVisualType.Red;
                ShowGridPositionRangeSquare(selectedUnit.GetGridPosition(), swordAction.GetMaxSwordDistance(), GridVisualType.RedSoft);
                break;
            case InteractAction interactAction:
                gridVisualType = GridVisualType.Blue;
                ShowGridPositionRangeSquare(selectedUnit.GetGridPosition(), interactAction.GetMaxInteractDistance(), GridVisualType.RedSoft);
                break;
        }
        if (baseAction == null) return;
        ShowGridPositionList(baseAction.GetValidActionGridPositionList(), gridVisualType);
    }

    private Material GetGridVisualTypeMaterial(GridVisualType gridVisualType)
    {
        foreach (GridVisualTypeMaterial gridVisualTypeMaterial in gridVisualTypeMaterialsList)
        {
            if (gridVisualTypeMaterial.gridVisualType == gridVisualType)
            {
                return gridVisualTypeMaterial.material;
            }
        }

        Debug.LogError("Could not find GridVisualTypeMaterial for GridVisualType " + gridVisualType);
        return null;
    }
}
