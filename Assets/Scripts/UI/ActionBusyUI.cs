using UnityEngine;

public class ActionBusyUI : MonoBehaviour
{
    private void Awake()
    {
        UnitActionSystem.Instance.OnBusyChanged += UnitActionSystem_OnActionBusyChanged;
    }

    private void Start()
    {
        Hide();
    }

    private void UnitActionSystem_OnActionBusyChanged(object sender, bool isBusy)
    {
        if (isBusy)
        {
            Show();
        }
        else
        {
            Hide();
        }
    }

    private void Show()
    {
        gameObject.SetActive(true);
    }

    private void Hide()
    {
        gameObject.SetActive(false);
    }
}
