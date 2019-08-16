using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace Mewlist.MassiveGrass
{
    [RequireComponent(typeof(MeshRenderer))]
    [RequireComponent(typeof(MeshFilter))]
    [ExecuteInEditMode]
    public class MassiveGrass : MonoBehaviour
    {
        [SerializeField] private Terrain targetTerrain = default;
        [SerializeField] private List<Texture2D> alphaMaps = default;

        private Dictionary<Camera, MassiveGrassRenderer> renderers = new Dictionary<Camera, MassiveGrassRenderer>();

        private MeshFilter meshFilter;
        private MeshFilter MeshFilter => meshFilter ? meshFilter : (meshFilter = GetComponent<MeshFilter>());
        private Mesh boundsMesh;
        
        public MassiveGrassProfile profile;

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
            foreach (var massiveGrassRenderer in renderers.Values)
                massiveGrassRenderer.Render();
        }

        public void Bake()
        {
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
                var texture = AlphamapBaker.CreateAndBake(targetTerrain, new []{i});
                alphaMaps.Add(texture);
            }
        }

        public void Refresh()
        {
            Clear();
            SetupBounds();
        }

        private void OnWillRenderObject()
        {
            OnBeginRender(Camera.current);
        }

        private void OnBeginRender(Camera camera)
        {
            if (profile == null) return;

            // カメラ毎に Renderer を作る
            if (!renderers.ContainsKey(camera))
            {
                renderers[camera] = new MassiveGrassRenderer(camera, targetTerrain, alphaMaps, profile);
                Debug.Log(camera + " renderer created");
            }

            foreach (var massiveGrassRenderer in renderers.Values)
            {
                massiveGrassRenderer.OnBeginRender();
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
            if (grassRenderer != null)
            {
                // grid
                foreach (var gridActiveRect in renderers[Camera.current].Grid.ActiveRects)
                {
                    Gizmos.color = Color.green;
                    var localPos = gridActiveRect.center - new Vector2(targetTerrain.transform.position.x, targetTerrain.transform.position.z);
                    localPos /= targetTerrain.terrainData.bounds.size.x;
                    var height = targetTerrain.terrainData.GetInterpolatedHeight(localPos.x, localPos.y);
                    MassiveGrassGizmo.DrawRect(gridActiveRect, height);
                }
            }
        }
#endif
    }
}
