using UnityEngine;

/// <summary>
/// 基础buff
/// </summary>
public abstract class BaseBuff
{
    protected string buffName;
    protected int effectiveTime; //buff有效回合数
    protected Unit unit;
    protected int stackCount;

    public BaseBuff( Unit unit, int effectiveTime)
    {
        this.unit = unit;
        this.effectiveTime = effectiveTime;
        stackCount = 1;
    }

    /// <summary>
    /// 修改单位攻击力
    /// </summary>
    public virtual int ModifyAttack(int origin) => origin;
    public virtual int ModifyDefense(int origin) => origin;
    public virtual int ModifyVisionRange(int origin) => origin;

    public virtual void OnRemove() { }
    public virtual void OnTurnStart() { }
    public virtual void OnTurnEnd()
    {
        ReduceDuration();
        if (effectiveTime <= 0)
        {
            OnRemove();
        }
    }

    public string GetBuffName() => buffName;
    public int GetEffectiveTime() => effectiveTime;
    public int GetStackCount() => stackCount;
    public void AddStack(int count = 1) => stackCount += count;
    public void ReduceDuration() => effectiveTime--;
}
