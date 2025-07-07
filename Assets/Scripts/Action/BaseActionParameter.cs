using System;
using System.Collections.Generic;

/// <summary>
/// 操作参数 - 封装行动执行所需的所有参数
/// </summary>
[Serializable]
public class BaseActionParameter
{
    /// <summary>
    /// 要执行的基础行动  
    /// </summary>
    public BaseAction baseAction;
    /// <summary>
    /// 存储目标位置的列表 部分行动可能需要多个目标位置
    /// </summary> 
    public List<GridPosition> targetPositionList;
    /// <summary>
    /// 行动后单位的最终位置
    /// </summary>
    public GridPosition unitFinalPosition;
    /// <summary>
    /// 执行轮次（1=第1轮执行，2=第2轮执行...）
    /// </summary>
    public int executeTurn;

    
    // 添加构造函数以便于创建
    public BaseActionParameter(BaseAction action, List<GridPosition> targetPos, GridPosition finalPos, int executeTurn)
    {
        baseAction = action;
        targetPositionList = targetPos;
        unitFinalPosition = finalPos;
        this.executeTurn = executeTurn;
    }

    public int GetActionPoint()
    {
        // 获取基础行动的行动点消耗
        return baseAction.GetActionPointsCost(this);
    }
}