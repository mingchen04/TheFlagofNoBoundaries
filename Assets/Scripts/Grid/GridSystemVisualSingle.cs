using UnityEngine;

public class GridSystemVisualSingle : MonoBehaviour
{
    [SerializeField] private SpriteRenderer fogOfWar; //战争迷雾
    [SerializeField] private MeshRenderer meshRenderer;
    [SerializeField] private MeshRenderer selectedView;

    public void Show(Material material)
    {
        meshRenderer.material = material;
        meshRenderer.enabled = true;
    }

    public void Hide()
    {
        meshRenderer.enabled = false;
    }

    public void ShowSelected()
    {
        selectedView.enabled = true;
    }

    public void HideSelected()
    {
        selectedView.enabled = false;
    }

    public void ShowFogOfWar()
    {
        fogOfWar.enabled = true;
    }

    public void HideFogOfWar()
    {
        fogOfWar.enabled = false;
    }
}
