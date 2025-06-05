using UnityEngine;

public class BulletProjectIle : MonoBehaviour
{
    [SerializeField] private TrailRenderer trailRenderer; //拖尾
    [SerializeField] private ParticleSystem bulletHitVfx; //粒子特效

    private Vector3 targetPosition;

    private void Update()
    {
        Vector3 moveDir = (targetPosition - transform.position).normalized;
        float distanceBeforeMoving = Vector3.Distance(transform.position, targetPosition);

        float shootSleep = 200f;
        transform.position += shootSleep * Time.deltaTime * moveDir;
        float distanceAfterMoving = Vector3.Distance(transform.position, targetPosition);

        if (distanceBeforeMoving < distanceAfterMoving)
        {
            Instantiate(bulletHitVfx, targetPosition, Quaternion.identity); //生成粒子特效
            transform.position = targetPosition;
            trailRenderer.transform.parent = null;
            Destroy(gameObject);
        }
    }

    public void Setup(Vector3 targetPosition)
    {
        this.targetPosition = targetPosition;
    }

}
