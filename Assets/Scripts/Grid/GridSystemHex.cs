using System;
using System.Collections.Generic;
using UnityEngine;

public class GridSystemHex<TGridObject>
{
    private const float HEX_VERTICAL_OFFSET_MULTIPLIER = 0.75F;

    private int width;
    private int height;
    private float cellSize;
    private TGridObject[,] gridObjectsArray;

    //Action	无返回值的方法	Action<参数, 参数>
    //Func  有返回值的方法 Func<参数, 参数, 返回值>

    //构造函数
    public GridSystemHex(int width, int height, float cellSize,
        Func<GridSystemHex<TGridObject>, GridPosition, TGridObject> createTGridObject)
    {
        this.width = width;
        this.height = height;
        this.cellSize = cellSize;
        gridObjectsArray = new TGridObject[width, height];

        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < height; z++)
            {
                GridPosition gridPosition = new GridPosition(x, z);
                //使用泛型 new 需要使用where new()约束 且不支持多个参数 利用方法参数传递new
                //gridObjectsArray[x, z] = new TGridObject(this, GridPosition);
                gridObjectsArray[x, z] = createTGridObject(this, gridPosition);
            }
        }
    }

    public Vector3 GetWorldPositon(GridPosition gridPosition)
    {
        // 计算基础位置（不考虑行偏移）
        Vector3 worldPosition = new Vector3(gridPosition.x, 0, gridPosition.z * HEX_VERTICAL_OFFSET_MULTIPLIER) * cellSize;

        // 修正奇数行偏移（兼容负数坐标）
        bool isOddRow = Mathf.Abs(gridPosition.z % 2) == 1;
        if (isOddRow)
        {
            // 根据z坐标的正负决定偏移方向
            float offsetDirection = Mathf.Sign(gridPosition.z);
            worldPosition += new Vector3(0.5f * offsetDirection, 0, 0) * cellSize;
        }

        return worldPosition;
    }


    public GridPosition GetGridPosition(Vector3 worldPosition)
    {
        GridPosition roughXZ = new GridPosition(
                Mathf.RoundToInt(worldPosition.x / cellSize),
                Mathf.RoundToInt(worldPosition.z / cellSize / HEX_VERTICAL_OFFSET_MULTIPLIER)
            );

        bool oddRow = (roughXZ.z % 2f == 0);

        //获取周围的邻近节点
        List<GridPosition> neighbourGridPositionList = new List<GridPosition>
        {
            roughXZ + new GridPosition(+1,0),
            roughXZ + new GridPosition(-1,0),
            roughXZ + new GridPosition(0,+1),
            roughXZ + new GridPosition(0,-1),

            //根据奇偶行改变获取的位置
            roughXZ + new GridPosition(oddRow? -1:+1,+1),
            roughXZ + new GridPosition(oddRow? -1:+1,-1),
        };


        //获取距离最短的节点
        GridPosition closestGridPosition = roughXZ;
        foreach (GridPosition neighbourGridPosition in neighbourGridPositionList)
        {
            if (!IsValidGridPosition(neighbourGridPosition)) continue;

            Vector3 shortestWorldPosition = GetWorldPositon(closestGridPosition);
            Vector3 itemWorldposition = GetWorldPositon(neighbourGridPosition);

            if (Vector3.Distance(shortestWorldPosition, worldPosition) > Vector3.Distance(itemWorldposition, worldPosition))
            {
                closestGridPosition = neighbourGridPosition;
            }
        }

        return closestGridPosition;

    }

    public void CreateDebugObject(Transform debugPrefab)
    {
        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < height; z++)
            {
                GridPosition gridPosition = new GridPosition(x, z);
                Transform debugTransform = GameObject.Instantiate(debugPrefab, GetWorldPositon(gridPosition), Quaternion.identity);
                GridDebugObject gridDebugObject = debugTransform.GetComponent<GridDebugObject>();

                gridDebugObject.SetGridObject(GetGridObject(gridPosition));
            }
        }
    }

    public TGridObject GetGridObject(GridPosition gridPosition)
    {
        return gridObjectsArray[gridPosition.x, gridPosition.z];
    }

    public bool IsValidGridPosition(GridPosition gridPosition)
    {
        return
            gridPosition.x >= 0 &&
            gridPosition.z >= 0 &&
            gridPosition.x < width &&
            gridPosition.z < height;
    }

    public int GetWidth()
    {
        return width;
    }

    public int GetHeight()
    {
        return height;
    }

}
