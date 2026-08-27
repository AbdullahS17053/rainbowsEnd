using UnityEngine;

namespace RainbowsEnd.Gameplay.Unicorn
{
    public static class RainbowTrailCellUtility
    {
        public static Vector2Int GetCell(Vector2 worldPosition, float cellSizeWorldUnits)
        {
            float cellSize = Mathf.Max(0.01f, cellSizeWorldUnits);
            return new Vector2Int(
                Mathf.FloorToInt(worldPosition.x / cellSize),
                Mathf.FloorToInt(worldPosition.y / cellSize));
        }
    }
}