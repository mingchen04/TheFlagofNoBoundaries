using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class UnitWorldUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI actionPointsText;
    [SerializeField] private Unit unit;
    [SerializeField] private Image healthBarImage;
    [SerializeField] private HealthSystem healthSystem;

    private float oldfillAmount;
    private float healthAnimationTimeMax = 0.3f;
    private float healthAnimationTime;

    private void Awake()
    {
        healthAnimationTime = healthAnimationTimeMax;
    }

    private void Start()
    {
        PhysicalSystem.OnAnyActionPointsChanged += Unit_OnAnyActionPointsChanged;
        healthSystem.OnDamage += HealthSystem_OnDamage;

        UpdateActionPointsText();
        UpdateHealthBar();
    }

    private void Update()
    {
        if (healthAnimationTime < healthAnimationTimeMax)
        {
            healthAnimationTime += Time.deltaTime;
            UpdateHealthBar();
        }
        else
        {
            healthAnimationTime = healthAnimationTimeMax;
        }
    }

    private void HealthSystem_OnDamage(object sender, System.EventArgs e)
    {
        oldfillAmount = healthBarImage.fillAmount;
        healthAnimationTime = 0f;
        UpdateHealthBar();
    }

    private void Unit_OnAnyActionPointsChanged(object sender, System.EventArgs e)
    {
        UpdateActionPointsText();
    }

    private void UpdateActionPointsText()
    {
        actionPointsText.text = unit.GetActionPoints().ToString();
    }

    private void UpdateHealthBar()
    {
        float amountLerp = oldfillAmount - healthSystem.GetHealthNormalized();
        healthBarImage.fillAmount = oldfillAmount - (amountLerp * Mathf.Clamp01(healthAnimationTime / healthAnimationTimeMax));
    }
}
