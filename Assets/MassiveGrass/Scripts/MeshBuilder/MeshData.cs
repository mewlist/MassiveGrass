using UnityEngine;

namespace Mewlist.MassiveGrass
{
    public class MeshData
    {
        public Vector3[] vertices;
        public int[]     triangles;
        public Vector4[] uvs;
        public Color[]   colors;
        public Vector3[] normals;

        public MeshData(int amount, int triangleCount)
        {
            var vertSize = triangleCount + 2;
            var triSize = triangleCount * 3;

            vertices  = new Vector3[amount * vertSize];
            triangles = new int    [amount * triSize];
            uvs       = new Vector4[amount * vertSize];
            colors    = new Color  [amount * vertSize];
            normals   = new Vector3[amount * vertSize];
        }
    }
}
