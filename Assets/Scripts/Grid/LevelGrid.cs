using System;
using System.Collections.Generic;
using UnityEngine;

public class LevelGrid : MonoBehaviour
{
    public event EventHandler OnAnyUnitMoveGridPosition;

    [SerializeField] private int width;
    [SerializeField] private int height;
    [SerializeField] private float cellSize;
    [SerializeField] private Transform gridDebugObjectPrefab;

    private GridSystemHex<GridObject> gridSystem;
    public static LevelGrid Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null)
        {
            Debug.Log("There's more than one LevelGrid! " + transform + " - " + Instance);
            Destroy(gameObject);
            return;
        }
        Instance = this;

        gridSystem = new GridSystemHex<GridObject>(width, height, cellSize,
            (GridSystemHex<GridObject> g, GridPosition gridPosition) => new GridObject(g, gridPosition));

        //显示网格的x z坐标
        //gridSystem.CreateDebugObject(gridDebugObjectPrefab);
    }

    private void Start()
    {
        Pathfinding.Instance.Setup(width, height, cellSize);
    }

    public void AddUnitAtGridPosition(GridPosition gridPosition, Unit unit)
    {
        gridSystem.GetGridObject(gridPosition).addUnit(unit);
    }

    public List<Unit> GetUnitListAtGridPosition(GridPosition gridPosition)
    {
        return gridSystem.GetGridObject(gridPosition).getUnitList();
    }

    public void ClearUnitAtGridPosition(GridPosition gridPosition, Unit unit)
    {
        //Debug.Log("clear" + gridPosition);
        gridSystem.GetGridObject(gridPosition).RemoveUnit(unit);
    }

    public void UnitMovedGridPosition(Unit unit, GridPosition fromGridPosition, GridPosition toGridPosition)
    {
        ClearUnitAtGridPosition(fromGridPosition, unit);
        AddUnitAtGridPosition(toGridPosition, unit);
        OnAnyUnitMoveGridPosition?.Invoke(this, EventArgs.Empty);
    }

    public GridPosition GetGridPosition(Vector3 worldPosition) => gridSystem.GetGridPosition(worldPosition);
    public Vector3 GetWorldPosition(GridPosition gridPosition) => gridSystem.GetWorldPositon(gridPosition);

    public bool IsValidGridPosition(GridPosition gridPosition) => gridSystem.IsValidGridPosition(gridPosition);

    public int GetWidth() => gridSystem.GetWidth();

    public int GetHight() => gridSystem.GetHeight();

    public bool HasAnyUnitOnGridPosition(GridPosition gridPosition)
    {
        GridObject gridObject = gridSystem.GetGridObject(gridPosition);
        return gridObject.HasAnyUnit();
    }

    public Unit GetUnitAtGridPosition(GridPosition gridPosition)
    {
        GridObject gridObject = gridSystem.GetGridObject(gridPosition);
        return gridObject.GetUnit();
    }

    public IInteractable GetInteractableAtGridPosition(GridPosition gridPosition)
    {
        IInteractable interactable = gridSystem.GetGridObject(gridPosition).GetInteractable();
        return interactable;
    }

    public void SetInteractableAtGridPosition(GridPosition gridPosition, IInteractable interactable)
    {
        gridSystem.GetGridObject(gridPosition).SetInteractable(interactable);
        
    }

    /// <summary>
    /// 生成以center为中心、半径为N的六边形区域所有坐标
    /// </summary>
    public List<GridPosition> GenerateHexArea(GridPosition center, int radius)
    {
        List<GridPosition> results = new List<GridPosition>();
        bool isEvenRow = center.z % 2 == 0;

        for (int q = -radius; q <= radius; q++)
        {
            int rMin = Math.Max(-radius, -q - radius);
            int rMax = Math.Min(radius, -q + radius);

            for (int r = rMin; r <= rMax; r++)
            {
                // 计算x偏移量（根据行号奇偶性调整）
                int xOffset = q;
                if (r != 0) // 只有非中心行需要调整
                {
                    bool isNegative = r < 0;
                    int absR = Math.Abs(r);

                    // 统一计算偏移量
                    int offset = (absR + (isEvenRow == isNegative ? 1 : 0)) / 2;

                    xOffset += isNegative ? -offset : offset;

                    // 特殊处理第一行的情况
                    if (absR == 1)
                    {
                        if (isEvenRow && !isNegative) xOffset = q; // 偶数行上方第一行不偏移
                        else if (!isEvenRow && isNegative) xOffset = q; // 奇数行下方第一行不偏移
                    }
                }

                GridPosition hex = center + new GridPosition(xOffset, r);
                results.Add(hex);
            }
        }

        return results;
    }

}
