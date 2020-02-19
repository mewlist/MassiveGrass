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

        public void Refresh()
        {
            Debug.Log("Refresh");
            foreach (var cameraCollection in rendererCollections.Values)
                foreach (var massiveGrassRenderer in cameraCollection.Values)
                    massiveGrassRenderer.Dispose();
            rendererCollections.Clear();
                
            SetupBounds();
            Render();
        }
        
        private void OnDrawGizmosSelected()
        {
            foreach (var terrain in terrains)
            {
                if (terrain != null)
                {
                    Gizmos.color = Color.cyan;
                    MassiveGrassGizmo.DrawBounds(terrain.transform.position, terrain.terrainData.bounds);
                }
            }
                
            if (!isActiveAndEnabled) return;
            foreach (var keyValuePair in rendererCollections)
            {
                var targetTerrain = keyValuePair.Key;
                var cameraCollection = keyValuePair.Value;
                if (targetTerrain == null) continue;
                // bounds
                Gizmos.color = Color.black;
//                MassiveGrassGizmo.DrawBounds(targetTerrain.transform.position, boundsMesh.bounds);

                cameraCollection.TryGetValue(Camera.current, out var grassRenderer);
                var count = cameraCollection[Camera.current].renderers.Count;
                var colors = Enumerable
                    .Range(0, count)
                    .Select(v => new Color((float) v / count, (1f + Mathf.Sin((float)v / count)) / 2f, 1 - (float) v / count))
                    .ToList();
                if (grassRenderer != null)
                {
                    int i = 0;
                    foreach (var v in cameraCollection[Camera.current].renderers.Values)
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
    }
#endif
}