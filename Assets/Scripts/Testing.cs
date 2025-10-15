using System.Collections.Generic;
using System.Linq;
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
            GridPosition gridPoison = new GridPosition(0, 0);
            GridPosition mousePosition = MouseWorld.GetPosition();

            Cube cube = Cube.OffsetToCube(gridPoison);
            Cube mCube = Cube.OffsetToCube(mousePosition);

            List<Cube> cubes = Cube.HexRing(mCube, 3);

            List<GridPosition> gridPositions = cubes
                .Select(cube => Cube.CubeToOffset(cube))
                .ToList();
            gridPositions.RemoveAll(gridPoison => !LevelGrid.Instance.IsValidGridPosition(gridPoison));

            GridSystemVisual.Instance.ShowGridPositionList(gridPositions, GridSystemVisual.GridVisualType.Red);
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
