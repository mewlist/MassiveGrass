using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using Directory = UnityEngine.Windows.Directory;
#endif

namespace Mewlist.MassiveGrass
{
#if UNITY_EDITOR
    public partial class MassiveGrass
    {
        private bool baking        = false;
        private bool reserveBaking = false;

        public void Bake()
        {
            if (baking)
            {
                reserveBaking = true;
                return;
            }

            var terrainPath = AssetDatabase.GetAssetPath(targetTerrain.terrainData);
            var terrainDir = Path.GetDirectoryName(terrainPath);
            var terrainName = Path.GetFileNameWithoutExtension(terrainPath);
            var bakeDir = Path.Combine(terrainDir, terrainName + "_MassiveGrass");
            Directory.CreateDirectory(bakeDir);
            Debug.Log("BakeDir: " + bakeDir);

            Debug.Log("Baking");
            baking = true;
            foreach (var texture2D in bakedAlphaMaps)
            {
                DestroyImmediate(texture2D);
            }

            bakedAlphaMaps.Clear();

            // Bake
            var terrainData = targetTerrain.terrainData;
            var w           = terrainData.alphamapWidth;
            var h           = terrainData.alphamapHeight;
            var layers      = terrainData.alphamapLayers;

            for (var i = 0; i < layers; i++)
            {
                var texturePath = Path.Combine(bakeDir, "alphamap" + i + ".png");
                AssetDatabase.DeleteAsset(texturePath);
                var texture = AlphamapBaker.CreateAndBake(targetTerrain, new [] {i});
                byte [] pngData = texture.EncodeToPNG();
                File.WriteAllBytes(texturePath, pngData);
                AssetDatabase.ImportAsset(texturePath);
                Debug.Log("load " + texturePath);

                var importer = AssetImporter.GetAtPath(texturePath) as TextureImporter;
                importer.isReadable = true;
                AssetDatabase.ImportAsset(texturePath, ImportAssetOptions.ForceUpdate);

                var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
                bakedAlphaMaps.Add(tex);
            }

            baking = false;
            Debug.Log("Baking Done");
            if (reserveBaking)
            {
                reserveBaking = false;
                Bake();
            }
        }

        public async void BakeAndRefreshAsync()
        {
            var context = SynchronizationContext.Current;
            await Task.Run(async () =>
            {
                context.Post(_ => Bake(), null);
                while (baking) await Task.Delay(1);
                context.Post(_ => Refresh(), null);
            });
        }

        public async void Refresh()
        {
            Debug.Log("Refresh");
            foreach (var massiveGrassRenderer in rendererCollections.Values)
                massiveGrassRenderer.UpdateAlphaMaps(bakedAlphaMaps);
            SetupBounds();
            Render();
        }
        
        private void OnDrawGizmosSelected()
        {
            if (!isActiveAndEnabled) return;
            // bounds
            Gizmos.color = Color.black;
            MassiveGrassGizmo.DrawBounds(targetTerrain.transform.position, boundsMesh.bounds);

            rendererCollections.TryGetValue(Camera.current, out var grassRenderer);
            var count = rendererCollections[Camera.current].renderers.Count;
            var colors = Enumerable
                .Range(0, count)
                .Select(v => new Color((float) v / count, 1, 1 - (float) v / count))
                .ToList();
            if (grassRenderer != null)
            {
                int i = 0;
                foreach (var v in rendererCollections[Camera.current].renderers.Values)
                {
                    Gizmos.color = colors[i++];
                    // grid
                    foreach (var gridActiveRect in v.Grid.ActiveRects)
                    {
                        var localPos = gridActiveRect.center - new Vector2(targetTerrain.transform.position.x,
                                           targetTerrain.transform.position.z);
                        localPos /= targetTerrain.terrainData.bounds.size.x;
                        var height = targetTerrain.terrainData.GetInterpolatedHeight(localPos.x, localPos.y);
                        MassiveGrassGizmo.DrawRect(gridActiveRect, height);
                    }
                }
            }
        }
    }
#endif
}