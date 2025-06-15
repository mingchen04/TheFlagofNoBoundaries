using UnityEngine;
using UnityEngine.UI;

public class UnitWorldUI : MonoBehaviour
{
    [SerializeField] private Unit unit;
    [SerializeField] private Image healthBarImage;
    [SerializeField] private Image strengthBarImage;
    [SerializeField] private HealthSystem healthSystem;
    [SerializeField] private SrengthSystem strengthSystem;

    private float healthOldfillAmount;
    private bool isHealthChange;

    private float strengthOldfillAmount;
    private bool isStrengthChange;

    private const float animationTimeMax = 0.3f;
    private float animationTimer = animationTimeMax;

    private void Start()
    {
        if (strengthSystem != null)
            strengthSystem.OnActionPointsChanged += StrengthSystem_OnActionPointsChanged;
        if (healthSystem != null)
            healthSystem.OnDamage += HealthSystem_OnDamage;

        // 初始化填充值，避免第一次动画异常
        healthOldfillAmount = healthSystem != null ? healthSystem.GetHealthNormalized() : 1f;
        strengthOldfillAmount = strengthSystem != null ? strengthSystem.GetStrengthPointsNormalized() : 1f;

        UpdateHealthBarImmediate();
        UpdateActionPointsBarImmediate();
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
        bool needUpdate = false;

        if (isHealthChange)
        {
            animationTimer += Time.deltaTime;
            float t = Mathf.Clamp01(animationTimer / animationTimeMax);
            float target = healthSystem != null ? healthSystem.GetHealthNormalized() : 1f;
            healthBarImage.fillAmount = Mathf.Lerp(healthOldfillAmount, target, t);

            if (t >= 1f)
            {
                isHealthChange = false;
                healthOldfillAmount = target;
            }
            needUpdate = true;
        }

        if (isStrengthChange)
        {
            animationTimer += Time.deltaTime;
            float t = Mathf.Clamp01(animationTimer / animationTimeMax);
            float target = strengthSystem != null ? strengthSystem.GetStrengthPointsNormalized() : 1f;
            strengthBarImage.fillAmount = Mathf.Lerp(strengthOldfillAmount, target, t);

            if (t >= 1f)
            {
                isStrengthChange = false;
                strengthOldfillAmount = target;
            }
            needUpdate = true;
        }

        if (!needUpdate)
        {
            animationTimer = animationTimeMax;
        }
    }

    private void HealthSystem_OnDamage(object sender, System.EventArgs e)
    {
        if (healthSystem == null || healthBarImage == null) return;
        isHealthChange = true;
        healthOldfillAmount = healthBarImage.fillAmount;
        animationTimer = 0f;
    }

    private void StrengthSystem_OnActionPointsChanged(object sender, System.EventArgs e)
    {
        if (strengthSystem == null || strengthBarImage == null) return;
        isStrengthChange = true;
        strengthOldfillAmount = strengthBarImage.fillAmount;
        animationTimer = 0f;
    }

    private void UpdateActionPointsBarImmediate()
    {
        if (strengthSystem != null && strengthBarImage != null)
            strengthBarImage.fillAmount = strengthSystem.GetStrengthPointsNormalized();
    }

    private void UpdateHealthBarImmediate()
    {
        if (healthSystem != null && healthBarImage != null)
            healthBarImage.fillAmount = healthSystem.GetHealthNormalized();
    }
}
