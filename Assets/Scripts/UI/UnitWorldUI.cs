using UnityEngine;
using UnityEngine.UI;

public class UnitWorldUI : MonoBehaviour
{
    [SerializeField] private Unit unit;
    [SerializeField] private Image healthBarImage;
    [SerializeField] private Image physicalBarImage;
    [SerializeField] private HealthSystem healthSystem;// 生命系统
    [SerializeField] private PhysicalSystem physicalSystem;// 体力系统

    private float healthOldfillAmount; // 记录上一次生命值的填充量
    private bool isHealthChange;

    private float physicalOldfillAmount; // 记录上一次体力值的填充量
    private bool isPhysicalChange;

    private float animationTimeMax = 0.3f;
    private float animationTimer;

    private void Awake()
    {
        animationTimer = animationTimeMax;
    }

    private void Start()
    {
        physicalSystem.OnActionPointsChanged += PhysicalSystem_OnActionPointsChanged;
        healthSystem.OnDamage += HealthSystem_OnDamage;

        UpdateActionPointsBar();
        UpdateHealthBar();
    }

    private void Update()
    {
        if (animationTimer < animationTimeMax)
        {
            animationTimer += Time.deltaTime;
            if (isHealthChange)
            {
                UpdateHealthBar();
            }
            if (isPhysicalChange)
            {
                UpdateActionPointsBar();
            }
        }
        else
        {
            isHealthChange = false;
            isPhysicalChange = false;
            animationTimer = animationTimeMax;
        }
    }

    private void HealthSystem_OnDamage(object sender, System.EventArgs e)
    {
        isHealthChange = true;
        healthOldfillAmount = healthBarImage.fillAmount;
        animationTimer = 0f;
        UpdateHealthBar();
    }

    private void PhysicalSystem_OnActionPointsChanged(object sender, System.EventArgs e)
    {
        isPhysicalChange = true;
        physicalOldfillAmount = physicalBarImage.fillAmount;
        animationTimer = 0f;
        UpdateActionPointsBar();
    }

    private void UpdateActionPointsBar()
    {
        float amountLerp = physicalOldfillAmount - physicalSystem.GetPhysicalPointsNormalized();
        physicalBarImage.fillAmount =
            physicalOldfillAmount // 当前体力值
            -
            (amountLerp * Mathf.Clamp01(animationTimer / animationTimeMax));
    }

    private void UpdateHealthBar()
    {
        float amountLerp = healthOldfillAmount - healthSystem.GetHealthNormalized();// 计算生命值变化量
        healthBarImage.fillAmount =
            healthOldfillAmount // 当前生命值
            -
            (amountLerp * Mathf.Clamp01(animationTimer / animationTimeMax));// 线性插值计算当前生命值
    }
}
