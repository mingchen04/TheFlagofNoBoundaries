using UnityEngine;

public class UnitVision : MonoBehaviour
{
    [SerializeField] private Renderer[] excludeRenderers; // 排除的渲染器

    // 新增：单位可见性属性
    private bool isVisible = true;
    public bool IsVisible() => isVisible;
    public void SetVisible(bool visible)
    {
        isVisible = visible;
        // 可选：自动刷新渲染器
        UpdateVisibility();
    }

    // 新增：根据可见性隐藏/显示渲染器
    private void UpdateVisibility()
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        Canvas[] canvases = GetComponentsInChildren<Canvas>();

        foreach (Renderer renderer in renderers)
        {
            if (excludeRenderers != null 
                && System.Array.Exists(excludeRenderers, r => r == renderer))
                continue; // 排除特定渲染器
            renderer.enabled = isVisible;
        }

        foreach (Canvas canvas in canvases)
        {
            canvas.enabled = isVisible;
        }

    }
}
