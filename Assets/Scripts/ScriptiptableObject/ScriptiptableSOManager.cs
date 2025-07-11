using System.Collections.Generic;
using UnityEngine;

public class ScriptiptableSOManager : MonoBehaviour
{
    public static ScriptiptableSOManager Instance { get; private set; }
    [SerializeField] private List<BaseActionSO> baseActionListSO;
    [SerializeField] private List<BaseBuffSO> baseBuffSOs;

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

    public ActionTags GetActionTags(BaseAction baseAction)
    {
        return GetBaseActionSO(baseAction).actionTags;
    }

    private BaseActionSO GetBaseActionSO(BaseAction baseAction)
    {
        foreach (BaseActionSO baseActionSO in baseActionListSO)
        {
            if (baseActionSO.actionType.ToString() == baseAction.GetType().ToString())
            {
                return baseActionSO;
            }
        }
        return null;
    }

    public BaseBuffSO GetBuffSO(BaseBuff baseBuff)
    {
        foreach (BaseBuffSO baseBuffSO in baseBuffSOs)
        {
            if (baseBuffSO.bufftype.ToString() == baseBuff.GetType().ToString())
            {
                return baseBuffSO;
            }
        }
        return null;
    }
}
