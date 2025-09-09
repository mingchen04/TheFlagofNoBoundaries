using System.Collections.Generic;
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
            TurnSystem.Instance.NextTurn();
        }

        if (Input.GetKeyDown(KeyCode.F))
        {
            // 11,10 true
            // 11,9 false
            var a = new GridPosition(10, 2);

            List<GridPosition> positions = new List<GridPosition>
            {
                new GridPosition(11, 1), //false
                new GridPosition(9, 0), //true
            };

            foreach (GridPosition position in positions)
            {
                a.IsStraightLine(position);
            }

        }

    }
}
