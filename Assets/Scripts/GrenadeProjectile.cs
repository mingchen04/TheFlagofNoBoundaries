using System;
using UnityEngine;

public class GrenadeProjectile : MonoBehaviour
{
    public static event EventHandler OnAnyGrenadeExploded;

    [SerializeField] private Transform grenadeExplodeVfxPrfefab;
    [SerializeField] private TrailRenderer trailRenderer;
    //Y坐标曲线投掷
    [SerializeField] private AnimationCurve arcYAnimationCurve;

    private Vector3 targetPosition;
    private Action onGrenadeBehaviourComplete;
    private float totalDistance;
    private Vector3 positionXZ;


    private void Update()
    {
        Vector3 moveDir = (targetPosition - positionXZ).normalized;

        float moveSpeed = 15f;
        positionXZ += moveDir * moveSpeed * Time.deltaTime;

        float distance = Vector3.Distance(positionXZ, targetPosition);
        float distanceNormalized = 1 - distance / totalDistance; //获得距离进度百分比

        float maxHeight = totalDistance / 4f;
        float positionY = arcYAnimationCurve.Evaluate(distanceNormalized) * maxHeight;//抓取曲线上的值
        transform.position = new Vector3(positionXZ.x, positionY, positionXZ.z);

        float reachedTargetDistance = .2f;

        if (Vector3.Distance(positionXZ, targetPosition) < reachedTargetDistance)
        {

            float damageRadius = 4f; //检测半径
            //检测球形区域内所有碰撞体
            Collider[] colliderArray = Physics.OverlapSphere(targetPosition, damageRadius);

            foreach (Collider collider in colliderArray)
            {

                if (collider.TryGetComponent<Unit>(out Unit targetUnit))
                {
                    targetUnit.Damage(30);
                }

                if (collider.TryGetComponent<DestructibleCrate>(out DestructibleCrate destructibleCrate))
                {
                    destructibleCrate.Damage();
                }
            }

            OnAnyGrenadeExploded?.Invoke(this, EventArgs.Empty);
            Instantiate(grenadeExplodeVfxPrfefab, transform.position, Quaternion.identity); //生成粒子特效
            trailRenderer.transform.parent = null;
            Destroy(gameObject);
            onGrenadeBehaviourComplete();
        }
    }

    public void Setup(GridPosition targetGridPosition, Action onGrenadeBehaviourComplete)
    {
        targetPosition = targetGridPosition;
        this.onGrenadeBehaviourComplete = onGrenadeBehaviourComplete;

        positionXZ = transform.position;
        positionXZ.y = 0f;
        totalDistance = Vector3.Distance(transform.position, targetPosition); //目标和当前位置的总距离 
    }
}
