using UnityEngine;

public class PathfindingUpdate : MonoBehaviour
{
    private void Start()
    {
        DestructibleCrate.OnAnyDestroyed += DestructibleCrate_OnAnyDestroyed;
    }

    private void DestructibleCrate_OnAnyDestroyed(object sender, System.EventArgs e)
    {
        DestructibleCrate destructibleCrate = sender as DestructibleCrate;
        Pathfinding.Instance.SetIsWalkableGridPosition
            (
                LevelGrid.Instance.GetGridPosition(destructibleCrate.GetGridPosition()),
                true
            );
    }

}
