public class AtkUpBuff : BaseBuff
{
    private int atkBonus; //攻击力加成

    /// <summary>
    /// 初始化buff
    /// </summary>
    /// <param name="unit">收益单位</param>
    /// <param name="bonus">增加的伤害</param>
    /// <param name="effectiveTime">持续回合</param>
    public AtkUpBuff(Unit unit, int bonus, int effectiveTime) : base(unit, effectiveTime)
    {
        buffName = "攻击力提升";
        atkBonus = bonus;
    }

    public override int ModifyAttack(int atk)
    {
        return atk + atkBonus;
    }
}
