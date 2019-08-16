using UnityEngine;

namespace Mewlist.MassiveGrass
{
#if UNITY_EDITOR
    public static class MassiveGrassGizmo
    {
        public static void DrawRect(Rect rect, float height)
        {
            Gizmos.DrawLine(new Vector3(rect.xMin, height, rect.yMin), new Vector3(rect.xMax, height, rect.yMin));
            Gizmos.DrawLine(new Vector3(rect.xMax, height, rect.yMin), new Vector3(rect.xMax, height, rect.yMax));
            Gizmos.DrawLine(new Vector3(rect.xMax, height, rect.yMax), new Vector3(rect.xMin, height, rect.yMax));
            Gizmos.DrawLine(new Vector3(rect.xMin, height, rect.yMax), new Vector3(rect.xMin, height, rect.yMin));
        }

        public static void DrawBounds(Vector3 position, Bounds bounds)
        {
            Gizmos.DrawWireCube(bounds.center + position, bounds.size);
        }
    }
#endif
}
