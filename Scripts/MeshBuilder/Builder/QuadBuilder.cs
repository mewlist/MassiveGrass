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
        private Stack<MeshData> pool = new Stack<MeshData>();

        public async Task<MeshData> BuildMeshData(Terrain terrain, IReadOnlyCollection<Texture2D> alphaMaps, MassiveGrassProfile profile, Element[] elements)
        {
            var terrainData = terrain.terrainData;
            var w           = terrainData.alphamapWidth;
            var h           = terrainData.alphamapHeight;

            if (pool.Count <= 0)
            {
                pool.Push(new MeshData(elements.Length, 2));
            }
            var meshData = pool.Pop();
            var actualCount = 0;
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
                    var texIndex = layer / 4;
                    if (texIndex < alphaMaps.Count)
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
                }

                alphas[i] = alpha;

            }

            await Task.Run(() =>
            {
                for (var i = 0; i < elements.Length; i++)
                {
                    var element  = elements[i];
                    var alpha = alphas[i];
                    if (alpha >= profile.AlphaMapThreshold)
                    {
                        AddQuad(meshData, profile, element, alpha, actualCount++);
                    }
                    else
                    {
                        var rand = 1 - Mathf.Repeat(1f, ParkAndMiller.Get(i));
                        if (alpha > profile.DensityFactor * rand * rand)
                            AddQuad(meshData, profile, element, alpha, actualCount++);
                    }
                }

                meshData.SetActualCount(
                    4 * actualCount,
                    6 * actualCount);
            });
            return meshData;
        }

        public void BuildMesh(Mesh mesh, MeshData meshData)
        {
            var vertices  = new Vector3[meshData.VertexCount];
            var normals   = new Vector3[meshData.VertexCount];
            var triangles = new int[meshData.IndexCount];
            var colors    = new Color[meshData.VertexCount];
            var uvs       = new Vector4[meshData.VertexCount];
            for (var c = 0; c < meshData.VertexCount; c++) vertices[c] = meshData.vertices[c];
            for (var c = 0; c < meshData.VertexCount; c++) normals[c]  = meshData.normals[c];
            for (var c = 0; c < meshData.IndexCount; c++) triangles[c] = meshData.triangles[c];
            for (var c = 0; c < meshData.VertexCount; c++) colors[c]   = meshData.colors[c];
            for (var c = 0; c < meshData.VertexCount; c++) uvs[c]      = meshData.uvs[c];

            mesh.Clear();
            mesh.name = "MassiveGrass Quad Mesh";
            mesh.SetVertices( vertices, 0, meshData.VertexCount);
            mesh.SetUVs(0, uvs, 0, meshData.VertexCount);
            mesh.SetNormals(normals, 0, meshData.VertexCount);
            mesh.SetTriangles(triangles, 0, meshData.IndexCount, 0);
            mesh.SetColors(colors, 0, meshData.VertexCount);

            pool.Push(meshData);
        }

        private void AddQuad(MeshData meshData, MassiveGrassProfile profile, Element element, float density, int index)
        {
            var vOrigin = index * 4;
            var iOrigin = index * 6;
            var rand    = ParkAndMiller.Get(element.index);
            var normalRot = Quaternion.LookRotation(element.normal); 
            var slant     = Quaternion.AngleAxis(profile.Slant * 90f * (rand - 0.5f), Vector3.right);
            var slantWeak = Quaternion.AngleAxis(profile.Slant * 45f * (rand - 0.5f), Vector3.right);
            var upRot     = Quaternion.AngleAxis(360f * rand, Vector3.up);
            var rot       = normalRot *
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
            meshData.vertices[vOrigin+0] = element.position + p1;
            meshData.vertices[vOrigin+1] = element.position + p2;
            meshData.vertices[vOrigin+2] = element.position + p3;
            meshData.vertices[vOrigin+3] = element.position + p4;
            meshData.normals[vOrigin+0] = normal;
            meshData.normals[vOrigin+1] = normal;
            meshData.normals[vOrigin+2] = normalBottom;
            meshData.normals[vOrigin+3] = normalBottom;

            var attr1 = new MassiveGrassProfile.VertAttribute(density, rand, element.position, p1);
            var attr2 = new MassiveGrassProfile.VertAttribute(density, rand, element.position, p2);
            var attr3 = new MassiveGrassProfile.VertAttribute(density, rand, element.position, p3);
            var attr4 = new MassiveGrassProfile.VertAttribute(density, rand, element.position, p4);

            {
                var vColorR = profile.GetCustomVertexData(VertexDataType.VertexColorR, attr1);
                var vColorG = profile.GetCustomVertexData(VertexDataType.VertexColorG, attr1);
                var vColorB = profile.GetCustomVertexData(VertexDataType.VertexColorB, attr1);
                var vColorA = profile.GetCustomVertexData(VertexDataType.VertexColorA, attr1);
                meshData.colors[vOrigin + 0] = new Color(vColorR, vColorG, vColorB, vColorA);
            }
            {
                var vColorR = profile.GetCustomVertexData(VertexDataType.VertexColorR, attr2);
                var vColorG = profile.GetCustomVertexData(VertexDataType.VertexColorG, attr2);
                var vColorB = profile.GetCustomVertexData(VertexDataType.VertexColorB, attr2);
                var vColorA = profile.GetCustomVertexData(VertexDataType.VertexColorA, attr2);
                meshData.colors[vOrigin+1] = new Color(vColorR, vColorG, vColorB, vColorA);
            }
            {
                var vColorR = profile.GetCustomVertexData(VertexDataType.VertexColorR, attr3);
                var vColorG = profile.GetCustomVertexData(VertexDataType.VertexColorG, attr3);
                var vColorB = profile.GetCustomVertexData(VertexDataType.VertexColorB, attr3);
                var vColorA = profile.GetCustomVertexData(VertexDataType.VertexColorA, attr3);
                meshData.colors[vOrigin+2] = new Color(vColorR, vColorG, vColorB, vColorA);
            }
            {
                var vColorR = profile.GetCustomVertexData(VertexDataType.VertexColorR, attr4);
                var vColorG = profile.GetCustomVertexData(VertexDataType.VertexColorG, attr4);
                var vColorB = profile.GetCustomVertexData(VertexDataType.VertexColorB, attr4);
                var vColorA = profile.GetCustomVertexData(VertexDataType.VertexColorA, attr4);
                meshData.colors[vOrigin+3] = new Color(vColorR, vColorG, vColorB, vColorA);
            }
            {
                var uv1Z    = profile.GetCustomVertexData(VertexDataType.UV1Z, attr1);
                var uv1W    = profile.GetCustomVertexData(VertexDataType.UV1W, attr1);
                meshData.uvs[vOrigin+0] = new Vector4(0f, 1f, uv1Z, uv1W);
            }
            {
                var uv1Z    = profile.GetCustomVertexData(VertexDataType.UV1Z, attr2);
                var uv1W    = profile.GetCustomVertexData(VertexDataType.UV1W, attr2);
                meshData.uvs[vOrigin+1] = new Vector4(1f, 1f, uv1Z, uv1W);
            }
            {
                var uv1Z    = profile.GetCustomVertexData(VertexDataType.UV1Z, attr3);
                var uv1W    = profile.GetCustomVertexData(VertexDataType.UV1W, attr3);
                meshData.uvs[vOrigin+2] = new Vector4(1f, 0f, uv1Z, uv1W);
            }
            {
                var uv1Z    = profile.GetCustomVertexData(VertexDataType.UV1Z, attr4);
                var uv1W    = profile.GetCustomVertexData(VertexDataType.UV1W, attr4);
                meshData.uvs[vOrigin+3] = new Vector4(0f, 0f, uv1Z, uv1W);
            }
            meshData.triangles[iOrigin+0] = vOrigin+0;
            meshData.triangles[iOrigin+1] = vOrigin+1;
            meshData.triangles[iOrigin+2] = vOrigin+2;
            meshData.triangles[iOrigin+3] = vOrigin+2;
            meshData.triangles[iOrigin+4] = vOrigin+3;
            meshData.triangles[iOrigin+5] = vOrigin+0;
        }
    }
}