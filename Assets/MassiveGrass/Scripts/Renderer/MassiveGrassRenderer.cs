using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Object = System.Object;

namespace Mewlist.MassiveGrass
{
    public class MassiveGrassRenderer : IDisposable, ICellOperationCallbacks
    {
        private Camera           camera;
        private Terrain          terrain;
        private List<Texture2D>  alphaMaps;
        private MassiveGrassGrid grid;

        public MassiveGrassGrid Grid => grid;

        private Dictionary<MassiveGrassGrid.CellIndex, Mesh> meshes
            = new Dictionary<MassiveGrassGrid.CellIndex, Mesh>();

        private MeshBuilder meshBuilder;
        private MassiveGrassProfile profile;

        public MassiveGrassRenderer(Camera camera, Terrain terrain, List<Texture2D> alphaMaps, MassiveGrassProfile profile)
        {
            this.camera    = camera;
            this.terrain   = terrain;
            this.profile   = profile;
            this.alphaMaps = alphaMaps;

            var terrainSize = terrain.terrainData.bounds.size.x;
            grid        = new MassiveGrassGrid(terrain, Mathf.CeilToInt(terrainSize / profile.GridSize));
            meshBuilder = new MeshBuilder();
        }

        public void OnBeginRender()
        {
            if (camera == null) return;
            grid.Activate(camera.transform.position, profile.Radius, this);
        }

        public void Dispose()
        {
            camera = null;
        }

        public async void Create(MassiveGrassGrid.CellIndex index, Rect rect)
        {
            // メッシュ生成タスクを呼び出す
            meshes[index] = await meshBuilder.Build(terrain, alphaMaps, index, rect, profile);
        }

        public void Remove(MassiveGrassGrid.CellIndex index)
        {
            if (Application.isPlaying)
                UnityEngine.Object.Destroy(meshes[index]);
            else
                UnityEngine.Object.DestroyImmediate(meshes[index]);
            meshes.Remove(index);
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
    }
}