using System.Collections.Generic;

using System.Linq;
using UnityEngine;

public class MoveActionParameter : BaseActionParameter
{
    private List<GridPosition> movePath;

    public MoveActionParameter(MoveAction action, List<GridPosition> targetPos, GridPosition finalPos, int executeTurn) : base(action, targetPos, finalPos, executeTurn)
    {
        baseAction = action;
        movePath = new List<GridPosition>();
    }

    public void SetMovePath(List<GridPosition> path)
    {
        movePath = new List<GridPosition>(path);
    }

    public List<GridPosition> GetMovePathForGridPosition()
    {
        return movePath;
    }

    public List<Vector3> GetMovePathForWorldPosition()
    {
        return movePath.Select(p => LevelGrid.Instance.GetWorldPosition(p)).ToList();
    }
}
