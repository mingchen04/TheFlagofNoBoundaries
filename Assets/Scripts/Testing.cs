using UnityEngine;

public class Testing : MonoBehaviour
{
    [SerializeField] private Unit unit;

    private void Start()
    {

    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.T))
        {
            UnitActionSystem.Instance.ExecuteAllQueuedActions();
        }

        if (Input.GetKeyDown(KeyCode.F))
        {
            // 11,10 true
            // 11,9 false
            var a = new GridPosition(11, 10);
            var b = new GridPosition(6, 0);
            bool isLine = b.IsStraightLine(a); // 返回true
            Debug.Log($"IsStraightLine: {isLine}");

            isLine = a.IsStraightLine(b); // 返回true
            Debug.Log($"IsStraightLine: {isLine}");
        }

    }
}
