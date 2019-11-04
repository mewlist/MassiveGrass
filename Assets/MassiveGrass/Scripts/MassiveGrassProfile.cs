using System;
using UnityEngine;

namespace Mewlist.MassiveGrass
{
    public class LayerAttribute : PropertyAttribute { }

    public enum BuilderType
    {
        Quad = 0,
        Mesh = 10,
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
        CustomData1,
        CustomData2,
    }

    [Serializable]
    public struct CustomVertexData
    {
        public VertexDataType VertexDataType;
        public OutDataType    OutDataType;
    }
    
    [CreateAssetMenu(fileName = "MkassiveGrass", menuName = "MassiveGrass", order = 1)]
    public class MassiveGrassProfile : ScriptableObject
    {
        [SerializeField]        public int[]       PaintTextureIndex = new int[] { };
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
        [SerializeField]        public CustomVertexData[] VertexDataDefinitions;
        [SerializeField]        public int         Seed;
        public IMeshBuilder CreateBuilder()
        {
            switch(BuilderType)
            {
                case BuilderType.Quad:
                    return new QuadBuilder();
                case BuilderType.Mesh:
                    return new CombinedMeshBuilder();
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public float GetCustomVertexData(VertexDataType vertexDataType, float density, float rand)
        {
            foreach (var v in VertexDataDefinitions)
            {
                if (vertexDataType == v.VertexDataType)
                    return GetData(v.OutDataType, density, rand);
            }
            return 0f;
        }

        private float GetData(OutDataType outDataType, float density, float rand)
        {
            switch(outDataType)
            {
                case OutDataType.Density:
                    return density;
                case OutDataType.Range:
                    return Radius;
                case OutDataType.Random:
                    return rand;
                case OutDataType.CustomData1:
                case OutDataType.CustomData2:
                default:
                    throw new ArgumentOutOfRangeException(nameof(outDataType), outDataType, null);
            }
        }
    }
}