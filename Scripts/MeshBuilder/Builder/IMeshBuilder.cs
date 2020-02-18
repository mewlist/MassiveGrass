using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace Mewlist.MassiveGrass
{
    public interface IMeshBuilder
    {
        Task<MeshData> BuildMeshData(Terrain terrain, IReadOnlyCollection<Texture2D> alphaMaps, MassiveGrassProfile profile, Element[] elements);
        void BuildMesh(Mesh mesh, MeshData meshData);
    }
}