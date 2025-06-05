using UnityEngine;

public class UnitRagdollSpanner : MonoBehaviour
{
    [SerializeField] private Transform unitRagdollPrefabs;
    [SerializeField] private Transform originalRootBone;


    private HealthSystem healthSystem;

    private void Awake()
    {
        healthSystem = GetComponent<HealthSystem>();
        healthSystem.OnDie += HealthSystem_OnDie;
    }

    private void HealthSystem_OnDie(object sender, System.EventArgs e)
    {
        Transform ragdollTransform = Instantiate(unitRagdollPrefabs, transform.position, transform.rotation);
        UnitRagdoll unitRagdoll = ragdollTransform.GetComponent<UnitRagdoll>();
        unitRagdoll.Setup(originalRootBone);
    }
}
