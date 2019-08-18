using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace Mewlist.MassiveGrass
{
    [RequireComponent(typeof(MeshRenderer))]
    [RequireComponent(typeof(MeshFilter))]
    [ExecuteInEditMode]
    public class MassiveGrass : MonoBehaviour
    {
        private class Renderers : IDisposable
        {
            public Dictionary<MassiveGrassProfile, MassiveGrassRenderer> renderers =
                new Dictionary<MassiveGrassProfile, MassiveGrassRenderer>();

            public void OnBeginRender(
                Camera camera,
                MassiveGrassProfile profile,
                Terrain terrain,
                List<Texture2D> alphaMaps,
                int maxParallelJobCount)
            {
                if (!renderers.ContainsKey(profile))
                {
                    renderers[profile] = new MassiveGrassRenderer(camera, terrain, alphaMaps, profile, maxParallelJobCount);
                    Debug.Log($" renderer for {profile} created on {camera}");
                }
                renderers[profile].OnBeginRender();
            }

            public void Render()
            {
                foreach (var renderersValue in renderers.Values)
                    renderersValue.Render();
            }
            
            public void Dispose()
            {
                foreach (var renderersValue in renderers.Values)
                    renderersValue.Dispose();
                renderers.Clear();
            }

            public void Reset(List<Texture2D> alphaMaps)
            {
                foreach (var v in renderers)
                {
                    var profile = v.Key;
                    var renderer = v.Value;
                    renderer.Reset(alphaMaps, profile);
                }
            }
        }

        [SerializeField] private Terrain targetTerrain = default;
        [SerializeField] private List<Texture2D> alphaMaps = default;
        [SerializeField, Range(1, 200)] private int maxParallelJobCount = 50;

        private Dictionary<Camera, Renderers> renderers = new Dictionary<Camera, Renderers>();

        private MeshFilter meshFilter;
        private MeshFilter MeshFilter => meshFilter ? meshFilter : (meshFilter = GetComponent<MeshFilter>());
        private Mesh boundsMesh;

        public List<MassiveGrassProfile> profiles;

        // Terrain と同じ大きさの Bounds をセットして
        // Terrain が描画されるときに強制的に描画処理を走らせるようにする
        private void SetupBounds()
        {
            if (targetTerrain == null)
            {
                Debug.LogError("Set target terrain");
                return;
            }

            if (boundsMesh == null)
            {
                boundsMesh = new Mesh();
                boundsMesh.name = "Massive Grass Terrain Bounds";
            }

            boundsMesh.bounds = targetTerrain.terrainData.bounds;
            MeshFilter.sharedMesh = boundsMesh;
        }

        private void DestroyBounds()
        {
            if (boundsMesh != null)
            {
                if (Application.isPlaying) Destroy(boundsMesh);
                else                       DestroyImmediate(boundsMesh);
                boundsMesh = null;
            }

            MeshFilter.sharedMesh = null;
        }

        private void OnEnable()
        {
            SetupBounds();
            RenderPipeline.beginCameraRendering += OnBeginRender;
        }

        private void OnDisable()
        {
            RenderPipeline.beginCameraRendering -= OnBeginRender;
            Clear();
        }

        private void Clear()
        {
            foreach (var massiveGrassRenderer in renderers.Values)
                massiveGrassRenderer.Dispose();

            renderers.Clear();
            DestroyBounds();
        }

        private void Update()
        {
            Render();
        }

        private void Render()
        {
            foreach (var massiveGrassRenderer in renderers.Values)
                massiveGrassRenderer.Render();
        }

        private bool baking = false;
        private bool reserveBaking = false;

        public void Bake()
        {
            if (baking)
            {
                reserveBaking = true;
                return;
            }

            Debug.Log("Baking");
            baking = true;
            foreach (var texture2D in alphaMaps)
            {
                DestroyImmediate(texture2D);
            }

            alphaMaps.Clear();

            // Bake
            var terrainData = targetTerrain.terrainData;
            var w           = terrainData.alphamapWidth;
            var h           = terrainData.alphamapHeight;
            var layers      = terrainData.alphamapLayers;

            for (var i = 0; i < layers; i++)
            {
                var texture = AlphamapBaker.CreateAndBake(targetTerrain, new [] {i});
                alphaMaps.Add(texture);
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
            foreach (var massiveGrassRenderer in renderers.Values)
                massiveGrassRenderer.Reset(alphaMaps);
            SetupBounds();
            Render();
        }

        private void OnValidate()
        {
            foreach (var massiveGrassRenderer in renderers.Values)
            {
                foreach (var renderer in massiveGrassRenderer.renderers.Values)
                {
                    renderer.MaxParallelJobCount = maxParallelJobCount;
                }
            }
        }

        private void OnWillRenderObject()
        {
            OnBeginRender(Camera.current);
        }

        private void OnBeginRender(Camera camera)
        {
            if (camera == null) return;
            if (!profiles.Any()) return;

            // カメラ毎に Renderer を作る
            if (!renderers.ContainsKey(camera))
                renderers[camera] = new Renderers();
            
            foreach (var profile in profiles)
            {
                if (profile != null)
                    renderers[camera].OnBeginRender(camera, profile, targetTerrain, alphaMaps, maxParallelJobCount);
            }
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (!isActiveAndEnabled) return;
            // bounds
            Gizmos.color = Color.black;
            MassiveGrassGizmo.DrawBounds(targetTerrain.transform.position, boundsMesh.bounds);

            renderers.TryGetValue(Camera.current, out var grassRenderer);
            var count = renderers[Camera.current].renderers.Count;
            var colors = Enumerable
                .Range(0, count)
                .Select(v => new Color((float)v / count, 1, 1 - (float)v / count))
                .ToList();
            if (grassRenderer != null)
            {
                int i = 0;
                foreach (var v in renderers[Camera.current].renderers.Values)
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
#endif
    }
}