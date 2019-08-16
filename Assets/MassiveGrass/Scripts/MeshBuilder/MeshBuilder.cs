using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace Mewlist.MassiveGrass
{
    public class MeshBuilder
    {
        public async Task<Mesh> Build(
            Terrain terrain,
            List<Texture2D> alphaMaps,
            MassiveGrassGrid.CellIndex index,
            Rect rect,
            MassiveGrassProfile profile)
        {
            var elements = await GenerateElements(terrain, rect, profile, index.hash % 50000);
            var builder = profile.CreateBuilder();
            return await builder.Build(terrain, alphaMaps, profile, elements);
        }
        
        private async Task<List<Element>> GenerateElements(Terrain terrain, Rect rect, MassiveGrassProfile profile, int haltonOffset)
        {
            var list = new List<Element>();
            var terrainPos = terrain.transform.position;
            var terrainSize = terrain.terrainData.size.x;
            var terrainXZPos = new Vector2(terrainPos.x, terrainPos.z);
            var localRect = new Rect(rect.min - terrainXZPos, rect.size);
            var localNormalizedRect = new Rect(localRect.position / terrainSize, localRect.size / terrainSize);
            for (var i = 0; i < profile.AmountPerBlock; i++)
            {
                var haltonPos = new Vector2(HaltonSequence.Base2(i + haltonOffset), HaltonSequence.Base3(i + haltonOffset));
                var normalizedPosition = localNormalizedRect.min +
                                         haltonPos * localNormalizedRect.size;
                var height = terrain.terrainData.GetInterpolatedHeight(normalizedPosition.x, normalizedPosition.y);
                var normal = terrain.terrainData.GetInterpolatedNormal(normalizedPosition.x, normalizedPosition.y);
                var position = haltonPos * rect.size + rect.min;
                list.Add(
                    new Element(
                        i,
                        new Vector3(position.x, height, position.y),
                        normalizedPosition,
                        normal));
            }

            return list;
        }
    }
}