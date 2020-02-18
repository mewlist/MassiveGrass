using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Mewlist.MassiveGrass
{
    public class MeshBuilder
    {
        private IMeshBuilder builder;

        private async Task<Element[]> GenerateElements(Terrain terrain, Rect rect, MassiveGrassProfile profile,
            int haltonOffset)
        {
            var context = SynchronizationContext.Current;
            var terrainPos = terrain.transform.position;
            var terrainSize = terrain.terrainData.size.x;
            var terrainXZPos = new Vector2(terrainPos.x, terrainPos.z);
            var localRect = new Rect(rect.min - terrainXZPos, rect.size);
            var localNormalizedRect = new Rect(localRect.position / terrainSize, localRect.size / terrainSize);

            var haltons = new Vector2[profile.AmountPerBlock];
            var normalizedPositions = new Vector2[profile.AmountPerBlock];
            var heights = new float[profile.AmountPerBlock];
            var normals = new Vector3[profile.AmountPerBlock];
//            var list = new List<Element>(profile.AmountPerBlock);
            var list = new Element[profile.AmountPerBlock];

            var done = false;
            var range = Enumerable.Range(0, profile.AmountPerBlock);
            for (var i = 0; i < list.Length; i++)
                list[i] = default(Element);

            await Task.Run(() =>
            {
                for (var i = 0; i < profile.AmountPerBlock; i++)
                {
                    haltons[i] = new Vector2(
                        HaltonSequence.Base2(i + haltonOffset + profile.Seed),
                        HaltonSequence.Base3(i + haltonOffset + profile.Seed));
                    normalizedPositions[i] = localNormalizedRect.min + haltons[i] * localNormalizedRect.size;
                }
            });

            await Task.Run(async () =>
            {
                context.Post(_ =>
                {
                    for (var i = 0; i < profile.AmountPerBlock; i++)
                    {
                        if (terrain == null) break;
                        var normalizedPosition = normalizedPositions[i];
                        heights[i] =
                            terrain.terrainData.GetInterpolatedHeight(normalizedPosition.x, normalizedPosition.y);
                        normals[i] =
                            terrain.terrainData.GetInterpolatedNormal(normalizedPosition.x, normalizedPosition.y);
                    }

                    done = true;
                }, null);
            });

            while (!done) await Task.Delay(1);

            await Task.Run(() =>
            {
                for (var i = 0; i < profile.AmountPerBlock; i++)
                {
                    var haltonPos = haltons[i];
                    var position = haltonPos * rect.size + rect.min;
                    var normalizedPosition = localNormalizedRect.min + haltons[i] * localNormalizedRect.size;
                    list[i] =new Element(
                        i,
                        new Vector3(position.x, heights[i], position.y),
                        normalizedPosition,
                        normals[i]);
                }
            });

            return list;
        }

        public async Task<MeshData> BuildMeshData(Terrain terrain, IReadOnlyCollection<Texture2D> alphaMaps, MassiveGrassGrid.CellIndex index, Rect rect, MassiveGrassProfile profile)
        {
            var elements = await GenerateElements(terrain, rect, profile, index.hash % 50000);
            builder = builder ?? profile.CreateBuilder();
            return await builder.BuildMeshData(terrain, alphaMaps, profile, elements);
        }

        public void BuildMesh(Mesh mesh, MeshData meshData)
        {
            builder.BuildMesh(mesh, meshData);
        }
    }
}