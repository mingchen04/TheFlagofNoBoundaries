using System;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using static Cube;
public class FogOfWarManager : MonoBehaviour
{
    public static FogOfWarManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null)
        {
            Debug.Log("There's more than one FogOfWarManager! " + transform + " - " + Instance);
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        UpdateFriendlyUnitsVision();

        // 注册事件监听
        Unit.OnAnyUnitSpawned += Unit_OnAnyUnitSpawned;
        Unit.OnAnyUnitDead += Unit_OnAnyUnitDead;
        LevelGrid.Instance.OnAnyUnitMoveGridPosition += LevelGrid_OnAnyUnitMoveGridPosition;
    }

    private void OnDestroy()
    {
        Unit.OnAnyUnitSpawned -= Unit_OnAnyUnitSpawned;
        Unit.OnAnyUnitDead -= Unit_OnAnyUnitDead;
        LevelGrid.Instance.OnAnyUnitMoveGridPosition -= LevelGrid_OnAnyUnitMoveGridPosition;
    }

    private void LevelGrid_OnAnyUnitMoveGridPosition(object sender, EventArgs e)
    {
        UpdateFriendlyUnitsVision();
    }

    private void Unit_OnAnyUnitDead(object sender, EventArgs e)
    {
        UpdateFriendlyUnitsVision();
    }

    private void Unit_OnAnyUnitSpawned(object sender, EventArgs e)
    {
        UpdateFriendlyUnitsVision();
    }


    /// <summary>
    /// 更新所有友方单位的视野
    /// </summary>
    private void UpdateFriendlyUnitsVision()
    {
        List<Unit> units = UnitManager.Instance.GetFriendlyUnitList();
        HashSet<GridPosition> visiblePositions = new HashSet<GridPosition>();

        foreach (Unit unit in units)
        {
            // 例如在刷新战争迷雾时
            visiblePositions.AddRange
                (
                    ComputeHexShadowcastingSector
                    (
                        unit.transform.position, //不调用 GetGridPosition()，避免使用预测位置
                        unit.GetVisionRange(),
                        pos =>
                        {
                            /*障碍物判定*/
                            return !Pathfinding.Instance.IsWalkableGridPosition(pos);
                        }
                    )
                );
        }

        UpdateGridVisibility(visiblePositions);

        // 通知视觉系统（你原来的接口）
        GridSystemVisual.Instance.AreaDiscovered(visiblePositions.ToList());
        
        UnitManager.Instance.UpdateAnyUnitIsVisible(visiblePositions.ToList());
    }

    // -----------------------------
    // 新：基于 cube 坐标 + ring/line 的光线投射（ray per boundary-hex）
    // -----------------------------

    /// <summary>
    /// 使用 ring + line 的射线投射（raycast）实现六边形视野（可靠、直观）
    /// origin: 中心格
    /// radius: 半径
    /// isOpaque: 判断某格是否遮挡（true = 阻挡）
    /// </summary>
    public HashSet<GridPosition> ComputeHexShadowcastingSector(GridPosition origin, int radius, Func<GridPosition, bool> isOpaque)
    {
        // visible 集合
        HashSet<GridPosition> visible = new HashSet<GridPosition>();
        visible.Add(origin);

        // origin 的 cube 坐标
        Cube originCube = OffsetToCube(origin);

        // 对每个半径 r 的 ring 上每个格子作为射线目标，向其发出直线
        for (int r = 1; r <= radius; r++)
        {
            List<Cube> ring = HexRing(originCube, r);
            foreach (Cube target in ring)
            {
                List<Cube> line = HexLine(originCube, target);
                bool blocked = false;
                foreach (Cube step in line)
                {
                    GridPosition pos = CubeToOffset(step);
                    if (!LevelGrid.Instance.IsValidGridPosition(pos)) { blocked = true; break; }

                    // 超出 radius 检查（冗余，但保险）
                    if (GridPosition.Distance(origin, pos) > radius) { blocked = true; break; }

                    visible.Add(pos);

                    if (isOpaque(pos))
                    {
                        // 遇到阻挡，射线在此终止（阻挡格仍可视）
                        blocked = true;
                        break;
                    }
                }
                // 下一条射线继续（对 ring 上每个目标都重复）
            }
        }

        return visible;
    }

    /// <summary>
    /// 将可见集合应用到网格对象上（重置->设置）
    /// </summary>
    private void UpdateGridVisibility(HashSet<GridPosition> visiblePositions)
    {
        GridSystemHex<GridObject> gridSystem = LevelGrid.Instance.GetGridSystem();
        // 首先重置所有格子的可见性
        for (int x = 0; x < LevelGrid.Instance.GetWidth(); x++)
        {
            for (int z = 0; z < LevelGrid.Instance.GetHight(); z++)
            {
                GridPosition gridPos = new GridPosition(x, z);
                gridSystem.GetGridObject(gridPos).SetIsVisible(false);
            }
        }

        // 然后设置可见格子的状态
        foreach (var pos in visiblePositions)
        {
            if (LevelGrid.Instance.IsValidGridPosition(pos))
            {
                gridSystem.GetGridObject(pos).SetIsVisible(true);
            }
        }
    }
}
