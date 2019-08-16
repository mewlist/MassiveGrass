using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace Mewlist.MassiveGrass.Strategy
{
    public interface IMeshBuilder
    {
        Task<Mesh> Build(Terrain terrain, List<Texture2D> alphaMaps, MassiveGrassProfile profile, List<Element> elements);
    }
}