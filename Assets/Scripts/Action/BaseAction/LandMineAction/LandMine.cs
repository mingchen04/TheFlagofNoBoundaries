using System.Collections.Generic;
using UnityEngine;

public class LandMine : MonoBehaviour
{
    [SerializeField] private Transform grenadeExplodeVfxPrfefab; // 地雷爆炸特效预制体
    private GridPosition gridPosition;
    private int explodedDistance = 2; //爆炸范围


    private void Start()
    {
        LevelGrid.Instance.OnAnyUnitMoveGridPosition += LevelGrid_OnAnyUnitMoveGridPosition;
    }

    private void LevelGrid_OnAnyUnitMoveGridPosition(object sender, System.EventArgs e)
    {
        HandleLandmines();
    }

    public void Setup(GridPosition targetGridPosition)
    {
        gridPosition = targetGridPosition;
    }

    private void LandMineExploded()
    {
        LevelGrid.Instance.OnAnyUnitMoveGridPosition -= LevelGrid_OnAnyUnitMoveGridPosition;
        List<GridPosition> gridPositions = LevelGrid.Instance.GenerateHexArea(gridPosition, explodedDistance); // 生成爆炸范围
        Instantiate(grenadeExplodeVfxPrfefab, transform.position, Quaternion.identity); // 生成爆炸特效

        LevelGrid.Instance.DamageUnitForGridPosition(gridPositions, 90 , out List<Unit> units);
        foreach (Unit targetUnit in units)
        {
            targetUnit.GetActionQueue().ActionQueueStop(); // 停止单位的行动队列
        }

        Destroy(gameObject); // 销毁地雷对象
    }

    private void HandleLandmines()
    {
        Unit unit = LevelGrid.Instance.GetUnitAtGridPosition(gridPosition);
        if (unit != null)
        {
            LandMineExploded();
        }
    }
}
