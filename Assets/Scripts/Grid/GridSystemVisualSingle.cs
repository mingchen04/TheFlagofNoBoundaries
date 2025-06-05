using UnityEngine;

public class GridSystemVisualSingle : MonoBehaviour
{
    [SerializeField] private MeshRenderer meshRenderer;
    [SerializeField] private GameObject SelectedGameObject;

    public void Show(Material material)
    {
        meshRenderer.material = material;
        meshRenderer.enabled = true;
    }

    public void Hide()
    {
        meshRenderer.enabled = false;
    }

    public void showSelected()
    {
        SelectedGameObject.SetActive(true);
    }

    public void hideSelected()
    {
        SelectedGameObject.SetActive(false);
    }
}
