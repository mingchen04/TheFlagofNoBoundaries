public class AtkUpBuff : BaseBuff
{
    private int atkBonus; //攻击力加成

    public AtkUpBuff(Unit unit, int bonus, int duration) : base(unit, duration)
    {
        buffName = "攻击力提升";
        atkBonus = bonus;
    }

    public override int ModifyAttack(int atk)
    {
        return atk + atkBonus;
    }

    public override void OnTurnEnd()
    {
        base.OnTurnEnd();
    }
}
