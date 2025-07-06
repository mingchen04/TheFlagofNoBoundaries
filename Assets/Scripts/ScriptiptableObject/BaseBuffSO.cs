using UnityEngine;

public enum BuffType
{
    AtkUpBuff
}

[CreateAssetMenu(menuName = "BaseBuffSO")]
public class BaseBuffSO : ScriptableObject
{
    public BuffType bufftype;
    public Sprite buffIcon;
}
