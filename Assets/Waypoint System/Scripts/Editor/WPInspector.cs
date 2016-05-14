/*============================================

A Custom editor for the PathInspector type,
just to add some cool buttons. I gotta learn
more about this stuff.

============================================*/

using UnityEngine;
using System.Collections;
using UnityEditor;

namespace Waypoints
{
    [CustomEditor(typeof(WPController))]
    public class WPInspector : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            WPController PC = (WPController)target;

            if (GUILayout.Button("Toggle Group 1"))
            {
                PC.ToggleGroup(NODE_GROUP.GROUP1);
            }
            if (GUILayout.Button("Toggle Group 2"))
            {
                PC.ToggleGroup(NODE_GROUP.GROUP2);
            }
            if (GUILayout.Button("Toggle Group 3"))
            {
                PC.ToggleGroup(NODE_GROUP.GROUP3);
            }
        }
    }
}
