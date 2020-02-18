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
            meshData.uvs[vOrigin+0] = new Vector4(0f, 1f, uv1Z, p1.y);
            meshData.uvs[vOrigin+1] = new Vector4(1f, 1f, uv1Z, p2.y);
            meshData.uvs[vOrigin+2] = new Vector4(1f, 0f, uv1Z, p3.y);
            meshData.uvs[vOrigin+3] = new Vector4(0f, 0f, uv1Z, p4.y);
//            meshData.colors[vOrigin+0] = color;
//            meshData.colors[vOrigin+1] = color;
//            meshData.colors[vOrigin+2] = color;
//            meshData.colors[vOrigin+3] = color;
//            meshData.colors[vOrigin+0] = new Color(-0.5f * rightVec.x, -0.5f * rightVec.y, -0.5f * rightVec.z);
//            meshData.colors[vOrigin+1] = new Color(0.5f * rightVec.x, 0.5f * rightVec.y, 0.5f * rightVec.z);
            meshData.colors[vOrigin+0] = new Color(-rightVec.x, -rightVec.y, -rightVec.z);
            meshData.colors[vOrigin+1] = new Color(rightVec.x,  rightVec.y,  rightVec.z);
            meshData.colors[vOrigin+2] = new Color(rightVec.x, rightVec.y, rightVec.z);
            meshData.colors[vOrigin+3] = new Color(-rightVec.x, -rightVec.y, -rightVec.z);
            meshData.triangles[iOrigin+0] = vOrigin+0;
            meshData.triangles[iOrigin+1] = vOrigin+1;
            meshData.triangles[iOrigin+2] = vOrigin+2;
            meshData.triangles[iOrigin+3] = vOrigin+2;
            meshData.triangles[iOrigin+4] = vOrigin+3;
            meshData.triangles[iOrigin+5] = vOrigin+0;
        }
    }
}