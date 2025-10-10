using System.Collections.Generic;

public class GridObject
{
    private List<Unit> unitList;
    private GridSystemHex<GridObject> gridSystem;
    private GridPosition gridPosition;
    private IInteractable interactable;
    private bool isVisible;

    public GridObject(GridSystemHex<GridObject> gridSystem, GridPosition gridPosition)
    {
        this.gridSystem = gridSystem;
        this.gridPosition = gridPosition;
        unitList = new List<Unit>();
        isVisible = false;
    }

    public void AddUnit(Unit unit)
    {
        unitList.Add(unit);
    }

    public List<Unit> GetUnitList()
    {
        return unitList;
    }

    public void RemoveUnit(Unit unit)
    {
        unitList.Remove(unit);
    }

    public override string ToString()
    {
        string unitString = "";
        foreach (Unit unit in unitList)
        {
            unitString += unit + "\n";
        }

        return gridPosition.ToString() + "\n" + unitString;

    }

    public bool HasAnyUnit()
    {
        return unitList.Count > 0;
    }

    public Unit GetUnit()
    {
        if (HasAnyUnit())
        {
            return unitList[0];
        }
        else
        {
            return null;
        }
    }

    public IInteractable GetInteractable()
    {
        return interactable;
    }

    public void SetInteractable(IInteractable interactable)
    {
        this.interactable = interactable;
    }

    public bool IsVisible()
    {
        return isVisible;
    }

    public void SetIsVisible(bool isVisible)
    {
        this.isVisible = isVisible;
    }
}
