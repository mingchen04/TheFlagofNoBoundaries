using System;
using UnityEngine;

//IEquatable<T> 是 .NET 中的一个接口，用于为类型提供强类型的相等性比较方法 Equals
public struct GridPosition : IEquatable<GridPosition>
{

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
    /// 允许GridPosition隐式转换
    /// </summary>
    public static implicit operator GridPosition(Vector3 worldPosition)
    {
        return LevelGrid.Instance.GetGridPosition(worldPosition);
    }
}