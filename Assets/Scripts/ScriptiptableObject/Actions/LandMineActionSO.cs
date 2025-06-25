using System;
using UnityEngine;

[CreateAssetMenu(menuName = "Actions/LandMineActionSO")]

public class LandMineActionSO : BaseActionSO
{
    public override Type ActionType => typeof(LandMineAction);
}
