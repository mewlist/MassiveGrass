using UnityEngine;

namespace Mewlist.MassiveGrass
{
    public class AlphamapBaker
    {
        public static Texture2D CreateAndBake(Terrain terrain, int[] layers)
        {
            var terrainData = terrain.terrainData;
            var w           = terrainData.alphamapWidth;
            var h           = terrainData.alphamapHeight;
            var tex         = new Texture2D(w, h, TextureFormat.Alpha8, false);

            BakeTo(tex, terrain, layers);

            return tex;
        }

        public static void BakeTo(Texture2D to, Terrain terrain, int[] layers)
        {
            var terrainData = terrain.terrainData;
            var w           = terrainData.alphamapWidth;
            var h           = terrainData.alphamapHeight;
            var alphaMaps   = terrainData.GetAlphamaps(0, 0, w, h);

            for (var x = 0; x < w; x++)
            {
                for (var y = 0; y < h; y++)
                {
                    var a = 0f;
                    for (var l = 0; l < layers.Length; l++)
                    {
                        var v = alphaMaps[x, y, layers[l]];
                        a = Mathf.Max(a, v);
                    }
                    to.SetPixel(x, y, new Color(a, 0, 0, a));
                }
            }
        }
    }
}