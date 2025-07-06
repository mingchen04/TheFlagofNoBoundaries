using UnityEngine;
using UnityEngine.UI;

public class BuffUI : MonoBehaviour
{
    [SerializeField] private Image iconTransform;

    public void SetIcon(Sprite icon)
    {
        iconTransform.sprite = icon;
    }
}
