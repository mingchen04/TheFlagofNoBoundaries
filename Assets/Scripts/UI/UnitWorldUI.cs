using UnityEngine;
using UnityEngine.UI;
using static System.Collections.Specialized.BitVector32;

public class UnitWorldUI : MonoBehaviour
{
    [SerializeField] private Unit unit;
    [SerializeField] private Image healthBarImage;
    [SerializeField] private Image strengthBarImage;
    [SerializeField] private HealthSystem healthSystem;
    [SerializeField] private SrengthSystem strengthSystem;
    [SerializeField] private Transform buffPref; // Buff的UI预制体
    [SerializeField] private Transform buffListTransform; // 用于显示Buff的UI列表

    private float healthAnimFrom;
    private float healthAnimTo;
    private float healthAnimTimer;

    private float strengthAnimFrom;
    private float strengthAnimTo;
    private float strengthAnimTimer;

    private const float animationTimeMax = 0.3f;

    private void Start()
    {
        BuffListUpdateVisual();

        if (strengthSystem != null)
            strengthSystem.OnActionPointsChanged += StrengthSystem_OnActionPointsChanged;
        if (healthSystem != null)
            healthSystem.OnDamage += HealthSystem_OnDamage;

        unit.OnBuffListChange += Unit_OnBuffListChange;

        // 初始化填充值
        float healthNorm = healthSystem != null ? healthSystem.GetHealthNormalized() : 1f;
        float strengthNorm = strengthSystem != null ? strengthSystem.GetStrengthPointsNormalized() : 1f;

        healthBarImage.fillAmount = healthNorm;
        strengthBarImage.fillAmount = strengthNorm;

        healthAnimFrom = healthAnimTo = healthNorm;
        strengthAnimFrom = strengthAnimTo = strengthNorm;
        healthAnimTimer = strengthAnimTimer = animationTimeMax;
    }

    private void Unit_OnBuffListChange(object sender, System.EventArgs e)
    {
        BuffListUpdateVisual();
    }

    private void OnDestroy()
    {
        if (strengthSystem != null)
            strengthSystem.OnActionPointsChanged -= StrengthSystem_OnActionPointsChanged;
        if (healthSystem != null)
            healthSystem.OnDamage -= HealthSystem_OnDamage;
    }

    private void Update()
    {
        bool animating = false;

        // 血量动画
        if (healthAnimTimer < animationTimeMax)
        {
            healthAnimTimer += Time.deltaTime;
            float t = Mathf.Clamp01(healthAnimTimer / animationTimeMax);
            healthBarImage.fillAmount = Mathf.Lerp(healthAnimFrom, healthAnimTo, t);
            animating = true;
        }

        // 体力动画
        if (strengthAnimTimer < animationTimeMax)
        {
            strengthAnimTimer += Time.deltaTime;
            float t = Mathf.Clamp01(strengthAnimTimer / animationTimeMax);
            strengthBarImage.fillAmount = Mathf.Lerp(strengthAnimFrom, strengthAnimTo, t);
            animating = true;
        }

        // 避免多余的Update
        if (!animating)
        {
            healthAnimTimer = strengthAnimTimer = animationTimeMax;
        }
    }

    private void HealthSystem_OnDamage(object sender, System.EventArgs e)
    {
        if (healthSystem == null || healthBarImage == null) return;
        healthAnimFrom = healthBarImage.fillAmount;
        healthAnimTo = healthSystem.GetHealthNormalized();
        healthAnimTimer = 0f;
    }

    private void StrengthSystem_OnActionPointsChanged(object sender, System.EventArgs e)
    {
        if (strengthSystem == null || strengthBarImage == null) return;
        strengthAnimFrom = strengthBarImage.fillAmount;
        strengthAnimTo = strengthSystem.GetStrengthPointsNormalized();
        strengthAnimTimer = 0f;
    }

    private void BuffListUpdateVisual()
    {
        foreach (Transform buff in buffListTransform)
        {
            Destroy(buff.gameObject);
        }

        foreach (BaseBuff buff in unit.GetBuffList())
        {
            Transform buffUI = Instantiate(buffPref, buffListTransform);
            buffUI.GetComponent<BuffUI>().SetIcon(ScriptiptableSOManager.Instance.GetBuffSO(buff).buffIcon);
        }
    }
}
