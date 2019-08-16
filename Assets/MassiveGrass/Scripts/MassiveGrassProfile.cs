using System;
using Mewlist.MassiveGrass.Strategy;
using UnityEngine;

namespace Mewlist.MassiveGrass
{
    public class LayerAttribute : PropertyAttribute { }

    [CreateAssetMenu(fileName = "MkassiveGrass", menuName = "MassiveGrass", order = 1)]
    public class MassiveGrassProfile : ScriptableObject
    {
        [SerializeField]        public int[]    PaintTextureIndex = new int[] { };
        [SerializeField]        public Vector2  Scale;
        [SerializeField]        public float    Radius = 1000f;
        [SerializeField]        public float    GridSize = 50f;
        [SerializeField]        public float    Slant;
        [SerializeField]        public int      AmountPerBlock = 10000;
        [SerializeField]        public Material Material;
        [SerializeField, Layer] public int      Layer = 0;
        [SerializeField]        public float    AlphaMapThreshold = 0.3f;
        [SerializeField]        public bool     CastShadows = false;

        public IMeshBuilder CreateBuilder()
        {
            return new QuadBuilder();
        }
    }
}