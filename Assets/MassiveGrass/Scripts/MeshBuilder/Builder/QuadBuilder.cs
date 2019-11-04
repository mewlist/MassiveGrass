using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

namespace Mewlist.MassiveGrass
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
                    try
                    {
                        var v = alphaMaps[layer].GetPixel(
                            Mathf.RoundToInt((float) w * element.normalizedPosition.y),
                            Mathf.RoundToInt((float) h * element.normalizedPosition.x)).a;
                        alpha = Mathf.Max(alpha, v);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                        throw;
                    }
                }

                alphas[i] = alpha;

            }

            await Task.Run(() =>
            {
                for (var i = 0; i < elements.Count; i++)
                {
                    var element  = elements[i];
                    if (alphas[i] >= profile.AlphaMapThreshold)
                    {
                        if (alphas[i] + profile.DensityFactor > ParkAndMiller.Get(i))
                            AddQuad(meshData, profile, element, alphas[i], actualCount++);
                    }
                }
            });

            var mesh = new Mesh();
            mesh.name = "MassiveGrass Quad Mesh";
            mesh.vertices  = meshData.vertices.Take(4 * actualCount).ToArray();
            mesh.normals   = meshData.normals.Take(4 * actualCount).ToArray();
            mesh.triangles = meshData.triangles.Take(6 * actualCount).ToArray();
            mesh.colors    = meshData.colors.Take(4 * actualCount).ToArray();
            mesh.SetUVs(0, meshData.uvs.Take(4 * actualCount).ToList());
            return mesh;
        }
        
        private void AddQuad(MeshData meshData, MassiveGrassProfile profile, Element element, float density, int index)
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
            var scale = profile.Scale * (1 + 0.4f * (rand - 0.5f));
            var rightVec = rot * Vector3.right;
            var upVec = rot * Vector3.up;
            var p1 = scale.x * -rightVec * 0.5f + scale.y * upVec + Vector3.up * profile.GroundOffset;
            var p2 = scale.x *  rightVec * 0.5f + scale.y * upVec + Vector3.up * profile.GroundOffset;
            var p3 = scale.x *  rightVec * 0.5f + Vector3.up * profile.GroundOffset;
            var p4 = scale.x * -rightVec * 0.5f + Vector3.up * profile.GroundOffset;
            var normal = element.normal;
            var normalBottom = element.normal;
            switch(profile.NormalType)
            {
                case NormalType.KeepMesh:
                    break;
                case NormalType.Up:
                    normalBottom = normal = rot * Vector3.up;
                    break;
                case NormalType.Shading:
                    normal = rot * Vector3.up;
                    normalBottom = rot * Vector3.forward;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            var vColorR = profile.GetCustomVertexData(VertexDataType.VertexColorR, density, rand);
            var vColorG = profile.GetCustomVertexData(VertexDataType.VertexColorG, density, rand);
            var vColorB = profile.GetCustomVertexData(VertexDataType.VertexColorB, density, rand);
            var vColorA = profile.GetCustomVertexData(VertexDataType.VertexColorA, density, rand);
            var uv1Z    = profile.GetCustomVertexData(VertexDataType.UV1Z, density, rand);
            var uv1W    = profile.GetCustomVertexData(VertexDataType.UV1W, density, rand);
            var color = new Color(vColorR, vColorG, vColorB, vColorA);
            meshData.vertices[vOrigin+0] = element.position + p1;
            meshData.vertices[vOrigin+1] = element.position + p2;
            meshData.vertices[vOrigin+2] = element.position + p3;
            meshData.vertices[vOrigin+3] = element.position + p4;
            meshData.normals[vOrigin+0] = normal;
            meshData.normals[vOrigin+1] = normal;
            meshData.normals[vOrigin+2] = normalBottom;
            meshData.normals[vOrigin+3] = normalBottom;
            meshData.uvs[vOrigin+0] = new Vector4(0f, 1f, uv1Z, uv1W);
            meshData.uvs[vOrigin+1] = new Vector4(1f, 1f, uv1Z, uv1W);
            meshData.uvs[vOrigin+2] = new Vector4(1f, 0f, uv1Z, uv1W);
            meshData.uvs[vOrigin+3] = new Vector4(0f, 0f, uv1Z, uv1W);
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