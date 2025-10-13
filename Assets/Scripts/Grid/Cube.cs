using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Cube 坐标系（适用于六边形格子）
/// </summary>
public struct Cube
{
    // --------------------------------
    // 辅助：cube 坐标结构与转换 / line / ring / distance 等
    // 说明：偏移坐标为 odd-r（GetWorldPosition 使用的奇数行右移逻辑），
    // 因此这里采用 odd-r <-> cube 的转换公式（来自 Amit / RedBlobGames）。
    // --------------------------------
    public int x;
    public int y;
    public int z;
    public Cube(int x, int y, int z) { this.x = x; this.y = y; this.z = z; }
    public static Cube operator +(Cube a, Cube b) => new Cube(a.x + b.x, a.y + b.y, a.z + b.z);
    public static Cube operator -(Cube a, Cube b) => new Cube(a.x - b.x, a.y - b.y, a.z - b.z);
    public static Cube operator *(Cube a, int k) => new Cube(a.x * k, a.y * k, a.z * k);

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
    public static Cube OffsetToCube(GridPosition offset)
    {
        int col = offset.x;
        int row = offset.z;
        int x = col - (row - (row & 1)) / 2;
        int z = row;
        int y = -x - z;
        return new Cube(x, y, z);
    }

    // cube -> odd-r offset
    public static GridPosition CubeToOffset(Cube cube)
    {
        int x = cube.x;
        int z = cube.z;
        int col = x + (z - (z & 1)) / 2;
        int row = z;
        return new GridPosition(col, row);
    }

    public static int CubeDistance(Cube a, Cube b)
    {
        return Math.Max(Math.Abs(a.x - b.x), Math.Max(Math.Abs(a.y - b.y), Math.Abs(a.z - b.z)));
    }

    // 线性插值（cube float）
    public static (float x, float y, float z) CubeLerp(Cube a, Cube b, float t)
    {
        return (a.x + (b.x - a.x) * t, a.y + (b.y - a.y) * t, a.z + (b.z - a.z) * t);
    }

    // 四舍五入到最近的整数 cube（来自 redblob 的 hex_round）
    public static Cube CubeRound((float x, float y, float z) frac)
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
    public static List<Cube> HexLine(Cube a, Cube b)
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
    public static List<Cube> HexRing(Cube origin, int radius)
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