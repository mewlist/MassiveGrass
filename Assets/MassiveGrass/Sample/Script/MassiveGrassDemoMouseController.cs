using UnityEngine;
using UnityEngine.EventSystems;

public class MassiveGrassDemoMouseController : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public Transform target;
        [Range(0.01f, 1f)]
        public float sensitivity = 1f;

        private float pitch = -16f;
        private float yaw   = 90f;

        public void OnBeginDrag(PointerEventData eventData)
        {
        }

        public void Start()
        {
            target.rotation = Quaternion.Euler(pitch, yaw, 0);
        }

        public void OnDrag(PointerEventData eventData)
        {
            var diff = eventData.delta;

            pitch = Mathf.Clamp (pitch - diff.y * sensitivity, -85f, 85f);
            yaw   = Mathf.Repeat(yaw + diff.x * sensitivity, 360f);

            target.rotation = Quaternion.Euler(pitch, yaw, 0);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
        }
}
