using System;
using System.Collections.Generic;
using UnityEngine;

//IEquatable<T> 是 .NET 中的一个接口，用于为类型提供强类型的相等性比较方法 Equals
public struct GridPosition : IEquatable<GridPosition>
{

    public static GridPosition nullGridPosition = new(-1, -1); //定义一个空的网格位置

    //结构体作为函数参数时可被改变 class作为引用类型参数不能改变
    public int x;
    public int z;

    public GridPosition(int x, int z)
    {
        this.x = x;
        this.z = z;
    }

    public override bool Equals(object obj)
    {
        return obj is GridPosition position &&
               x == position.x &&
               z == position.z;
    }

    public bool Equals(GridPosition other)
    {
        return this == other;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(x, z);
    }

    public override string ToString()
    {
        //return "x:" + x + "; z:" + z ;
        return $"x:{x} z:{z}";
    }

    //重新定义运算符
    public static bool operator ==(GridPosition a, GridPosition b)
    {
        return a.x == b.x && a.z == b.z;
    }

    public static bool operator !=(GridPosition a, GridPosition b)
    {
        return !(a == b);
    }

    public static GridPosition operator +(GridPosition a, GridPosition b)
    {
        GridPosition gridPosition = new GridPosition(a.x + b.x, a.z + b.z);

        return gridPosition;
    }
    public static GridPosition operator -(GridPosition a, GridPosition b)
    {
        GridPosition gridPosition = new GridPosition(a.x - b.x, a.z - b.z);

        return gridPosition;
    }

    public static GridPosition operator *(GridPosition a, GridPosition b)
    {
        GridPosition gridPosition = new GridPosition(a.x * b.x, a.z * b.z);
        return gridPosition;
    }

    public static GridPosition operator *(GridPosition a, int k)
    => new GridPosition(a.x * k, a.z * k);

    /// <summary>
    /// 允许Vector3隐式转换
    /// </summary>
    public static implicit operator Vector3(GridPosition gridPosition)
    {
        return LevelGrid.Instance.GetWorldPosition(gridPosition);
    }

    /// <summary>
    /// 允许Vector2隐式转换
    /// </summary>
    public static implicit operator Vector2(GridPosition gridPosition)
    {
        Vector3 vector3 = LevelGrid.Instance.GetWorldPosition(gridPosition);
        Vector2 vector2 = new Vector2(vector3.x, vector3.z);

        return vector2;
    }

    /// <summary>
    /// 允许GridPosition隐式转换
    /// </summary>
    public static implicit operator GridPosition(Vector3 worldPosition)
    {
        return LevelGrid.Instance.GetGridPosition(worldPosition);
    }


}

public static class GridPositionExtensions
{
    // 计算六边形网格中的直线方向
    //this 关键字表示这是一个扩展方法
    public static GridPosition GetHexLineDirection(this GridPosition from, GridPosition to)
    {
        GridPosition delta = to - from;

        // 六边形网格的六个基本方向
        var hexDirections = new List<GridPosition>
        {
            new(+1, 0),  // 右
            new(-1, 0),  // 左
            new(0, -1), // 右下
            new(0, +1),  // 右上
            new(-1, -1),  // 左下
            new(-1, +1), // 左上           
        };

        // 找到最接近的方向
        GridPosition closestDirection = hexDirections[0];
        float minAngle = float.MaxValue;

        foreach (GridPosition dir in hexDirections)
        {

            //计算两个向量之间的夹角 例如从1，0到-1,0的夹角为180度，从0,1到1,1的夹角为45度
            float angle = Vector2.Angle(delta, dir);
            if (angle < minAngle)
            {
                minAngle = angle;
                closestDirection = dir;
                Debug.Log($"位置{(Vector2)delta} ：{(Vector2)dir} 夹角：{minAngle}");
            }
        }
        return closestDirection;
    }

    // 检查两点是否在一条直线上
    public static bool IsStraightLine(this GridPosition from, GridPosition to)
    {
        Vector2 delta = (Vector2)to - (Vector2)from;
        Vector2 direction = from.GetHexLineDirection(to); //获取最小夹角的向量

        // 检查delta是否是direction的整数倍
        if (direction.x == 0)
        {
            return delta.x == 0 && delta.y % direction.y == 0;
        }
        else if (direction.y == 0)
        {
            return delta.y == 0 && delta.x % direction.x == 0;
        }
        else
        {
            //计算 delta 在 x 方向与 direction.x 的比值 ratio。
            float ratio = delta.x / direction.x;
            return Mathf.Approximately(ratio, delta.y / direction.y);
        }
    }
}