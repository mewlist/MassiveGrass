using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

namespace Mewlist.MassiveGrass
{
    public class FromMeshBuilder : IMeshBuilder
    {
        private readonly Stack<MeshData> pool = new Stack<MeshData>();
        private MeshTemplateData templateData;

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
            mesh.name = "MassiveGrass Template Mesh";
            mesh.SetVertices( vertices, 0, meshData.VertexCount);
            mesh.SetUVs(0, uvs, 0, meshData.VertexCount);
            mesh.SetNormals(normals, 0, meshData.VertexCount);
            mesh.SetTriangles(triangles, 0, meshData.IndexCount, 0);
            mesh.SetColors(colors, 0, meshData.VertexCount);

            pool.Push(meshData);
        }

        public async Task<MeshData> BuildMeshData(Terrain terrain, IReadOnlyCollection<Texture2D> alphaMaps,
            MassiveGrassProfile profile, Element[] elements)
        {
            if (templateData == null)
            {
                var scale = new Vector3(profile.Scale.x, profile.Scale.y, profile.Scale.x);
                templateData = new MeshTemplateData(profile.Mesh, scale);
            }

            if (pool.Count <= 0)
                pool.Push(new MeshData(profile.AmountPerBlock, templateData.vertexCount, templateData.indecesCount));

            var terrainData = terrain.terrainData;
            var w           = terrainData.alphamapWidth;
            var h           = terrainData.alphamapHeight;

            var meshData = pool.Pop();
            var actualCount = 0;
            var alphas      = new float[elements.Length];
            var layers = new List<int>(profile.TerrainLayers.Length);
            foreach (var terrainLayer in profile.TerrainLayers)
                for (var i = 0; i < terrainData.terrainLayers.Length; i++)
                    if (terrainData.terrainLayers[i].name == terrainLayer.name)
                        layers.Add(i);

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
                            Mathf.RoundToInt(w * element.normalizedPosition.x),
                            Mathf.RoundToInt(h * element.normalizedPosition.y));
                        switch (layer % 4)
                        {
                            case 0:
                                alpha = Mathf.Max(alpha, pixel.r);
                                break;
                            case 1:
                                alpha = Mathf.Max(alpha, pixel.g);
                                break;
                            case 2:
                                alpha = Mathf.Max(alpha, pixel.b);
                                break;
                            case 3:
                                alpha = Mathf.Max(alpha, pixel.a);
                                break;
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
                    var validHeight = profile.HeightRange.x <= element.position.y &&
                                      element.position.y <= profile.HeightRange.y;
                    if (!validHeight) continue;
                    var alpha = alphas[i];
                    if (alpha >= profile.AlphaMapThreshold)
                    {
                        Add(meshData, profile, element, alpha, actualCount++);
                    }
                    else
                    {
                        var rand = 1 - Mathf.Repeat(1f, ParkAndMiller.Get(i));
                        if (alpha > profile.DensityFactor * rand * rand)
                            Add(meshData, profile, element, alpha, actualCount++);
                    }
                }

                meshData.SetActualCount(
                    templateData.vertexCount * actualCount,
                    templateData.indecesCount * actualCount);
            });

            return meshData;
        }

        private void Add(MeshData meshData, MassiveGrassProfile profile, Element element, float density, int index)
        {
            var vOrigin = index * templateData.vertexCount;
            var iOrigin = index * templateData.indecesCount;
            var rand    = ParkAndMiller.Get(element.index + 1000);

            var normalRot = Quaternion.LookRotation(element.normal);
            var slant     = Quaternion.AngleAxis(profile.Slant * 90f * (rand - 0.5f), Vector3.right);
            var slantWeak = Quaternion.AngleAxis(profile.Slant * 45f * (rand - 0.5f), Vector3.right);
            var upRot = Quaternion.AngleAxis(360f * rand, Vector3.up);
            var idealRot = normalRot *
                           Quaternion.AngleAxis(90f, Vector3.right) *
                           upRot;
            var rot       = idealRot * slant;

//            var scale = profile.Scale * (1 + 0.4f * (rand - 0.5f));
            var scale = Vector3.one * (1 + 0.4f * (rand - 0.5f));
            var rightVec = element.index % 2 == 0 ? rot * Vector3.right : rot * -Vector3.right;
            var upVec = rot * Vector3.up;

            for (var i = 0; i < templateData.vertexCount; i++)
            {
                var vertPos = element.position +
                              rot * Vector3.Scale(templateData.scaledVertices[i], scale);
                meshData.vertices[vOrigin + i] = vertPos;
                switch (profile.NormalType)
                {
                    case NormalType.Up:
                        meshData.normals[vOrigin + i] = idealRot * Vector3.up;
                        break;
                    default:
                        meshData.normals[vOrigin + i] = templateData.normals[i];
                        break;
                }

                var uv = templateData.uvs[i];

                var attr = new MassiveGrassProfile.VertAttribute(density, rand, element.position, templateData.vertices[i]);
                var vColorR = profile.GetCustomVertexData(VertexDataType.VertexColorR, attr);
                var vColorG = profile.GetCustomVertexData(VertexDataType.VertexColorG, attr);
                var vColorB = profile.GetCustomVertexData(VertexDataType.VertexColorB, attr);
                var vColorA = profile.GetCustomVertexData(VertexDataType.VertexColorA, attr);
                var uv1Z    = profile.GetCustomVertexData(VertexDataType.UV1Z, attr);
                var uv1W    = profile.GetCustomVertexData(VertexDataType.UV1W, attr);
                var color = new Color(vColorR, vColorG, vColorB, vColorA);

                meshData.colors[vOrigin + i] = color;
                meshData.uvs[vOrigin + i] = new Vector4(uv.x, uv.y, uv1Z, uv1W);
            }

            for (var i = 0; i < templateData.indecesCount; i++)
            {
                var vi = vOrigin + templateData.triangles[i];
                if (vi >= vOrigin + templateData.vertexCount)
                {
                    var a = 100;
                }

                meshData.triangles[iOrigin + i] = vOrigin + templateData.triangles[i];
            }
        }

        private class MeshTemplateData
        {
            public readonly int indecesCount;

            public readonly int vertexCount;
            public readonly List<Color> colors = new List<Color>();
            public readonly List<Vector3> normals = new List<Vector3>();
            public readonly List<Vector3> scaledVertices = new List<Vector3>();
            public readonly List<int> triangles = new List<int>();
            public readonly List<Vector4> uvs = new List<Vector4>();
            public readonly List<Vector3> vertices = new List<Vector3>();

            public MeshTemplateData(Mesh mesh, Vector3 scale)
            {
                mesh.GetVertices(vertices);
                mesh.GetNormals(normals);
                mesh.GetUVs(0, uvs);
                mesh.GetColors(colors);
                mesh.GetTriangles(triangles, 0);
                vertexCount = vertices.Count;
                indecesCount = triangles.Count;

                scaledVertices = vertices.Select(x => Vector3.Scale(x, scale)).ToList();
            }
        }
    }
}