
using UnityEngine;

[CreateAssetMenu(menuName = "Actions/GrenadeActionSO")]
public class GrenadeActionSO : BaseActionSO
{
    public override System.Type ActionType => typeof(GrenadeAction);
}
