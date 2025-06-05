using UnityEngine;

[CreateAssetMenu(menuName = "Actions/InteractActionSO")]
public class InteractActionSO : BaseActionSO
{
    public override System.Type ActionType => typeof(InteractAction);
}
