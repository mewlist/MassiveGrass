using UnityEngine;
using UnityEditor;

namespace Mewlist.MassiveGrass
{
    [CustomEditor(typeof(MassiveGrass))]
    public class MassiveGrassEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            if (GUILayout.Button("Bake"))
            {
                MassiveGrass controller = (MassiveGrass)target;
                controller.Bake();
            }
            if (GUILayout.Button("Refresh"))
            {
                MassiveGrass controller = (MassiveGrass)target;
                controller.Refresh();
            }
        }
    }
}
