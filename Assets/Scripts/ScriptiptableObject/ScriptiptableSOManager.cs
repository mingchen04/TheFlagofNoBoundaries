using UnityEngine;

public class ScriptiptableSOManager : MonoBehaviour
{
    public static ScriptiptableSOManager Instance { get; private set; }
    [SerializeField] private BaseActionListSO BaseActionListSO;

    private void Awake()
    {
        if (Instance != null)
        {
            Debug.Log("There's more than one ScriptiptableSOManager! " + transform + " - " + Instance);
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public Sprite GetActionIcon(BaseAction baseAction)
    {
        return GetBaseActionSO(baseAction).baseActionIcon;
    }

    public BaseActionSO.ActionTags GetActionTags(BaseAction baseAction)
    {
        return GetBaseActionSO(baseAction).actionTags;
    }

    private BaseActionSO GetBaseActionSO(BaseAction baseAction)
    {
        foreach (BaseActionSO baseActionSO in BaseActionListSO.actionListSO)
        {
            if (baseActionSO.ActionType == baseAction.GetType())
            {
                return baseActionSO;
            }
        }
        return null;
    }
}
