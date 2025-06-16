using System;
using UnityEngine;

public abstract class BaseActionSO : ScriptableObject
{

    [Flags]
    public enum ActionTags
    {
        None = 0,
        Move = 1 << 0, //行动带有位移
        Damage = 1 << 1, //行动带有伤害
        Aoe = 1 << 2, //行动有范围效果
        Deploy = 1 << 3, //行动是部署行动
    }

    // 添加标签
    //actionTags |= ActionTags.Move;
    // 移除标签
    //actionTags &= ~ActionTags.Move;

    // 检查是否有此标签
    //bool = (actionTags & ActionTags.Move) == ActionTags.Move;
    // Flags扩展方法更简洁
    //bool = actionTags.HasFlag(ActionTags.Move);

    public abstract System.Type ActionType { get; }
    [SerializeField] public Sprite baseActionIcon;
    [SerializeField] public ActionTags actionTags;
}
