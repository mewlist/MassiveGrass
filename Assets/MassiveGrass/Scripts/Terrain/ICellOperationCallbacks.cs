using UnityEngine;

namespace Mewlist.MassiveGrass
{
    public interface ICellOperationCallbacks
    {
        void Create(MassiveGrassGrid.CellIndex index, Rect rect);
        void Remove(MassiveGrassGrid.CellIndex index);
    }
}