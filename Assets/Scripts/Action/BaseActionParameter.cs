/// <summary>
/// 操作参数 - 封装行动执行所需的所有参数
/// </summary>
[System.Serializable]
public class BaseActionParameter
{
    /// <summary>
    /// 要执行的基础行动  
    /// </summary>
    public BaseAction baseAction;
    /// <summary>
    /// 目标网格位置
    /// </summary>
    public GridPosition targetGridPosition;
    /// <summary>
    /// 行动后单位的最终位置
    /// </summary>
    public GridPosition unitFinalPosition;
    /// <summary>
    /// 执行轮次（1=第1轮执行，2=第2轮执行...）
    /// </summary>
    public int executeTurn; 

    // 添加构造函数以便于创建
    public BaseActionParameter(BaseAction action, GridPosition targetPos, GridPosition finalPos, int executeTurn)
    {
        baseAction = action;
        targetGridPosition = targetPos;
        unitFinalPosition = finalPos;
        this.executeTurn = executeTurn;
    }
}