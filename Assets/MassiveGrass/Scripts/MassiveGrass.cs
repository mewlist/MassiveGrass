using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

namespace Mewlist.MassiveGrass
{
    [RequireComponent(typeof(MeshRenderer))]
    [RequireComponent(typeof(MeshFilter))]
    [ExecuteInEditMode]
    public partial class MassiveGrass : MonoBehaviour
    {
        [SerializeField] private Terrain                   targetTerrain       = default;
        [SerializeField] private List<Texture2D>           bakedAlphaMaps      = default;
        [Range(1, 200)] [SerializeField] private int                       maxParallelJobCount = 50;
        [SerializeField] private List<MassiveGrassProfile> profiles            = default(List<MassiveGrassProfile>);

        private readonly Dictionary<Camera, RendererCollection>
            rendererCollections = new Dictionary<Camera, RendererCollection>();

        private MeshFilter meshFilter;
        private Mesh       boundsMesh;


        private MeshFilter MeshFilter => meshFilter ? meshFilter : (meshFilter = GetComponent<MeshFilter>());


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
            RenderPipelineManager.beginCameraRendering += OnBeginRender; // for SRP
        }

        private void OnDisable()
        {
            RenderPipelineManager.beginCameraRendering -= OnBeginRender; // for SRP
            Clear();
        }

        private void Clear()
        {
            foreach (var massiveGrassRenderer in rendererCollections.Values)
                massiveGrassRenderer.Dispose();

            rendererCollections.Clear();
            DestroyBounds();
        }

        private void Update()
        {
            Render();
        }

        private void Render()
        {
            foreach (var massiveGrassRenderer in rendererCollections.Values)
                massiveGrassRenderer.Render();
        }

        private void OnValidate()
        {
            foreach (var massiveGrassRenderer in rendererCollections.Values)
            {
                foreach (var renderer in massiveGrassRenderer.renderers.Values)
                {
                    renderer.MaxParallelJobCount = maxParallelJobCount;
                }
            }
        }

        private void OnWillRenderObject()
        {
            OnBeginRender(default, Camera.current);
        }

        private void OnBeginRender(ScriptableRenderContext context, Camera camera)
        {
            if (camera == null) return;
            if (!profiles.Any()) return;

            // カメラ毎に Renderer を作る
            if (!rendererCollections.ContainsKey(camera))
                rendererCollections[camera] = new RendererCollection();

            foreach (var profile in profiles)
                rendererCollections[camera]
                    .OnBeginRender(camera, profile, targetTerrain, bakedAlphaMaps, maxParallelJobCount);
        }


        private class RendererCollection : IDisposable
        {
            public Dictionary<MassiveGrassProfile, MassiveGrassRenderer> renderers =
                new Dictionary<MassiveGrassProfile, MassiveGrassRenderer>();

            public void OnBeginRender(
                Camera              camera,
                MassiveGrassProfile profile,
                Terrain             terrain,
                List<Texture2D>     alphaMaps,
                int                 maxParallelJobCount)
            {
                if (profile == null) return;
                if (!renderers.ContainsKey(profile))
                {
                    renderers[profile] = new MassiveGrassRenderer(
                        camera,
                        terrain,
                        alphaMaps,
                        profile,
                        maxParallelJobCount);
                    Debug.Log($" renderer for {profile} created on {camera}");
                }

                renderers[profile].OnBeginRender();
            }

            public void Render()
            {
                foreach (var v in renderers.Values)
                    v.Render();
            }

            public void Dispose()
            {
                foreach (var v in renderers.Values)
                    v.Dispose();
                renderers.Clear();
            }

            public void UpdateAlphaMaps(List<Texture2D> alphaMaps)
            {
                foreach (var v in renderers.Values)
                    v.UpdateAlphaMaps(alphaMaps);
            }
        }
    }
}