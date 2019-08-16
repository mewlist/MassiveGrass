using UnityEngine;

namespace Mewlist.MassiveGrass
{
    public struct Element
    {
        public readonly int index;
        public readonly Vector3 position;
        public readonly Vector2 normalizedPosition;
        public readonly Vector3 normal;

        public Element(int index, Vector3 position, Vector2 normalizedPosition, Vector3 normal)
        {
            this.index = index;
            this.position = position;
            this.normalizedPosition = normalizedPosition;
            this.normal = normal;
        }
    }
}