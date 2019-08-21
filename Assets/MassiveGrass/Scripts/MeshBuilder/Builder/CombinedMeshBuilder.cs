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
        public async Task<Mesh> Build(Terrain terrain, List<Texture2D> alphaMaps, MassiveGrassProfile profile, List<Element> elements)
        {
            var terrainData = terrain.terrainData;
            var w           = terrainData.alphamapWidth;
            var h           = terrainData.alphamapHeight;
            var alphas      = new float[elements.Count];

            for (var i = 0; i < elements.Count; i++)
            {
                var element  = elements[i];
                var layers   = profile.PaintTextureIndex;
                var alpha    = 0f;

                foreach (var layer in layers)
                {
                    var v = alphaMaps[layer].GetPixel(
                        Mathf.RoundToInt((float) w * element.normalizedPosition.y),
                        Mathf.RoundToInt((float) h * element.normalizedPosition.x)).a;
                    alpha = Mathf.Max(alpha, v);
                }

                alphas[i] = alpha;
            }

            var mesh = new Mesh();
            var combine = new List<CombineInstance>();
            if (cache == null)
            {
                cache = Mesh.Instantiate(profile.Mesh);
                switch(profile.NormalType)
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
            for (var i = 0; i < elements.Count; i++)
            {
                var element  = elements[i];
                if (alphas[i] >= profile.AlphaMapThreshold)
                {
                    var rand    = ParkAndMiller.Get(element.index);
                    Quaternion normalRot = Quaternion.LookRotation(element.normal); 
                    Quaternion slant     = Quaternion.AngleAxis(profile.Slant * 90f * (rand - 0.5f), Vector3.right);
                    Quaternion upRot     = Quaternion.AngleAxis(360f * rand, Vector3.up);
                    Quaternion rot       = normalRot *
                                           Quaternion.AngleAxis(90f, Vector3.right) *
                                           upRot *
                                           slant;
                    var instance = new CombineInstance();
                    instance.mesh = cache;
                    instance.transform = Matrix4x4.TRS(
                        element.position + Vector3.up * profile.GroundOffset,
                        rot,
                        scale);
                    combine.Add(instance);
                }
            }
            mesh.CombineMeshes(combine.ToArray());
            return mesh;
        }
    }
}