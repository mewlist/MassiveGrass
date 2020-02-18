using System;
using UnityEditor;
using UnityEngine;

namespace Mewlist.MassiveGrass
{
    [CustomEditor(typeof(MassiveGrassTerrain))]
    public class MassiveGrassTerrainEditor : Editor
    {
        private void OnSceneGUI()
        {
            switch (Event.current.type)
            {
                case EventType.MouseDown:
                    break;
                case EventType.MouseUp:
                    if (Event.current.button == 0)
                        Refresh();
                    break;
                case EventType.MouseMove:
                    break;
                case EventType.MouseDrag:
                    break;
                case EventType.KeyDown:
                    break;
                case EventType.KeyUp:
                    break;
                case EventType.ScrollWheel:
                    break;
                case EventType.Repaint:
                    break;
                case EventType.Layout:
                    break;
                case EventType.DragUpdated:
                    break;
                case EventType.DragPerform:
                    break;
                case EventType.DragExited:
                    break;
                case EventType.Ignore:
                    break;
                case EventType.Used:
                    break;
                case EventType.ValidateCommand:
                    if (Event.current.commandName == "UndoRedoPerformed")
                        Refresh();
                    break;
                case EventType.ExecuteCommand:
                    break;
                case EventType.ContextClick:
                    break;
                case EventType.MouseEnterWindow:
                    break;
                case EventType.MouseLeaveWindow:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void Refresh()
        {
            foreach (var massiveGrass in FindObjectsOfType<MassiveGrass>())
            {
                massiveGrass.Refresh();
            }
        }
    }
}