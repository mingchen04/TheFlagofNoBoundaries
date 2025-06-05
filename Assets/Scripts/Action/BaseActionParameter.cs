/// <summary>
/// 操作参数 - 封装行动执行所需的所有参数
/// </summary>
[System.Serializable]
public class BaseActionParameter
{
    public BaseAction baseAction;          // 要执行的基础行动
    public GridPosition targetGridPosition; // 目标网格位置
    public GridPosition unitFinalPosition;  // 行动后单位的最终位置
    public int executeTurn;             // 执行轮次（1=第1轮执行，2=第2轮执行...）

    // 添加构造函数以便于创建
    public BaseActionParameter(BaseAction action, GridPosition targetPos, GridPosition finalPos, int order)
    {
        baseAction = action;
        targetGridPosition = targetPos;
        unitFinalPosition = finalPos;
        executeTurn = order;
    }

    // 用于调试输出
    public override string ToString()
    {
        return $"{baseAction.GetActionName()} (顺序:{executeTurn}) 目标:{targetGridPosition}";
    }
}