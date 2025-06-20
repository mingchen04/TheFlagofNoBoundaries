using UnityEngine;

public class UnitAnimator : MonoBehaviour
{
    [SerializeField] private Animator unitAnimator;
    [SerializeField] private Transform bulletProjectIlePrefab;
    [SerializeField] private Transform shootPointTransform;
    [SerializeField] private Transform rifleTransform;
    [SerializeField] private Transform swordTransform;

    private void Awake()
    {
        if (TryGetComponent<MoveAction>(out MoveAction moveAction))
        {
            moveAction.OnStartMoving += MoveAction_OnStartMoving;
            moveAction.OnStopMoving += MoveAction_OnStopMoving;
        }

        if (TryGetComponent<ShootAction>(out ShootAction shootAction))
        {
            shootAction.OnShoot += ShootAction_OnShoot;
        }

        if(TryGetComponent<SwordAction>(out SwordAction swordAction))
        {
            swordAction.OnSwordActionStarted += SwordAction_OnSordActionStarted;
            swordAction.OnSwordActionCompleted += SwordAction_OnSwordActionCompleted;
        }
    }

    private void Start()
    {
        EquipRifle();
    }

    private void SwordAction_OnSwordActionCompleted(object sender, System.EventArgs e)
    {
        EquipRifle();
    }

    private void SwordAction_OnSordActionStarted(object sender, System.EventArgs e)
    {
        EquipSword();
        unitAnimator.SetTrigger("SwordSlash");
    }

    private void MoveAction_OnStopMoving(object sender, System.EventArgs e)
    {
        unitAnimator.SetBool("IsWalking", false);
    }

    private void MoveAction_OnStartMoving(object sender, System.EventArgs e)
    {
        unitAnimator.SetBool("IsWalking", true);
    }

    private void ShootAction_OnShoot(object sender, ShootAction.OnShootEventArgs e)
    {
        unitAnimator.SetTrigger("Shoot");
        Transform BulletProjectIle = Instantiate(bulletProjectIlePrefab, shootPointTransform.position, Quaternion.identity);
        BulletProjectIle bulletProjectIle = BulletProjectIle.GetComponent<BulletProjectIle>();

        Vector3 targetUnitShootAtPosition= e.targetUnitPosition;
        targetUnitShootAtPosition.y = shootPointTransform.position.y;

        bulletProjectIle.Setup(targetUnitShootAtPosition);
    }

    private void EquipSword()
    {
        swordTransform.gameObject.SetActive(true);
        rifleTransform.gameObject.SetActive(false);
    }

    private void EquipRifle()
    {
        swordTransform.gameObject.SetActive(false);
        rifleTransform.gameObject.SetActive(true);
    }
}
