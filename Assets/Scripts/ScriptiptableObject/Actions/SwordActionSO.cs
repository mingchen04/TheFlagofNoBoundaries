using UnityEngine;


[CreateAssetMenu(menuName = "Actions/SwordActionSO")]
public class SwordActionSO : BaseActionSO
{
    public override System.Type ActionType => typeof(SwordAction);
}