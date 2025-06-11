using UnityEngine;
using UnityEngine.UI;

public class UnitWorldUI : MonoBehaviour
{
    [SerializeField] private Unit unit;
    [SerializeField] private Image healthBarImage;
    [SerializeField] private Image physicalBarImage;
    [SerializeField] private HealthSystem healthSystem;
    [SerializeField] private PhysicalSystem physicalSystem;

    private float healthOldfillAmount;
    private bool isHealthChange;

    private float physicalOldfillAmount;
    private bool isPhysicalChange;

    private const float animationTimeMax = 0.3f;
    private float animationTimer = animationTimeMax;

    private void Start()
    {
        if (physicalSystem != null)
            physicalSystem.OnActionPointsChanged += PhysicalSystem_OnActionPointsChanged;
        if (healthSystem != null)
            healthSystem.OnDamage += HealthSystem_OnDamage;

        // 初始化填充值，避免第一次动画异常
        healthOldfillAmount = healthSystem != null ? healthSystem.GetHealthNormalized() : 1f;
        physicalOldfillAmount = physicalSystem != null ? physicalSystem.GetPhysicalPointsNormalized() : 1f;

        UpdateHealthBarImmediate();
        UpdateActionPointsBarImmediate();
    }

    private void OnDestroy()
    {
        if (physicalSystem != null)
            physicalSystem.OnActionPointsChanged -= PhysicalSystem_OnActionPointsChanged;
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

        if (isPhysicalChange)
        {
            animationTimer += Time.deltaTime;
            float t = Mathf.Clamp01(animationTimer / animationTimeMax);
            float target = physicalSystem != null ? physicalSystem.GetPhysicalPointsNormalized() : 1f;
            physicalBarImage.fillAmount = Mathf.Lerp(physicalOldfillAmount, target, t);

            if (t >= 1f)
            {
                isPhysicalChange = false;
                physicalOldfillAmount = target;
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

    private void PhysicalSystem_OnActionPointsChanged(object sender, System.EventArgs e)
    {
        if (physicalSystem == null || physicalBarImage == null) return;
        isPhysicalChange = true;
        physicalOldfillAmount = physicalBarImage.fillAmount;
        animationTimer = 0f;
    }

    private void UpdateActionPointsBarImmediate()
    {
        if (physicalSystem != null && physicalBarImage != null)
            physicalBarImage.fillAmount = physicalSystem.GetPhysicalPointsNormalized();
    }

    private void UpdateHealthBarImmediate()
    {
        if (healthSystem != null && healthBarImage != null)
            healthBarImage.fillAmount = healthSystem.GetHealthNormalized();
    }
}
