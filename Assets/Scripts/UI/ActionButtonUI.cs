using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ActionButtonUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI textMeshPro;
    [SerializeField] private Button button;
    [SerializeField] private GameObject selectedGameObject;
    [SerializeField] private Image baseActionImage;
    
    private BaseAction baseAction;

    public void SetBaseAction(BaseAction baseAction)
    {
        textMeshPro.text = baseAction.GetActionName().ToUpper();
        this.baseAction = baseAction;
        button.onClick.AddListener(() => { UnitActionSystem.Instance.SetSelectedAction(baseAction); });
    }

    public void SetBaseActionImage(Sprite sprite) {
        baseActionImage.sprite = sprite;
    }

    public void UpdateSelectedVisual()
    {
        if(UnitActionSystem.Instance.GetSelectedAction() == baseAction)
        {
            selectedGameObject.SetActive(true);
        }
        else
        {
            selectedGameObject.SetActive(false);
        }        
    }
}
