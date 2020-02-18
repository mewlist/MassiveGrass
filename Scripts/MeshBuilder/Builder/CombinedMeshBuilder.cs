using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

namespace Mewlist.MassiveGrass
{
    public class CombinedMeshBuilder : IMeshBuilder
    {
        private Mesh cache;

        public async Task<Mesh> Build(Mesh mesh, Terrain terrain, IReadOnlyCollection<Texture2D> alphaMaps, MassiveGrassProfile profile,
            Element[] elements)
        {
            var terrainData = terrain.terrainData;
            var w           = terrainData.alphamapWidth;
            var h           = terrainData.alphamapHeight;
            var alphas      = new float[elements.Length];
            var layers = new List<int>(profile.TerrainLayers.Length);
            foreach (var terrainLayer in profile.TerrainLayers)
            {
                for (var i = 0; i < terrainData.terrainLayers.Length; i++)
                {
                    if (terrainData.terrainLayers[i] == terrainLayer)
                        layers.Add(i);
                }
            }

            for (var i = 0; i < elements.Length; i++)
            {
                var element  = elements[i];
                var alpha    = 0f;

                foreach (var layer in layers)
                {
                    var pixel = alphaMaps.ElementAt(layer / 4).GetPixel(
                        Mathf.RoundToInt((float) w * element.normalizedPosition.x),
                        Mathf.RoundToInt((float) h * element.normalizedPosition.y));
                    switch (layer % 4)
                    {
                        case 0: alpha = Mathf.Max(alpha, pixel.r); break;
                        case 1: alpha = Mathf.Max(alpha, pixel.g); break;
                        case 2: alpha = Mathf.Max(alpha, pixel.b); break;
                        case 3: alpha = Mathf.Max(alpha, pixel.a); break;
                    }
                }

                alphas[i] = alpha;
            }

            if (cache == null)
            {
                cache = Mesh.Instantiate(profile.Mesh);
                switch (profile.NormalType)
                {
                    case NormalType.KeepMesh:
                        break;
                    case NormalType.Up:
                        cache.normals = cache.normals.Select(_ => Vector3.up).ToArray();
                        break;
                    case NormalType.Shading:
                        cache.normals = cache.normals.Select((_, i) =>
                            Vector3.Slerp(cache.normals[i], Vector3.up, cache.vertices[i].y)).ToArray();
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            var scale = new Vector3(profile.Scale.x, profile.Scale.y, profile.Scale.x);
            var count = 0;
            for (var i = 0; i < elements.Length; i++)
            {
                if (alphas[i] >= profile.AlphaMapThreshold)
                    if (alphas[i] - profile.DensityFactor > ParkAndMiller.Get(i))
                        count++;
            }

            var combine = new CombineInstance[count];
            var ic = 0;

            for (var i = 0; i < elements.Length; i++)
            {
                var element  = elements[i];
                if (alphas[i] >= profile.AlphaMapThreshold)
                {
                    if (alphas[i] - profile.DensityFactor > ParkAndMiller.Get(i))
                    {
                        var density = alphas[i];
                        var rand    = ParkAndMiller.Get(element.index);
                        var vColorR = profile.GetCustomVertexData(VertexDataType.VertexColorR, density, rand);
                        var vColorG = profile.GetCustomVertexData(VertexDataType.VertexColorG, density, rand);
                        var vColorB = profile.GetCustomVertexData(VertexDataType.VertexColorB, density, rand);
                        var vColorA = profile.GetCustomVertexData(VertexDataType.VertexColorA, density, rand);
                        var uv1Z    = profile.GetCustomVertexData(VertexDataType.UV1Z, density, rand);
                        var uv1W    = profile.GetCustomVertexData(VertexDataType.UV1W, density, rand);
                        var color = new Color(vColorR, vColorG, vColorB, vColorA);

                        Quaternion normalRot = Quaternion.LookRotation(element.normal);
                        Quaternion slant     = Quaternion.AngleAxis(profile.Slant * 90f * (rand - 0.5f), Vector3.right);
                        Quaternion upRot     = Quaternion.AngleAxis(360f * rand, Vector3.up);
                        Quaternion rot       = normalRot *
                                               Quaternion.AngleAxis(90f, Vector3.right) *
                                               upRot *
                                               slant;
                        var instance = new CombineInstance();
                        instance.mesh = cache;
                        instance.mesh.colors = instance.mesh.colors.Select(_ => color).ToArray();
                        var uvs = new List<Vector4>();
                        instance.mesh.GetUVs(0, uvs);
                        for (var i1 = 0; i1 < uvs.Count; i1++)
                            uvs[i1] = new Vector4(uvs[i1].x, uvs[i1].y, uv1Z, instance.mesh.vertices[i1].y);
                        instance.mesh.SetUVs(0, uvs);
                        instance.transform = Matrix4x4.TRS(
                            element.position + Vector3.up * profile.GroundOffset,
                            rot,
                            scale);
                        combine[ic++] = instance;
                    }
                }
            }

            mesh.Clear();
            mesh.CombineMeshes(combine);
//            foreach (var combineInstance in combine)
//            {
//                if (Application.isPlaying)
//                    Object.Destroy(combineInstance.mesh);
//                else
//                    Object.DestroyImmediate(combineInstance.mesh);
//            }
            mesh.name = "MassiveGrass Combined Mesh";
            return mesh;
        }

        public Task<MeshData> BuildMeshData(Terrain terrain, IReadOnlyCollection<Texture2D> alphaMaps, MassiveGrassProfile profile, Element[] elements)
        {
            throw new NotImplementedException();
        }

        public void BuildMesh(Mesh mesh, MeshData meshData)
        {
            throw new NotImplementedException();
        }
    }
}