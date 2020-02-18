using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

namespace Mewlist.MassiveGrass
{
    [RequireComponent(typeof(MeshRenderer))]
    [RequireComponent(typeof(MeshFilter))]
    [ExecuteInEditMode]
    public partial class MassiveGrass : MonoBehaviour
    {
        [Range(1, 200)] [SerializeField] private int       maxParallelJobCount = 50;
        [SerializeField] private List<MassiveGrassProfile> profiles            = default(List<MassiveGrassProfile>);

        private readonly Dictionary<Terrain, Dictionary<Camera, RendererCollection>>
            rendererCollections = new Dictionary<Terrain, Dictionary<Camera, RendererCollection>>();

        private MeshFilter meshFilter;
        private Mesh       boundsMesh;


        private MeshFilter MeshFilter => meshFilter ? meshFilter : (meshFilter = GetComponent<MeshFilter>());


        // Terrain と同じ大きさの Bounds をセットして
        // Terrain が描画されるときに強制的に描画処理を走らせるようにする
        private void SetupBounds()
        {
            if (boundsMesh == null)
            {
                boundsMesh = new Mesh();
                boundsMesh.name = "Massive Grass Terrain Bounds";
            }

//            boundsMesh.bounds = targetTerrain.terrainData.bounds;
            boundsMesh.bounds = new Bounds(Vector3.zero, 50000f * Vector3.one);
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
            foreach (var cameraCollection in rendererCollections.Values)
                foreach (var massiveGrassRenderer in cameraCollection.Values)
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
            foreach (var cameraCollection in rendererCollections.Values)
                foreach (var massiveGrassRenderer in cameraCollection.Values)
                    massiveGrassRenderer.Render();
        }

        private void OnValidate()
        {
            foreach (var cameraCollection in rendererCollections.Values)
                foreach (var massiveGrassRenderer in cameraCollection.Values)
                    foreach (var renderer in massiveGrassRenderer.renderers.Values)
                        renderer.MaxParallelJobCount = maxParallelJobCount;
        }

        private void OnWillRenderObject()
        {
            OnBeginRender(default, Camera.current);
        }

        private void OnBeginRender(ScriptableRenderContext context, Camera camera)
        {
            if (camera == null || profiles == null) return;
            if (!profiles.Any()) return;

            DetectTerrains();

            // カメラ毎に Renderer を作る
            foreach (var keyValuePair in rendererCollections)
            {
                var terrain = keyValuePair.Key;
                if (terrain != null)
                {
                    var cameraCollection = keyValuePair.Value;
                    if (!cameraCollection.ContainsKey(camera))
                        cameraCollection[camera] = new RendererCollection();

                    foreach (var profile in profiles)
                        cameraCollection[camera]
                            .OnBeginRender(camera, profile, terrain, terrain.terrainData.alphamapTextures, maxParallelJobCount);
                }
            }
            foreach (var cameraCollection in rendererCollections.Values)
                foreach (var massiveGrassRenderer in cameraCollection.Values)
                    massiveGrassRenderer.Update();
        }

        private int waitCounter = 10;
        private HashSet<Terrain> terrains = new HashSet<Terrain>();
        private void DetectTerrains()
        {
            if (--waitCounter > 0) return;
            var found = FindObjectsOfType<Terrain>();
            foreach (var terrain in found)
            {
                if (!terrains.Contains(terrain))
                {
                    terrains.Add(terrain);
                }

                if (!rendererCollections.ContainsKey(terrain))
                {
                    rendererCollections[terrain] = new Dictionary<Camera, RendererCollection>();
                }


                var toRemove = new List<Terrain>();
                foreach (var t in terrains)
                {
                    if (t == null) toRemove.Add(t);
                }
                foreach (var t in toRemove)
                {
                    if (rendererCollections.ContainsKey(t))
                    {
                        foreach (var rendererCollection in rendererCollections[t].Values)
                            rendererCollection.Dispose();
                        rendererCollections.Remove(t);
                    }
                    terrains.Remove(t);
                }
            }
            waitCounter = 10;
        }



        private class RendererCollection : IDisposable
        {
            public Dictionary<MassiveGrassProfile, MassiveGrassRenderer> renderers =
                new Dictionary<MassiveGrassProfile, MassiveGrassRenderer>();

            public void OnBeginRender(
                Camera              camera,
                MassiveGrassProfile profile,
                Terrain             terrain,
                Texture2D[]         alphaMaps,
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

            public void Update()
            {
                foreach (var v in renderers.Values)
                {
                    v.InstantiateQueuedMesh();
                    v.ProcessQueue();
                }
            }
        }
    }
}