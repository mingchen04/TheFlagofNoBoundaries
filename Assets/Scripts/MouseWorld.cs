using Unity.VisualScripting;
using UnityEngine;

public class MouseWorld : MonoBehaviour
{
    private static MouseWorld instance;

    [SerializeField] private LayerMask mousePlaneLayerMask;
    [SerializeField] private LayerMask mouseUnitLayerMask;

    private void Awake()
    {
        instance = this;
    }

    public static Vector3 GetPosition()
    {
        TryGetMouseRaycastHit(instance.mousePlaneLayerMask, out RaycastHit raycastHit);
        return raycastHit.point;
    }

    public static bool TryGetUnit(out Unit unit)
    {
        unit = null;
        bool isUnit = TryGetMouseRaycastHit(instance.mouseUnitLayerMask, out RaycastHit raycastHit);
        GridPosition mousePosition = GetPosition();

        if (isUnit)
        {
            unit = raycastHit.transform.GetComponent<Unit>();
        }
        else if (LevelGrid.Instance.IsValidGridPosition(mousePosition))
        {
            unit = LevelGrid.Instance.GetUnitAtGridPosition(mousePosition);
        }

        if (unit != null)
        {
            return true;
        }

        return false;
    }

    private static bool TryGetMouseRaycastHit(LayerMask layerMask, out RaycastHit raycastHit)
    {
        //Debug.Log(Input.mousePosition); //鼠标在屏幕的位置
        Ray ray = Camera.main.ScreenPointToRay(InputManager.Instance.GetMouseScreenPosition());//摄像机根据鼠标位置发送射线
        return Physics.Raycast(ray, out raycastHit, float.MaxValue, layerMask); //检测是否有碰撞物体 有则为true;
    }
}
