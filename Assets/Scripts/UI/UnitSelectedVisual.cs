using System;
using UnityEngine;

public class UnitSelectedVisual : MonoBehaviour
{
    [SerializeField] private Unit unit;
    private Animator selectedAnimator;
    private MeshRenderer meshRenderer;

    private void Awake()
    {
        meshRenderer = GetComponent<MeshRenderer>();
        selectedAnimator = GetComponent<Animator>();
    }

    private void Start()
    {
        meshRenderer.enabled = false;
        unit.OnActionAddQueueSuccess += Unit_OnActionAddQueueSuccess;
        unit.OnActionAddQueueFailed += Unit_OnActionAddQueueFailed;

        UnitActionSystem.Instance.OnSelectedUnitChanged += UnitActionSystem_OnSelectedUnitChanged;
        UpdateVisual();
    }

    private void Unit_OnActionAddQueueFailed(object sender, EventArgs e)
    {
        selectedAnimator.SetTrigger("Fail");
    }

    private void Unit_OnActionAddQueueSuccess(object sender, EventArgs e)
    {
        selectedAnimator.SetTrigger("Success");
    }

    private void UnitActionSystem_OnSelectedUnitChanged(object sender, EventArgs e)
    {
        UpdateVisual();
    }

    private void UpdateVisual()
    {
        if (UnitActionSystem.Instance.IsUnitSelected(unit))
        {
            meshRenderer.enabled = true;
        }
        else
        {
            meshRenderer.enabled = false;
        }
    }

    private void OnDestroy()
    {
        UnitActionSystem.Instance.OnSelectedUnitChanged -= UnitActionSystem_OnSelectedUnitChanged;
        unit.OnActionAddQueueSuccess -= Unit_OnActionAddQueueSuccess;
        unit.OnActionAddQueueFailed -= Unit_OnActionAddQueueFailed;
    }
}
