using UnityEngine;

public class UnitRagdoll : MonoBehaviour
{
    [SerializeField] private Transform ragdollRootBone;

    public void Setup(Transform originalRootBone)
    {
        MatchAllChildTransorms(originalRootBone, ragdollRootBone);

        Vector3 randomDir = new Vector3(Random.Range(-1f, +1f), 0, Random.Range(-1f, +1f)); //使布娃娃被炸飞的方向随机
        ApplyExplosionToragdoll(ragdollRootBone, 300f, transform.position + randomDir, 10f);
    }

    private void MatchAllChildTransorms(Transform root, Transform clone)
    {
        //设置娃娃骨骼位置与本体相同
        foreach (Transform child in root)
        {
            Transform cloneChild = clone.Find(child.name);

            if (cloneChild != null)
            {
                cloneChild.SetPositionAndRotation(child.position, child.rotation);
                MatchAllChildTransorms(child, cloneChild);
            }
        }
    }

    /// <summary>
    /// 递归地对角色布娃娃系统的所有刚体施加爆炸力
    /// </summary>
    /// <param name="root">布娃娃系统的根节点Transform</param>
    /// <param name="explosionForce">爆炸力的强度</param>
    /// <param name="explosionPosition">爆炸中心的3D世界坐标位置</param>
    /// <param name="explosionRange">爆炸力的作用范围半径</param>
    /// <remarks>
    /// 该方法会遍历root下的所有子物体，对每个包含Rigidbody的子物体执行以下操作：
    /// 1. 使用Unity的AddExplosionForce方法施加爆炸力
    /// 2. 递归处理所有子物体，确保整个布娃娃系统都能受到爆炸影响
    /// 注意：爆炸力会随距离衰减，衰减曲线遵循Unity物理引擎的标准计算方式
    /// </remarks> 
    private void ApplyExplosionToragdoll(Transform root, float explosionForce, Vector3 explosionPosition, float explosionRange)
    {
        foreach (Transform child in root)
        {
            if (child.TryGetComponent<Rigidbody>(out Rigidbody childRigidbody))
            {
                //AddExplosionForce 对刚体施加一个爆炸力
                childRigidbody.AddExplosionForce(explosionForce, explosionPosition, explosionRange);
                ApplyExplosionToragdoll(child, explosionForce, explosionPosition, explosionRange);
            }
        }
    }
}