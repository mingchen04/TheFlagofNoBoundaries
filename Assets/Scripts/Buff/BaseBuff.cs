/// <summary>
/// 基础buff
/// </summary>
public abstract class BaseBuff
{
    protected string buffName;
    protected int effectiveTime; //buff有效回合数
    protected Unit unit;

    public BaseBuff(string buffName, Unit unit,int effectiveTime)
    {

        this.buffName = buffName;
        this.unit = unit;
        this.effectiveTime = effectiveTime; 
    }

    public abstract void UseBuff();


    public string GetBuffName()
    {
        return buffName;
    }
    
    public virtual int GetEffectiveTime()
    {
        return effectiveTime;
    }
}
