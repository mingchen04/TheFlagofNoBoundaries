using System;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

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
    }

    void Start()
    {
        UpdateFriendlyUnitsVision();

        // 注册事件监听
        Unit.OnAnyUnitSpawned += Unit_OnAnyUnitSpawned;
        Unit.OnAnyUnitDead += Unit_OnAnyUnitDead;
        LevelGrid.Instance.OnAnyUnitMoveGridPosition += LevelGrid_OnAnyUnitMoveGridPosition;
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


    // 更新所有友方单位的视野
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
    public HashSet<GridPosition> ComputeHexShadowcastingSector(GridPosition origin, int radius, Func<GridPosition, bool> isOpaque, int sideOffset = 1)
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

    // --------------------------------
    // 辅助：cube 坐标结构与转换 / line / ring / distance 等
    // 说明：偏移坐标为 odd-r（GetWorldPosition 使用的奇数行右移逻辑），
    // 因此这里采用 odd-r <-> cube 的转换公式（来自 Amit / RedBlobGames）。
    // --------------------------------

    private struct Cube
    {
        public int x;
        public int y;
        public int z;
        public Cube(int x, int y, int z) { this.x = x; this.y = y; this.z = z; }
        public static Cube operator +(Cube a, Cube b) => new Cube(a.x + b.x, a.y + b.y, a.z + b.z);
        public static Cube operator -(Cube a, Cube b) => new Cube(a.x - b.x, a.y - b.y, a.z - b.z);
        public static Cube operator *(Cube a, int k) => new Cube(a.x * k, a.y * k, a.z * k);
    }

    // cube 的 6 个方向向量（标准）
    private static readonly Cube[] cubeDirections = new Cube[]
    {
        new Cube(+1, -1, 0),
        new Cube(+1, 0, -1),
        new Cube(0, +1, -1),
        new Cube(-1, +1, 0),
        new Cube(-1, 0, +1),
        new Cube(0, -1, +1),
    };

    // odd-r offset -> cube
    private Cube OffsetToCube(GridPosition offset)
    {
        int col = offset.x;
        int row = offset.z;
        int x = col - (row - (row & 1)) / 2;
        int z = row;
        int y = -x - z;
        return new Cube(x, y, z);
    }

    // cube -> odd-r offset
    private GridPosition CubeToOffset(Cube cube)
    {
        int x = cube.x;
        int z = cube.z;
        int col = x + (z - (z & 1)) / 2;
        int row = z;
        return new GridPosition(col, row);
    }

    private int CubeDistance(Cube a, Cube b)
    {
        return Math.Max(Math.Abs(a.x - b.x), Math.Max(Math.Abs(a.y - b.y), Math.Abs(a.z - b.z)));
    }

    // 线性插值（cube float）
    private (float x, float y, float z) CubeLerp(Cube a, Cube b, float t)
    {
        return (a.x + (b.x - a.x) * t, a.y + (b.y - a.y) * t, a.z + (b.z - a.z) * t);
    }

    // 四舍五入到最近的整数 cube（来自 redblob 的 hex_round）
    private Cube CubeRound((float x, float y, float z) frac)
    {
        int rx = Mathf.RoundToInt(frac.x);
        int ry = Mathf.RoundToInt(frac.y);
        int rz = Mathf.RoundToInt(frac.z);

        float xDiff = Math.Abs(rx - frac.x);
        float yDiff = Math.Abs(ry - frac.y);
        float zDiff = Math.Abs(rz - frac.z);

        if (xDiff > yDiff && xDiff > zDiff)
        {
            rx = -ry - rz;
        }
        else if (yDiff > zDiff)
        {
            ry = -rx - rz;
        }
        else
        {
            rz = -rx - ry;
        }

        return new Cube(rx, ry, rz);
    }

    // 两个 cube 之间的直线（返回包含两端点的 cube 列表）
    private List<Cube> HexLine(Cube a, Cube b)
    {
        int N = CubeDistance(a, b);
        List<Cube> results = new List<Cube>();
        for (int i = 0; i <= N; i++)
        {
            var frac = CubeLerp(a, b, N == 0 ? 0f : (float)i / (float)N);
            Cube c = CubeRound(frac);
            results.Add(c);
        }
        return results;
    }

    // 从 origin 的 radius r 的 ring（按顺序返回 ring 上的 cube 坐标）
    private List<Cube> HexRing(Cube origin, int radius)
    {
        List<Cube> results = new List<Cube>();
        if (radius == 0)
        {
            results.Add(origin);
            return results;
        }

        // start = origin + direction[4] * radius  (任意固定方向都可)
        Cube cube = origin + cubeDirections[4] * radius;
        for (int side = 0; side < 6; side++)
        {
            for (int step = 0; step < radius; step++)
            {
                results.Add(cube);
                cube = cube + cubeDirections[side];
            }
        }
        return results;
    }
}
