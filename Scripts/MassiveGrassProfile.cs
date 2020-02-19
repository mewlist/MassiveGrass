using System;
using UnityEngine;

namespace Mewlist.MassiveGrass
{
    public class LayerAttribute : PropertyAttribute { }

    public enum BuilderType
    {
        Quad = 0,
        FromMesh = 5,
    }
    
    public enum NormalType
    {
        KeepMesh = 0,
        Up = 1,
        Shading = 2,
    }

    public enum VertexDataType
    {
        VertexColorR,
        VertexColorG,
        VertexColorB,
        VertexColorA,
        UV1Z,
        UV1W,
    }

    public enum OutDataType
    {
        Density,
        Range,
        Random,
        VertexPosX,
        VertexPosY,
        VertexPosZ,
        PivotX,
        PivotY,
        PivotZ,
        CustomData1,
        CustomData2,
    }

    [Serializable]
    public struct CustomVertexData
    {
        public OutDataType    OutDataType;
    }
    
    [CreateAssetMenu(fileName = "MassiveGrass", menuName = "MassiveGrass", order = 1)]
    public class MassiveGrassProfile : ScriptableObject
    {
        [SerializeField] public TerrainLayer[] TerrainLayers = new TerrainLayer[] { };
        [SerializeField]        public Vector2     Scale;
        [SerializeField]        public float       Radius = 1000f;
        [SerializeField]        public float       GridSize = 50f;
        [SerializeField]        public float       Slant;
        [SerializeField]        public float       GroundOffset = 0f;
        [SerializeField]        public int         AmountPerBlock = 10000;
        [SerializeField]        public Material    Material;
        [SerializeField, Layer] public int         Layer = 0;
        [SerializeField]        public float       AlphaMapThreshold = 0.3f;
        [SerializeField]        public float       DensityFactor = 0.5f;
        [SerializeField]        public bool        CastShadows = false;
        [SerializeField]        public BuilderType BuilderType;
        [SerializeField]        public Mesh        Mesh;
        [SerializeField]        public NormalType  NormalType = NormalType.Up;
        [SerializeField]        public CustomVertexData UV1Z;
        [SerializeField]        public CustomVertexData UV1W;
        [SerializeField]        public CustomVertexData VertexColorR;
        [SerializeField]        public CustomVertexData VertexColorG;
        [SerializeField]        public CustomVertexData VertexColorB;
        [SerializeField]        public CustomVertexData VertexColorA;
        [SerializeField]        public int         Seed;
        [SerializeField] public Vector2 HeightRange;
        public IMeshBuilder CreateBuilder()
        {
            switch(BuilderType)
            {
                case BuilderType.Quad:
                    return new QuadBuilder();
                case BuilderType.FromMesh:
                    return new FromMeshBuilder();
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public float GetCustomVertexData(VertexDataType vertexDataType, VertAttribute attr)
        {
            switch (vertexDataType)
            {
                case VertexDataType.VertexColorR:
                    return GetData(VertexColorR.OutDataType, attr);
                case VertexDataType.VertexColorG:
                    return GetData(VertexColorG.OutDataType, attr);
                case VertexDataType.VertexColorB:
                    return GetData(VertexColorB.OutDataType, attr);
                case VertexDataType.VertexColorA:
                    return GetData(VertexColorA.OutDataType, attr);
                case VertexDataType.UV1Z:
                    return GetData(UV1Z.OutDataType, attr);
                case VertexDataType.UV1W:
                    return GetData(UV1W.OutDataType, attr);
                default:
                    throw new ArgumentOutOfRangeException(nameof(vertexDataType), vertexDataType, null);
            }
        }

        private float GetData(OutDataType outDataType, VertAttribute attr)
        {
            switch(outDataType)
            {
                case OutDataType.Density:
                    return attr.Density;
                case OutDataType.Range:
                    return Radius;
                case OutDataType.Random:
                    return attr.Rand;
                case OutDataType.VertexPosX:
                    return attr.VertPos.x;
                case OutDataType.VertexPosY:
                    return attr.VertPos.y;
                case OutDataType.VertexPosZ:
                    return attr.VertPos.z;
                case OutDataType.PivotX:
                    return attr.Pivot.x;
                case OutDataType.PivotY:
                    return attr.Pivot.y;
                case OutDataType.PivotZ:
                    return attr.Pivot.z;
                case OutDataType.CustomData1:
                case OutDataType.CustomData2:
                default:
                    throw new ArgumentOutOfRangeException(nameof(outDataType), outDataType, null);
            }
        }

        public struct VertAttribute
        {
            public readonly float Density;
            public readonly float Rand;
            public readonly Vector3 Pivot;
            public readonly Vector3 VertPos;

            public VertAttribute(
                float density,
                float rand,
                Vector3 pivot,
                Vector3 vertPos)
            {
                Density = density;
                Rand = rand;
                Pivot = pivot;
                VertPos = vertPos;
            }
        }
    }
}