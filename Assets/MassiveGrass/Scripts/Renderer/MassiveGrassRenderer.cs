using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

namespace Mewlist.MassiveGrass
{
    public class MassiveGrassRenderer : IDisposable, ICellOperationCallbacks
    {
        private int                 maxParallelJobCount;
        private List<Texture2D>     alphaMaps;

        private readonly Camera              camera;
        private readonly Terrain             terrain;
        private readonly MassiveGrassGrid    grid;
        private readonly MeshBuilder         meshBuilder;
        private readonly MassiveGrassProfile profile;

        public int MaxParallelJobCount
        {
            get { return maxParallelJobCount; }
            set { maxParallelJobCount = Mathf.Max(1, value); }
        }
        public MassiveGrassGrid Grid => grid;

        private Dictionary<MassiveGrassGrid.CellIndex, Mesh>    meshes
            = new Dictionary<MassiveGrassGrid.CellIndex, Mesh>();
        private HashSet<MassiveGrassGrid.CellIndex>             activeIndices
            = new HashSet<MassiveGrassGrid.CellIndex>();
        private Dictionary<MassiveGrassGrid.CellIndex, Request> requestQueue
            = new Dictionary<MassiveGrassGrid.CellIndex, Request>();

        public MassiveGrassRenderer(
            Camera camera,
            Terrain terrain,
            List<Texture2D> alphaMaps,
            MassiveGrassProfile profile,
            int maxParallelJobCount)
        {
            this.camera    = camera;
            this.terrain   = terrain;
            this.profile   = profile;
            this.alphaMaps = alphaMaps;
            MaxParallelJobCount = maxParallelJobCount;

            var terrainSize = terrain.terrainData.bounds.size.x;
            grid        = new MassiveGrassGrid(terrain, Mathf.CeilToInt(terrainSize / profile.GridSize));
            meshBuilder = new MeshBuilder();
        }


        public void Render()
        {
            foreach (var keyValuePair in meshes)
            {
                var mesh = keyValuePair.Value;
                Graphics.DrawMesh(
                    mesh,
                    Vector3.zero,
                    Quaternion.identity,
                    profile.Material,
                    profile.Layer,
                    null,
                    0,
                    null,
                    profile.CastShadows);
            }
        }

        public async void UpdateAlphaMaps(List<Texture2D> alphaMaps)
        {
            this.alphaMaps = alphaMaps;
            await grid.Activate(camera.transform.position, -1, this);
            OnBeginRender();
        }

        private async Task Build(MassiveGrassGrid.CellIndex index)
        {
            var request = requestQueue[index];
            var rect  = request.rect;
            var mesh = await meshBuilder.Build(terrain, alphaMaps, index, rect, profile);
            if (!activeIndices.Contains(index))
            {
                SafeDestroy(mesh);
                if (requestQueue.ContainsKey(index))
                    requestQueue.Remove(index);
                return;
            }

            meshes[index] = mesh;
            requestQueue.Remove(index);
        }

        public async void OnBeginRender()
        {
            // mesh preparation
            if (camera == null) return;
            await grid.Activate(camera.transform.position, profile.Radius, this);
        }

        private async Task ProcessQueue()
        {
            while (requestQueue.Count > 0)
            {
                var processSize = Mathf.Min(MaxParallelJobCount, Mathf.CeilToInt(requestQueue.Count));
                var tasks = requestQueue.Take(processSize).Select(x => Build(x.Key));
                await Task.WhenAll(tasks);
            }
        }

        private void SafeDestroy(Mesh mesh)
        {
            if (Application.isPlaying)
                UnityEngine.Object.Destroy(mesh);
            else
                UnityEngine.Object.DestroyImmediate(mesh);
        }

        
        #region IDisposable

        public void Dispose()
        {
            activeIndices.Clear();
            requestQueue.Clear();
            foreach (var v in meshes)
                SafeDestroy(v.Value);
            meshes.Clear();
            
        }

        #endregion

        
        #region ICellOperationCallbacks

        public void Create(MassiveGrassGrid.CellIndex index, Rect rect)
        {
            if (activeIndices.Contains(index)) return;

            activeIndices.Add(index);
            if (!requestQueue.ContainsKey(index))
            {
                requestQueue[index] = (new Request(index, rect));
                if (requestQueue.Count == 1)
                    ProcessQueue();
            }
        }

        public void Remove(MassiveGrassGrid.CellIndex index)
        {
            if (!activeIndices.Contains(index)) return;
            if (meshes.ContainsKey(index))
            {
                SafeDestroy(meshes[index]);
                meshes.Remove(index);
            }

            activeIndices.Remove(index);
        }

        #endregion

        private class Request
        {
            public MassiveGrassGrid.CellIndex index;
            public Rect rect;

            public Request(MassiveGrassGrid.CellIndex index, Rect rect)
            {
                this.index = index;
                this.rect = rect;
            }
        }
    }
}