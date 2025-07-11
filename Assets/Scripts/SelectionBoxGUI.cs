using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 使用GUI绘制鼠标选择框的组件
/// 注意：需要挂载到场景中任意激活的GameObject上
/// </summary>
public class SelectionBoxGUI : MonoBehaviour
{
    public static event EventHandler OnClick;

    // 绘制线条的材质
    private Material lineMaterial;
    // 鼠标按下时的起始屏幕坐标
    private Vector2 startScreenPos;
    // 当前鼠标位置的屏幕坐标
    private Vector2 currentScreenPos;
    // 是否正在选择中
    private bool isSelecting = false;
    // 添加拖拽阈值(像素)，防止轻微移动被误判为拖拽
    private const float DRAG_THRESHOLD = 10f;

    private void Awake()
    {
        CreateLineMaterial(); // 初始化绘制材质
    }

    /// <summary>
    /// 创建用于GL绘制的线材质
    /// </summary>
    private void CreateLineMaterial()
    {
        // 使用UI/Default着色器创建材质
        lineMaterial = new Material(Shader.Find("UI/Default"));
        // 隐藏材质防止在Project视图中显示
        lineMaterial.hideFlags = HideFlags.HideAndDontSave;
    }

    ///之后把鼠标操作的代码移动到MouseWorld
    private void Update()
    {      

        if (InputManager.Instance.IsMouseButtonDownThisFrame())
        {
            startScreenPos = InputManager.Instance.GetMouseScreenPosition();
            isSelecting = true;
        }

        if (isSelecting)
        {
            currentScreenPos = InputManager.Instance.GetMouseScreenPosition();
        }

        if (InputManager.Instance.IsMouseButtonUpThisFrame())
        {
            // 检查是否达到拖拽阈值
            if (Vector2.Distance(startScreenPos, currentScreenPos) > DRAG_THRESHOLD)
            {
                // 拖拽结束后处理单位选择
                SelectUnits(startScreenPos, currentScreenPos);
            }
            else
            {
                OnClick?.Invoke(this, new EventArgs());
            }

            isSelecting = false;
        }
    }


    /// <summary>
    /// GUI绘制回调函数
    /// </summary>
    private void OnGUI()
    {
        if (!isSelecting) return;

        // 将屏幕坐标(左下角为原点)转换为GUI坐标(左上角为原点)
        Vector2 guiStart = new Vector2(startScreenPos.x, Screen.height - startScreenPos.y);
        Vector2 guiCurrent = new Vector2(currentScreenPos.x, Screen.height - currentScreenPos.y);

        // 获取选择框的矩形区域
        Rect selectionRect = GetScreenRect(guiStart, guiCurrent);
        // 绘制选择框(边框宽度2px，白色)
        DrawScreenRect(selectionRect, 2, Color.white);
    }

    /// <summary>
    /// 根据两个屏幕点计算矩形区域
    /// </summary>
    /// <param name="screenPos1">点1</param>
    /// <param name="screenPos2">点2</param>
    /// <returns>规范化后的矩形</returns>
    private Rect GetScreenRect(Vector2 screenPos1, Vector2 screenPos2)
    {
        // 确保无论从哪个方向拖动都能形成正确的矩形
        Vector2 topLeft = Vector2.Min(screenPos1, screenPos2);
        Vector2 bottomRight = Vector2.Max(screenPos1, screenPos2);

        // 返回最小/最大坐标定义的矩形
        return Rect.MinMaxRect(topLeft.x, topLeft.y, bottomRight.x, bottomRight.y);
    }

    /// <summary>
    /// 绘制屏幕矩形
    /// </summary>
    /// <param name="rect">矩形区域</param>
    /// <param name="thickness">边框厚度</param>
    /// <param name="color">颜色</param>
    private void DrawScreenRect(Rect rect, float thickness, Color color)
    {
        // 创建1x1的临时纹理用于绘制
        Texture2D texture = new Texture2D(1, 1);
        texture.SetPixel(0, 0, color);
        texture.Apply();

        // 绘制四条边组成矩形边框
        // 上边
        GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width, thickness), texture);
        // 下边
        GUI.DrawTexture(new Rect(rect.x, rect.y + rect.height - thickness, rect.width, thickness), texture);
        // 左边
        GUI.DrawTexture(new Rect(rect.x, rect.y, thickness, rect.height), texture);
        // 右边
        GUI.DrawTexture(new Rect(rect.x + rect.width - thickness, rect.y, thickness, rect.height), texture);

        // 可选：绘制半透明填充(alpha值为0.1)
        Color fillColor = new Color(color.r, color.g, color.b, 0.1f);
        texture.SetPixel(0, 0, fillColor);
        texture.Apply();
        GUI.DrawTexture(rect, texture);

        // 销毁临时纹理防止内存泄漏
        DestroyImmediate(texture);
    }

    /// <summary>
    /// 选择矩形区域内的单位
    /// </summary>
    /// <param name="start">起始屏幕坐标</param>
    /// <param name="end">结束屏幕坐标</param>
    private void SelectUnits(Vector2 start, Vector2 end)
    {
        Bounds viewportBounds = GetViewportBounds(start, end);

        UnitActionSystem.Instance.ClearSelectedUnits();
        List<Unit> selectedUnits = new List<Unit>();
        foreach (Unit unit in UnitManager.Instance.GetFriendlyUnitList())
        {
            Vector3 viewportPos = Camera.main.WorldToViewportPoint(unit.transform.position);
            viewportPos.z = 0;
            
            if (viewportBounds.Contains(viewportPos) && !unit.IsEnemy())
            {
                
                selectedUnits.Add(unit);
            }            
        }
        UnitActionSystem.Instance.AddSelectedUnit(selectedUnits);
    }

    /// <summary>
    /// 将屏幕坐标转换为视口边界
    /// </summary>
    private Bounds GetViewportBounds(Vector2 screenPos1, Vector2 screenPos2)
    {
        // 屏幕坐标转视口坐标(0-1范围)
        Vector3 v1 = Camera.main.ScreenToViewportPoint(screenPos1);
        Vector3 v2 = Camera.main.ScreenToViewportPoint(screenPos2);

        // 计算最小/最大边界
        Vector3 min = Vector3.Min(v1, v2);
        Vector3 max = Vector3.Max(v1, v2);

        // 创建并设置边界
        Bounds bounds = new Bounds();
        bounds.SetMinMax(min, max);
        return bounds;
    }
}