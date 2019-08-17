using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

namespace Mewlist.MassiveGrass.Strategy
{
    /// <summary>
    /// Quad のプリミティブを生成する
    /// </summary>
    public class QuadBuilder : IMeshBuilder
    {
        public async Task<Mesh> Build(Terrain terrain, List<Texture2D> alphaMaps, MassiveGrassProfile profile, List<Element> elements)
        {
            var terrainData = terrain.terrainData;
            var w           = terrainData.alphamapWidth;
            var h           = terrainData.alphamapHeight;

            var meshData = new MeshData(elements.Count, 2);
            var actualCount = 0;
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

            await Task.Run(() =>
            {
                for (var i = 0; i < elements.Count; i++)
                {
                    var element  = elements[i];
                    if (alphas[i] >= profile.AlphaMapThreshold)
                        AddQuad(meshData, profile, element, actualCount++);
                }
            });

            var mesh = new Mesh();
            mesh.vertices  = meshData.vertices.Take(4 * actualCount).ToArray();
            mesh.normals   = meshData.normals.Take(4 * actualCount).ToArray();
            mesh.triangles = meshData.triangles.Take(6 * actualCount).ToArray();
            mesh.colors    = meshData.colors.Take(4 * actualCount).ToArray();
            mesh.SetUVs(0, meshData.uvs.Take(4 * actualCount).ToList());
            return mesh;
        }
        
        private void AddQuad(MeshData meshData, MassiveGrassProfile profile, Element element, int index)
        {
            var vOrigin = index * 4;
            var iOrigin = index * 6;
            var rand    = ParkAndMiller.Get(element.index);
            Quaternion normalRot = Quaternion.LookRotation(element.normal); 
            Quaternion slant     = Quaternion.AngleAxis(profile.Slant * 90f * (rand - 0.5f), Vector3.right);
            Quaternion slantWeak = Quaternion.AngleAxis(profile.Slant * 45f * (rand - 0.5f), Vector3.right);
            Quaternion upRot     = Quaternion.AngleAxis(360f * rand, Vector3.up);
            Quaternion rot       = normalRot *
                                   Quaternion.AngleAxis(90f, Vector3.right) *
                                   upRot *
                                   slant;
            var scale = profile.Scale;
            var p1 = rot * new Vector3(-0.5f * scale.x, 1f * scale.y, 0f);
            var p2 = rot * new Vector3( 0.5f * scale.x, 1f * scale.y, 0f);
            var p3 = rot * new Vector3( 0.5f * scale.x, 0f,           0f);
            var p4 = rot * new Vector3(-0.5f * scale.x, 0f,           0f);
            var normal = element.normal;
            var color = Color.Lerp(Color.white, Color.yellow, rand);
            meshData.vertices[vOrigin+0] = element.position + p1;
            meshData.vertices[vOrigin+1] = element.position + p2;
            meshData.vertices[vOrigin+2] = element.position + p3;
            meshData.vertices[vOrigin+3] = element.position + p4;
            meshData.normals[vOrigin+0] = normal * 1f;
            meshData.normals[vOrigin+1] = normal * 1f;
            meshData.normals[vOrigin+2] = normal * 0.5f;
            meshData.normals[vOrigin+3] = normal * 0.5f;
            meshData.uvs[vOrigin+0] = new Vector4(0f, 1f, 0f, 0f);
            meshData.uvs[vOrigin+1] = new Vector4(1f, 1f, 0f, 0f);
            meshData.uvs[vOrigin+2] = new Vector4(1f, 0f, 0f, 0f);
            meshData.uvs[vOrigin+3] = new Vector4(0f, 0f, 0f, 0f);
            meshData.colors[vOrigin+0] = color;
            meshData.colors[vOrigin+1] = color;
            meshData.colors[vOrigin+2] = color;
            meshData.colors[vOrigin+3] = color;
            meshData.triangles[iOrigin+0] = vOrigin+0;
            meshData.triangles[iOrigin+1] = vOrigin+1;
            meshData.triangles[iOrigin+2] = vOrigin+2;
            meshData.triangles[iOrigin+3] = vOrigin+2;
            meshData.triangles[iOrigin+4] = vOrigin+3;
            meshData.triangles[iOrigin+5] = vOrigin+0;
        }
    }
}