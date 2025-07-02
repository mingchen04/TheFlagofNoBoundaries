using UnityEngine;

public class AtkUpBuff : BaseBuff
{
    [SerializeField] private int atkBonus; //攻击力加成

    public AtkUpBuff(Unit unit, int bonus, int duration) : base("攻击力提升", unit, duration)
    {
        atkBonus = bonus;
    }

    public override int ModifyAttack(int atk)
    {
        return atk + 10;
    }

    public override void OnTurnEnd()
    {       
        ReduceDuration();
        if (GetEffectiveTime() <= 0)
        {
            OnRemove();
        }
    }
}
