using System;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using static GridSystemVisual;

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
        UnitActionSystem.Instance.OnSelectedUnitChanged += UnitActionSystem_OnSelectedUnitChanged;
        LevelGrid.Instance.OnAnyUnitMoveGridPosition += LevelGrid_OnAnyUnitMoveGridPosition;
        BaseAction.OnAnyActionCompleted += BaseAction_OnAnyActionCompleted;
        Unit.OnAnyUnitPositionChanged += Unit_OnMoveActionUnitPositionChange;
        UpdateGridVisual();
    }

   

    private void OnDestroy()
    {
        UnitActionSystem.Instance.OnSelectActionChanged -= UnitActionSystem_OnSelectActionChanged;
        UnitActionSystem.Instance.OnDeselectActionChanged -= UnitActionSystem_OnDeselectActionChanged;
        UnitActionSystem.Instance.OnSelectedUnitChanged -= UnitActionSystem_OnSelectedUnitChanged;
        LevelGrid.Instance.OnAnyUnitMoveGridPosition -= LevelGrid_OnAnyUnitMoveGridPosition;
        BaseAction.OnAnyActionCompleted -= BaseAction_OnAnyActionCompleted;
        Unit.OnAnyUnitPositionChanged -= Unit_OnMoveActionUnitPositionChange;
    }

    private void UnitActionSystem_OnSelectedUnitChanged(object sender, EventArgs e)
    {
        UpdateGridVisual();
    }

    private void UnitActionSystem_OnDeselectActionChanged(object sender, bool e)
    {
        UpdateGridVisual();
    }

    private void Unit_OnMoveActionUnitPositionChange(object sender, EventArgs e)
    {
        UpdateGridVisual();
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
            if (lastSelectedGridSystemVisualSingle != null) lastSelectedGridSystemVisualSingle.HideSelected();
            lastSelectedGridSystemVisualSingle = gridSystemVisualSingleArray[gridPosition.x, gridPosition.z];
            lastSelectedGridSystemVisualSingle.ShowSelected();
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
        List<GridPosition> gridPositionList = new();

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

    private void UpdateGridVisual()
    {
        //如果UpdateGridVisualTime大于0，说明还没有到更新网格视觉的时间，直接返回
        HidegridSystemVisual();

        List<Unit> selectedUnits = UnitActionSystem.Instance.GetSelectedUnits();
        if (selectedUnits == null || selectedUnits.Count == 0) return;

        BaseAction selectedActionAll = UnitActionSystem.Instance.GetSelectedAction();
        ShowUnitActionGridVisual(selectedUnits, selectedActionAll);

    }

    private void ShowUnitActionGridVisual(List<Unit> units, BaseAction baseAction)
    {
        HashSet<GridPosition> showGridPositions = new();
        GridVisualType gridVisualType = GridVisualType.White;
        //先计算所有单位的网格视觉
        foreach (Unit selectedUnit in units)
        {
            BaseAction selectedAction = selectedUnit.GetBaseActionOfType(baseAction);
            gridVisualType = ShowUnitActionGridVisual(selectedUnit, selectedAction); //
            //添加所有单位的网格视觉
            showGridPositions.AddRange(selectedAction.GetValidActionGridPositionList());
        }
        ShowGridPositionList(showGridPositions.ToList(), gridVisualType);

    }

    private GridVisualType ShowUnitActionGridVisual(Unit selectedUnit, BaseAction baseAction)
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
        if (baseAction == null) return GridVisualType.White;
        //ShowGridPositionList(baseAction.GetValidActionGridPositionList(), gridVisualType);
        return gridVisualType;
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

    /// <summary>
    /// 战争迷雾显形
    /// </summary>
    /// <param name="gridPositions"></param>
    public void AreaDiscovered(List<GridPosition> gridPositions)
    {
        foreach (GridSystemVisualSingle gridSystemVisualSingle in gridSystemVisualSingleArray)
        {
            gridSystemVisualSingle.ShowFogOfWar();
        }

        foreach (GridPosition gridPosition in gridPositions) {
            gridSystemVisualSingleArray[gridPosition.x, gridPosition.z].HideFogOfWar();
        }
    }
}
