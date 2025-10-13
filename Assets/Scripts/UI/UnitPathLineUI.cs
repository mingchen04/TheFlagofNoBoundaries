using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

public class UnitPathLineUI : MonoBehaviour
{
    private Unit unit;
    private LineRenderer lineRenderer; //线条渲染器，用于显示路径
    private bool isSelected = false;

    private void Awake()
    {
        unit = GetComponent<Unit>();
        unit.GetAction<MoveAction>().OnStopMoving += UnitPathLineUI_OnStopMoving;
        LineRendererAwake();
    }
    
    private void Start()
    {
        ActionQueue.OnAnyActionCompleted += ActionQueue_OnAnyActionCompleted;
        UnitActionSystem.Instance.OnSelectedUnitChanged += UnitActionSystem_OnSelectedUnitChanged;
        Unit.OnAnyUnitPositionChanged += Unit_OnAnyUnitPositionChanged;
    }

    private void OnDestroy()
    {
        ActionQueue.OnAnyActionCompleted -= ActionQueue_OnAnyActionCompleted;
        UnitActionSystem.Instance.OnSelectedUnitChanged -= UnitActionSystem_OnSelectedUnitChanged;
        Unit.OnAnyUnitPositionChanged -= Unit_OnAnyUnitPositionChanged;
    }

    private void ActionQueue_OnAnyActionCompleted(object sender, System.EventArgs e)
    {
        UpdatePathLine();
    }

    private void Unit_OnAnyUnitPositionChanged(object sender, System.EventArgs e)
    {
        UpdatePathLine();
    }

    private void UnitPathLineUI_OnStopMoving(object sender, System.EventArgs e)
    {
        UpdatePathLine();
    }

    private void UnitActionSystem_OnSelectedUnitChanged(object sender, System.EventArgs e)
    {
        isSelected = UnitActionSystem.Instance.IsUnitSelected(unit);
        UpdatePathLine();
    }

    private void UpdatePathLine()
    {
        if (isSelected)
        {
            ShowPathLine();
        }
        else
        {
            HidePathLine();
        }
    }

    /// <summary>
    /// 显示路径线
    /// </summary>
    public void ShowPathLine()
    {
        List<GridPosition> path = unit.GetActionQueue().GetAllPath();
        if (path == null)
        {
            lineRenderer.enabled = false;
            return;
        }
        List<Vector3> worldPositions = path.Select(p => LevelGrid.Instance.GetWorldPosition(p)).ToList();
        worldPositions.Insert(0, transform.position); // 添加当前单位位置到路径开头

        for (int i = 0; i < worldPositions.Count; i++)
        {
            worldPositions[i] = new Vector3(worldPositions[i].x, 1f, worldPositions[i].z); // 调整高度
        }

        lineRenderer.positionCount = worldPositions.Count;
        lineRenderer.SetPositions(worldPositions.ToArray());
        lineRenderer.enabled = true;
    }

    public void HidePathLine()
    {
        if (lineRenderer == null) return;
        lineRenderer.enabled = false;
    }

    private void LineRendererAwake()
    {
        // 初始化LineRenderer
        lineRenderer = gameObject.AddComponent<LineRenderer>();
        lineRenderer.positionCount = 0;
        lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        lineRenderer.widthMultiplier = 0.15f;
        lineRenderer.startColor = Color.cyan;
        lineRenderer.endColor = Color.cyan;
        lineRenderer.enabled = false;
    }
}
