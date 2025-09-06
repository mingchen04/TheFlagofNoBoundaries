using System;
using UnityEngine;

[Flags]
public enum ActionTags
{
    None = 0,
    Move = 1 << 0, //行动带有位移
    Damage = 1 << 1, //行动带有伤害
    Aoe = 1 << 2, //行动有范围效果
    Deploy = 1 << 3, //行动是部署类
}
// 添加标签
//actionTags |= ActionTags.Move;
// 移除标签
//actionTags &= ~ActionTags.Move;

// 检查是否有此标签
//bool = (actionTags & ActionTags.Move) == ActionTags.Move;
// Flags扩展方法更简洁
//bool = actionTags.HasFlag(ActionTags.Move);

public enum ActionType
{
    AttackMagicAction,
    GrenadeAction,
    InteractAction,
    LandMineAction,
    MoveAction,
    ShootAction,
    SpinAction,
    SwordAction,
    MobileSlashAction
}

[CreateAssetMenu(menuName = "BaseActionSO")]
public class BaseActionSO : ScriptableObject
{
    public string actionName;
    public Sprite baseActionIcon;
    public ActionType actionType;
    public ActionTags actionTags;
}
