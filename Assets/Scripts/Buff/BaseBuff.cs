/// <summary>
/// 基础buff
/// </summary>
public abstract class BaseBuff
{
    protected string buffName;
    protected int effectiveTime; //buff有效回合数
    protected Unit unit;

    public virtual void UseBuff(Unit unit)
    {
        this.unit = unit;
    }

    public string GetBuffName()
    {
        return buffName;
    }
    
    public virtual int GetEffectiveTime()
    {
        return effectiveTime;
    }
}
