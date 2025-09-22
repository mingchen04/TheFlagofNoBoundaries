using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Experimental.GlobalIllumination;

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
            //if(TurnSystem.Instance.GetTurnState() == TurnSystem.TurnState.Main)
            //{
            //    TurnSystem.Instance.NextTurn();
            //}
            //else
            //{
            //    TurnSystem.Instance.EndTurn();
            //}

            GridPosition a = new GridPosition(2, 0);
            GridPosition b = new GridPosition(0, 0);
            Vector3 aWorld = LevelGrid.Instance.GetWorldPosition(a);
            Vector3 bWorld = LevelGrid.Instance.GetWorldPosition(b);
            aWorld.y += 1;
            bWorld.y += 1;

            Debug.Log(aWorld + " " + bWorld);
            Debug.DrawLine(aWorld, bWorld, Color.blue);
            Debug.Log(Vector3.Distance(aWorld, bWorld));
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
