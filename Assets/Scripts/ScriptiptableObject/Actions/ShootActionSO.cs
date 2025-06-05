using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Actions/ShootActionSO")]
public class ShootActionSO : BaseActionSO
{
    public override System.Type ActionType => typeof(ShootAction);
}
