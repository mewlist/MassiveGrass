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
        [SerializeField]        public bool        CastShadows = false;
        [SerializeField]        public BuilderType BuilderType;
        [SerializeField]        public Mesh        Mesh;
        [SerializeField]        public NormalType  NormalType = NormalType.Up;
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
    }
}